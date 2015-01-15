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
using System.Collections.Generic;

namespace ZXing
{
   /// <summary>
   /// Encapsulates the result of decoding a barcode within an image.
   /// </summary>
   public sealed class Result
   {
      /// <returns>raw text encoded by the barcode, if applicable, otherwise <code>null</code></returns>
      public String Text { get; private set; }

      /// <returns>raw bytes encoded by the barcode, if applicable, otherwise <code>null</code></returns>
      public byte[] RawBytes { get; private set; }

      /// <returns>
      /// points related to the barcode in the image. These are typically points
      /// identifying finder patterns or the corners of the barcode. The exact meaning is
      /// specific to the type of barcode that was decoded.
      /// </returns>
      public ResultPoint[] ResultPoints { get; private set; }

      /// <returns>{@link BarcodeFormat} representing the format of the barcode that was decoded</returns>
      public BarcodeFormat BarcodeFormat { get; private set; }

      /// <returns>
      /// {@link Hashtable} mapping {@link ResultMetadataType} keys to values. May be
      /// <code>null</code>. This contains optional metadata about what was detected about the barcode,
      /// like orientation.
      /// </returns>
      public IDictionary<ResultMetadataType, object> ResultMetadata { get; private set; }

      /// <summary>
      /// Gets the timestamp.
      /// </summary>
      public long Timestamp { get; private set; }

      /// <summary>
      /// Initializes a new instance of the <see cref="Result"/> class.
      /// </summary>
      /// <param name="text">The text.</param>
      /// <param name="rawBytes">The raw bytes.</param>
      /// <param name="resultPoints">The result points.</param>
      /// <param name="format">The format.</param>
      public Result(String text,
                    byte[] rawBytes,
                    ResultPoint[] resultPoints,
                    BarcodeFormat format)
         : this(text, rawBytes, resultPoints, format, DateTime.Now.Ticks)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="Result"/> class.
      /// </summary>
      /// <param name="text">The text.</param>
      /// <param name="rawBytes">The raw bytes.</param>
      /// <param name="resultPoints">The result points.</param>
      /// <param name="format">The format.</param>
      /// <param name="timestamp">The timestamp.</param>
      public Result(String text, byte[] rawBytes, ResultPoint[] resultPoints, BarcodeFormat format, long timestamp)
      {
         if (text == null && rawBytes == null)
         {
            throw new ArgumentException("Text and bytes are null");
         }
         Text = text;
         RawBytes = rawBytes;
         ResultPoints = resultPoints;
         BarcodeFormat = format;
         ResultMetadata = null;
         Timestamp = timestamp;
      }

      /// <summary>
      /// Adds one metadata to the result
      /// </summary>
      /// <param name="type">The type.</param>
      /// <param name="value">The value.</param>
      public void putMetadata(ResultMetadataType type, Object value)
      {
         if (ResultMetadata == null)
         {
            ResultMetadata = new Dictionary<ResultMetadataType, object>();
         }
         ResultMetadata[type] = value;
      }

      /// <summary>
      /// Adds a list of metadata to the result
      /// </summary>
      /// <param name="metadata">The metadata.</param>
      public void putAllMetadata(IDictionary<ResultMetadataType, object> metadata)
      {
         if (metadata != null)
         {
            if (ResultMetadata == null)
            {
               ResultMetadata = metadata;
            }
            else
            {
               foreach (var entry in metadata)
                  ResultMetadata[entry.Key] = entry.Value;
            }
         }
      }

      /// <summary>
      /// Adds the result points.
      /// </summary>
      /// <param name="newPoints">The new points.</param>
      public void addResultPoints(ResultPoint[] newPoints)
      {
         var oldPoints = ResultPoints;
         if (oldPoints == null)
         {
            ResultPoints = newPoints;
         }
         else if (newPoints != null && newPoints.Length > 0)
         {
            var allPoints = new ResultPoint[oldPoints.Length + newPoints.Length];
            Array.Copy(oldPoints, 0, allPoints, 0, oldPoints.Length);
            Array.Copy(newPoints, 0, allPoints, oldPoints.Length, newPoints.Length);
            ResultPoints = allPoints;
         }
      }

      /// <summary>
      /// Returns a <see cref="System.String"/> that represents this instance.
      /// </summary>
      /// <returns>
      /// A <see cref="System.String"/> that represents this instance.
      /// </returns>
      public override String ToString()
      {
         if (Text == null)
         {
            return "[" + RawBytes.Length + " bytes]";
         }
         return Text;
      }
   }
}