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

namespace ZXing.QrCode.Internal
{
   internal sealed class BlockPair
   {
      private readonly byte[] dataBytes;
      private readonly byte[] errorCorrectionBytes;

      public BlockPair(byte[] data, byte[] errorCorrection)
      {
         dataBytes = data;
         errorCorrectionBytes = errorCorrection;
      }

      public byte[] DataBytes
      {
         get { return dataBytes; }
      }

      public byte[] ErrorCorrectionBytes
      {
         get { return errorCorrectionBytes; }
      }
   }
}