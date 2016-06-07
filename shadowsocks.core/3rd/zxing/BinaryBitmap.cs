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

using System;
using ZXing.Common;

namespace ZXing
{

   /// <summary> This class is the core bitmap class used by ZXing to represent 1 bit data. Reader objects
   /// accept a BinaryBitmap and attempt to decode it.
   /// 
   /// </summary>
   /// <author>  dswitkin@google.com (Daniel Switkin)
   /// </author>
   /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source 
   /// </author>

   public sealed class BinaryBitmap
   {
      private Binarizer binarizer;
      private BitMatrix matrix;

      public BinaryBitmap(Binarizer binarizer)
      {
         if (binarizer == null)
         {
            throw new ArgumentException("Binarizer must be non-null.");
         }
         this.binarizer = binarizer;
      }

      /// <returns> The width of the bitmap.
      /// </returns>
      public int Width
      {
         get
         {
            return binarizer.Width;
         }

      }
      /// <returns> The height of the bitmap.
      /// </returns>
      public int Height
      {
         get
         {
            return binarizer.Height;
         }

      }

      /// <summary> Converts one row of luminance data to 1 bit data. May actually do the conversion, or return
      /// cached data. Callers should assume this method is expensive and call it as seldom as possible.
      /// This method is intended for decoding 1D barcodes and may choose to apply sharpening.
      /// 
      /// </summary>
      /// <param name="y">The row to fetch, 0 &lt;= y &lt; bitmap height.
      /// </param>
      /// <param name="row">An optional preallocated array. If null or too small, it will be ignored.
      /// If used, the Binarizer will call BitArray.clear(). Always use the returned object.
      /// </param>
      /// <returns> The array of bits for this row (true means black).
      /// </returns>
      public BitArray getBlackRow(int y, BitArray row)
      {
         return binarizer.getBlackRow(y, row);
      }

      /// <summary> Converts a 2D array of luminance data to 1 bit. As above, assume this method is expensive
      /// and do not call it repeatedly. This method is intended for decoding 2D barcodes and may or
      /// may not apply sharpening. Therefore, a row from this matrix may not be identical to one
      /// fetched using getBlackRow(), so don't mix and match between them.
      /// 
      /// </summary>
      /// <returns> The 2D array of bits for the image (true means black).
      /// </returns>
      public BitMatrix BlackMatrix
      {
         get
         {
            // The matrix is created on demand the first time it is requested, then cached. There are two
            // reasons for this:
            // 1. This work will never be done if the caller only installs 1D Reader objects, or if a
            //    1D Reader finds a barcode before the 2D Readers run.
            // 2. This work will only be done once even if the caller installs multiple 2D Readers.
            if (matrix == null)
            {
               matrix = binarizer.BlackMatrix;
            }
            return matrix;
         }
      }

      /// <returns> Whether this bitmap can be cropped.
      /// </returns>
      public bool CropSupported
      {
         get
         {
            return binarizer.LuminanceSource.CropSupported;
         }

      }

      /// <summary> Returns a new object with cropped image data. Implementations may keep a reference to the
      /// original data rather than a copy. Only callable if isCropSupported() is true.
      /// 
      /// </summary>
      /// <param name="left">The left coordinate, 0 &lt;= left &lt; getWidth().
      /// </param>
      /// <param name="top">The top coordinate, 0 &lt;= top &lt;= getHeight().
      /// </param>
      /// <param name="width">The width of the rectangle to crop.
      /// </param>
      /// <param name="height">The height of the rectangle to crop.
      /// </param>
      /// <returns> A cropped version of this object.
      /// </returns>
      public BinaryBitmap crop(int left, int top, int width, int height)
      {
         var newSource = binarizer.LuminanceSource.crop(left, top, width, height);
         return new BinaryBitmap(binarizer.createBinarizer(newSource));
      }

      /// <returns> Whether this bitmap supports counter-clockwise rotation.
      /// </returns>
      public bool RotateSupported
      {
         get
         {
            return binarizer.LuminanceSource.RotateSupported;
         }

      }

      /// <summary>
      /// Returns a new object with rotated image data by 90 degrees counterclockwise.
      /// Only callable if {@link #isRotateSupported()} is true.
      /// </summary>
      /// <returns> A rotated version of this object.
      /// </returns>
      public BinaryBitmap rotateCounterClockwise()
      {
         var newSource = binarizer.LuminanceSource.rotateCounterClockwise();
         return new BinaryBitmap(binarizer.createBinarizer(newSource));
      }

      /// <summary>
      /// Returns a new object with rotated image data by 45 degrees counterclockwise.
      /// Only callable if {@link #isRotateSupported()} is true.
      /// </summary>
      /// <returns>A rotated version of this object.</returns>
      public BinaryBitmap rotateCounterClockwise45()
      {
         LuminanceSource newSource = binarizer.LuminanceSource.rotateCounterClockwise45();
         return new BinaryBitmap(binarizer.createBinarizer(newSource));
      }

      /// <summary>
      /// Returns a <see cref="System.String"/> that represents this instance.
      /// </summary>
      /// <returns>
      /// A <see cref="System.String"/> that represents this instance.
      /// </returns>
      public override string ToString()
      {
         var blackMatrix = BlackMatrix;
         return blackMatrix != null ? blackMatrix.ToString() : String.Empty;
      }
   }
}