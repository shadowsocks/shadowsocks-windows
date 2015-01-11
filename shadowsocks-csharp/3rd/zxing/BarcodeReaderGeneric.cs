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
using System.Collections.Generic;

using ZXing.Common;
using ZXing.QrCode;

namespace ZXing
{
   /// <summary>
   /// A smart class to decode the barcode inside a bitmap object
   /// </summary>
   public class BarcodeReaderGeneric<T> : IBarcodeReaderGeneric<T>
   {
      private static readonly Func<LuminanceSource, Binarizer> defaultCreateBinarizer =
         (luminanceSource) => new HybridBinarizer(luminanceSource);

      protected static readonly Func<byte[], int, int, RGBLuminanceSource.BitmapFormat, LuminanceSource> defaultCreateRGBLuminanceSource =
         (rawBytes, width, height, format) => new RGBLuminanceSource(rawBytes, width, height, format);

      private Reader reader;
      private readonly Func<byte[], int, int, RGBLuminanceSource.BitmapFormat, LuminanceSource> createRGBLuminanceSource;
#if !UNITY
      private readonly Func<T, LuminanceSource> createLuminanceSource;
#else
      private readonly Func<T, int, int, LuminanceSource> createLuminanceSource;
#endif
      private readonly Func<LuminanceSource, Binarizer> createBinarizer;
      private bool usePreviousState;
      private DecodingOptions options;

      /// <summary>
      /// Gets or sets the options.
      /// </summary>
      /// <value>
      /// The options.
      /// </value>
      public DecodingOptions Options
      {
         get { return options ?? (options = new DecodingOptions()); }
         set { options = value; }
      }

      /// <summary>
      /// Gets the reader which should be used to find and decode the barcode.
      /// </summary>
      /// <value>
      /// The reader.
      /// </value>
      protected Reader Reader
      {
         get
         {
            return reader ?? (reader = new QRCodeReader());
         }
      }

      /// <summary>
      /// Gets or sets a method which is called if an important point is found
      /// </summary>
      /// <value>
      /// The result point callback.
      /// </value>
      public event Action<ResultPoint> ResultPointFound
      {
         add
         {
            if (!Options.Hints.ContainsKey(DecodeHintType.NEED_RESULT_POINT_CALLBACK))
            {
               var callback = new ResultPointCallback(OnResultPointFound);
               Options.Hints[DecodeHintType.NEED_RESULT_POINT_CALLBACK] = callback;
            }
            explicitResultPointFound += value;
            usePreviousState = false;
         }
         remove
         {
            explicitResultPointFound -= value;
            if (explicitResultPointFound == null)
               Options.Hints.Remove(DecodeHintType.NEED_RESULT_POINT_CALLBACK);
            usePreviousState = false;
         }
      }

      private event Action<ResultPoint> explicitResultPointFound;

      /// <summary>
      /// event is executed if a result was found via decode
      /// </summary>
      public event Action<Result> ResultFound;

      /// <summary>
      /// Gets or sets a flag which cause a deeper look into the bitmap
      /// </summary>
      /// <value>
      ///   <c>true</c> if [try harder]; otherwise, <c>false</c>.
      /// </value>
      [Obsolete("Please use the Options.TryHarder property instead.")]
      public bool TryHarder
      {
         get { return Options.TryHarder; }
         set { Options.TryHarder = value; }
      }

      /// <summary>
      /// Image is a pure monochrome image of a barcode.
      /// </summary>
      /// <value>
      ///   <c>true</c> if monochrome image of a barcode; otherwise, <c>false</c>.
      /// </value>
      [Obsolete("Please use the Options.PureBarcode property instead.")]
      public bool PureBarcode
      {
         get { return Options.PureBarcode; }
         set { Options.PureBarcode = value; }
      }

      /// <summary>
      /// Specifies what character encoding to use when decoding, where applicable (type String)
      /// </summary>
      /// <value>
      /// The character set.
      /// </value>
      [Obsolete("Please use the Options.CharacterSet property instead.")]
      public string CharacterSet
      {
         get { return Options.CharacterSet; }
         set { Options.CharacterSet = value; }
      }

      /// <summary>
      /// Image is known to be of one of a few possible formats.
      /// Maps to a {@link java.util.List} of {@link BarcodeFormat}s.
      /// </summary>
      /// <value>
      /// The possible formats.
      /// </value>
      [Obsolete("Please use the Options.PossibleFormats property instead.")]
      public IList<BarcodeFormat> PossibleFormats
      {
         get { return Options.PossibleFormats; }
         set { Options.PossibleFormats = value; }
      }

      /// <summary>
      /// Gets or sets a value indicating whether the image should be automatically rotated.
      /// Rotation is supported for 90, 180 and 270 degrees
      /// </summary>
      /// <value>
      ///   <c>true</c> if image should be rotated; otherwise, <c>false</c>.
      /// </value>
      public bool AutoRotate { get; set; }

      /// <summary>
      /// Gets or sets a value indicating whether the image should be automatically inverted
      /// if no result is found in the original image.
      /// ATTENTION: Please be carefully because it slows down the decoding process if it is used
      /// </summary>
      /// <value>
      ///   <c>true</c> if image should be inverted; otherwise, <c>false</c>.
      /// </value>
      public bool TryInverted { get; set; }

#if !UNITY
      /// <summary>
      /// Optional: Gets or sets the function to create a luminance source object for a bitmap.
      /// If null a platform specific default LuminanceSource is used
      /// </summary>
      /// <value>
      /// The function to create a luminance source object.
      /// </value>
      protected Func<T, LuminanceSource> CreateLuminanceSource
#else
      /// <summary>
      /// Optional: Gets or sets the function to create a luminance source object for a bitmap.
      /// If null a platform specific default LuminanceSource is used
      /// </summary>
      /// <value>
      /// The function to create a luminance source object.
      /// </value>
      protected Func<T, int, int, LuminanceSource> CreateLuminanceSource
#endif
      {
         get
         {
            return createLuminanceSource;
         }
      }

      /// <summary>
      /// Optional: Gets or sets the function to create a binarizer object for a luminance source.
      /// If null then HybridBinarizer is used
      /// </summary>
      /// <value>
      /// The function to create a binarizer object.
      /// </value>
      protected Func<LuminanceSource, Binarizer> CreateBinarizer
      {
         get
         {
            return createBinarizer ?? defaultCreateBinarizer;
         }
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeReaderGeneric{T}"/> class.
      /// </summary>
      public BarcodeReaderGeneric()
          : this(new QRCodeReader(), null, defaultCreateBinarizer)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeReaderGeneric{T}"/> class.
      /// </summary>
      /// <param name="reader">Sets the reader which should be used to find and decode the barcode.
      /// If null then MultiFormatReader is used</param>
      /// <param name="createLuminanceSource">Sets the function to create a luminance source object for a bitmap.
      /// If null, an exception is thrown when Decode is called</param>
      /// <param name="createBinarizer">Sets the function to create a binarizer object for a luminance source.
      /// If null then HybridBinarizer is used</param>
      public BarcodeReaderGeneric(Reader reader,
#if !UNITY
         Func<T, LuminanceSource> createLuminanceSource,
#else
         Func<T, int, int, LuminanceSource> createLuminanceSource,
#endif
         Func<LuminanceSource, Binarizer> createBinarizer
         )
         : this(reader, createLuminanceSource, createBinarizer, null)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeReaderGeneric{T}"/> class.
      /// </summary>
      /// <param name="reader">Sets the reader which should be used to find and decode the barcode.
      /// If null then MultiFormatReader is used</param>
      /// <param name="createLuminanceSource">Sets the function to create a luminance source object for a bitmap.
      /// If null, an exception is thrown when Decode is called</param>
      /// <param name="createBinarizer">Sets the function to create a binarizer object for a luminance source.
      /// If null then HybridBinarizer is used</param>
      /// <param name="createRGBLuminanceSource">Sets the function to create a luminance source object for a rgb array.
      /// If null the RGBLuminanceSource is used. The handler is only called when Decode with a byte[] array is called.</param>
      public BarcodeReaderGeneric(Reader reader,
#if !UNITY
         Func<T, LuminanceSource> createLuminanceSource,
#else
         Func<T, int, int, LuminanceSource> createLuminanceSource,
#endif
         Func<LuminanceSource, Binarizer> createBinarizer,
         Func<byte[], int, int, RGBLuminanceSource.BitmapFormat, LuminanceSource> createRGBLuminanceSource
         )
      {
          this.reader = reader ?? new QRCodeReader();
         this.createLuminanceSource = createLuminanceSource;
         this.createBinarizer = createBinarizer ?? defaultCreateBinarizer;
         this.createRGBLuminanceSource = createRGBLuminanceSource ?? defaultCreateRGBLuminanceSource;
         Options.ValueChanged += (o, args) => usePreviousState = false;
         usePreviousState = false;
      }

#if !PORTABLE
#if !UNITY
      /// <summary>
      /// Decodes the specified barcode bitmap.
      /// </summary>
      /// <param name="barcodeBitmap">The barcode bitmap.</param>
      /// <returns>the result data or null</returns>
      public Result Decode(T barcodeBitmap)
#else
      /// <summary>
      /// Decodes the specified barcode bitmap.
      /// </summary>
      /// <param name="rawRGB">raw bytes of the image in RGB order</param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      /// <returns>
      /// the result data or null
      /// </returns>
      public Result Decode(T rawRGB, int width, int height)
#endif
      {
         if (CreateLuminanceSource == null)
         {
            throw new InvalidOperationException("You have to declare a luminance source delegate.");
         }

#if !UNITY
         if (barcodeBitmap == null)
            throw new ArgumentNullException("barcodeBitmap");
#else
         if (rawRGB == null)
            throw new ArgumentNullException("rawRGB");
#endif

#if !UNITY
         var luminanceSource = CreateLuminanceSource(barcodeBitmap);
#else
         var luminanceSource = CreateLuminanceSource(rawRGB, width, height);
#endif

         return Decode(luminanceSource);
      }
#endif

      /// <summary>
      /// Tries to decode a barcode within an image which is given by a luminance source.
      /// That method gives a chance to prepare a luminance source completely before calling
      /// the time consuming decoding method. On the other hand there is a chance to create
      /// a luminance source which is independent from external resources (like Bitmap objects)
      /// and the decoding call can be made in a background thread.
      /// </summary>
      /// <param name="luminanceSource">The luminance source.</param>
      /// <returns></returns>
      virtual public Result Decode(LuminanceSource luminanceSource)
      {
         var result = default(Result);
         var binarizer = CreateBinarizer(luminanceSource);
         var binaryBitmap = new BinaryBitmap(binarizer);
         var multiformatReader = Reader as QRCodeReader;
         var rotationCount = 0;
         var rotationMaxCount = 1;

         if (AutoRotate)
         {
            Options.Hints[DecodeHintType.TRY_HARDER_WITHOUT_ROTATION] = true;
            rotationMaxCount = 4;
         }
         else
         {
            if (Options.Hints.ContainsKey(DecodeHintType.TRY_HARDER_WITHOUT_ROTATION))
               Options.Hints.Remove(DecodeHintType.TRY_HARDER_WITHOUT_ROTATION);
         }

         for (; rotationCount < rotationMaxCount; rotationCount++)
         {
            result = Reader.decode(binaryBitmap, Options.Hints);
            usePreviousState = true;


            if (result != null ||
                !luminanceSource.RotateSupported ||
                !AutoRotate)
               break;

            binaryBitmap = new BinaryBitmap(CreateBinarizer(luminanceSource.rotateCounterClockwise()));
         }

         if (result != null)
         {
            if (result.ResultMetadata == null)
            {
               result.putMetadata(ResultMetadataType.ORIENTATION, rotationCount * 90);
            }
            else if (!result.ResultMetadata.ContainsKey(ResultMetadataType.ORIENTATION))
            {
               result.ResultMetadata[ResultMetadataType.ORIENTATION] = rotationCount * 90;
            }
            else
            {
               // perhaps the core decoder rotates the image already (can happen if TryHarder is specified)
               result.ResultMetadata[ResultMetadataType.ORIENTATION] = ((int)(result.ResultMetadata[ResultMetadataType.ORIENTATION]) + rotationCount * 90) % 360;
            }

            OnResultFound(result);
         }

         return result;
      }


      protected void OnResultsFound(IEnumerable<Result> results)
      {
         if (ResultFound != null)
         {
            foreach (var result in results)
            {
               ResultFound(result);
            }
         }
      }

      protected void OnResultFound(Result result)
      {
         if (ResultFound != null)
         {
            ResultFound(result);
         }
      }

      protected void OnResultPointFound(ResultPoint resultPoint)
      {
         if (explicitResultPointFound != null)
         {
            explicitResultPointFound(resultPoint);
         }
      }

      /// <summary>
      /// Decodes the specified barcode bitmap.
      /// </summary>
      /// <param name="rawRGB">The image as byte[] array.</param>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      /// <param name="format">The format.</param>
      /// <returns>
      /// the result data or null
      /// </returns>
      public Result Decode(byte[] rawRGB, int width, int height, RGBLuminanceSource.BitmapFormat format)
      {
         if (rawRGB == null)
            throw new ArgumentNullException("rawRGB");
         
         var luminanceSource = createRGBLuminanceSource(rawRGB, width, height, format);

         return Decode(luminanceSource);
      }

   }
}
