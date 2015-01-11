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
   /// <summary> <p>Encapsulates the result of detecting a barcode in an image. This includes the raw
   /// matrix of black/white pixels corresponding to the barcode, and possibly points of interest
   /// in the image, like the location of finder patterns or corners of the barcode in the image.</p>
   /// 
   /// </summary>
   /// <author>  Sean Owen
   /// </author>
   /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source 
   /// </author>
   public class DetectorResult
   {
      public BitMatrix Bits { get; private set; }
      public ResultPoint[] Points { get; private set; }

      public DetectorResult(BitMatrix bits, ResultPoint[] points)
      {
         Bits = bits;
         Points = points;
      }
   }
}