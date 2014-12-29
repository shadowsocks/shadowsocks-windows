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
      public static readonly Mode BYTE = new Mode(new int[] { 8, 16, 16 }, 0x04, "BYTE");

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
            case 0x4:
               return BYTE;
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
   }
}