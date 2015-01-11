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
#if !PORTABLE
#if !(SILVERLIGHT || NETFX_CORE)
#if !UNITY
using System.Drawing;
using ZXing.QrCode;
#else
using UnityEngine;
#endif
#elif NETFX_CORE
using Windows.UI.Xaml.Media.Imaging;
#else
using System.Windows.Media.Imaging;
#endif
#endif
#if MONOANDROID
using Android.Graphics;
#endif

namespace ZXing
{
   /// <summary>
   /// A smart class to decode the barcode inside a bitmap object
   /// </summary>
#if MONOTOUCH
   public class BarcodeReader : BarcodeReaderGeneric<MonoTouch.UIKit.UIImage>, IBarcodeReader, IMultipleBarcodeReader
   {
      private static readonly Func<MonoTouch.UIKit.UIImage, LuminanceSource> defaultCreateLuminanceSource = 
         (img) => new RGBLuminanceSource(img);
#else
#if !PORTABLE
#if !(SILVERLIGHT || NETFX_CORE)
#if !UNITY
   public class BarcodeReader : BarcodeReaderGeneric<Bitmap>, IBarcodeReader
   {
      private static readonly Func<Bitmap, LuminanceSource> defaultCreateLuminanceSource =
         (bitmap) => new BitmapLuminanceSource(bitmap);
#else
   public class BarcodeReader : BarcodeReaderGeneric<Color32[]>, IBarcodeReader, IMultipleBarcodeReader
   {
      private static readonly Func<Color32[], int, int, LuminanceSource> defaultCreateLuminanceSource =
         (rawColor32, width, height) => new Color32LuminanceSource(rawColor32, width, height);
#endif
#else
   public class BarcodeReader : BarcodeReaderGeneric<WriteableBitmap>, IBarcodeReader, IMultipleBarcodeReader
   {
      private static readonly Func<WriteableBitmap, LuminanceSource> defaultCreateLuminanceSource =
         (bitmap) => new BitmapLuminanceSource(bitmap);
#endif
#else
   public class BarcodeReader : BarcodeReaderGeneric<byte[]>, IBarcodeReader, IMultipleBarcodeReader
   {
      private static readonly Func<byte[], LuminanceSource> defaultCreateLuminanceSource =
         (data) => null;
#endif
#endif
      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeReader"/> class.
      /// </summary>
      public BarcodeReader()
          : this(new QRCodeReader(), defaultCreateLuminanceSource, null)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeReader"/> class.
      /// </summary>
      /// <param name="reader">Sets the reader which should be used to find and decode the barcode.
      /// If null then MultiFormatReader is used</param>
      /// <param name="createLuminanceSource">Sets the function to create a luminance source object for a bitmap.
      /// If null, an exception is thrown when Decode is called</param>
      /// <param name="createBinarizer">Sets the function to create a binarizer object for a luminance source.
      /// If null then HybridBinarizer is used</param>
      public BarcodeReader(Reader reader,
#if MONOTOUCH
         Func<MonoTouch.UIKit.UIImage, LuminanceSource> createLuminanceSource,
#elif MONOANDROID
         Func<Android.Graphics.Bitmap, LuminanceSource> createLuminanceSource,
#else
#if !(SILVERLIGHT || NETFX_CORE)
#if !UNITY
#if !PORTABLE
         Func<Bitmap, LuminanceSource> createLuminanceSource,
#else
         Func<byte[], LuminanceSource> createLuminanceSource,
#endif
#else
         Func<Color32[], int, int, LuminanceSource> createLuminanceSource,
#endif
#else
         Func<WriteableBitmap, LuminanceSource> createLuminanceSource,
#endif
#endif
         Func<LuminanceSource, Binarizer> createBinarizer
         )
         : base(reader, createLuminanceSource ?? defaultCreateLuminanceSource, createBinarizer)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeReader"/> class.
      /// </summary>
      /// <param name="reader">Sets the reader which should be used to find and decode the barcode.
      /// If null then MultiFormatReader is used</param>
      /// <param name="createLuminanceSource">Sets the function to create a luminance source object for a bitmap.
      /// If null, an exception is thrown when Decode is called</param>
      /// <param name="createBinarizer">Sets the function to create a binarizer object for a luminance source.
      /// If null then HybridBinarizer is used</param>
      public BarcodeReader(Reader reader,
#if MONOTOUCH
         Func<MonoTouch.UIKit.UIImage, LuminanceSource> createLuminanceSource,
#elif MONOANDROID
         Func<Android.Graphics.Bitmap, LuminanceSource> createLuminanceSource,
#else
#if !(SILVERLIGHT || NETFX_CORE)
#if !UNITY
#if !PORTABLE
         Func<Bitmap, LuminanceSource> createLuminanceSource,
#else
         Func<byte[], LuminanceSource> createLuminanceSource,
#endif
#else
         Func<Color32[], int, int, LuminanceSource> createLuminanceSource,
#endif
#else
         Func<WriteableBitmap, LuminanceSource> createLuminanceSource,
#endif
#endif
         Func<LuminanceSource, Binarizer> createBinarizer,
         Func<byte[], int, int, RGBLuminanceSource.BitmapFormat, LuminanceSource> createRGBLuminanceSource
         )
         : base(reader, createLuminanceSource ?? defaultCreateLuminanceSource, createBinarizer, createRGBLuminanceSource)
      {
      }
   }
}
