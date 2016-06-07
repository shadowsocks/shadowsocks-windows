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

   /// <author>  Sean Owen
   /// </author>
   /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source 
   /// </author>
   public sealed class DefaultGridSampler : GridSampler
   {
      public override BitMatrix sampleGrid(BitMatrix image, int dimensionX, int dimensionY, float p1ToX, float p1ToY, float p2ToX, float p2ToY, float p3ToX, float p3ToY, float p4ToX, float p4ToY, float p1FromX, float p1FromY, float p2FromX, float p2FromY, float p3FromX, float p3FromY, float p4FromX, float p4FromY)
      {
         PerspectiveTransform transform = PerspectiveTransform.quadrilateralToQuadrilateral(
            p1ToX, p1ToY, p2ToX, p2ToY, p3ToX, p3ToY, p4ToX, p4ToY, 
            p1FromX, p1FromY, p2FromX, p2FromY, p3FromX, p3FromY, p4FromX, p4FromY);
         return sampleGrid(image, dimensionX, dimensionY, transform);
      }

      public override BitMatrix sampleGrid(BitMatrix image, int dimensionX, int dimensionY, PerspectiveTransform transform)
      {
         if (dimensionX <= 0 || dimensionY <= 0)
         {
            return null;
         }
         BitMatrix bits = new BitMatrix(dimensionX, dimensionY);
         float[] points = new float[dimensionX << 1];
         for (int y = 0; y < dimensionY; y++)
         {
            int max = points.Length;
            //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
            float iValue = (float)y + 0.5f;
            for (int x = 0; x < max; x += 2)
            {
               //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
               points[x] = (float)(x >> 1) + 0.5f;
               points[x + 1] = iValue;
            }
            transform.transformPoints(points);
            // Quick check to see if points transformed to something inside the image;
            // sufficient to check the endpoints
            if (!checkAndNudgePoints(image, points))
               return null;
            try
            {
               for (int x = 0; x < max; x += 2)
               {
                  //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
                  bits[x >> 1, y] = image[(int)points[x], (int)points[x + 1]];
               }
            }
            catch (System.IndexOutOfRangeException)
            {
               // This feels wrong, but, sometimes if the finder patterns are misidentified, the resulting
               // transform gets "twisted" such that it maps a straight line of points to a set of points
               // whose endpoints are in bounds, but others are not. There is probably some mathematical
               // way to detect this about the transformation that I don't know yet.
               // This results in an ugly runtime exception despite our clever checks above -- can't have
               // that. We could check each point's coordinates but that feels duplicative. We settle for
               // catching and wrapping ArrayIndexOutOfBoundsException.
               return null;
            }
         }
         return bits;
      }
   }
}