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
using System.Text;

namespace ZXing.Common.ReedSolomon
{
   /// <summary>
   /// <p>Represents a polynomial whose coefficients are elements of a GF.
   /// Instances of this class are immutable.</p>
   /// <p>Much credit is due to William Rucklidge since portions of this code are an indirect
   /// port of his C++ Reed-Solomon implementation.</p>
   /// </summary>
   /// <author>Sean Owen</author>
   internal sealed class GenericGFPoly
   {
      private readonly GenericGF field;
      private readonly int[] coefficients;

      /// <summary>
      /// Initializes a new instance of the <see cref="GenericGFPoly"/> class.
      /// </summary>
      /// <param name="field">the {@link GenericGF} instance representing the field to use
      /// to perform computations</param>
      /// <param name="coefficients">coefficients as ints representing elements of GF(size), arranged
      /// from most significant (highest-power term) coefficient to least significant</param>
      /// <exception cref="ArgumentException">if argument is null or empty,
      /// or if leading coefficient is 0 and this is not a
      /// constant polynomial (that is, it is not the monomial "0")</exception>
      internal GenericGFPoly(GenericGF field, int[] coefficients)
      {
         if (coefficients.Length == 0)
         {
            throw new ArgumentException();
         }
         this.field = field;
         int coefficientsLength = coefficients.Length;
         if (coefficientsLength > 1 && coefficients[0] == 0)
         {
            // Leading term must be non-zero for anything except the constant polynomial "0"
            int firstNonZero = 1;
            while (firstNonZero < coefficientsLength && coefficients[firstNonZero] == 0)
            {
               firstNonZero++;
            }
            if (firstNonZero == coefficientsLength)
            {
               this.coefficients = field.Zero.coefficients;
            }
            else
            {
               this.coefficients = new int[coefficientsLength - firstNonZero];
               Array.Copy(coefficients,
                   firstNonZero,
                   this.coefficients,
                   0,
                   this.coefficients.Length);
            }
         }
         else
         {
            this.coefficients = coefficients;
         }
      }

      internal int[] Coefficients
      {
         get { return coefficients; }
      }

      /// <summary>
      /// degree of this polynomial
      /// </summary>
      internal int Degree
      {
         get
         {
            return coefficients.Length - 1;
         }
      }

      /// <summary>
      /// Gets a value indicating whether this <see cref="GenericGFPoly"/> is zero.
      /// </summary>
      /// <value>true iff this polynomial is the monomial "0"</value>
      internal bool isZero
      {
         get { return coefficients[0] == 0; }
      }

      /// <summary>
      /// coefficient of x^degree term in this polynomial
      /// </summary>
      /// <param name="degree">The degree.</param>
      /// <returns>coefficient of x^degree term in this polynomial</returns>
      internal int getCoefficient(int degree)
      {
         return coefficients[coefficients.Length - 1 - degree];
      }

      /// <summary>
      /// evaluation of this polynomial at a given point
      /// </summary>
      /// <param name="a">A.</param>
      /// <returns>evaluation of this polynomial at a given point</returns>
      internal int evaluateAt(int a)
      {
         int result = 0;
         if (a == 0)
         {
            // Just return the x^0 coefficient
            return getCoefficient(0);
         }
         int size = coefficients.Length;
         if (a == 1)
         {
            // Just the sum of the coefficients
            foreach (var coefficient in coefficients)
            {
               result = GenericGF.addOrSubtract(result, coefficient);
            }
            return result;
         }
         result = coefficients[0];
         for (int i = 1; i < size; i++)
         {
            result = GenericGF.addOrSubtract(field.multiply(a, result), coefficients[i]);
         }
         return result;
      }

      internal GenericGFPoly addOrSubtract(GenericGFPoly other)
      {
         if (!field.Equals(other.field))
         {
            throw new ArgumentException("GenericGFPolys do not have same GenericGF field");
         }
         if (isZero)
         {
            return other;
         }
         if (other.isZero)
         {
            return this;
         }

         int[] smallerCoefficients = this.coefficients;
         int[] largerCoefficients = other.coefficients;
         if (smallerCoefficients.Length > largerCoefficients.Length)
         {
            int[] temp = smallerCoefficients;
            smallerCoefficients = largerCoefficients;
            largerCoefficients = temp;
         }
         int[] sumDiff = new int[largerCoefficients.Length];
         int lengthDiff = largerCoefficients.Length - smallerCoefficients.Length;
         // Copy high-order terms only found in higher-degree polynomial's coefficients
         Array.Copy(largerCoefficients, 0, sumDiff, 0, lengthDiff);

         for (int i = lengthDiff; i < largerCoefficients.Length; i++)
         {
            sumDiff[i] = GenericGF.addOrSubtract(smallerCoefficients[i - lengthDiff], largerCoefficients[i]);
         }

         return new GenericGFPoly(field, sumDiff);
      }

      internal GenericGFPoly multiply(GenericGFPoly other)
      {
         if (!field.Equals(other.field))
         {
            throw new ArgumentException("GenericGFPolys do not have same GenericGF field");
         }
         if (isZero || other.isZero)
         {
            return field.Zero;
         }
         int[] aCoefficients = this.coefficients;
         int aLength = aCoefficients.Length;
         int[] bCoefficients = other.coefficients;
         int bLength = bCoefficients.Length;
         int[] product = new int[aLength + bLength - 1];
         for (int i = 0; i < aLength; i++)
         {
            int aCoeff = aCoefficients[i];
            for (int j = 0; j < bLength; j++)
            {
               product[i + j] = GenericGF.addOrSubtract(product[i + j],
                   field.multiply(aCoeff, bCoefficients[j]));
            }
         }
         return new GenericGFPoly(field, product);
      }

      internal GenericGFPoly multiply(int scalar)
      {
         if (scalar == 0)
         {
            return field.Zero;
         }
         if (scalar == 1)
         {
            return this;
         }
         int size = coefficients.Length;
         int[] product = new int[size];
         for (int i = 0; i < size; i++)
         {
            product[i] = field.multiply(coefficients[i], scalar);
         }
         return new GenericGFPoly(field, product);
      }

      internal GenericGFPoly multiplyByMonomial(int degree, int coefficient)
      {
         if (degree < 0)
         {
            throw new ArgumentException();
         }
         if (coefficient == 0)
         {
            return field.Zero;
         }
         int size = coefficients.Length;
         int[] product = new int[size + degree];
         for (int i = 0; i < size; i++)
         {
            product[i] = field.multiply(coefficients[i], coefficient);
         }
         return new GenericGFPoly(field, product);
      }

      internal GenericGFPoly[] divide(GenericGFPoly other)
      {
         if (!field.Equals(other.field))
         {
            throw new ArgumentException("GenericGFPolys do not have same GenericGF field");
         }
         if (other.isZero)
         {
            throw new ArgumentException("Divide by 0");
         }

         GenericGFPoly quotient = field.Zero;
         GenericGFPoly remainder = this;

         int denominatorLeadingTerm = other.getCoefficient(other.Degree);
         int inverseDenominatorLeadingTerm = field.inverse(denominatorLeadingTerm);

         while (remainder.Degree >= other.Degree && !remainder.isZero)
         {
            int degreeDifference = remainder.Degree - other.Degree;
            int scale = field.multiply(remainder.getCoefficient(remainder.Degree), inverseDenominatorLeadingTerm);
            GenericGFPoly term = other.multiplyByMonomial(degreeDifference, scale);
            GenericGFPoly iterationQuotient = field.buildMonomial(degreeDifference, scale);
            quotient = quotient.addOrSubtract(iterationQuotient);
            remainder = remainder.addOrSubtract(term);
         }

         return new GenericGFPoly[] { quotient, remainder };
      }

      public override String ToString()
      {
         StringBuilder result = new StringBuilder(8 * Degree);
         for (int degree = Degree; degree >= 0; degree--)
         {
            int coefficient = getCoefficient(degree);
            if (coefficient != 0)
            {
               if (coefficient < 0)
               {
                  result.Append(" - ");
                  coefficient = -coefficient;
               }
               else
               {
                  if (result.Length > 0)
                  {
                     result.Append(" + ");
                  }
               }
               if (degree == 0 || coefficient != 1)
               {
                  int alphaPower = field.log(coefficient);
                  if (alphaPower == 0)
                  {
                     result.Append('1');
                  }
                  else if (alphaPower == 1)
                  {
                     result.Append('a');
                  }
                  else
                  {
                     result.Append("a^");
                     result.Append(alphaPower);
                  }
               }
               if (degree != 0)
               {
                  if (degree == 1)
                  {
                     result.Append('x');
                  }
                  else
                  {
                     result.Append("x^");
                     result.Append(degree);
                  }
               }
            }
         }
         return result.ToString();
      }
   }
}