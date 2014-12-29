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

using ZXing.Common;

namespace ZXing.QrCode.Internal
{
   /// <summary>
   /// See ISO 18004:2006 Annex D
   /// </summary>
   /// <author>Sean Owen</author>
   public sealed class Version
   {

      private static readonly Version[] VERSIONS = buildVersions();

      private readonly int versionNumber;
      private readonly int[] alignmentPatternCenters;
      private readonly ECBlocks[] ecBlocks;
      private readonly int totalCodewords;

      private Version(int versionNumber, int[] alignmentPatternCenters, params ECBlocks[] ecBlocks)
      {
         this.versionNumber = versionNumber;
         this.alignmentPatternCenters = alignmentPatternCenters;
         this.ecBlocks = ecBlocks;
         int total = 0;
         int ecCodewords = ecBlocks[0].ECCodewordsPerBlock;
         ECB[] ecbArray = ecBlocks[0].getECBlocks();
         foreach (var ecBlock in ecbArray)
         {
            total += ecBlock.Count * (ecBlock.DataCodewords + ecCodewords);
         }
         this.totalCodewords = total;
      }

      /// <summary>
      /// Gets the version number.
      /// </summary>
      public int VersionNumber
      {
         get
         {
            return versionNumber;
         }

      }

      /// <summary>
      /// Gets the total codewords.
      /// </summary>
      public int TotalCodewords
      {
         get
         {
            return totalCodewords;
         }

      }

      /// <summary>
      /// Gets the dimension for version.
      /// </summary>
      public int DimensionForVersion
      {
         get
         {
            return 17 + 4 * versionNumber;
         }

      }

      /// <summary>
      /// Gets the EC blocks for level.
      /// </summary>
      /// <param name="ecLevel">The ec level.</param>
      /// <returns></returns>
      public ECBlocks getECBlocksForLevel(ErrorCorrectionLevel ecLevel)
      {
         return ecBlocks[ecLevel.ordinal()];
      }

      /// <summary>
      /// Gets the version for number.
      /// </summary>
      /// <param name="versionNumber">The version number.</param>
      /// <returns></returns>
      public static Version getVersionForNumber(int versionNumber)
      {
         if (versionNumber < 1 || versionNumber > 40)
         {
            throw new ArgumentException();
         }
         return VERSIONS[versionNumber - 1];
      }


      /// <summary> <p>Encapsulates a set of error-correction blocks in one symbol version. Most versions will
      /// use blocks of differing sizes within one version, so, this encapsulates the parameters for
      /// each set of blocks. It also holds the number of error-correction codewords per block since it
      /// will be the same across all blocks within one version.</p>
      /// </summary>
      public sealed class ECBlocks
      {
         private readonly int ecCodewordsPerBlock;
         private readonly ECB[] ecBlocks;

         internal ECBlocks(int ecCodewordsPerBlock, params ECB[] ecBlocks)
         {
            this.ecCodewordsPerBlock = ecCodewordsPerBlock;
            this.ecBlocks = ecBlocks;
         }

         /// <summary>
         /// Gets the EC codewords per block.
         /// </summary>
         public int ECCodewordsPerBlock
         {
            get
            {
               return ecCodewordsPerBlock;
            }
         }

         /// <summary>
         /// Gets the num blocks.
         /// </summary>
         public int NumBlocks
         {
            get
            {
               int total = 0;
               foreach (var ecBlock in ecBlocks)
               {
                  total += ecBlock.Count;
               }
               return total;
            }
         }

         /// <summary>
         /// Gets the total EC codewords.
         /// </summary>
         public int TotalECCodewords
         {
            get
            {
               return ecCodewordsPerBlock * NumBlocks;
            }
         }

         /// <summary>
         /// Gets the EC blocks.
         /// </summary>
         /// <returns></returns>
         public ECB[] getECBlocks()
         {
            return ecBlocks;
         }
      }

      /// <summary> <p>Encapsualtes the parameters for one error-correction block in one symbol version.
      /// This includes the number of data codewords, and the number of times a block with these
      /// parameters is used consecutively in the QR code version's format.</p>
      /// </summary>
      public sealed class ECB
      {
         private readonly int count;
         private readonly int dataCodewords;

         internal ECB(int count, int dataCodewords)
         {
            this.count = count;
            this.dataCodewords = dataCodewords;
         }

         /// <summary>
         /// Gets the count.
         /// </summary>
         public int Count
         {
            get
            {
               return count;
            }

         }
         /// <summary>
         /// Gets the data codewords.
         /// </summary>
         public int DataCodewords
         {
            get
            {
               return dataCodewords;
            }

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
         return Convert.ToString(versionNumber);
      }

      /// <summary> See ISO 18004:2006 6.5.1 Table 9</summary>
      private static Version[] buildVersions()
      {
         return new Version[]
            {
               new Version(1, new int[] {},
                           new ECBlocks(7, new ECB(1, 19)),
                           new ECBlocks(10, new ECB(1, 16)),
                           new ECBlocks(13, new ECB(1, 13)),
                           new ECBlocks(17, new ECB(1, 9))),
               new Version(2, new int[] {6, 18},
                           new ECBlocks(10, new ECB(1, 34)),
                           new ECBlocks(16, new ECB(1, 28)),
                           new ECBlocks(22, new ECB(1, 22)),
                           new ECBlocks(28, new ECB(1, 16))),
               new Version(3, new int[] {6, 22},
                           new ECBlocks(15, new ECB(1, 55)),
                           new ECBlocks(26, new ECB(1, 44)),
                           new ECBlocks(18, new ECB(2, 17)),
                           new ECBlocks(22, new ECB(2, 13))),
               new Version(4, new int[] {6, 26},
                           new ECBlocks(20, new ECB(1, 80)),
                           new ECBlocks(18, new ECB(2, 32)),
                           new ECBlocks(26, new ECB(2, 24)),
                           new ECBlocks(16, new ECB(4, 9))),
               new Version(5, new int[] {6, 30},
                           new ECBlocks(26, new ECB(1, 108)),
                           new ECBlocks(24, new ECB(2, 43)),
                           new ECBlocks(18, new ECB(2, 15),
                                        new ECB(2, 16)),
                           new ECBlocks(22, new ECB(2, 11),
                                        new ECB(2, 12))),
               new Version(6, new int[] {6, 34},
                           new ECBlocks(18, new ECB(2, 68)),
                           new ECBlocks(16, new ECB(4, 27)),
                           new ECBlocks(24, new ECB(4, 19)),
                           new ECBlocks(28, new ECB(4, 15))),
               new Version(7, new int[] {6, 22, 38},
                           new ECBlocks(20, new ECB(2, 78)),
                           new ECBlocks(18, new ECB(4, 31)),
                           new ECBlocks(18, new ECB(2, 14),
                                        new ECB(4, 15)),
                           new ECBlocks(26, new ECB(4, 13),
                                        new ECB(1, 14))),
               new Version(8, new int[] {6, 24, 42},
                           new ECBlocks(24, new ECB(2, 97)),
                           new ECBlocks(22, new ECB(2, 38),
                                        new ECB(2, 39)),
                           new ECBlocks(22, new ECB(4, 18),
                                        new ECB(2, 19)),
                           new ECBlocks(26, new ECB(4, 14),
                                        new ECB(2, 15))),
               new Version(9, new int[] {6, 26, 46},
                           new ECBlocks(30, new ECB(2, 116)),
                           new ECBlocks(22, new ECB(3, 36),
                                        new ECB(2, 37)),
                           new ECBlocks(20, new ECB(4, 16),
                                        new ECB(4, 17)),
                           new ECBlocks(24, new ECB(4, 12),
                                        new ECB(4, 13))),
               new Version(10, new int[] {6, 28, 50},
                           new ECBlocks(18, new ECB(2, 68),
                                        new ECB(2, 69)),
                           new ECBlocks(26, new ECB(4, 43),
                                        new ECB(1, 44)),
                           new ECBlocks(24, new ECB(6, 19),
                                        new ECB(2, 20)),
                           new ECBlocks(28, new ECB(6, 15),
                                        new ECB(2, 16))),
               new Version(11, new int[] {6, 30, 54},
                           new ECBlocks(20, new ECB(4, 81)),
                           new ECBlocks(30, new ECB(1, 50),
                                        new ECB(4, 51)),
                           new ECBlocks(28, new ECB(4, 22),
                                        new ECB(4, 23)),
                           new ECBlocks(24, new ECB(3, 12),
                                        new ECB(8, 13))),
               new Version(12, new int[] {6, 32, 58},
                           new ECBlocks(24, new ECB(2, 92),
                                        new ECB(2, 93)),
                           new ECBlocks(22, new ECB(6, 36),
                                        new ECB(2, 37)),
                           new ECBlocks(26, new ECB(4, 20),
                                        new ECB(6, 21)),
                           new ECBlocks(28, new ECB(7, 14),
                                        new ECB(4, 15))),
               new Version(13, new int[] {6, 34, 62},
                           new ECBlocks(26, new ECB(4, 107)),
                           new ECBlocks(22, new ECB(8, 37),
                                        new ECB(1, 38)),
                           new ECBlocks(24, new ECB(8, 20),
                                        new ECB(4, 21)),
                           new ECBlocks(22, new ECB(12, 11),
                                        new ECB(4, 12))),
               new Version(14, new int[] {6, 26, 46, 66},
                           new ECBlocks(30, new ECB(3, 115),
                                        new ECB(1, 116)),
                           new ECBlocks(24, new ECB(4, 40),
                                        new ECB(5, 41)),
                           new ECBlocks(20, new ECB(11, 16),
                                        new ECB(5, 17)),
                           new ECBlocks(24, new ECB(11, 12),
                                        new ECB(5, 13))),
               new Version(15, new int[] {6, 26, 48, 70},
                           new ECBlocks(22, new ECB(5, 87),
                                        new ECB(1, 88)),
                           new ECBlocks(24, new ECB(5, 41),
                                        new ECB(5, 42)),
                           new ECBlocks(30, new ECB(5, 24),
                                        new ECB(7, 25)),
                           new ECBlocks(24, new ECB(11, 12),
                                        new ECB(7, 13)))
            };
      }
   }
}