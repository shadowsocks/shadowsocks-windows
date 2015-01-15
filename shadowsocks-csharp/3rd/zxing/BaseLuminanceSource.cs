/*
* Copyright 2012 ZXing.Net authors
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

namespace ZXing
{
   /// <summary>
   /// The base class for luminance sources which supports 
   /// cropping and rotating based upon the luminance values.
   /// </summary>
   public abstract class BaseLuminanceSource : LuminanceSource
   {
      // the following channel weights give nearly the same
      // gray scale picture as the java version with BufferedImage.TYPE_BYTE_GRAY
      // they are used in sub classes for luminance / gray scale calculation
      protected const int RChannelWeight = 19562;
      protected const int GChannelWeight = 38550;
      protected const int BChannelWeight = 7424;
      protected const int ChannelWeight = 16;

      /// <summary>
      /// 
      /// </summary>
      protected byte[] luminances;

      /// <summary>
      /// Initializes a new instance of the <see cref="BaseLuminanceSource"/> class.
      /// </summary>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      protected BaseLuminanceSource(int width, int height)
         : base(width, height)
      {
         luminances = new byte[width * height];
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BaseLuminanceSource"/> class.
      /// </summary>
      /// <param name="luminanceArray">The luminance array.</param>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      protected BaseLuminanceSource(byte[] luminanceArray, int width, int height)
         : base(width, height)
      {
         luminances = new byte[width * height];
         Buffer.BlockCopy(luminanceArray, 0, luminances, 0, width * height);
      }

      /// <summary>
      /// Fetches one row of luminance data from the underlying platform's bitmap. Values range from
      /// 0 (black) to 255 (white). It is preferable for implementations of this method
      /// to only fetch this row rather than the whole image, since no 2D Readers may be installed and
      /// getMatrix() may never be called.
      /// </summary>
      /// <param name="y">The row to fetch, 0 &lt;= y &lt; Height.</param>
      /// <param name="row">An optional preallocated array. If null or too small, it will be ignored.
      /// Always use the returned object, and ignore the .length of the array.</param>
      /// <returns>
      /// An array containing the luminance data.
      /// </returns>
      override public byte[] getRow(int y, byte[] row)
      {
         int width = Width;
         if (row == null || row.Length < width)
         {
            row = new byte[width];
         }
         for (int i = 0; i < width; i++)
            row[i] = luminances[y * width + i];
         return row;
      }

      public override byte[] Matrix
      {
         get { return luminances; }
      }

      /// <summary>
      /// Returns a new object with rotated image data by 90 degrees counterclockwise.
      /// Only callable if {@link #isRotateSupported()} is true.
      /// </summary>
      /// <returns>
      /// A rotated version of this object.
      /// </returns>
      public override LuminanceSource rotateCounterClockwise()
      {
         var rotatedLuminances = new byte[Width * Height];
         var newWidth = Height;
         var newHeight = Width;
         var localLuminances = Matrix;
         for (var yold = 0; yold < Height; yold++)
         {
            for (var xold = 0; xold < Width; xold++)
            {
               var ynew = newHeight - xold - 1;
               var xnew = yold;
               rotatedLuminances[ynew * newWidth + xnew] = localLuminances[yold * Width + xold];
            }
         }
         return CreateLuminanceSource(rotatedLuminances, newWidth, newHeight);
      }

      /// <summary>
      /// TODO: not implemented yet
      /// </summary>
      /// <returns>
      /// A rotated version of this object.
      /// </returns>
      public override LuminanceSource rotateCounterClockwise45()
      {
         // TODO: implement a good 45 degrees rotation without lost of information
         return base.rotateCounterClockwise45();
      }

      /// <summary>
      /// </summary>
      /// <returns> Whether this subclass supports counter-clockwise rotation.</returns>
      public override bool RotateSupported
      {
         get
         {
            return true;
         }
      }

      /// <summary>
      /// Returns a new object with cropped image data. Implementations may keep a reference to the
      /// original data rather than a copy. Only callable if CropSupported is true.
      /// </summary>
      /// <param name="left">The left coordinate, 0 &lt;= left &lt; Width.</param>
      /// <param name="top">The top coordinate, 0 &lt;= top &lt;= Height.</param>
      /// <param name="width">The width of the rectangle to crop.</param>
      /// <param name="height">The height of the rectangle to crop.</param>
      /// <returns>
      /// A cropped version of this object.
      /// </returns>
      public override LuminanceSource crop(int left, int top, int width, int height)
      {
         if (left + width > Width || top + height > Height)
         {
            throw new ArgumentException("Crop rectangle does not fit within image data.");
         }
         var croppedLuminances = new byte[width * height];
         var oldLuminances = Matrix;
         var oldWidth = Width;
         var oldRightBound = left + width;
         var oldBottomBound = top + height;
         for (int yold = top, ynew = 0; yold < oldBottomBound; yold++, ynew++)
         {
            for (int xold = left, xnew = 0; xold < oldRightBound; xold++, xnew++)
            {
               croppedLuminances[ynew * width + xnew] = oldLuminances[yold * oldWidth + xold];
            }
         }
         return CreateLuminanceSource(croppedLuminances, width, height);
      }

      /// <summary>
      /// </summary>
      /// <returns> Whether this subclass supports cropping.</returns>
      public override bool CropSupported
      {
         get
         {
            return true;
         }
      }

      /// <summary>
      /// </summary>
      /// <returns>Whether this subclass supports invertion.</returns>
      public override bool InversionSupported
      {
         get
         {
            return true;
         }
      }


      /// <summary>
      /// Should create a new luminance source with the right class type.
      /// The method is used in methods crop and rotate.
      /// </summary>
      /// <param name="newLuminances">The new luminances.</param>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      /// <returns></returns>
      protected abstract LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height);
   }
}
