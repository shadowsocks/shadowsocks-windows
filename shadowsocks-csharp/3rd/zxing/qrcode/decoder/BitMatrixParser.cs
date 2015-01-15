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

using ZXing.Common;

namespace ZXing.QrCode.Internal
{
   /// <author>Sean Owen</author>
   sealed class BitMatrixParser
   {
      private readonly BitMatrix bitMatrix;
      private Version parsedVersion;
      private FormatInformation parsedFormatInfo;
      private bool mirrored;

      /// <param name="bitMatrix">{@link BitMatrix} to parse</param>
      /// <throws>ReaderException if dimension is not >= 21 and 1 mod 4</throws>
      internal static BitMatrixParser createBitMatrixParser(BitMatrix bitMatrix)
      {
         int dimension = bitMatrix.Height;
         if (dimension < 21 || (dimension & 0x03) != 1)
         {
            return null;
         }
         return new BitMatrixParser(bitMatrix);
      }

      private BitMatrixParser(BitMatrix bitMatrix)
      {
         // Should only be called from createBitMatrixParser with the important checks before
         this.bitMatrix = bitMatrix;
      }

      /// <summary> <p>Reads format information from one of its two locations within the QR Code.</p>
      /// 
      /// </summary>
      /// <returns> {@link FormatInformation} encapsulating the QR Code's format info
      /// </returns>
      /// <throws>  ReaderException if both format information locations cannot be parsed as </throws>
      /// <summary> the valid encoding of format information
      /// </summary>
      internal FormatInformation readFormatInformation()
      {
         if (parsedFormatInfo != null)
         {
            return parsedFormatInfo;
         }

         // Read top-left format info bits
         int formatInfoBits1 = 0;
         for (int i = 0; i < 6; i++)
         {
            formatInfoBits1 = copyBit(i, 8, formatInfoBits1);
         }
         // .. and skip a bit in the timing pattern ...
         formatInfoBits1 = copyBit(7, 8, formatInfoBits1);
         formatInfoBits1 = copyBit(8, 8, formatInfoBits1);
         formatInfoBits1 = copyBit(8, 7, formatInfoBits1);
         // .. and skip a bit in the timing pattern ...
         for (int j = 5; j >= 0; j--)
         {
            formatInfoBits1 = copyBit(8, j, formatInfoBits1);
         }
         // Read the top-right/bottom-left pattern too
         int dimension = bitMatrix.Height;
         int formatInfoBits2 = 0;
         int jMin = dimension - 7;
         for (int j = dimension - 1; j >= jMin; j--)
         {
            formatInfoBits2 = copyBit(8, j, formatInfoBits2);
         }
         for (int i = dimension - 8; i < dimension; i++)
         {
            formatInfoBits2 = copyBit(i, 8, formatInfoBits2);
         }

         parsedFormatInfo = FormatInformation.decodeFormatInformation(formatInfoBits1, formatInfoBits2);
         if (parsedFormatInfo != null)
         {
            return parsedFormatInfo;
         }
         return null;
      }

      /// <summary> <p>Reads version information from one of its two locations within the QR Code.</p>
      /// 
      /// </summary>
      /// <returns> {@link Version} encapsulating the QR Code's version
      /// </returns>
      /// <throws>  ReaderException if both version information locations cannot be parsed as </throws>
      /// <summary> the valid encoding of version information
      /// </summary>
      internal Version readVersion()
      {
         if (parsedVersion != null)
         {
            return parsedVersion;
         }

         int dimension = bitMatrix.Height;

         int provisionalVersion = (dimension - 17) >> 2;
         if (provisionalVersion <= 6)
         {
            return Version.getVersionForNumber(provisionalVersion);
         }

         // Read top-right version info: 3 wide by 6 tall
         int versionBits = 0;
         int ijMin = dimension - 11;
         for (int j = 5; j >= 0; j--)
         {
            for (int i = dimension - 9; i >= ijMin; i--)
            {
               versionBits = copyBit(i, j, versionBits);
            }
         }

         parsedVersion = Version.decodeVersionInformation(versionBits);
         if (parsedVersion != null && parsedVersion.DimensionForVersion == dimension)
         {
            return parsedVersion;
         }

         // Hmm, failed. Try bottom left: 6 wide by 3 tall
         versionBits = 0;
         for (int i = 5; i >= 0; i--)
         {
            for (int j = dimension - 9; j >= ijMin; j--)
            {
               versionBits = copyBit(i, j, versionBits);
            }
         }

         parsedVersion = Version.decodeVersionInformation(versionBits);
         if (parsedVersion != null && parsedVersion.DimensionForVersion == dimension)
         {
            return parsedVersion;
         }
         return null;
      }

      private int copyBit(int i, int j, int versionBits)
      {
         bool bit = mirrored ? bitMatrix[j, i] : bitMatrix[i, j];
         return bit ? (versionBits << 1) | 0x1 : versionBits << 1;
      }

      /// <summary> <p>Reads the bits in the {@link BitMatrix} representing the finder pattern in the
      /// correct order in order to reconstruct the codewords bytes contained within the
      /// QR Code.</p>
      /// 
      /// </summary>
      /// <returns> bytes encoded within the QR Code
      /// </returns>
      /// <throws>  ReaderException if the exact number of bytes expected is not read </throws>
      internal byte[] readCodewords()
      {
         FormatInformation formatInfo = readFormatInformation();
         if (formatInfo == null)
            return null;
         Version version = readVersion();
         if (version == null)
            return null;

         // Get the data mask for the format used in this QR Code. This will exclude
         // some bits from reading as we wind through the bit matrix.
         DataMask dataMask = DataMask.forReference(formatInfo.DataMask);
         int dimension = bitMatrix.Height;
         dataMask.unmaskBitMatrix(bitMatrix, dimension);

         BitMatrix functionPattern = version.buildFunctionPattern();

         bool readingUp = true;
         byte[] result = new byte[version.TotalCodewords];
         int resultOffset = 0;
         int currentByte = 0;
         int bitsRead = 0;
         // Read columns in pairs, from right to left
         for (int j = dimension - 1; j > 0; j -= 2)
         {
            if (j == 6)
            {
               // Skip whole column with vertical alignment pattern;
               // saves time and makes the other code proceed more cleanly
               j--;
            }
            // Read alternatingly from bottom to top then top to bottom
            for (int count = 0; count < dimension; count++)
            {
               int i = readingUp ? dimension - 1 - count : count;
               for (int col = 0; col < 2; col++)
               {
                  // Ignore bits covered by the function pattern
                  if (!functionPattern[j - col, i])
                  {
                     // Read a bit
                     bitsRead++;
                     currentByte <<= 1;
                     if (bitMatrix[j - col, i])
                     {
                        currentByte |= 1;
                     }
                     // If we've made a whole byte, save it off
                     if (bitsRead == 8)
                     {
                        result[resultOffset++] = (byte)currentByte;
                        bitsRead = 0;
                        currentByte = 0;
                     }
                  }
               }
            }
            readingUp ^= true; // readingUp = !readingUp; // switch directions
         }
         if (resultOffset != version.TotalCodewords)
         {
            return null;
         }
         return result;
      }

      /**
       * Revert the mask removal done while reading the code words. The bit matrix should revert to its original state.
       */
      internal void remask()
      {
         if (parsedFormatInfo == null)
         {
            return; // We have no format information, and have no data mask
         }
         DataMask dataMask = DataMask.forReference(parsedFormatInfo.DataMask);
         int dimension = bitMatrix.Height;
         dataMask.unmaskBitMatrix(bitMatrix, dimension);
      }

      /**
       * Prepare the parser for a mirrored operation.
       * This flag has effect only on the {@link #readFormatInformation()} and the
       * {@link #readVersion()}. Before proceeding with {@link #readCodewords()} the
       * {@link #mirror()} method should be called.
       * 
       * @param mirror Whether to read version and format information mirrored.
       */
      internal void setMirror(bool mirror)
      {
         parsedVersion = null;
         parsedFormatInfo = null;
         mirrored = mirror;
      }

      /** Mirror the bit matrix in order to attempt a second reading. */
      internal void mirror()
      {
         for (int x = 0; x < bitMatrix.Width; x++)
         {
            for (int y = x + 1; y < bitMatrix.Height; y++)
            {
               if (bitMatrix[x, y] != bitMatrix[y, x])
               {
                  bitMatrix.flip(y, x);
                  bitMatrix.flip(x, y);
               }
            }
         }
      }
   }
}