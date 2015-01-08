/*
* Copyright 2008 ZXing authors
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

namespace ZXing.QrCode.Internal
{
   /// <author>satorux@google.com (Satoru Takabayashi) - creator</author>
   /// <author>dswitkin@google.com (Daniel Switkin) - ported from C++</author>
   public class QRCode
   {
      /// <summary>
      /// 
      /// </summary>
      public static int NUM_MASK_PATTERNS = 8;

      /// <summary>
      /// Initializes a new instance of the <see cref="QRCode"/> class.
      /// </summary>
      public QRCode()
      {
         MaskPattern = -1;
      }

      /// <summary>
      /// Gets or sets the mode.
      /// </summary>
      /// <value>
      /// The mode.
      /// </value>
      public Mode Mode { get; set; }

      /// <summary>
      /// Gets or sets the EC level.
      /// </summary>
      /// <value>
      /// The EC level.
      /// </value>
      public ErrorCorrectionLevel ECLevel { get; set; }

      /// <summary>
      /// Gets or sets the version.
      /// </summary>
      /// <value>
      /// The version.
      /// </value>
      public Version Version { get; set; }

      /// <summary>
      /// Gets or sets the mask pattern.
      /// </summary>
      /// <value>
      /// The mask pattern.
      /// </value>
      public int MaskPattern { get; set; }

      /// <summary>
      /// Gets or sets the matrix.
      /// </summary>
      /// <value>
      /// The matrix.
      /// </value>
      public ByteMatrix Matrix { get; set; }

      /// <summary>
      /// Check if "mask_pattern" is valid.
      /// </summary>
      /// <param name="maskPattern">The mask pattern.</param>
      /// <returns>
      ///   <c>true</c> if [is valid mask pattern] [the specified mask pattern]; otherwise, <c>false</c>.
      /// </returns>
      public static bool isValidMaskPattern(int maskPattern)
      {
         return maskPattern >= 0 && maskPattern < NUM_MASK_PATTERNS;
      }
   }
}