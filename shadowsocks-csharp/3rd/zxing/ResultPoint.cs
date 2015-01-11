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

using ZXing.Common.Detector;

namespace ZXing
{
   /// <summary>
   /// Encapsulates a point of interest in an image containing a barcode. Typically, this
   /// would be the location of a finder pattern or the corner of the barcode, for example.
   /// </summary>
   /// <author>Sean Owen</author>
   /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source</author>
   public class ResultPoint
   {
      private readonly float x;
      private readonly float y;
      private readonly byte[] bytesX;
      private readonly byte[] bytesY;
      private String toString;

      /// <summary>
      /// Initializes a new instance of the <see cref="ResultPoint"/> class.
      /// </summary>
      public ResultPoint()
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="ResultPoint"/> class.
      /// </summary>
      /// <param name="x">The x.</param>
      /// <param name="y">The y.</param>
      public ResultPoint(float x, float y)
      {
         this.x = x;
         this.y = y;
         // calculate only once for GetHashCode
         bytesX = BitConverter.GetBytes(x);
         bytesY = BitConverter.GetBytes(y);
      }

      /// <summary>
      /// Gets the X.
      /// </summary>
      virtual public float X
      {
         get
         {
            return x;
         }
      }

      /// <summary>
      /// Gets the Y.
      /// </summary>
      virtual public float Y
      {
         get
         {
            return y;
         }
      }

      /// <summary>
      /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
      /// </summary>
      /// <param name="other">The <see cref="System.Object"/> to compare with this instance.</param>
      /// <returns>
      ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
      /// </returns>
      public override bool Equals(Object other)
      {
         var otherPoint = other as ResultPoint;
         if (otherPoint == null)
            return false;
         return x == otherPoint.x && y == otherPoint.y;
      }

      /// <summary>
      /// Returns a hash code for this instance.
      /// </summary>
      /// <returns>
      /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
      /// </returns>
      public override int GetHashCode()
      {
         return 31 * ((bytesX[0] << 24) + (bytesX[1] << 16) + (bytesX[2] << 8) + bytesX[3]) +
                      (bytesY[0] << 24) + (bytesY[1] << 16) + (bytesY[2] << 8) + bytesY[3];
      }

      /// <summary>
      /// Returns a <see cref="System.String"/> that represents this instance.
      /// </summary>
      /// <returns>
      /// A <see cref="System.String"/> that represents this instance.
      /// </returns>
      public override String ToString()
      {
         if (toString == null)
         {
            var result = new System.Text.StringBuilder(25);
            result.AppendFormat(System.Globalization.CultureInfo.CurrentUICulture, "({0}, {1})", x, y);
            toString = result.ToString();
         }
         return toString;
      }

      /// <summary>
      /// Orders an array of three ResultPoints in an order [A,B,C] such that AB &lt; AC and
      /// BC &lt; AC and the angle between BC and BA is less than 180 degrees.
      /// </summary>
      public static void orderBestPatterns(ResultPoint[] patterns)
      {
         // Find distances between pattern centers
         float zeroOneDistance = distance(patterns[0], patterns[1]);
         float oneTwoDistance = distance(patterns[1], patterns[2]);
         float zeroTwoDistance = distance(patterns[0], patterns[2]);

         ResultPoint pointA, pointB, pointC;
         // Assume one closest to other two is B; A and C will just be guesses at first
         if (oneTwoDistance >= zeroOneDistance && oneTwoDistance >= zeroTwoDistance)
         {
            pointB = patterns[0];
            pointA = patterns[1];
            pointC = patterns[2];
         }
         else if (zeroTwoDistance >= oneTwoDistance && zeroTwoDistance >= zeroOneDistance)
         {
            pointB = patterns[1];
            pointA = patterns[0];
            pointC = patterns[2];
         }
         else
         {
            pointB = patterns[2];
            pointA = patterns[0];
            pointC = patterns[1];
         }

         // Use cross product to figure out whether A and C are correct or flipped.
         // This asks whether BC x BA has a positive z component, which is the arrangement
         // we want for A, B, C. If it's negative, then we've got it flipped around and
         // should swap A and C.
         if (crossProductZ(pointA, pointB, pointC) < 0.0f)
         {
            ResultPoint temp = pointA;
            pointA = pointC;
            pointC = temp;
         }

         patterns[0] = pointA;
         patterns[1] = pointB;
         patterns[2] = pointC;
      }


      /// <returns>
      /// distance between two points
      /// </returns>
      public static float distance(ResultPoint pattern1, ResultPoint pattern2)
      {
         return MathUtils.distance(pattern1.x, pattern1.y, pattern2.x, pattern2.y);
      }

      /// <summary>
      /// Returns the z component of the cross product between vectors BC and BA.
      /// </summary>
      private static float crossProductZ(ResultPoint pointA, ResultPoint pointB, ResultPoint pointC)
      {
         float bX = pointB.x;
         float bY = pointB.y;
         return ((pointC.x - bX) * (pointA.y - bY)) - ((pointC.y - bY) * (pointA.x - bX));
      }
   }
}