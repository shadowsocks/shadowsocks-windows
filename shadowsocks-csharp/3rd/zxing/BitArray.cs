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
         set
         {
            if (value)
               bits[i >> 5] |= 1 << (i & 0x1F);
         }
      }

      public BitArray()
      {
         this.size = 0;
         this.bits = new int[1];
      }

      public BitArray(int size)
      {
         if (size < 1)
         {
            throw new ArgumentException("size must be at least 1");
         }
         this.size = size;
         this.bits = makeArray(size);
      }

      // For testing only
      private BitArray(int[] bits, int size)
      {
         this.bits = bits;
         this.size = size;
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

      /// <summary> Flips bit i.
      /// 
      /// </summary>
      /// <param name="i">bit to set
      /// </param>
      public void flip(int i)
      {
         bits[i >> 5] ^= 1 << (i & 0x1F);
      }

      private static int numberOfTrailingZeros(int num)
      {
         var index = (-num & num)%37;
         if (index < 0)
            index *= -1;
         return _lookup[index];
      }

      private static readonly int[] _lookup =
         {
            32, 0, 1, 26, 2, 23, 27, 0, 3, 16, 24, 30, 28, 11, 0, 13, 4, 7, 17,
            0, 25, 22, 31, 15, 29, 10, 12, 6, 0, 21, 14, 9, 5, 20, 8, 19, 18
         };

      /// <summary>
      /// Gets the next set.
      /// </summary>
      /// <param name="from">first bit to check</param>
      /// <returns>index of first bit that is set, starting from the given index, or size if none are set
      /// at or beyond this given index</returns>
      public int getNextSet(int from)
      {
         if (from >= size)
         {
            return size;
         }
         int bitsOffset = from >> 5;
         int currentBits = bits[bitsOffset];
         // mask off lesser bits first
         currentBits &= ~((1 << (from & 0x1F)) - 1);
         while (currentBits == 0)
         {
            if (++bitsOffset == bits.Length)
            {
               return size;
            }
            currentBits = bits[bitsOffset];
         }
         int result = (bitsOffset << 5) + numberOfTrailingZeros(currentBits);
         return result > size ? size : result;
      }

      /// <summary>
      /// see getNextSet(int)
      /// </summary>
      /// <param name="from">index to start looking for unset bit</param>
      /// <returns>index of next unset bit, or <see cref="Size"/> if none are unset until the end</returns>
      public int getNextUnset(int from)
      {
         if (from >= size)
         {
            return size;
         }
         int bitsOffset = from >> 5;
         int currentBits = ~bits[bitsOffset];
         // mask off lesser bits first
         currentBits &= ~((1 << (from & 0x1F)) - 1);
         while (currentBits == 0)
         {
            if (++bitsOffset == bits.Length)
            {
               return size;
            }
            currentBits = ~bits[bitsOffset];
         }
         int result = (bitsOffset << 5) + numberOfTrailingZeros(currentBits);
         return result > size ? size : result;
      }

      /// <summary> Sets a block of 32 bits, starting at bit i.
      /// 
      /// </summary>
      /// <param name="i">first bit to set
      /// </param>
      /// <param name="newBits">the new value of the next 32 bits. Note again that the least-significant bit
      /// corresponds to bit i, the next-least-significant to i+1, and so on.
      /// </param>
      public void setBulk(int i, int newBits)
      {
         bits[i >> 5] = newBits;
      }

      /// <summary>
      /// Sets a range of bits.
      /// </summary>
      /// <param name="start">start of range, inclusive.</param>
      /// <param name="end">end of range, exclusive</param>
      public void setRange(int start, int end)
      {
         if (end < start)
         {
            throw new ArgumentException();
         }
         if (end == start)
         {
            return;
         }
         end--; // will be easier to treat this as the last actually set bit -- inclusive
         int firstInt = start >> 5;
         int lastInt = end >> 5;
         for (int i = firstInt; i <= lastInt; i++)
         {
            int firstBit = i > firstInt ? 0 : start & 0x1F;
            int lastBit = i < lastInt ? 31 : end & 0x1F;
            int mask;
            if (firstBit == 0 && lastBit == 31)
            {
               mask = -1;
            }
            else
            {
               mask = 0;
               for (int j = firstBit; j <= lastBit; j++)
               {
                  mask |= 1 << j;
               }
            }
            bits[i] |= mask;
         }
      }

      /// <summary> Clears all bits (sets to false).</summary>
      public void clear()
      {
         int max = bits.Length;
         for (int i = 0; i < max; i++)
         {
            bits[i] = 0;
         }
      }

      /// <summary> Efficient method to check if a range of bits is set, or not set.
      /// 
      /// </summary>
      /// <param name="start">start of range, inclusive.
      /// </param>
      /// <param name="end">end of range, exclusive
      /// </param>
      /// <param name="value">if true, checks that bits in range are set, otherwise checks that they are not set
      /// </param>
      /// <returns> true iff all bits are set or not set in range, according to value argument
      /// </returns>
      /// <throws>  IllegalArgumentException if end is less than or equal to start </throws>
      public bool isRange(int start, int end, bool value)
      {
         if (end < start)
         {
            throw new System.ArgumentException();
         }
         if (end == start)
         {
            return true; // empty range matches
         }
         end--; // will be easier to treat this as the last actually set bit -- inclusive    
         int firstInt = start >> 5;
         int lastInt = end >> 5;
         for (int i = firstInt; i <= lastInt; i++)
         {
            int firstBit = i > firstInt ? 0 : start & 0x1F;
            int lastBit = i < lastInt ? 31 : end & 0x1F;
            int mask;
            if (firstBit == 0 && lastBit == 31)
            {
               mask = -1;
            }
            else
            {
               mask = 0;
               for (int j = firstBit; j <= lastBit; j++)
               {
                  mask |= 1 << j;
               }
            }

            // Return false if we're looking for 1s and the masked bits[i] isn't all 1s (that is,
            // equals the mask, or we're looking for 0s and the masked portion is not all 0s
            if ((bits[i] & mask) != (value ? mask : 0))
            {
               return false;
            }
         }
         return true;
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

      /// <returns> underlying array of ints. The first element holds the first 32 bits, and the least
      /// significant bit is bit 0.
      /// </returns>
      public int[] Array
      {
         get { return bits; }
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

      /// <summary> Reverses all bits in the array.</summary>
      public void reverse()
      {
         var newBits = new int[bits.Length];
         // reverse all int's first
         var len = ((size - 1) >> 5);
         var oldBitsLen = len + 1;
         for (var i = 0; i < oldBitsLen; i++)
         {
            var x = (long)bits[i];
            x = ((x >>  1) & 0x55555555u) | ((x & 0x55555555u) <<  1);
            x = ((x >>  2) & 0x33333333u) | ((x & 0x33333333u) <<  2);
            x = ((x >>  4) & 0x0f0f0f0fu) | ((x & 0x0f0f0f0fu) <<  4);
            x = ((x >>  8) & 0x00ff00ffu) | ((x & 0x00ff00ffu) <<  8);
            x = ((x >> 16) & 0x0000ffffu) | ((x & 0x0000ffffu) << 16);
            newBits[len - i] = (int)x;
         }
         // now correct the int's if the bit size isn't a multiple of 32
         if (size != oldBitsLen * 32)
         {
            var leftOffset = oldBitsLen * 32 - size;
            var mask = 1;
            for (var i = 0; i < 31 - leftOffset; i++)
               mask = (mask << 1) | 1;
            var currentInt = (newBits[0] >> leftOffset) & mask;
            for (var i = 1; i < oldBitsLen; i++)
            {
               var nextInt = newBits[i];
               currentInt |= nextInt << (32 - leftOffset);
               newBits[i - 1] = currentInt;
               currentInt = (nextInt >> leftOffset) & mask;
            }
            newBits[oldBitsLen - 1] = currentInt;
         }
         bits = newBits;
      }

      private static int[] makeArray(int size)
      {
         return new int[(size + 31) >> 5];
      }

      /// <summary>
      /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
      /// </summary>
      /// <param name="o">The <see cref="System.Object"/> to compare with this instance.</param>
      /// <returns>
      ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
      /// </returns>
      public override bool Equals(Object o)
      {
         var other = o as BitArray;
         if (other == null)
            return false;
         if (size != other.size)
            return false;
         for (var index = 0; index < size; index++)
         {
            if (bits[index] != other.bits[index])
               return false;
         }
         return true;
      }

      /// <summary>
      /// Returns a hash code for this instance.
      /// </summary>
      /// <returns>
      /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
      /// </returns>
      public override int GetHashCode()
      {
         var hash = size;
         foreach (var bit in bits)
         {
            hash = 31 * hash + bit.GetHashCode();
         }
         return hash;
      }

      /// <summary>
      /// Returns a <see cref="System.String"/> that represents this instance.
      /// </summary>
      /// <returns>
      /// A <see cref="System.String"/> that represents this instance.
      /// </returns>
      public override String ToString()
      {
         var result = new System.Text.StringBuilder(size);
         for (int i = 0; i < size; i++)
         {
            if ((i & 0x07) == 0)
            {
               result.Append(' ');
            }
            result.Append(this[i] ? 'X' : '.');
         }
         return result.ToString();
      }

      /// <summary>
      /// Erstellt ein neues Objekt, das eine Kopie der aktuellen Instanz darstellt.
      /// </summary>
      /// <returns>
      /// Ein neues Objekt, das eine Kopie dieser Instanz darstellt.
      /// </returns>
      public object Clone()
      {
         return new BitArray((int[])bits.Clone(), size);
      }
   }
}