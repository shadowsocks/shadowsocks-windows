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
            // CharacterSetECI currentCharacterSetECI = null;
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
                  else if (mode == Mode.ECI)
                  {
                      /*
                     // Count doesn't apply to ECI
                     int value = parseECIValue(bits);
                     currentCharacterSetECI = CharacterSetECI.getCharacterSetECIByValue(value);
                     if (currentCharacterSetECI == null)
                     {
                        return null;
                     }
                       * */
                  }
                  else
                  {
                     // First handle Hanzi mode which does not start with character count
                     if (mode == Mode.HANZI)
                     {
                        //chinese mode contains a sub set indicator right after mode indicator
                        int subset = bits.readBits(4);
                        int countHanzi = bits.readBits(mode.getCharacterCountBits(version));
                        if (subset == GB2312_SUBSET)
                        {
                           if (!decodeHanziSegment(bits, result, countHanzi))
                              return null;
                        }
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
                        else if (mode == Mode.BYTE)
                        {
                           if (!decodeByteSegment(bits, result, count, byteSegments, hints))
                              return null;
                        }
                        else if (mode == Mode.KANJI)
                        {
                           if (!decodeKanjiSegment(bits, result, count))
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

      /// <summary>
      /// See specification GBT 18284-2000
      /// </summary>
      /// <param name="bits">The bits.</param>
      /// <param name="result">The result.</param>
      /// <param name="count">The count.</param>
      /// <returns></returns>
      private static bool decodeHanziSegment(BitSource bits,
                                             StringBuilder result,
                                             int count)
      {
         // Don't crash trying to read more bits than we have available.
         if (count * 13 > bits.available())
         {
            return false;
         }

         // Each character will require 2 bytes. Read the characters as 2-byte pairs
         // and decode as GB2312 afterwards
         byte[] buffer = new byte[2 * count];
         int offset = 0;
         while (count > 0)
         {
            // Each 13 bits encodes a 2-byte character
            int twoBytes = bits.readBits(13);
            int assembledTwoBytes = ((twoBytes / 0x060) << 8) | (twoBytes % 0x060);
            if (assembledTwoBytes < 0x003BF)
            {
               // In the 0xA1A1 to 0xAAFE range
               assembledTwoBytes += 0x0A1A1;
            }
            else
            {
               // In the 0xB0A1 to 0xFAFE range
               assembledTwoBytes += 0x0A6A1;
            }
            buffer[offset] = (byte)((assembledTwoBytes >> 8) & 0xFF);
            buffer[offset + 1] = (byte)(assembledTwoBytes & 0xFF);
            offset += 2;
            count--;
         }

         try
         {
            result.Append(Encoding.GetEncoding(StringUtils.GB2312).GetString(buffer, 0, buffer.Length));
         }
#if (WINDOWS_PHONE70 || WINDOWS_PHONE71 || SILVERLIGHT4 || SILVERLIGHT5 || NETFX_CORE || MONOANDROID || MONOTOUCH)
         catch (ArgumentException)
         {
            try
            {
               // Silverlight only supports a limited number of character sets, trying fallback to UTF-8
               result.Append(Encoding.GetEncoding("UTF-8").GetString(buffer, 0, buffer.Length));
            }
            catch (Exception)
            {
               return false;
            }
         }
#endif
         catch (Exception)
         {
            return false;
         }

         return true;
      }

      private static bool decodeKanjiSegment(BitSource bits,
                                             StringBuilder result,
                                             int count)
      {
         // Don't crash trying to read more bits than we have available.
         if (count * 13 > bits.available())
         {
            return false;
         }

         // Each character will require 2 bytes. Read the characters as 2-byte pairs
         // and decode as Shift_JIS afterwards
         byte[] buffer = new byte[2 * count];
         int offset = 0;
         while (count > 0)
         {
            // Each 13 bits encodes a 2-byte character
            int twoBytes = bits.readBits(13);
            int assembledTwoBytes = ((twoBytes / 0x0C0) << 8) | (twoBytes % 0x0C0);
            if (assembledTwoBytes < 0x01F00)
            {
               // In the 0x8140 to 0x9FFC range
               assembledTwoBytes += 0x08140;
            }
            else
            {
               // In the 0xE040 to 0xEBBF range
               assembledTwoBytes += 0x0C140;
            }
            buffer[offset] = (byte)(assembledTwoBytes >> 8);
            buffer[offset + 1] = (byte)assembledTwoBytes;
            offset += 2;
            count--;
         }
         // Shift_JIS may not be supported in some environments:
         try
         {
            result.Append(Encoding.GetEncoding(StringUtils.SHIFT_JIS).GetString(buffer, 0, buffer.Length));
         }
#if (WINDOWS_PHONE70 || WINDOWS_PHONE71 || SILVERLIGHT4 || SILVERLIGHT5 || NETFX_CORE || MONOANDROID || MONOTOUCH)
         catch (ArgumentException)
         {
            try
            {
               // Silverlight only supports a limited number of character sets, trying fallback to UTF-8
               result.Append(Encoding.GetEncoding("UTF-8").GetString(buffer, 0, buffer.Length));
            }
            catch (Exception)
            {
               return false;
            }
         }
#endif
         catch (Exception)
         {
            return false;
         }
         return true;
      }

      private static bool decodeByteSegment(BitSource bits,
                                            StringBuilder result,
                                            int count,
                                            IList<byte[]> byteSegments,
                                            IDictionary<DecodeHintType, object> hints)
      {
         // Don't crash trying to read more bits than we have available.
         if (count << 3 > bits.available())
         {
            return false;
         }

         byte[] readBytes = new byte[count];
         for (int i = 0; i < count; i++)
         {
            readBytes[i] = (byte)bits.readBits(8);
         }
         String encoding;
         encoding = StringUtils.guessEncoding(readBytes, hints);
        
         try
         {
            result.Append(Encoding.GetEncoding(encoding).GetString(readBytes, 0, readBytes.Length));
         }
#if (WINDOWS_PHONE70 || WINDOWS_PHONE71 || SILVERLIGHT4 || SILVERLIGHT5 || NETFX_CORE || MONOANDROID || MONOTOUCH)
         catch (ArgumentException)
         {
            try
            {
               // Silverlight only supports a limited number of character sets, trying fallback to UTF-8
               result.Append(Encoding.GetEncoding("UTF-8").GetString(readBytes, 0, readBytes.Length));
            }
            catch (Exception)
            {
               return false;
            }
         }
#endif
#if WindowsCE
         catch (PlatformNotSupportedException)
         {
            try
            {
               // WindowsCE doesn't support all encodings. But it is device depended.
               // So we try here the some different ones
               if (encoding == "ISO-8859-1")
               {
                  result.Append(Encoding.GetEncoding(1252).GetString(readBytes, 0, readBytes.Length));
               }
               else
               {
                  result.Append(Encoding.GetEncoding("UTF-8").GetString(readBytes, 0, readBytes.Length));
               }
            }
            catch (Exception)
            {
               return false;
            }
         }
#endif
         catch (Exception)
         {
            return false;
         }
         byteSegments.Add(readBytes);

         return true;
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