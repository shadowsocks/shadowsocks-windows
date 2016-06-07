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

namespace ZXing.QrCode.Internal
{
   /// <summary>
   /// <p>Encapsulates a finder pattern, which are the three square patterns found in
   /// the corners of QR Codes. It also encapsulates a count of similar finder patterns,
   /// as a convenience to the finder's bookkeeping.</p>
   /// </summary>
   /// <author>Sean Owen</author>
   public sealed class FinderPattern : ResultPoint
   {
      private readonly float estimatedModuleSize;
      private int count;

      internal FinderPattern(float posX, float posY, float estimatedModuleSize)
         : this(posX, posY, estimatedModuleSize, 1)
      {
         this.estimatedModuleSize = estimatedModuleSize;
         this.count = 1;
      }

      internal FinderPattern(float posX, float posY, float estimatedModuleSize, int count)
         : base(posX, posY)
      {
         this.estimatedModuleSize = estimatedModuleSize;
         this.count = count;
      }

      /// <summary>
      /// Gets the size of the estimated module.
      /// </summary>
      /// <value>
      /// The size of the estimated module.
      /// </value>
      public float EstimatedModuleSize
      {
         get
         {
            return estimatedModuleSize;
         }
      }

      internal int Count
      {
         get
         {
            return count;
         }
      }

      /*
      internal void incrementCount()
      {
         this.count++;
      }
      */

      /// <summary> <p>Determines if this finder pattern "about equals" a finder pattern at the stated
      /// position and size -- meaning, it is at nearly the same center with nearly the same size.</p>
      /// </summary>
      internal bool aboutEquals(float moduleSize, float i, float j)
      {
         if (Math.Abs(i - Y) <= moduleSize && Math.Abs(j - X) <= moduleSize)
         {
            float moduleSizeDiff = Math.Abs(moduleSize - estimatedModuleSize);
            return moduleSizeDiff <= 1.0f || moduleSizeDiff <= estimatedModuleSize;

         }
         return false;
      }

      /// <summary>
      /// Combines this object's current estimate of a finder pattern position and module size
      /// with a new estimate. It returns a new {@code FinderPattern} containing a weighted average
      /// based on count.
      /// </summary>
      /// <param name="i">The i.</param>
      /// <param name="j">The j.</param>
      /// <param name="newModuleSize">New size of the module.</param>
      /// <returns></returns>
      internal FinderPattern combineEstimate(float i, float j, float newModuleSize)
      {
         int combinedCount = count + 1;
         float combinedX = (count * X + j) / combinedCount;
         float combinedY = (count * Y + i) / combinedCount;
         float combinedModuleSize = (count * estimatedModuleSize + newModuleSize) / combinedCount;
         return new FinderPattern(combinedX, combinedY, combinedModuleSize, combinedCount);
      }
   }
}