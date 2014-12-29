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

namespace ZXing.Common.ReedSolomon
{
   /// <summary>
   ///   <p>This class contains utility methods for performing mathematical operations over
   /// the Galois Fields. Operations use a given primitive polynomial in calculations.</p>
   ///   <p>Throughout this package, elements of the GF are represented as an {@code int}
   /// for convenience and speed (but at the cost of memory).
   ///   </p>
   /// </summary>
   /// <author>Sean Owen</author>
   public sealed class GenericGF
   {
      public static GenericGF QR_CODE_FIELD_256 = new GenericGF(0x011D, 256, 0); // x^8 + x^4 + x^3 + x^2 + 1

      private int[] expTable;
      private int[] logTable;
      private GenericGFPoly zero;
      private GenericGFPoly one;
      private readonly int size;
      private readonly int primitive;
      private readonly int generatorBase;

      /// <summary>
      /// Create a representation of GF(size) using the given primitive polynomial.
      /// </summary>
      /// <param name="primitive">irreducible polynomial whose coefficients are represented by
      /// *  the bits of an int, where the least-significant bit represents the constant
      /// *  coefficient</param>
      /// <param name="size">the size of the field</param>
      /// <param name="genBase">the factor b in the generator polynomial can be 0- or 1-based
      /// *  (g(x) = (x+a^b)(x+a^(b+1))...(x+a^(b+2t-1))).
      /// *  In most cases it should be 1, but for QR code it is 0.</param>
      public GenericGF(int primitive, int size, int genBase)
      {
         this.primitive = primitive;
         this.size = size;
         this.generatorBase = genBase;

         expTable = new int[size];
         logTable = new int[size];
         int x = 1;
         for (int i = 0; i < size; i++)
         {
            expTable[i] = x;
            x <<= 1; // x = x * 2; we're assuming the generator alpha is 2
            if (x >= size)
            {
               x ^= primitive;
               x &= size - 1;
            }
         }
         for (int i = 0; i < size - 1; i++)
         {
            logTable[expTable[i]] = i;
         }
         // logTable[0] == 0 but this should never be used
         zero = new GenericGFPoly(this, new int[] { 0 });
         one = new GenericGFPoly(this, new int[] { 1 });
      }

      internal GenericGFPoly Zero
      {
         get
         {
            return zero;
         }
      }

      /// <summary>
      /// Builds the monomial.
      /// </summary>
      /// <param name="degree">The degree.</param>
      /// <param name="coefficient">The coefficient.</param>
      /// <returns>the monomial representing coefficient * x^degree</returns>
      internal GenericGFPoly buildMonomial(int degree, int coefficient)
      {
         if (degree < 0)
         {
            throw new ArgumentException();
         }
         if (coefficient == 0)
         {
            return zero;
         }
         int[] coefficients = new int[degree + 1];
         coefficients[0] = coefficient;
         return new GenericGFPoly(this, coefficients);
      }

      /// <summary>
      /// Implements both addition and subtraction -- they are the same in GF(size).
      /// </summary>
      /// <returns>sum/difference of a and b</returns>
      static internal int addOrSubtract(int a, int b)
      {
         return a ^ b;
      }

      /// <summary>
      /// Exps the specified a.
      /// </summary>
      /// <returns>2 to the power of a in GF(size)</returns>
      internal int exp(int a)
      {
         return expTable[a];
      }


      /// <summary>
      /// Inverses the specified a.
      /// </summary>
      /// <returns>multiplicative inverse of a</returns>
      internal int inverse(int a)
      {
         if (a == 0)
         {
            throw new ArithmeticException();
         }
         return expTable[size - logTable[a] - 1];
      }

      /// <summary>
      /// Multiplies the specified a with b.
      /// </summary>
      /// <param name="a">A.</param>
      /// <param name="b">The b.</param>
      /// <returns>product of a and b in GF(size)</returns>
      internal int multiply(int a, int b)
      {
         if (a == 0 || b == 0)
         {
            return 0;
         }
         return expTable[(logTable[a] + logTable[b]) % (size - 1)];
      }

      /// <summary>
      /// Gets the generator base.
      /// </summary>
      public int GeneratorBase
      {
         get { return generatorBase; }
      }
   }
}