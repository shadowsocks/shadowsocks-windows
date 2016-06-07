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

namespace ZXing.QrCode.Internal
{

   /// <summary> <p>Encapsulates a QR Code's format information, including the data mask used and
   /// error correction level.</p>
   /// 
   /// </summary>
   /// <author>  Sean Owen
   /// </author>
   /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source 
   /// </author>
   /// <seealso cref="DataMask">
   /// </seealso>
   /// <seealso cref="ErrorCorrectionLevel">
   /// </seealso>
   sealed class FormatInformation
   {
      private const int FORMAT_INFO_MASK_QR = 0x5412;

      /// <summary> See ISO 18004:2006, Annex C, Table C.1</summary>
      private static readonly int[][] FORMAT_INFO_DECODE_LOOKUP = new int[][]
                                                                     {
                                                                        new [] { 0x5412, 0x00 },
                                                                        new [] { 0x5125, 0x01 },
                                                                        new [] { 0x5E7C, 0x02 },
                                                                        new [] { 0x5B4B, 0x03 },
                                                                        new [] { 0x45F9, 0x04 },
                                                                        new [] { 0x40CE, 0x05 },
                                                                        new [] { 0x4F97, 0x06 },
                                                                        new [] { 0x4AA0, 0x07 },
                                                                        new [] { 0x77C4, 0x08 },
                                                                        new [] { 0x72F3, 0x09 },
                                                                        new [] { 0x7DAA, 0x0A },
                                                                        new [] { 0x789D, 0x0B },
                                                                        new [] { 0x662F, 0x0C },
                                                                        new [] { 0x6318, 0x0D },
                                                                        new [] { 0x6C41, 0x0E },
                                                                        new [] { 0x6976, 0x0F },
                                                                        new [] { 0x1689, 0x10 },
                                                                        new [] { 0x13BE, 0x11 },
                                                                        new [] { 0x1CE7, 0x12 },
                                                                        new [] { 0x19D0, 0x13 },
                                                                        new [] { 0x0762, 0x14 },
                                                                        new [] { 0x0255, 0x15 },
                                                                        new [] { 0x0D0C, 0x16 },
                                                                        new [] { 0x083B, 0x17 },
                                                                        new [] { 0x355F, 0x18 },
                                                                        new [] { 0x3068, 0x19 },
                                                                        new [] { 0x3F31, 0x1A }, 
                                                                        new [] { 0x3A06, 0x1B },
                                                                        new [] { 0x24B4, 0x1C },
                                                                        new [] { 0x2183, 0x1D },
                                                                        new [] { 0x2EDA, 0x1E }, 
                                                                        new [] { 0x2BED, 0x1F }
                                                                     };

      /// <summary> Offset i holds the number of 1 bits in the binary representation of i</summary>
      private static readonly int[] BITS_SET_IN_HALF_BYTE = new [] 
         { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 };

      private readonly ErrorCorrectionLevel errorCorrectionLevel;
      private readonly byte dataMask;

      private FormatInformation(int formatInfo)
      {
         // Bits 3,4
         errorCorrectionLevel = ErrorCorrectionLevel.forBits((formatInfo >> 3) & 0x03);
         // Bottom 3 bits
         dataMask = (byte)(formatInfo & 0x07);
      }

      internal static int numBitsDiffering(int a, int b)
      {
         a ^= b; // a now has a 1 bit exactly where its bit differs with b's
         // Count bits set quickly with a series of lookups:
         return BITS_SET_IN_HALF_BYTE[a & 0x0F] +
            BITS_SET_IN_HALF_BYTE[(((int)((uint)a >> 4)) & 0x0F)] +
            BITS_SET_IN_HALF_BYTE[(((int)((uint)a >> 8)) & 0x0F)] +
            BITS_SET_IN_HALF_BYTE[(((int)((uint)a >> 12)) & 0x0F)] +
            BITS_SET_IN_HALF_BYTE[(((int)((uint)a >> 16)) & 0x0F)] +
            BITS_SET_IN_HALF_BYTE[(((int)((uint)a >> 20)) & 0x0F)] +
            BITS_SET_IN_HALF_BYTE[(((int)((uint)a >> 24)) & 0x0F)] +
            BITS_SET_IN_HALF_BYTE[(((int)((uint)a >> 28)) & 0x0F)];
      }

      /// <summary>
      /// Decodes the format information.
      /// </summary>
      /// <param name="maskedFormatInfo1">format info indicator, with mask still applied</param>
      /// <param name="maskedFormatInfo2">The masked format info2.</param>
      /// <returns>
      /// information about the format it specifies, or <code>null</code>
      /// if doesn't seem to match any known pattern
      /// </returns>
      internal static FormatInformation decodeFormatInformation(int maskedFormatInfo1, int maskedFormatInfo2)
      {
         FormatInformation formatInfo = doDecodeFormatInformation(maskedFormatInfo1, maskedFormatInfo2);
         if (formatInfo != null)
         {
            return formatInfo;
         }
         // Should return null, but, some QR codes apparently
         // do not mask this info. Try again by actually masking the pattern
         // first
         return doDecodeFormatInformation(maskedFormatInfo1 ^ FORMAT_INFO_MASK_QR,
                                          maskedFormatInfo2 ^ FORMAT_INFO_MASK_QR);
      }

      private static FormatInformation doDecodeFormatInformation(int maskedFormatInfo1, int maskedFormatInfo2)
      {
         // Find the int in FORMAT_INFO_DECODE_LOOKUP with fewest bits differing
         int bestDifference = Int32.MaxValue;
         int bestFormatInfo = 0;
         foreach (var decodeInfo in FORMAT_INFO_DECODE_LOOKUP)
         {
            int targetInfo = decodeInfo[0];
            if (targetInfo == maskedFormatInfo1 || targetInfo == maskedFormatInfo2)
            {
               // Found an exact match
               return new FormatInformation(decodeInfo[1]);
            }
            int bitsDifference = numBitsDiffering(maskedFormatInfo1, targetInfo);
            if (bitsDifference < bestDifference)
            {
               bestFormatInfo = decodeInfo[1];
               bestDifference = bitsDifference;
            }
            if (maskedFormatInfo1 != maskedFormatInfo2)
            {
               // also try the other option
               bitsDifference = numBitsDiffering(maskedFormatInfo2, targetInfo);
               if (bitsDifference < bestDifference)
               {
                  bestFormatInfo = decodeInfo[1];
                  bestDifference = bitsDifference;
               }
            }
         }
         // Hamming distance of the 32 masked codes is 7, by construction, so <= 3 bits
         // differing means we found a match
         if (bestDifference <= 3)
         {
            return new FormatInformation(bestFormatInfo);
         }
         return null;
      }

      internal ErrorCorrectionLevel ErrorCorrectionLevel
      {
         get
         {
            return errorCorrectionLevel;
         }
      }

      internal byte DataMask
      {
         get
         {
            return dataMask;
         }
      }

      public override int GetHashCode()
      {
         return (errorCorrectionLevel.ordinal() << 3) | dataMask;
      }

      public override bool Equals(Object o)
      {
         if (!(o is FormatInformation))
         {
            return false;
         }
         var other = (FormatInformation)o;
         return errorCorrectionLevel == other.errorCorrectionLevel && dataMask == other.dataMask;
      }
   }
}