/*
* Copyright 2007 ZXing authors
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

namespace ZXing.Common
{
   /// <summary> Implementations of this class can, given locations of finder patterns for a QR code in an
   /// image, sample the right points in the image to reconstruct the QR code, accounting for
   /// perspective distortion. It is abstracted since it is relatively expensive and should be allowed
   /// to take advantage of platform-specific optimized implementations, like Sun's Java Advanced
   /// Imaging library, but which may not be available in other environments such as J2ME, and vice
   /// versa.
   /// 
   /// The implementation used can be controlled by calling {@link #setGridSampler(GridSampler)}
   /// with an instance of a class which implements this interface.
   /// 
   /// </summary>
   /// <author>  Sean Owen
   /// </author>
   /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source 
   /// </author>
   public abstract class GridSampler
   {
      /// <returns> the current implementation of {@link GridSampler}
      /// </returns>
      public static GridSampler Instance
      {
         get
         {
            return gridSampler;
         }

      }

      private static GridSampler gridSampler = new DefaultGridSampler();

      /// <summary> Sets the implementation of {@link GridSampler} used by the library. One global
      /// instance is stored, which may sound problematic. But, the implementation provided
      /// ought to be appropriate for the entire platform, and all uses of this library
      /// in the whole lifetime of the JVM. For instance, an Android activity can swap in
      /// an implementation that takes advantage of native platform libraries.
      /// 
      /// </summary>
      /// <param name="newGridSampler">The platform-specific object to install.
      /// </param>
      public static void setGridSampler(GridSampler newGridSampler)
      {
         if (newGridSampler == null)
         {
            throw new System.ArgumentException();
         }
         gridSampler = newGridSampler;
      }

      /// <summary> <p>Samples an image for a square matrix of bits of the given dimension. This is used to extract
      /// the black/white modules of a 2D barcode like a QR Code found in an image. Because this barcode
      /// may be rotated or perspective-distorted, the caller supplies four points in the source image
      /// that define known points in the barcode, so that the image may be sampled appropriately.</p>
      /// 
      /// <p>The last eight "from" parameters are four X/Y coordinate pairs of locations of points in
      /// the image that define some significant points in the image to be sample. For example,
      /// these may be the location of finder pattern in a QR Code.</p>
      /// 
      /// <p>The first eight "to" parameters are four X/Y coordinate pairs measured in the destination
      /// {@link BitMatrix}, from the top left, where the known points in the image given by the "from"
      /// parameters map to.</p>
      /// 
      /// <p>These 16 parameters define the transformation needed to sample the image.</p>
      /// 
      /// </summary>
      /// <param name="image">image to sample
      /// </param>
      /// <param name="dimension">width/height of {@link BitMatrix} to sample from image
      /// </param>
      /// <returns> {@link BitMatrix} representing a grid of points sampled from the image within a region
      /// defined by the "from" parameters
      /// </returns>
      /// <throws>  ReaderException if image can't be sampled, for example, if the transformation defined </throws>
      /// <summary>   by the given points is invalid or results in sampling outside the image boundaries
      /// </summary>
      public abstract BitMatrix sampleGrid(BitMatrix image, int dimensionX, int dimensionY, float p1ToX, float p1ToY, float p2ToX, float p2ToY, float p3ToX, float p3ToY, float p4ToX, float p4ToY, float p1FromX, float p1FromY, float p2FromX, float p2FromY, float p3FromX, float p3FromY, float p4FromX, float p4FromY);

      public virtual BitMatrix sampleGrid(BitMatrix image, int dimensionX, int dimensionY, PerspectiveTransform transform)
      {
         throw new System.NotSupportedException();
      }


      /// <summary> <p>Checks a set of points that have been transformed to sample points on an image against
      /// the image's dimensions to see if the point are even within the image.</p>
      /// 
      /// <p>This method will actually "nudge" the endpoints back onto the image if they are found to be
      /// barely (less than 1 pixel) off the image. This accounts for imperfect detection of finder
      /// patterns in an image where the QR Code runs all the way to the image border.</p>
      /// 
      /// <p>For efficiency, the method will check points from either end of the line until one is found
      /// to be within the image. Because the set of points are assumed to be linear, this is valid.</p>
      /// 
      /// </summary>
      /// <param name="image">image into which the points should map
      /// </param>
      /// <param name="points">actual points in x1,y1,...,xn,yn form
      /// </param>
      protected internal static bool checkAndNudgePoints(BitMatrix image, float[] points)
      {
         int width = image.Width;
         int height = image.Height;
         // Check and nudge points from start until we see some that are OK:
         bool nudged = true;
         for (int offset = 0; offset < points.Length && nudged; offset += 2)
         {
            //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
            int x = (int)points[offset];
            //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
            int y = (int)points[offset + 1];
            if (x < -1 || x > width || y < -1 || y > height)
            {
               return false;
            }
            nudged = false;
            if (x == -1)
            {
               points[offset] = 0.0f;
               nudged = true;
            }
            else if (x == width)
            {
               points[offset] = width - 1;
               nudged = true;
            }
            if (y == -1)
            {
               points[offset + 1] = 0.0f;
               nudged = true;
            }
            else if (y == height)
            {
               points[offset + 1] = height - 1;
               nudged = true;
            }
         }
         // Check and nudge points from end:
         nudged = true;
         for (int offset = points.Length - 2; offset >= 0 && nudged; offset -= 2)
         {
            //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
            int x = (int)points[offset];
            //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
            int y = (int)points[offset + 1];
            if (x < -1 || x > width || y < -1 || y > height)
            {
               return false;
            }
            nudged = false;
            if (x == -1)
            {
               points[offset] = 0.0f;
               nudged = true;
            }
            else if (x == width)
            {
               points[offset] = width - 1;
               nudged = true;
            }
            if (y == -1)
            {
               points[offset + 1] = 0.0f;
               nudged = true;
            }
            else if (y == height)
            {
               points[offset + 1] = height - 1;
               nudged = true;
            }
         }

         return true;
      }
   }
}