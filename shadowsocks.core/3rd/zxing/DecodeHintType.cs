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

using System;
using System.Collections.Generic;

namespace ZXing
{
   /// <summary>
   /// Encapsulates a type of hint that a caller may pass to a barcode reader to help it
   /// more quickly or accurately decode it. It is up to implementations to decide what,
   /// if anything, to do with the information that is supplied.
   /// <seealso cref="Reader.decode(BinaryBitmap, IDictionary{DecodeHintType, object})" />
   /// </summary>
   /// <author>Sean Owen</author>
   /// <author>dswitkin@google.com (Daniel Switkin)</author>
   public enum DecodeHintType
   {
      /// <summary>
      /// Unspecified, application-specific hint. Maps to an unspecified <see cref="System.Object" />.
      /// </summary>
      OTHER,

      /// <summary>
      /// Image is a pure monochrome image of a barcode. Doesn't matter what it maps to;
      /// use <see cref="bool" /> = true.
      /// </summary>
      PURE_BARCODE,

      /// <summary>
      /// Image is known to be of one of a few possible formats.
      /// Maps to a <see cref="System.Collections.ICollection" /> of <see cref="BarcodeFormat" />s.
      /// </summary>
      POSSIBLE_FORMATS,

      /// <summary>
      /// Spend more time to try to find a barcode; optimize for accuracy, not speed.
      /// Doesn't matter what it maps to; use <see cref="bool" /> = true.
      /// </summary>
      TRY_HARDER,

      /// <summary>
      /// Specifies what character encoding to use when decoding, where applicable (type String)
      /// </summary>
      CHARACTER_SET,

      /// <summary>
      /// Allowed lengths of encoded data -- reject anything else. Maps to an int[].
      /// </summary>
      ALLOWED_LENGTHS,

      /// <summary>
      /// Assume Code 39 codes employ a check digit. Maps to <see cref="bool" />.
      /// </summary>
      ASSUME_CODE_39_CHECK_DIGIT,

      /// <summary>
      /// The caller needs to be notified via callback when a possible <see cref="ResultPoint" />
      /// is found. Maps to a <see cref="ResultPointCallback" />.
      /// </summary>
      NEED_RESULT_POINT_CALLBACK,

      /// <summary>
      /// Assume MSI codes employ a check digit. Maps to <see cref="bool" />.
      /// </summary>
      ASSUME_MSI_CHECK_DIGIT,

      /// <summary>
      /// if Code39 could be detected try to use extended mode for full ASCII character set
      /// Maps to <see cref="bool" />.
      /// </summary>
      USE_CODE_39_EXTENDED_MODE,

      /// <summary>
      /// Don't fail if a Code39 is detected but can't be decoded in extended mode.
      /// Return the raw Code39 result instead. Maps to <see cref="bool" />.
      /// </summary>
      RELAXED_CODE_39_EXTENDED_MODE,

      /// <summary>
      /// 1D readers supporting rotation with TRY_HARDER enabled.
      /// But BarcodeReader class can do auto-rotating for 1D and 2D codes.
      /// Enabling that option prevents 1D readers doing double rotation.
      /// BarcodeReader enables that option automatically if "global" auto-rotation is enabled.
      /// Maps to <see cref="bool" />.
      /// </summary>
      TRY_HARDER_WITHOUT_ROTATION,

      /// <summary>
      /// Assume the barcode is being processed as a GS1 barcode, and modify behavior as needed.
      /// For example this affects FNC1 handling for Code 128 (aka GS1-128). Doesn't matter what it maps to;
      /// use <see cref="bool" />.
      /// </summary>
      ASSUME_GS1,

      /// <summary>
      /// If true, return the start and end digits in a Codabar barcode instead of stripping them. They
      /// are alpha, whereas the rest are numeric. By default, they are stripped, but this causes them
      /// to not be. Doesn't matter what it maps to; use <see cref="bool" />.
      /// </summary>
      RETURN_CODABAR_START_END,

      /// <summary>
      /// Allowed extension lengths for EAN or UPC barcodes. Other formats will ignore this.
      /// Maps to an <see cref="Array.int" /> of the allowed extension lengths, for example [2], [5], or [2, 5].
      /// If it is optional to have an extension, do not set this hint. If this is set,
      /// and a UPC or EAN barcode is found but an extension is not, then no result will be returned
      /// at all.
      /// </summary>
      ALLOWED_EAN_EXTENSIONS
   }
}