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

namespace ZXing.Common
{
   /// <summary> <p>This class implements a perspective transform in two dimensions. Given four source and four
   /// destination points, it will compute the transformation implied between them. The code is based
   /// directly upon section 3.4.2 of George Wolberg's "Digital Image Warping"; see pages 54-56.</p>
   /// 
   /// </summary>
   /// <author>  Sean Owen
   /// </author>
   /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source 
   /// </author>
   public sealed class PerspectiveTransform
   {
      private float a11;
      private float a12;
      private float a13;
      private float a21;
      private float a22;
      private float a23;
      private float a31;
      private float a32;
      private float a33;

      private PerspectiveTransform(float a11, float a21, float a31, float a12, float a22, float a32, float a13, float a23, float a33)
      {
         this.a11 = a11;
         this.a12 = a12;
         this.a13 = a13;
         this.a21 = a21;
         this.a22 = a22;
         this.a23 = a23;
         this.a31 = a31;
         this.a32 = a32;
         this.a33 = a33;
      }

      public static PerspectiveTransform quadrilateralToQuadrilateral(float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float x0p, float y0p, float x1p, float y1p, float x2p, float y2p, float x3p, float y3p)
      {

         PerspectiveTransform qToS = quadrilateralToSquare(x0, y0, x1, y1, x2, y2, x3, y3);
         PerspectiveTransform sToQ = squareToQuadrilateral(x0p, y0p, x1p, y1p, x2p, y2p, x3p, y3p);
         return sToQ.times(qToS);
      }

      public void transformPoints(float[] points)
      {
         int max = points.Length;
         float a11 = this.a11;
         float a12 = this.a12;
         float a13 = this.a13;
         float a21 = this.a21;
         float a22 = this.a22;
         float a23 = this.a23;
         float a31 = this.a31;
         float a32 = this.a32;
         float a33 = this.a33;
         for (int i = 0; i < max; i += 2)
         {
            float x = points[i];
            float y = points[i + 1];
            float denominator = a13 * x + a23 * y + a33;
            points[i] = (a11 * x + a21 * y + a31) / denominator;
            points[i + 1] = (a12 * x + a22 * y + a32) / denominator;
         }
      }

      /// <summary>Convenience method, not optimized for performance. </summary>
      public void transformPoints(float[] xValues, float[] yValues)
      {
         int n = xValues.Length;
         for (int i = 0; i < n; i++)
         {
            float x = xValues[i];
            float y = yValues[i];
            float denominator = a13 * x + a23 * y + a33;
            xValues[i] = (a11 * x + a21 * y + a31) / denominator;
            yValues[i] = (a12 * x + a22 * y + a32) / denominator;
         }
      }

      public static PerspectiveTransform squareToQuadrilateral(float x0, float y0,
                                                               float x1, float y1,
                                                               float x2, float y2,
                                                               float x3, float y3)
      {
         float dx3 = x0 - x1 + x2 - x3;
         float dy3 = y0 - y1 + y2 - y3;
         if (dx3 == 0.0f && dy3 == 0.0f)
         {
            // Affine
            return new PerspectiveTransform(x1 - x0, x2 - x1, x0,
                                            y1 - y0, y2 - y1, y0,
                                            0.0f, 0.0f, 1.0f);
         }
         else
         {
            float dx1 = x1 - x2;
            float dx2 = x3 - x2;
            float dy1 = y1 - y2;
            float dy2 = y3 - y2;
            float denominator = dx1*dy2 - dx2*dy1;
            float a13 = (dx3*dy2 - dx2*dy3)/denominator;
            float a23 = (dx1*dy3 - dx3*dy1)/denominator;
            return new PerspectiveTransform(x1 - x0 + a13*x1, x3 - x0 + a23*x3, x0,
                                            y1 - y0 + a13*y1, y3 - y0 + a23*y3, y0,
                                            a13, a23, 1.0f);
         }
      }

      public static PerspectiveTransform quadrilateralToSquare(float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
      {
         // Here, the adjoint serves as the inverse:
         return squareToQuadrilateral(x0, y0, x1, y1, x2, y2, x3, y3).buildAdjoint();
      }

      internal PerspectiveTransform buildAdjoint()
      {
         // Adjoint is the transpose of the cofactor matrix:
         return new PerspectiveTransform(a22 * a33 - a23 * a32, 
            a23 * a31 - a21 * a33, 
            a21 * a32 - a22 * a31, 
            a13 * a32 - a12 * a33, 
            a11 * a33 - a13 * a31, 
            a12 * a31 - a11 * a32, 
            a12 * a23 - a13 * a22, 
            a13 * a21 - a11 * a23, 
            a11 * a22 - a12 * a21);
      }

      internal PerspectiveTransform times(PerspectiveTransform other)
      {
         return new PerspectiveTransform(a11 * other.a11 + a21 * other.a12 + a31 * other.a13, 
            a11 * other.a21 + a21 * other.a22 + a31 * other.a23, 
            a11 * other.a31 + a21 * other.a32 + a31 * other.a33, 
            a12 * other.a11 + a22 * other.a12 + a32 * other.a13, 
            a12 * other.a21 + a22 * other.a22 + a32 * other.a23, 
            a12 * other.a31 + a22 * other.a32 + a32 * other.a33, 
            a13 * other.a11 + a23 * other.a12 + a33 * other.a13, 
            a13 * other.a21 + a23 * other.a22 + a33 * other.a23, 
            a13 * other.a31 + a23 * other.a32 + a33 * other.a33);
      }
   }
}