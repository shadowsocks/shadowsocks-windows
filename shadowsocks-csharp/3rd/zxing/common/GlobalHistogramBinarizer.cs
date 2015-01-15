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

namespace ZXing.Common
{
   /// <summary> This Binarizer implementation uses the old ZXing global histogram approach. It is suitable
   /// for low-end mobile devices which don't have enough CPU or memory to use a local thresholding
   /// algorithm. However, because it picks a global black point, it cannot handle difficult shadows
   /// and gradients.
   /// 
   /// Faster mobile devices and all desktop applications should probably use HybridBinarizer instead.
   /// 
   /// <author>dswitkin@google.com (Daniel Switkin)</author>
   /// <author>Sean Owen</author>
   /// </summary>
   public class GlobalHistogramBinarizer : Binarizer
   {
      private const int LUMINANCE_BITS = 5;
      private const int LUMINANCE_SHIFT = 8 - LUMINANCE_BITS;
      private const int LUMINANCE_BUCKETS = 1 << LUMINANCE_BITS;
      private static readonly byte[] EMPTY = new byte[0];

      private byte[] luminances;
      private readonly int[] buckets;

      /// <summary>
      /// Initializes a new instance of the <see cref="GlobalHistogramBinarizer"/> class.
      /// </summary>
      /// <param name="source">The source.</param>
      public GlobalHistogramBinarizer(LuminanceSource source)
         : base(source)
      {
         luminances = EMPTY;
         buckets = new int[LUMINANCE_BUCKETS];
      }

      /// <summary>
      /// Applies simple sharpening to the row data to improve performance of the 1D Readers.
      /// </summary>
      /// <param name="y"></param>
      /// <param name="row"></param>
      /// <returns></returns>
      public override BitArray getBlackRow(int y, BitArray row)
      {
         LuminanceSource source = LuminanceSource;
         int width = source.Width;
         if (row == null || row.Size < width)
         {
            row = new BitArray(width);
         }
         else
         {
            row.clear();
         }

         initArrays(width);
         byte[] localLuminances = source.getRow(y, luminances);
         int[] localBuckets = buckets;
         for (int x = 0; x < width; x++)
         {
            int pixel = localLuminances[x] & 0xff;
            localBuckets[pixel >> LUMINANCE_SHIFT]++;
         }
         int blackPoint;
         if (!estimateBlackPoint(localBuckets, out blackPoint))
            return null;

         int left = localLuminances[0] & 0xff;
         int center = localLuminances[1] & 0xff;
         for (int x = 1; x < width - 1; x++)
         {
            int right = localLuminances[x + 1] & 0xff;
            // A simple -1 4 -1 box filter with a weight of 2.
            int luminance = ((center << 2) - left - right) >> 1;
            row[x] = (luminance < blackPoint);
            left = center;
            center = right;
         }
         return row;
      }

      /// <summary>
      /// Does not sharpen the data, as this call is intended to only be used by 2D Readers.
      /// </summary>
      override public BitMatrix BlackMatrix
      {
         get
         {
            LuminanceSource source = LuminanceSource;
            byte[] localLuminances;

            int width = source.Width;
            int height = source.Height;
            BitMatrix matrix = new BitMatrix(width, height);

            // Quickly calculates the histogram by sampling four rows from the image. This proved to be
            // more robust on the blackbox tests than sampling a diagonal as we used to do.
            initArrays(width);
            int[] localBuckets = buckets;
            for (int y = 1; y < 5; y++)
            {
               int row = height * y / 5;
               localLuminances = source.getRow(row, luminances);
               int right = (width << 2) / 5;
               for (int x = width / 5; x < right; x++)
               {
                  int pixel = localLuminances[x] & 0xff;
                  localBuckets[pixel >> LUMINANCE_SHIFT]++;
               }
            }
            int blackPoint;
            if (!estimateBlackPoint(localBuckets, out blackPoint))
               return null;

            // We delay reading the entire image luminance until the black point estimation succeeds.
            // Although we end up reading four rows twice, it is consistent with our motto of
            // "fail quickly" which is necessary for continuous scanning.
            localLuminances = source.Matrix;
            for (int y = 0; y < height; y++)
            {
               int offset = y * width;
               for (int x = 0; x < width; x++)
               {
                  int pixel = localLuminances[offset + x] & 0xff;
                  matrix[x, y] = (pixel < blackPoint);
               }
            }

            return matrix;
         }
      }

      /// <summary>
      /// Creates a new object with the same type as this Binarizer implementation, but with pristine
      /// state. This is needed because Binarizer implementations may be stateful, e.g. keeping a cache
      /// of 1 bit data. See Effective Java for why we can't use Java's clone() method.
      /// </summary>
      /// <param name="source">The LuminanceSource this Binarizer will operate on.</param>
      /// <returns>
      /// A new concrete Binarizer implementation object.
      /// </returns>
      public override Binarizer createBinarizer(LuminanceSource source)
      {
         return new GlobalHistogramBinarizer(source);
      }

      private void initArrays(int luminanceSize)
      {
         if (luminances.Length < luminanceSize)
         {
            luminances = new byte[luminanceSize];
         }
         for (int x = 0; x < LUMINANCE_BUCKETS; x++)
         {
            buckets[x] = 0;
         }
      }

      private static bool estimateBlackPoint(int[] buckets, out int blackPoint)
      {
         blackPoint = 0;
         // Find the tallest peak in the histogram.
         int numBuckets = buckets.Length;
         int maxBucketCount = 0;
         int firstPeak = 0;
         int firstPeakSize = 0;
         for (int x = 0; x < numBuckets; x++)
         {
            if (buckets[x] > firstPeakSize)
            {
               firstPeak = x;
               firstPeakSize = buckets[x];
            }
            if (buckets[x] > maxBucketCount)
            {
               maxBucketCount = buckets[x];
            }
         }

         // Find the second-tallest peak which is somewhat far from the tallest peak.
         int secondPeak = 0;
         int secondPeakScore = 0;
         for (int x = 0; x < numBuckets; x++)
         {
            int distanceToBiggest = x - firstPeak;
            // Encourage more distant second peaks by multiplying by square of distance.
            int score = buckets[x] * distanceToBiggest * distanceToBiggest;
            if (score > secondPeakScore)
            {
               secondPeak = x;
               secondPeakScore = score;
            }
         }

         // Make sure firstPeak corresponds to the black peak.
         if (firstPeak > secondPeak)
         {
            int temp = firstPeak;
            firstPeak = secondPeak;
            secondPeak = temp;
         }

         // If there is too little contrast in the image to pick a meaningful black point, throw rather
         // than waste time trying to decode the image, and risk false positives.
         // TODO: It might be worth comparing the brightest and darkest pixels seen, rather than the
         // two peaks, to determine the contrast.
         if (secondPeak - firstPeak <= numBuckets >> 4)
         {
            return false;
         }

         // Find a valley between them that is low and closer to the white peak.
         int bestValley = secondPeak - 1;
         int bestValleyScore = -1;
         for (int x = secondPeak - 1; x > firstPeak; x--)
         {
            int fromFirst = x - firstPeak;
            int score = fromFirst*fromFirst*(secondPeak - x)*(maxBucketCount - buckets[x]);
            if (score > bestValleyScore)
            {
               bestValley = x;
               bestValleyScore = score;
            }
         }

         blackPoint = bestValley << LUMINANCE_SHIFT;
         return true;
      }
   }
}