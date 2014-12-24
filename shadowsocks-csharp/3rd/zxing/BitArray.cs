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

namespace ZXing.Common
{
   /// <summary>
   /// A simple, fast array of bits, represented compactly by an array of ints internally.
   /// </summary>
   /// <author>Sean Owen</author>
   public sealed class BitArray
   {
      private int[] bits;
      private int size;

      public int Size
      {
         get
         {
            return size;
         }
      }

      public int SizeInBytes
      {
         get
         {
            return (size + 7) >> 3;
         }
      }

      public bool this[int i]
      {
         get
         {
            return (bits[i >> 5] & (1 << (i & 0x1F))) != 0;
         }
      }

      public BitArray()
      {
         this.size = 0;
         this.bits = new int[1];
      }


      private void ensureCapacity(int size)
      {
         if (size > bits.Length << 5)
         {
            int[] newBits = makeArray(size);
            System.Array.Copy(bits, 0, newBits, 0, bits.Length);
            bits = newBits;
         }
      }

      /// <summary>
      /// Appends the bit.
      /// </summary>
      /// <param name="bit">The bit.</param>
      public void appendBit(bool bit)
      {
         ensureCapacity(size + 1);
         if (bit)
         {
            bits[size >> 5] |= 1 << (size & 0x1F);
         }
         size++;
      }

      /// <summary>
      /// Appends the least-significant bits, from value, in order from most-significant to
      /// least-significant. For example, appending 6 bits from 0x000001E will append the bits
      /// 0, 1, 1, 1, 1, 0 in that order.
      /// </summary>
      /// <param name="value"><see cref="int"/> containing bits to append</param>
      /// <param name="numBits">bits from value to append</param>
      public void appendBits(int value, int numBits)
      {
         if (numBits < 0 || numBits > 32)
         {
            throw new ArgumentException("Num bits must be between 0 and 32");
         }
         ensureCapacity(size + numBits);
         for (int numBitsLeft = numBits; numBitsLeft > 0; numBitsLeft--)
         {
            appendBit(((value >> (numBitsLeft - 1)) & 0x01) == 1);
         }
      }

      public void appendBitArray(BitArray other)
      {
         int otherSize = other.size;
         ensureCapacity(size + otherSize);
         for (int i = 0; i < otherSize; i++)
         {
            appendBit(other[i]);
         }
      }

      public void xor(BitArray other)
      {
         if (bits.Length != other.bits.Length)
         {
            throw new ArgumentException("Sizes don't match");
         }
         for (int i = 0; i < bits.Length; i++)
         {
            // The last byte could be incomplete (i.e. not have 8 bits in
            // it) but there is no problem since 0 XOR 0 == 0.
            bits[i] ^= other.bits[i];
         }
      }

      /// <summary>
      /// Toes the bytes.
      /// </summary>
      /// <param name="bitOffset">first bit to start writing</param>
      /// <param name="array">array to write into. Bytes are written most-significant byte first. This is the opposite
      /// of the internal representation, which is exposed by BitArray</param>
      /// <param name="offset">position in array to start writing</param>
      /// <param name="numBytes">how many bytes to write</param>
      public void toBytes(int bitOffset, byte[] array, int offset, int numBytes)
      {
         for (int i = 0; i < numBytes; i++)
         {
            int theByte = 0;
            for (int j = 0; j < 8; j++)
            {
               if (this[bitOffset])
               {
                  theByte |= 1 << (7 - j);
               }
               bitOffset++;
            }
            array[offset + i] = (byte)theByte;
         }
      }

      private static int[] makeArray(int size)
      {
         return new int[(size + 31) >> 5];
      }

   }
}