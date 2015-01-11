/*
* Copyright 2009 ZXing authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

namespace ZXing.Common.Detector
{
   /// <summary> <p>A somewhat generic detector that looks for a barcode-like rectangular region within an image.
   /// It looks within a mostly white region of an image for a region of black and white, but mostly
   /// black. It returns the four corners of the region, as best it can determine.</p>
   /// 
   /// </summary>
   /// <author>  Sean Owen
   /// </author>
   /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source 
   /// </author>
   public sealed class MonochromeRectangleDetector
   {
      private const int MAX_MODULES = 32;

      private BitMatrix image;

      public MonochromeRectangleDetector(BitMatrix image)
      {
         this.image = image;
      }

      /// <summary> <p>Detects a rectangular region of black and white -- mostly black -- with a region of mostly
      /// white, in an image.</p>
      /// 
      /// </summary>
      /// <returns> {@link ResultPoint}[] describing the corners of the rectangular region. The first and
      /// last points are opposed on the diagonal, as are the second and third. The first point will be
      /// the topmost point and the last, the bottommost. The second point will be leftmost and the
      /// third, the rightmost
      /// </returns>
      public ResultPoint[] detect()
      {
         int height = image.Height;
         int width = image.Width;
         int halfHeight = height >> 1;
         int halfWidth = width >> 1;
         int deltaY = System.Math.Max(1, height / (MAX_MODULES << 3));
         int deltaX = System.Math.Max(1, width / (MAX_MODULES << 3));

         int top = 0;
         int bottom = height;
         int left = 0;
         int right = width;
         ResultPoint pointA = findCornerFromCenter(halfWidth, 0, left, right, halfHeight, -deltaY, top, bottom, halfWidth >> 1);
         if (pointA == null)
            return null;
         top = (int)pointA.Y - 1;
         ResultPoint pointB = findCornerFromCenter(halfWidth, -deltaX, left, right, halfHeight, 0, top, bottom, halfHeight >> 1);
         if (pointB == null)
            return null;
         left = (int)pointB.X - 1;
         ResultPoint pointC = findCornerFromCenter(halfWidth, deltaX, left, right, halfHeight, 0, top, bottom, halfHeight >> 1);
         if (pointC == null)
            return null;
         right = (int)pointC.X + 1;
         ResultPoint pointD = findCornerFromCenter(halfWidth, 0, left, right, halfHeight, deltaY, top, bottom, halfWidth >> 1);
         if (pointD == null)
            return null;
         bottom = (int)pointD.Y + 1;

         // Go try to find point A again with better information -- might have been off at first.
         pointA = findCornerFromCenter(halfWidth, 0, left, right, halfHeight, -deltaY, top, bottom, halfWidth >> 2);
         if (pointA == null)
            return null;

         return new ResultPoint[] { pointA, pointB, pointC, pointD };
      }

      /// <summary> Attempts to locate a corner of the barcode by scanning up, down, left or right from a center
      /// point which should be within the barcode.
      /// 
      /// </summary>
      /// <param name="centerX">center's x component (horizontal)
      /// </param>
      /// <param name="deltaX">same as deltaY but change in x per step instead
      /// </param>
      /// <param name="left">minimum value of x
      /// </param>
      /// <param name="right">maximum value of x
      /// </param>
      /// <param name="centerY">center's y component (vertical)
      /// </param>
      /// <param name="deltaY">change in y per step. If scanning up this is negative; down, positive;
      /// left or right, 0
      /// </param>
      /// <param name="top">minimum value of y to search through (meaningless when di == 0)
      /// </param>
      /// <param name="bottom">maximum value of y
      /// </param>
      /// <param name="maxWhiteRun">maximum run of white pixels that can still be considered to be within
      /// the barcode
      /// </param>
      /// <returns> a {@link com.google.zxing.ResultPoint} encapsulating the corner that was found
      /// </returns>
      private ResultPoint findCornerFromCenter(int centerX, int deltaX, int left, int right, int centerY, int deltaY, int top, int bottom, int maxWhiteRun)
      {
         int[] lastRange = null;
         for (int y = centerY, x = centerX; y < bottom && y >= top && x < right && x >= left; y += deltaY, x += deltaX)
         {
            int[] range;
            if (deltaX == 0)
            {
               // horizontal slices, up and down
               range = blackWhiteRange(y, maxWhiteRun, left, right, true);
            }
            else
            {
               // vertical slices, left and right
               range = blackWhiteRange(x, maxWhiteRun, top, bottom, false);
            }
            if (range == null)
            {
               if (lastRange == null)
               {
                  return null;
               }
               // lastRange was found
               if (deltaX == 0)
               {
                  int lastY = y - deltaY;
                  if (lastRange[0] < centerX)
                  {
                     if (lastRange[1] > centerX)
                     {
                        // straddle, choose one or the other based on direction
                        return new ResultPoint(deltaY > 0 ? lastRange[0] : lastRange[1], lastY);
                     }
                     return new ResultPoint(lastRange[0], lastY);
                  }
                  else
                  {
                     return new ResultPoint(lastRange[1], lastY);
                  }
               }
               else
               {
                  int lastX = x - deltaX;
                  if (lastRange[0] < centerY)
                  {
                     if (lastRange[1] > centerY)
                     {
                        return new ResultPoint(lastX, deltaX < 0 ? lastRange[0] : lastRange[1]);
                     }
                     return new ResultPoint(lastX, lastRange[0]);
                  }
                  else
                  {
                     return new ResultPoint(lastX, lastRange[1]);
                  }
               }
            }
            lastRange = range;
         }
         return null;
      }

      /// <summary> Computes the start and end of a region of pixels, either horizontally or vertically, that could
      /// be part of a Data Matrix barcode.
      /// 
      /// </summary>
      /// <param name="fixedDimension">if scanning horizontally, this is the row (the fixed vertical location)
      /// where we are scanning. If scanning vertically it's the column, the fixed horizontal location
      /// </param>
      /// <param name="maxWhiteRun">largest run of white pixels that can still be considered part of the
      /// barcode region
      /// </param>
      /// <param name="minDim">minimum pixel location, horizontally or vertically, to consider
      /// </param>
      /// <param name="maxDim">maximum pixel location, horizontally or vertically, to consider
      /// </param>
      /// <param name="horizontal">if true, we're scanning left-right, instead of up-down
      /// </param>
      /// <returns> int[] with start and end of found range, or null if no such range is found
      /// (e.g. only white was found)
      /// </returns>
      private int[] blackWhiteRange(int fixedDimension, int maxWhiteRun, int minDim, int maxDim, bool horizontal)
      {
         int center = (minDim + maxDim) >> 1;

         // Scan left/up first
         int start = center;
         while (start >= minDim)
         {
            if (horizontal ? image[start, fixedDimension] : image[fixedDimension, start])
            {
               start--;
            }
            else
            {
               int whiteRunStart = start;
               do
               {
                  start--;
               }
               while (start >= minDim && !(horizontal ? image[start, fixedDimension] : image[fixedDimension, start]));
               int whiteRunSize = whiteRunStart - start;
               if (start < minDim || whiteRunSize > maxWhiteRun)
               {
                  start = whiteRunStart;
                  break;
               }
            }
         }
         start++;

         // Then try right/down
         int end = center;
         while (end < maxDim)
         {
            if (horizontal ? image[end, fixedDimension] : image[fixedDimension, end])
            {
               end++;
            }
            else
            {
               int whiteRunStart = end;
               do
               {
                  end++;
               }
               while (end < maxDim && !(horizontal ? image[end, fixedDimension] : image[fixedDimension, end]));
               int whiteRunSize = end - whiteRunStart;
               if (end >= maxDim || whiteRunSize > maxWhiteRun)
               {
                  end = whiteRunStart;
                  break;
               }
            }
         }
         end--;

         return end > start ? new int[] { start, end } : null;
      }
   }
}