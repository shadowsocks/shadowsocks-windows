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
   /// Luminance source class which support different formats of images.
   /// </summary>
   public partial class RGBLuminanceSource : BaseLuminanceSource
   {
      /// <summary>
      /// enumeration of supported bitmap format which the RGBLuminanceSource can process
      /// </summary>
      public enum BitmapFormat
      {
         /// <summary>
         /// format of the byte[] isn't known. RGBLuminanceSource tries to determine the best possible value
         /// </summary>
         Unknown,
         /// <summary>
         /// grayscale array, the byte array is a luminance array with 1 byte per pixel
         /// </summary>
         Gray8,
         /// <summary>
         /// 3 bytes per pixel with the channels red, green and blue
         /// </summary>
         RGB24,
         /// <summary>
         /// 4 bytes per pixel with the channels red, green and blue
         /// </summary>
         RGB32,
         /// <summary>
         /// 4 bytes per pixel with the channels alpha, red, green and blue
         /// </summary>
         ARGB32,
         /// <summary>
         /// 3 bytes per pixel with the channels blue, green and red
         /// </summary>
         BGR24,
         /// <summary>
         /// 4 bytes per pixel with the channels blue, green and red
         /// </summary>
         BGR32,
         /// <summary>
         /// 4 bytes per pixel with the channels blue, green, red and alpha
         /// </summary>
         BGRA32,
         /// <summary>
         /// 2 bytes per pixel, 5 bit red, 6 bits green and 5 bits blue
         /// </summary>
         RGB565,
         /// <summary>
         /// 4 bytes per pixel with the channels red, green, blue and alpha
         /// </summary>
         RGBA32,
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="RGBLuminanceSource"/> class.
      /// </summary>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      protected RGBLuminanceSource(int width, int height)
         : base(width, height)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="RGBLuminanceSource"/> class.
      /// It supports a byte array with 3 bytes per pixel (RGB24).
      /// </summary>
      /// <param name="rgbRawBytes">The RGB raw bytes.</param>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      public RGBLuminanceSource(byte[] rgbRawBytes, int width, int height)
         : this(rgbRawBytes, width, height, BitmapFormat.RGB24)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="RGBLuminanceSource"/> class.
      /// It supports a byte array with 1 byte per pixel (Gray8).
      /// That means the whole array consists of the luminance values (grayscale).
      /// </summary>
      /// <param name="luminanceArray">The luminance array.</param>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      /// <param name="is8Bit">if set to <c>true</c> [is8 bit].</param>
      [Obsolete("Use RGBLuminanceSource(luminanceArray, width, height, BitmapFormat.Gray8)")]
      public RGBLuminanceSource(byte[] luminanceArray, int width, int height, bool is8Bit)
         : this(luminanceArray, width, height, BitmapFormat.Gray8)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="RGBLuminanceSource"/> class.
      /// It supports a byte array with 3 bytes per pixel (RGB24).
      /// </summary>
      /// <param name="rgbRawBytes">The RGB raw bytes.</param>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      /// <param name="bitmapFormat">The bitmap format.</param>
      public RGBLuminanceSource(byte[] rgbRawBytes, int width, int height, BitmapFormat bitmapFormat)
         : base(width, height)
      {
         CalculateLuminance(rgbRawBytes, bitmapFormat);
      }

      /// <summary>
      /// Should create a new luminance source with the right class type.
      /// The method is used in methods crop and rotate.
      /// </summary>
      /// <param name="newLuminances">The new luminances.</param>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      /// <returns></returns>
      protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
      {
         return new RGBLuminanceSource(width, height) { luminances = newLuminances };
      }

      private static BitmapFormat DetermineBitmapFormat(byte[] rgbRawBytes, int width, int height)
      {
         var square = width*height;
         var byteperpixel = rgbRawBytes.Length/square;

         switch (byteperpixel)
         {
            case 1:
               return BitmapFormat.Gray8;
            case 2:
               return BitmapFormat.RGB565;
            case 3:
               return BitmapFormat.RGB24;
            case 4:
               return BitmapFormat.RGB32;
            default:
               throw new ArgumentException("The bitmap format could not be determined. Please specify the correct value.");
         }
      }

      protected void CalculateLuminance(byte[] rgbRawBytes, BitmapFormat bitmapFormat)
      {
         if (bitmapFormat == BitmapFormat.Unknown)
         {
            bitmapFormat = DetermineBitmapFormat(rgbRawBytes, Width, Height);
         }
         switch (bitmapFormat)
         {
            case BitmapFormat.Gray8:
               Buffer.BlockCopy(rgbRawBytes, 0, luminances, 0, rgbRawBytes.Length < luminances.Length ? rgbRawBytes.Length : luminances.Length);
               break;
            case BitmapFormat.RGB24:
               CalculateLuminanceRGB24(rgbRawBytes);
               break;
            case BitmapFormat.BGR24:
               CalculateLuminanceBGR24(rgbRawBytes);
               break;
            case BitmapFormat.RGB32:
               CalculateLuminanceRGB32(rgbRawBytes);
               break;
            case BitmapFormat.BGR32:
               CalculateLuminanceBGR32(rgbRawBytes);
               break;
            case BitmapFormat.RGBA32:
               CalculateLuminanceRGBA32(rgbRawBytes);
               break;
            case BitmapFormat.ARGB32:
               CalculateLuminanceARGB32(rgbRawBytes);
               break;
            case BitmapFormat.BGRA32:
               CalculateLuminanceBGRA32(rgbRawBytes);
               break;
            case BitmapFormat.RGB565:
               CalculateLuminanceRGB565(rgbRawBytes);
               break;
            default:
               throw new ArgumentException("The bitmap format isn't supported.", bitmapFormat.ToString());
         }
      }

      private void CalculateLuminanceRGB565(byte[] rgb565RawData)
      {
         var luminanceIndex = 0;
         for (var index = 0; index < rgb565RawData.Length && luminanceIndex < luminances.Length; index += 2, luminanceIndex++)
         {
            var byte1 = rgb565RawData[index];
            var byte2 = rgb565RawData[index + 1];

            var b5 = byte1 & 0x1F;
            var g5 = (((byte1 & 0xE0) >> 5) | ((byte2 & 0x03) << 3)) & 0x1F;
            var r5 = (byte2 >> 2) & 0x1F;
            var r8 = (r5 * 527 + 23) >> 6;
            var g8 = (g5 * 527 + 23) >> 6;
            var b8 = (b5 * 527 + 23) >> 6;

            // cheap, not fully accurate conversion
            //var pixel = (byte2 << 8) | byte1;
            //b8 = (((pixel) & 0x001F) << 3);
            //g8 = (((pixel) & 0x07E0) >> 2) & 0xFF;
            //r8 = (((pixel) & 0xF800) >> 8);

            luminances[luminanceIndex] = (byte)((RChannelWeight * r8 + GChannelWeight * g8 + BChannelWeight * b8) >> ChannelWeight);
         }
      }

      private void CalculateLuminanceRGB24(byte[] rgbRawBytes)
      {
         for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
         {
            // Calculate luminance cheaply, favoring green.
            int r = rgbRawBytes[rgbIndex++];
            int g = rgbRawBytes[rgbIndex++];
            int b = rgbRawBytes[rgbIndex++];
            luminances[luminanceIndex] = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
         }
      }

      private void CalculateLuminanceBGR24(byte[] rgbRawBytes)
      {
         for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
         {
            // Calculate luminance cheaply, favoring green.
            int b = rgbRawBytes[rgbIndex++];
            int g = rgbRawBytes[rgbIndex++];
            int r = rgbRawBytes[rgbIndex++];
            luminances[luminanceIndex] = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
         }
      }

      private void CalculateLuminanceRGB32(byte[] rgbRawBytes)
      {
         for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
         {
            // Calculate luminance cheaply, favoring green.
            int r = rgbRawBytes[rgbIndex++];
            int g = rgbRawBytes[rgbIndex++];
            int b = rgbRawBytes[rgbIndex++];
            rgbIndex++;
            luminances[luminanceIndex] = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
         }
      }

      private void CalculateLuminanceBGR32(byte[] rgbRawBytes)
      {
         for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
         {
            // Calculate luminance cheaply, favoring green.
            int b = rgbRawBytes[rgbIndex++];
            int g = rgbRawBytes[rgbIndex++];
            int r = rgbRawBytes[rgbIndex++];
            rgbIndex++;
            luminances[luminanceIndex] = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
         }
      }

      private void CalculateLuminanceBGRA32(byte[] rgbRawBytes)
      {
         for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
         {
            // Calculate luminance cheaply, favoring green.
            var b = rgbRawBytes[rgbIndex++];
            var g = rgbRawBytes[rgbIndex++];
            var r = rgbRawBytes[rgbIndex++];
            var alpha = rgbRawBytes[rgbIndex++];
            var luminance = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
            luminances[luminanceIndex] = (byte)(((luminance * alpha) >> 8) + (255 * (255 - alpha) >> 8));
         }
      }

      private void CalculateLuminanceRGBA32(byte[] rgbRawBytes)
      {
         for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
         {
            // Calculate luminance cheaply, favoring green.
            var r = rgbRawBytes[rgbIndex++];
            var g = rgbRawBytes[rgbIndex++];
            var b = rgbRawBytes[rgbIndex++];
            var alpha = rgbRawBytes[rgbIndex++];
            var luminance = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
            luminances[luminanceIndex] = (byte)(((luminance * alpha) >> 8) + (255 * (255 - alpha) >> 8));
         }
      }

      private void CalculateLuminanceARGB32(byte[] rgbRawBytes)
      {
         for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
         {
            // Calculate luminance cheaply, favoring green.
            var alpha = rgbRawBytes[rgbIndex++];
            var r = rgbRawBytes[rgbIndex++];
            var g = rgbRawBytes[rgbIndex++];
            var b = rgbRawBytes[rgbIndex++];
            var luminance = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
            luminances[luminanceIndex] = (byte)(((luminance * alpha) >> 8) + (255 * (255 - alpha) >> 8));
         }
      }
   }
}