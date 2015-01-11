/*
 * Copyright 2008 ZXing authors
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

namespace ZXing
{
   /// <summary>
   /// Represents some type of metadata about the result of the decoding that the decoder
   /// wishes to communicate back to the caller.
   /// </summary>
   /// <author>Sean Owen</author>
   public enum ResultMetadataType
   {
      /// <summary>
      /// Unspecified, application-specific metadata. Maps to an unspecified {@link Object}.
      /// </summary>
      OTHER,

      /// <summary>
      /// Denotes the likely approximate orientation of the barcode in the image. This value
      /// is given as degrees rotated clockwise from the normal, upright orientation.
      /// For example a 1D barcode which was found by reading top-to-bottom would be
      /// said to have orientation "90". This key maps to an {@link Integer} whose
      /// value is in the range [0,360).
      /// </summary>
      ORIENTATION,

      /// <summary>
      /// <p>2D barcode formats typically encode text, but allow for a sort of 'byte mode'
      /// which is sometimes used to encode binary data. While {@link Result} makes available
      /// the complete raw bytes in the barcode for these formats, it does not offer the bytes
      /// from the byte segments alone.</p>
      /// <p>This maps to a {@link java.util.List} of byte arrays corresponding to the
      /// raw bytes in the byte segments in the barcode, in order.</p>
      /// </summary>
      BYTE_SEGMENTS,

      /// <summary>
      /// Error correction level used, if applicable. The value type depends on the
      /// format, but is typically a String.
      /// </summary>
      ERROR_CORRECTION_LEVEL,

      /// <summary>
      /// For some periodicals, indicates the issue number as an {@link Integer}.
      /// </summary>
      ISSUE_NUMBER,

      /// <summary>
      /// For some products, indicates the suggested retail price in the barcode as a
      /// formatted {@link String}.
      /// </summary>
      SUGGESTED_PRICE,

      /// <summary>
      /// For some products, the possible country of manufacture as a {@link String} denoting the
      /// ISO country code. Some map to multiple possible countries, like "US/CA".
      /// </summary>
      POSSIBLE_COUNTRY,

      /// <summary>
      /// For some products, the extension text
      /// </summary>
      UPC_EAN_EXTENSION,

      /// <summary>
      /// If the code format supports structured append and
      /// the current scanned code is part of one then the
      /// sequence number is given with it.
      /// </summary>
      STRUCTURED_APPEND_SEQUENCE,

      /// <summary>
      /// If the code format supports structured append and
      /// the current scanned code is part of one then the
      /// parity is given with it.
      /// </summary>
      STRUCTURED_APPEND_PARITY,

      /// <summary>
      /// PDF417-specific metadata
      /// </summary>
      PDF417_EXTRA_METADATA,

      /// <summary>
      /// Aztec-specific metadata
      /// </summary>
      AZTEC_EXTRA_METADATA
   }
}