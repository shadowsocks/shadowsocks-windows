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

using ZXing.Common;
using ZXing.QrCode.Internal;

namespace ZXing.QrCode
{
   /// <summary>
   /// This implementation can detect and decode QR Codes in an image.
   /// <author>Sean Owen</author>
   /// </summary>
   public class QRCodeReader
   {
      private static readonly ResultPoint[] NO_POINTS = new ResultPoint[0];

      private readonly Decoder decoder = new Decoder();

      /// <summary>
      /// Gets the decoder.
      /// </summary>
      /// <returns></returns>
      protected Decoder getDecoder()
      {
         return decoder;
      }

      /// <summary>
      /// Locates and decodes a QR code in an image.
      ///
      /// <returns>a String representing the content encoded by the QR code</returns>
      /// </summary>
      public Result decode(BinaryBitmap image)
      {
         return decode(image, null);
      }

      /// <summary>
      /// Locates and decodes a barcode in some format within an image. This method also accepts
      /// hints, each possibly associated to some data, which may help the implementation decode.
      /// </summary>
      /// <param name="image">image of barcode to decode</param>
      /// <param name="hints">passed as a <see cref="IDictionary{TKey, TValue}"/> from <see cref="DecodeHintType"/>
      /// to arbitrary data. The
      /// meaning of the data depends upon the hint type. The implementation may or may not do
      /// anything with these hints.</param>
      /// <returns>
      /// String which the barcode encodes
      /// </returns>
      public Result decode(BinaryBitmap image, IDictionary<DecodeHintType, object> hints)
      {
         DecoderResult decoderResult;
         ResultPoint[] points;
         if (image == null || image.BlackMatrix == null)
         {
            // something is wrong with the image
            return null;
         }
         if (hints != null && hints.ContainsKey(DecodeHintType.PURE_BARCODE))
         {
            var bits = extractPureBits(image.BlackMatrix);
            if (bits == null)
               return null;
            decoderResult = decoder.decode(bits, hints);
            points = NO_POINTS;
         }
         else
         {
            var detectorResult = new Detector(image.BlackMatrix).detect(hints);
            if (detectorResult == null)
               return null;
            decoderResult = decoder.decode(detectorResult.Bits, hints);
            points = detectorResult.Points;
         }
         if (decoderResult == null)
            return null;

         // If the code was mirrored: swap the bottom-left and the top-right points.
         var data = decoderResult.Other as QRCodeDecoderMetaData;
         if (data != null)
         {
            data.applyMirroredCorrection(points);
         }

         var result = new Result(decoderResult.Text, decoderResult.RawBytes, points, BarcodeFormat.QR_CODE);
         var byteSegments = decoderResult.ByteSegments;
         if (byteSegments != null)
         {
            result.putMetadata(ResultMetadataType.BYTE_SEGMENTS, byteSegments);
         }
         var ecLevel = decoderResult.ECLevel;
         if (ecLevel != null)
         {
            result.putMetadata(ResultMetadataType.ERROR_CORRECTION_LEVEL, ecLevel);
         }
         if (decoderResult.StructuredAppend)
         {
            result.putMetadata(ResultMetadataType.STRUCTURED_APPEND_SEQUENCE, decoderResult.StructuredAppendSequenceNumber);
            result.putMetadata(ResultMetadataType.STRUCTURED_APPEND_PARITY, decoderResult.StructuredAppendParity);
         }
         return result;
      }

      /// <summary>
      /// Resets any internal state the implementation has after a decode, to prepare it
      /// for reuse.
      /// </summary>
      public void reset()
      {
         // do nothing
      }

      /// <summary>
      /// This method detects a code in a "pure" image -- that is, pure monochrome image
      /// which contains only an unrotated, unskewed, image of a code, with some white border
      /// around it. This is a specialized method that works exceptionally fast in this special
      /// case.
      /// 
      /// <seealso cref="ZXing.Datamatrix.DataMatrixReader.extractPureBits(BitMatrix)" />
      /// </summary>
      private static BitMatrix extractPureBits(BitMatrix image)
      {
         int[] leftTopBlack = image.getTopLeftOnBit();
         int[] rightBottomBlack = image.getBottomRightOnBit();
         if (leftTopBlack == null || rightBottomBlack == null)
         {
            return null;
         }

         float moduleSize;
         if (!QRCodeReader.moduleSize(leftTopBlack, image, out moduleSize))
            return null;

         int top = leftTopBlack[1];
         int bottom = rightBottomBlack[1];
         int left = leftTopBlack[0];
         int right = rightBottomBlack[0];

         // Sanity check!
         if (left >= right || top >= bottom)
         {
            return null;
         }

         if (bottom - top != right - left)
         {
            // Special case, where bottom-right module wasn't black so we found something else in the last row
            // Assume it's a square, so use height as the width
            right = left + (bottom - top);
         }

         int matrixWidth = (int)Math.Round((right - left + 1) / moduleSize);
         int matrixHeight = (int)Math.Round((bottom - top + 1) / moduleSize);
         if (matrixWidth <= 0 || matrixHeight <= 0)
         {
            return null;
         }
         if (matrixHeight != matrixWidth)
         {
            // Only possibly decode square regions
            return null;
         }

         // Push in the "border" by half the module width so that we start
         // sampling in the middle of the module. Just in case the image is a
         // little off, this will help recover.
         int nudge = (int)(moduleSize / 2.0f);
         top += nudge;
         left += nudge;

         // But careful that this does not sample off the edge
         int nudgedTooFarRight = left + (int)((matrixWidth - 1) * moduleSize) - (right - 1);
         if (nudgedTooFarRight > 0)
         {
            if (nudgedTooFarRight > nudge)
            {
               // Neither way fits; abort
               return null;
            }
            left -= nudgedTooFarRight;
         }
         int nudgedTooFarDown = top + (int)((matrixHeight - 1) * moduleSize) - (bottom - 1);
         if (nudgedTooFarDown > 0)
         {
            if (nudgedTooFarDown > nudge)
            {
               // Neither way fits; abort
               return null;
            }
            top -= nudgedTooFarDown;
         }

         // Now just read off the bits
         BitMatrix bits = new BitMatrix(matrixWidth, matrixHeight);
         for (int y = 0; y < matrixHeight; y++)
         {
            int iOffset = top + (int)(y * moduleSize);
            for (int x = 0; x < matrixWidth; x++)
            {
               if (image[left + (int)(x * moduleSize), iOffset])
               {
                  bits[x, y] = true;
               }
            }
         }
         return bits;
      }

      private static bool moduleSize(int[] leftTopBlack, BitMatrix image, out float msize)
      {
         int height = image.Height;
         int width = image.Width;
         int x = leftTopBlack[0];
         int y = leftTopBlack[1];
         bool inBlack = true;
         int transitions = 0;
         while (x < width && y < height)
         {
            if (inBlack != image[x, y])
            {
               if (++transitions == 5)
               {
                  break;
               }
               inBlack = !inBlack;
            }
            x++;
            y++;
         }
         if (x == width || y == height)
         {
            msize = 0.0f;
            return false;
         }
         msize = (x - leftTopBlack[0]) / 7.0f;
         return true;
      }
   }
}