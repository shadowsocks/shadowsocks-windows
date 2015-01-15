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

namespace ZXing.QrCode.Internal
{
   /// <summary>
   /// <p>Encapsulates information about finder patterns in an image, including the location of
   /// the three finder patterns, and their estimated module size.</p>
   /// </summary>
   /// <author>Sean Owen</author>
   public sealed class FinderPatternInfo
   {
      private readonly FinderPattern bottomLeft;
      private readonly FinderPattern topLeft;
      private readonly FinderPattern topRight;

      /// <summary>
      /// Initializes a new instance of the <see cref="FinderPatternInfo"/> class.
      /// </summary>
      /// <param name="patternCenters">The pattern centers.</param>
      public FinderPatternInfo(FinderPattern[] patternCenters)
      {
         this.bottomLeft = patternCenters[0];
         this.topLeft = patternCenters[1];
         this.topRight = patternCenters[2];
      }

      /// <summary>
      /// Gets the bottom left.
      /// </summary>
      public FinderPattern BottomLeft
      {
         get
         {
            return bottomLeft;
         }
      }

      /// <summary>
      /// Gets the top left.
      /// </summary>
      public FinderPattern TopLeft
      {
         get
         {
            return topLeft;
         }
      }

      /// <summary>
      /// Gets the top right.
      /// </summary>
      public FinderPattern TopRight
      {
         get
         {
            return topRight;
         }
      }
   }
}