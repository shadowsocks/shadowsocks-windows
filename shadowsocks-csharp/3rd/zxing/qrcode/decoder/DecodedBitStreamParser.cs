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
using System.Text;

using ZXing.Common;

namespace ZXing.QrCode.Internal
{
   /// <summary> <p>QR Codes can encode text as bits in one of several modes, and can use multiple modes
   /// in one QR Code. This class decodes the bits back into text.</p>
   /// 
   /// <p>See ISO 18004:2006, 6.4.3 - 6.4.7</p>
   /// <author>Sean Owen</author>
   /// </summary>
   internal static class DecodedBitStreamParser
   {
      /// <summary>
      /// See ISO 18004:2006, 6.4.4 Table 5
      /// </summary>
      private static readonly char[] ALPHANUMERIC_CHARS = {
                                                             '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B',
                                                             'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N',
                                                             'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
                                                             ' ', '$', '%', '*', '+', '-', '.', '/', ':'
                                                          };
      private const int GB2312_SUBSET = 1;

      internal static DecoderResult decode(byte[] bytes,
                                           Version version,
                                           ErrorCorrectionLevel ecLevel,
                                           IDictionary<DecodeHintType, object> hints)
      {
         var bits = new BitSource(bytes);
         var result = new StringBuilder(50);
         var byteSegments = new List<byte[]>(1);
         var symbolSequence = -1;
         var parityData = -1;

         try
         {
            //CharacterSetECI currentCharacterSetECI = null;
            bool fc1InEffect = false;
            Mode mode;
            do
            {
               // While still another segment to read...
               if (bits.available() < 4)
               {
                  // OK, assume we're done. Really, a TERMINATOR mode should have been recorded here
                  mode = Mode.TERMINATOR;
               }
               else
               {
                  try
                  {
                     mode = Mode.forBits(bits.readBits(4)); // mode is encoded by 4 bits
                  }
                  catch (ArgumentException)
                  {
                     return null;
                  }
               }
               if (mode != Mode.TERMINATOR)
               {
                  if (mode == Mode.FNC1_FIRST_POSITION || mode == Mode.FNC1_SECOND_POSITION)
                  {
                     // We do little with FNC1 except alter the parsed result a bit according to the spec
                     fc1InEffect = true;
                  }
                  else if (mode == Mode.STRUCTURED_APPEND)
                  {
                     if (bits.available() < 16)
                     {
                        return null;
                     }
                     // not really supported; but sequence number and parity is added later to the result metadata
                     // Read next 8 bits (symbol sequence #) and 8 bits (parity data), then continue
                     symbolSequence = bits.readBits(8);
                     parityData = bits.readBits(8);
                  }
                  else
                  {
                     // First handle Hanzi mode which does not start with character count
                     if (mode == Mode.HANZI)
                     {
                        //chinese mode contains a sub set indicator right after mode indicator
                        //int subset = bits.readBits(4);
                        //int countHanzi = bits.readBits(mode.getCharacterCountBits(version));
                     }
                     else
                     {
                        // "Normal" QR code modes:
                        // How many characters will follow, encoded in this mode?
                        int count = bits.readBits(mode.getCharacterCountBits(version));
                        if (mode == Mode.NUMERIC)
                        {
                           if (!decodeNumericSegment(bits, result, count))
                              return null;
                        }
                        else if (mode == Mode.ALPHANUMERIC)
                        {
                           if (!decodeAlphanumericSegment(bits, result, count, fc1InEffect))
                              return null;
                        }
                        else
                        {
                           return null;
                        }
                     }
                  }
               }
            } while (mode != Mode.TERMINATOR);
         }
         catch (ArgumentException)
         {
            // from readBits() calls
            return null;
         }

#if WindowsCE
         var resultString = result.ToString().Replace("\n", "\r\n");
#else
         var resultString = result.ToString().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
#endif
         return new DecoderResult(bytes,
                                  resultString,
                                  byteSegments.Count == 0 ? null : byteSegments,
                                  ecLevel == null ? null : ecLevel.ToString(),
                                  symbolSequence, parityData);
      }




      private static char toAlphaNumericChar(int value)
      {
         if (value >= ALPHANUMERIC_CHARS.Length)
         {
            //throw FormatException.Instance;
         }
         return ALPHANUMERIC_CHARS[value];
      }

      private static bool decodeAlphanumericSegment(BitSource bits,
                                                    StringBuilder result,
                                                    int count,
                                                    bool fc1InEffect)
      {
         // Read two characters at a time
         int start = result.Length;
         while (count > 1)
         {
            if (bits.available() < 11)
            {
               return false;
            }
            int nextTwoCharsBits = bits.readBits(11);
            result.Append(toAlphaNumericChar(nextTwoCharsBits / 45));
            result.Append(toAlphaNumericChar(nextTwoCharsBits % 45));
            count -= 2;
         }
         if (count == 1)
         {
            // special case: one character left
            if (bits.available() < 6)
            {
               return false;
            }
            result.Append(toAlphaNumericChar(bits.readBits(6)));
         }

         // See section 6.4.8.1, 6.4.8.2
         if (fc1InEffect)
         {
            // We need to massage the result a bit if in an FNC1 mode:
            for (int i = start; i < result.Length; i++)
            {
               if (result[i] == '%')
               {
                  if (i < result.Length - 1 && result[i + 1] == '%')
                  {
                     // %% is rendered as %
                     result.Remove(i + 1, 1);
                  }
                  else
                  {
                     // In alpha mode, % should be converted to FNC1 separator 0x1D
                     result.Remove(i, 1);
                     result.Insert(i, new[] { (char)0x1D });
                  }
               }
            }
         }

         return true;
      }

      private static bool decodeNumericSegment(BitSource bits,
                                               StringBuilder result,
                                               int count)
      {
         // Read three digits at a time
         while (count >= 3)
         {
            // Each 10 bits encodes three digits
            if (bits.available() < 10)
            {
               return false;
            }
            int threeDigitsBits = bits.readBits(10);
            if (threeDigitsBits >= 1000)
            {
               return false;
            }
            result.Append(toAlphaNumericChar(threeDigitsBits / 100));
            result.Append(toAlphaNumericChar((threeDigitsBits / 10) % 10));
            result.Append(toAlphaNumericChar(threeDigitsBits % 10));

            count -= 3;
         }
         if (count == 2)
         {
            // Two digits left over to read, encoded in 7 bits
            if (bits.available() < 7)
            {
               return false;
            }
            int twoDigitsBits = bits.readBits(7);
            if (twoDigitsBits >= 100)
            {
               return false;
            }
            result.Append(toAlphaNumericChar(twoDigitsBits / 10));
            result.Append(toAlphaNumericChar(twoDigitsBits % 10));
         }
         else if (count == 1)
         {
            // One digit left over to read
            if (bits.available() < 4)
            {
               return false;
            }
            int digitBits = bits.readBits(4);
            if (digitBits >= 10)
            {
               return false;
            }
            result.Append(toAlphaNumericChar(digitBits));
         }

         return true;
      }

      private static int parseECIValue(BitSource bits)
      {
         int firstByte = bits.readBits(8);
         if ((firstByte & 0x80) == 0)
         {
            // just one byte
            return firstByte & 0x7F;
         }
         if ((firstByte & 0xC0) == 0x80)
         {
            // two bytes
            int secondByte = bits.readBits(8);
            return ((firstByte & 0x3F) << 8) | secondByte;
         }
         if ((firstByte & 0xE0) == 0xC0)
         {
            // three bytes
            int secondThirdBytes = bits.readBits(16);
            return ((firstByte & 0x1F) << 16) | secondThirdBytes;
         }
         throw new ArgumentException("Bad ECI bits starting with byte " + firstByte);
      }
   }
}