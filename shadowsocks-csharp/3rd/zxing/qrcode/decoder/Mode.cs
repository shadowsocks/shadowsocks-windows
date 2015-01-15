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
   /// <summary>
   /// <p>See ISO 18004:2006, 6.4.1, Tables 2 and 3. This enum encapsulates the various modes in which
   /// data can be encoded to bits in the QR code standard.</p>
   /// </summary>
   /// <author>Sean Owen</author>
   public sealed class Mode
   {
      /// <summary>
      /// Gets the name.
      /// </summary>
      public String Name
      {
         get
         {
            return name;
         }
      }

      // No, we can't use an enum here. J2ME doesn't support it.

      /// <summary>
      /// 
      /// </summary>
      public static readonly Mode TERMINATOR = new Mode(new int[] { 0, 0, 0 }, 0x00, "TERMINATOR"); // Not really a mode...
      /// <summary>
      /// 
      /// </summary>
      public static readonly Mode NUMERIC = new Mode(new int[] { 10, 12, 14 }, 0x01, "NUMERIC");
      /// <summary>
      /// 
      /// </summary>
      public static readonly Mode ALPHANUMERIC = new Mode(new int[] { 9, 11, 13 }, 0x02, "ALPHANUMERIC");
      /// <summary>
      /// 
      /// </summary>
      public static readonly Mode STRUCTURED_APPEND = new Mode(new int[] { 0, 0, 0 }, 0x03, "STRUCTURED_APPEND"); // Not supported
      /// <summary>
      /// 
      /// </summary>
      public static readonly Mode BYTE = new Mode(new int[] { 8, 16, 16 }, 0x04, "BYTE");
      /// <summary>
      /// 
      /// </summary>
      public static readonly Mode ECI = new Mode(null, 0x07, "ECI"); // character counts don't apply
      /// <summary>
      /// 
      /// </summary>
      public static readonly Mode KANJI = new Mode(new int[] { 8, 10, 12 }, 0x08, "KANJI");
      /// <summary>
      /// 
      /// </summary>
      public static readonly Mode FNC1_FIRST_POSITION = new Mode(null, 0x05, "FNC1_FIRST_POSITION");
      /// <summary>
      /// 
      /// </summary>
      public static readonly Mode FNC1_SECOND_POSITION = new Mode(null, 0x09, "FNC1_SECOND_POSITION");
      /// <summary>See GBT 18284-2000; "Hanzi" is a transliteration of this mode name.</summary>
      public static readonly Mode HANZI = new Mode(new int[] { 8, 10, 12 }, 0x0D, "HANZI");

      private readonly int[] characterCountBitsForVersions;
      private readonly int bits;
      private readonly String name;

      private Mode(int[] characterCountBitsForVersions, int bits, System.String name)
      {
         this.characterCountBitsForVersions = characterCountBitsForVersions;
         this.bits = bits;
         this.name = name;
      }

      /// <summary>
      /// Fors the bits.
      /// </summary>
      /// <param name="bits">four bits encoding a QR Code data mode</param>
      /// <returns>
      ///   <see cref="Mode"/> encoded by these bits
      /// </returns>
      /// <exception cref="ArgumentException">if bits do not correspond to a known mode</exception>
      public static Mode forBits(int bits)
      {
         switch (bits)
         {
            case 0x0:
               return TERMINATOR;
            case 0x1:
               return NUMERIC;
            case 0x2:
               return ALPHANUMERIC;
            case 0x3:
               return STRUCTURED_APPEND;
            case 0x4:
               return BYTE;
            case 0x5:
               return FNC1_FIRST_POSITION;
            case 0x7:
               return ECI;
            case 0x8:
               return KANJI;
            case 0x9:
               return FNC1_SECOND_POSITION;
            case 0xD:
               // 0xD is defined in GBT 18284-2000, may not be supported in foreign country
               return HANZI;
            default:
               throw new ArgumentException();
         }
      }

      /// <param name="version">version in question
      /// </param>
      /// <returns> number of bits used, in this QR Code symbol {@link Version}, to encode the
      /// count of characters that will follow encoded in this {@link Mode}
      /// </returns>
      public int getCharacterCountBits(Version version)
      {
         if (characterCountBitsForVersions == null)
         {
            throw new ArgumentException("Character count doesn't apply to this mode");
         }
         int number = version.VersionNumber;
         int offset;
         if (number <= 9)
         {
            offset = 0;
         }
         else if (number <= 26)
         {
            offset = 1;
         }
         else
         {
            offset = 2;
         }
         return characterCountBitsForVersions[offset];
      }

      /// <summary>
      /// Gets the bits.
      /// </summary>
      public int Bits
      {
         get
         {
            return bits;
         }
      }

      /// <summary>
      /// Returns a <see cref="System.String"/> that represents this instance.
      /// </summary>
      /// <returns>
      /// A <see cref="System.String"/> that represents this instance.
      /// </returns>
      public override String ToString()
      {
         return name;
      }
   }
}