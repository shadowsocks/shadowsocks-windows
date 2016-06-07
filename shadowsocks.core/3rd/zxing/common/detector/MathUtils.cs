/*
 * Copyright 2012 ZXing authors
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

namespace ZXing.Common.Detector
{
   public static class MathUtils
   {
      /// <summary>
      /// Ends up being a bit faster than {@link Math#round(float)}. This merely rounds its
      /// argument to the nearest int, where x.5 rounds up to x+1.
      /// </summary>
      /// <param name="d">The d.</param>
      /// <returns></returns>
      public static int round(float d)
      {
         return (int)(d + 0.5f);
      }

      public static float distance(float aX, float aY, float bX, float bY)
      {
         float xDiff = aX - bX;
         float yDiff = aY - bY;
         return (float)Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
      }

      public static float distance(int aX, int aY, int bX, int bY)
      {
         int xDiff = aX - bX;
         int yDiff = aY - bY;
         return (float)Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
      }
   }
}
