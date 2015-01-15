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
using System.Text;

namespace ZXing
{
   /// <summary> The purpose of this class hierarchy is to abstract different bitmap implementations across
   /// platforms into a standard interface for requesting greyscale luminance values. The interface
   /// only provides immutable methods; therefore crop and rotation create copies. This is to ensure
   /// that one Reader does not modify the original luminance source and leave it in an unknown state
   /// for other Readers in the chain.
   /// </summary>
   /// <author>  dswitkin@google.com (Daniel Switkin)
   /// </author>
   /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source 
   /// </author>
   public abstract class LuminanceSource
   {
      private int width;
      private int height;

      protected LuminanceSource(int width, int height)
      {
         this.width = width;
         this.height = height;
      }

      /// <summary> Fetches one row of luminance data from the underlying platform's bitmap. Values range from
      /// 0 (black) to 255 (white). Because Java does not have an unsigned byte type, callers will have
      /// to bitwise and with 0xff for each value. It is preferable for implementations of this method
      /// to only fetch this row rather than the whole image, since no 2D Readers may be installed and
      /// getMatrix() may never be called.
      /// 
      /// </summary>
      /// <param name="y">The row to fetch, 0 &lt;= y &lt; Height.
      /// </param>
      /// <param name="row">An optional preallocated array. If null or too small, it will be ignored.
      /// Always use the returned object, and ignore the .length of the array.
      /// </param>
      /// <returns> An array containing the luminance data.
      /// </returns>
      public abstract byte[] getRow(int y, byte[] row);

      /// <summary> Fetches luminance data for the underlying bitmap. Values should be fetched using:
      /// int luminance = array[y * width + x] &amp; 0xff;
      /// 
      /// </summary>
      /// <returns> A row-major 2D array of luminance values. Do not use result.length as it may be
      /// larger than width * height bytes on some platforms. Do not modify the contents
      /// of the result.
      /// </returns>
      public abstract byte[] Matrix { get; }

      /// <returns> The width of the bitmap.</returns>
      virtual public int Width
      {
         get
         {
            return width;
         }
         protected set
         {
            width = value;
         }
      }

      /// <returns> The height of the bitmap.</returns>
      virtual public int Height
      {
         get
         {
            return height;
         }
         protected set
         {
            height = value;
         }
      }

      /// <returns> Whether this subclass supports cropping.</returns>
      virtual public bool CropSupported
      {
         get
         {
            return false;
         }
      }

      /// <summary> Returns a new object with cropped image data. Implementations may keep a reference to the
      /// original data rather than a copy. Only callable if CropSupported is true.
      /// 
      /// </summary>
      /// <param name="left">The left coordinate, 0 &lt;= left &lt; Width.
      /// </param>
      /// <param name="top">The top coordinate, 0 &lt;= top &lt;= Height.
      /// </param>
      /// <param name="width">The width of the rectangle to crop.
      /// </param>
      /// <param name="height">The height of the rectangle to crop.
      /// </param>
      /// <returns> A cropped version of this object.
      /// </returns>
      public virtual LuminanceSource crop(int left, int top, int width, int height)
      {
         throw new NotSupportedException("This luminance source does not support cropping.");
      }

      /// <returns> Whether this subclass supports counter-clockwise rotation.</returns>
      virtual public bool RotateSupported
      {
         get
         {
            return false;
         }
      }

      /// <summary>
      /// Returns a new object with rotated image data by 90 degrees counterclockwise.
      /// Only callable if {@link #isRotateSupported()} is true.
      /// </summary>
      /// <returns> A rotated version of this object.
      /// </returns>
      public virtual LuminanceSource rotateCounterClockwise()
      {
         throw new NotSupportedException("This luminance source does not support rotation.");
      }

      /// <summary>
      /// Returns a new object with rotated image data by 45 degrees counterclockwise.
      /// Only callable if {@link #isRotateSupported()} is true.
      /// </summary>
      /// <returns>A rotated version of this object.</returns>
      public virtual LuminanceSource rotateCounterClockwise45()
      {
         throw new NotSupportedException("This luminance source does not support rotation by 45 degrees.");
      }

      /// <summary>
      /// </summary>
      /// <returns>Whether this subclass supports invertion.</returns>
      virtual public bool InversionSupported
      {
         get
         {
            return false;
         }
      }

      override public String ToString()
      {
         var row = new byte[width];
         var result = new StringBuilder(height * (width + 1));
         for (int y = 0; y < height; y++)
         {
            row = getRow(y, row);
            for (int x = 0; x < width; x++)
            {
               int luminance = row[x] & 0xFF;
               char c;
               if (luminance < 0x40)
               {
                  c = '#';
               }
               else if (luminance < 0x80)
               {
                  c = '+';
               }
               else if (luminance < 0xC0)
               {
                  c = '.';
               }
               else
               {
                  c = ' ';
               }
               result.Append(c);
            }
            result.Append('\n');
         }
         return result.ToString();
      }
   }
}