/*
 * Copyright 2010 ZXing authors
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

using System;

namespace ZXing.Common.Detector
{
   /// <summary>
   /// Detects a candidate barcode-like rectangular region within an image. It
   /// starts around the center of the image, increases the size of the candidate
   /// region until it finds a white rectangular region. By keeping track of the
   /// last black points it encountered, it determines the corners of the barcode.
   /// </summary>
   /// <author>David Olivier</author>
   public sealed class WhiteRectangleDetector
   {
      private const int INIT_SIZE = 10;
      private const int CORR = 1;

      private readonly BitMatrix image;
      private readonly int height;
      private readonly int width;
      private readonly int leftInit;
      private readonly int rightInit;
      private readonly int downInit;
      private readonly int upInit;

      /// <summary>
      /// Creates a WhiteRectangleDetector instance
      /// </summary>
      /// <param name="image">The image.</param>
      /// <returns>null, if image is too small, otherwise a WhiteRectangleDetector instance</returns>
      public static WhiteRectangleDetector Create(BitMatrix image)
      {
         if (image == null)
            return null;

         var instance = new WhiteRectangleDetector(image);

         if (instance.upInit < 0 || instance.leftInit < 0 || instance.downInit >= instance.height || instance.rightInit >= instance.width)
         {
            return null;
         }

         return instance;
      }

      /// <summary>
      /// Creates a WhiteRectangleDetector instance
      /// </summary>
      /// <param name="image">The image.</param>
      /// <param name="initSize">Size of the init.</param>
      /// <param name="x">The x.</param>
      /// <param name="y">The y.</param>
      /// <returns>
      /// null, if image is too small, otherwise a WhiteRectangleDetector instance
      /// </returns>
      public static WhiteRectangleDetector Create(BitMatrix image, int initSize, int x, int y)
      {
         var instance = new WhiteRectangleDetector(image, initSize, x, y);

         if (instance.upInit < 0 || instance.leftInit < 0 || instance.downInit >= instance.height || instance.rightInit >= instance.width)
         {
            return null;
         }

         return instance;
      }


      /// <summary>
      /// Initializes a new instance of the <see cref="WhiteRectangleDetector"/> class.
      /// </summary>
      /// <param name="image">The image.</param>
      /// <exception cref="ArgumentException">if image is too small</exception>
      internal WhiteRectangleDetector(BitMatrix image)
         : this(image, INIT_SIZE, image.Width/2, image.Height/2)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="WhiteRectangleDetector"/> class.
      /// </summary>
      /// <param name="image">The image.</param>
      /// <param name="initSize">Size of the init.</param>
      /// <param name="x">The x.</param>
      /// <param name="y">The y.</param>
      internal WhiteRectangleDetector(BitMatrix image, int initSize, int x, int y)
      {
         this.image = image;
         height = image.Height;
         width = image.Width;
         int halfsize = initSize / 2;
         leftInit = x - halfsize;
         rightInit = x + halfsize;
         upInit = y - halfsize;
         downInit = y + halfsize;
      }

      /// <summary>
      /// Detects a candidate barcode-like rectangular region within an image. It
      /// starts around the center of the image, increases the size of the candidate
      /// region until it finds a white rectangular region.
      /// </summary>
      /// <returns><see cref="ResultPoint" />[] describing the corners of the rectangular
      /// region. The first and last points are opposed on the diagonal, as
      /// are the second and third. The first point will be the topmost
      /// point and the last, the bottommost. The second point will be
      /// leftmost and the third, the rightmost</returns>
      public ResultPoint[] detect()
      {
         int left = leftInit;
         int right = rightInit;
         int up = upInit;
         int down = downInit;
         bool sizeExceeded = false;
         bool aBlackPointFoundOnBorder = true;
         bool atLeastOneBlackPointFoundOnBorder = false;

         bool atLeastOneBlackPointFoundOnRight = false;
         bool atLeastOneBlackPointFoundOnBottom = false;
         bool atLeastOneBlackPointFoundOnLeft = false;
         bool atLeastOneBlackPointFoundOnTop = false;

         while (aBlackPointFoundOnBorder)
         {

            aBlackPointFoundOnBorder = false;

            // .....
            // .   |
            // .....
            bool rightBorderNotWhite = true;
            while ((rightBorderNotWhite || !atLeastOneBlackPointFoundOnRight) && right < width)
            {
               rightBorderNotWhite = containsBlackPoint(up, down, right, false);
               if (rightBorderNotWhite)
               {
                  right++;
                  aBlackPointFoundOnBorder = true;
                  atLeastOneBlackPointFoundOnRight = true;
               }
               else if (!atLeastOneBlackPointFoundOnRight)
               {
                  right++;
               }
            }

            if (right >= width)
            {
               sizeExceeded = true;
               break;
            }

            // .....
            // .   .
            // .___.
            bool bottomBorderNotWhite = true;
            while ((bottomBorderNotWhite || !atLeastOneBlackPointFoundOnBottom) && down < height)
            {
               bottomBorderNotWhite = containsBlackPoint(left, right, down, true);
               if (bottomBorderNotWhite)
               {
                  down++;
                  aBlackPointFoundOnBorder = true;
                  atLeastOneBlackPointFoundOnBottom = true;
               }
               else if (!atLeastOneBlackPointFoundOnBottom)
               {
                  down++;
               }
            }

            if (down >= height)
            {
               sizeExceeded = true;
               break;
            }

            // .....
            // |   .
            // .....
            bool leftBorderNotWhite = true;
            while ((leftBorderNotWhite || !atLeastOneBlackPointFoundOnLeft) && left >= 0)
            {
               leftBorderNotWhite = containsBlackPoint(up, down, left, false);
               if (leftBorderNotWhite)
               {
                  left--;
                  aBlackPointFoundOnBorder = true;
                  atLeastOneBlackPointFoundOnLeft = true;
               }
               else if (!atLeastOneBlackPointFoundOnLeft)
               {
                  left--;
               }
            }

            if (left < 0)
            {
               sizeExceeded = true;
               break;
            }

            // .___.
            // .   .
            // .....
            bool topBorderNotWhite = true;
            while ((topBorderNotWhite  || !atLeastOneBlackPointFoundOnTop) && up >= 0)
            {
               topBorderNotWhite = containsBlackPoint(left, right, up, true);
               if (topBorderNotWhite)
               {
                  up--;
                  aBlackPointFoundOnBorder = true;
                  atLeastOneBlackPointFoundOnTop = true;
               }
               else if (!atLeastOneBlackPointFoundOnTop)
               {
                  up--;
               }
            }

            if (up < 0)
            {
               sizeExceeded = true;
               break;
            }

            if (aBlackPointFoundOnBorder)
            {
               atLeastOneBlackPointFoundOnBorder = true;
            }

         }

         if (!sizeExceeded && atLeastOneBlackPointFoundOnBorder)
         {

            int maxSize = right - left;

            ResultPoint z = null;
            for (int i = 1; i < maxSize; i++)
            {
               z = getBlackPointOnSegment(left, down - i, left + i, down);
               if (z != null)
               {
                  break;
               }
            }

            if (z == null)
            {
               return null;
            }

            ResultPoint t = null;
            //go down right
            for (int i = 1; i < maxSize; i++)
            {
               t = getBlackPointOnSegment(left, up + i, left + i, up);
               if (t != null)
               {
                  break;
               }
            }

            if (t == null)
            {
               return null;
            }

            ResultPoint x = null;
            //go down left
            for (int i = 1; i < maxSize; i++)
            {
               x = getBlackPointOnSegment(right, up + i, right - i, up);
               if (x != null)
               {
                  break;
               }
            }

            if (x == null)
            {
               return null;
            }

            ResultPoint y = null;
            //go up left
            for (int i = 1; i < maxSize; i++)
            {
               y = getBlackPointOnSegment(right, down - i, right - i, down);
               if (y != null)
               {
                  break;
               }
            }

            if (y == null)
            {
               return null;
            }

            return centerEdges(y, z, x, t);

         }
         else
         {
            return null;
         }
      }

      private ResultPoint getBlackPointOnSegment(float aX, float aY, float bX, float bY)
      {
         int dist = MathUtils.round(MathUtils.distance(aX, aY, bX, bY));
         float xStep = (bX - aX) / dist;
         float yStep = (bY - aY) / dist;

         for (int i = 0; i < dist; i++)
         {
            int x = MathUtils.round(aX + i * xStep);
            int y = MathUtils.round(aY + i * yStep);
            if (image[x, y])
            {
               return new ResultPoint(x, y);
            }
         }
         return null;
      }

      /// <summary>
      /// recenters the points of a constant distance towards the center
      /// </summary>
      /// <param name="y">bottom most point</param>
      /// <param name="z">left most point</param>
      /// <param name="x">right most point</param>
      /// <param name="t">top most point</param>
      /// <returns><see cref="ResultPoint"/>[] describing the corners of the rectangular
      /// region. The first and last points are opposed on the diagonal, as
      /// are the second and third. The first point will be the topmost
      /// point and the last, the bottommost. The second point will be
      /// leftmost and the third, the rightmost</returns>
      private ResultPoint[] centerEdges(ResultPoint y, ResultPoint z,
                                        ResultPoint x, ResultPoint t)
      {
         //
         //       t            t
         //  z                      x
         //        x    OR    z
         //   y                    y
         //

         float yi = y.X;
         float yj = y.Y;
         float zi = z.X;
         float zj = z.Y;
         float xi = x.X;
         float xj = x.Y;
         float ti = t.X;
         float tj = t.Y;

         if (yi < width / 2.0f)
         {
            return new[]
                      {
                         new ResultPoint(ti - CORR, tj + CORR),
                         new ResultPoint(zi + CORR, zj + CORR),
                         new ResultPoint(xi - CORR, xj - CORR),
                         new ResultPoint(yi + CORR, yj - CORR)
                      };
         }
         else
         {
            return new[]
                      {
                         new ResultPoint(ti + CORR, tj + CORR),
                         new ResultPoint(zi + CORR, zj - CORR),
                         new ResultPoint(xi - CORR, xj + CORR),
                         new ResultPoint(yi - CORR, yj - CORR)
                      };
         }
      }

      /// <summary>
      /// Determines whether a segment contains a black point
      /// </summary>
      /// <param name="a">min value of the scanned coordinate</param>
      /// <param name="b">max value of the scanned coordinate</param>
      /// <param name="fixed">value of fixed coordinate</param>
      /// <param name="horizontal">set to true if scan must be horizontal, false if vertical</param>
      /// <returns>
      ///   true if a black point has been found, else false.
      /// </returns>
      private bool containsBlackPoint(int a, int b, int @fixed, bool horizontal)
      {
         if (horizontal)
         {
            for (int x = a; x <= b; x++)
            {
               if (image[x, @fixed])
               {
                  return true;
               }
            }
         }
         else
         {
            for (int y = a; y <= b; y++)
            {
               if (image[@fixed, y])
               {
                  return true;
               }
            }
         }
         return false;
      }
   }
}