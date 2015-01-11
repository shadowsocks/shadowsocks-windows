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
using System.Collections.Generic;

namespace ZXing.Common
{
   /// <summary> Encapsulates a Character Set ECI, according to "Extended Channel Interpretations" 5.3.1.1
   /// of ISO 18004.
   /// 
   /// </summary>
   /// <author>Sean Owen</author>
   public sealed class CharacterSetECI : ECI
   {
      internal static readonly IDictionary<int, CharacterSetECI> VALUE_TO_ECI;
      internal static readonly IDictionary<string, CharacterSetECI> NAME_TO_ECI;

      private readonly String encodingName;

      public String EncodingName
      {
         get
         {
            return encodingName;
         }

      }

      static CharacterSetECI()
      {
         VALUE_TO_ECI = new Dictionary<int, CharacterSetECI>();
         NAME_TO_ECI = new Dictionary<string, CharacterSetECI>();
         // TODO figure out if these values are even right!
         addCharacterSet(0, "CP437");
         addCharacterSet(1, new[] { "ISO-8859-1", "ISO8859_1" });
         addCharacterSet(2, "CP437");
         addCharacterSet(3, new[] { "ISO-8859-1", "ISO8859_1" });
         addCharacterSet(4, new[] { "ISO-8859-2", "ISO8859_2" });
         addCharacterSet(5, new[] { "ISO-8859-3", "ISO8859_3" });
         addCharacterSet(6, new[] { "ISO-8859-4", "ISO8859_4" });
         addCharacterSet(7, new[] { "ISO-8859-5", "ISO8859_5" });
         addCharacterSet(8, new[] { "ISO-8859-6", "ISO8859_6" });
         addCharacterSet(9, new[] { "ISO-8859-7", "ISO8859_7" });
         addCharacterSet(10, new[] { "ISO-8859-8", "ISO8859_8" });
         addCharacterSet(11, new[] { "ISO-8859-9", "ISO8859_9" });
         addCharacterSet(12, new[] { "ISO-8859-4", "ISO-8859-10", "ISO8859_10" }); // use ISO-8859-4 because ISO-8859-16 isn't supported
         addCharacterSet(13, new[] { "ISO-8859-11", "ISO8859_11" });
         addCharacterSet(15, new[] { "ISO-8859-13", "ISO8859_13" });
         addCharacterSet(16, new[] { "ISO-8859-1", "ISO-8859-14", "ISO8859_14" }); // use ISO-8859-1 because ISO-8859-16 isn't supported
         addCharacterSet(17, new[] { "ISO-8859-15", "ISO8859_15" });
         addCharacterSet(18, new[] { "ISO-8859-3", "ISO-8859-16", "ISO8859_16" }); // use ISO-8859-3 because ISO-8859-16 isn't supported
         addCharacterSet(20, new[] { "SJIS", "Shift_JIS" });
         addCharacterSet(21, new[] { "WINDOWS-1250", "CP1250" });
         addCharacterSet(22, new[] { "WINDOWS-1251", "CP1251" });
         addCharacterSet(23, new[] { "WINDOWS-1252", "CP1252" });
         addCharacterSet(24, new[] { "WINDOWS-1256", "CP1256" });
         addCharacterSet(25, new[] { "UTF-16BE", "UNICODEBIG" });
         addCharacterSet(26, new[] { "UTF-8", "UTF8" });
         addCharacterSet(27, "US-ASCII");
         addCharacterSet(170, "US-ASCII");
         addCharacterSet(28, "BIG5");
         addCharacterSet(29, new[] { "GB18030", "GB2312", "EUC_CN", "GBK" });
         addCharacterSet(30, new[] { "EUC-KR", "EUC_KR" });
      }

      private CharacterSetECI(int value, String encodingName)
         : base(value)
      {
         this.encodingName = encodingName;
      }

      private static void addCharacterSet(int value, String encodingName)
      {
         var eci = new CharacterSetECI(value, encodingName);
         VALUE_TO_ECI[value] = eci; // can't use valueOf
         NAME_TO_ECI[encodingName] = eci;
      }

      private static void addCharacterSet(int value, String[] encodingNames)
      {
         var eci = new CharacterSetECI(value, encodingNames[0]);
         VALUE_TO_ECI[value] = eci; // can't use valueOf
         foreach (string t in encodingNames)
         {
            NAME_TO_ECI[t] = eci;
         }
      }

      /// <param name="value">character set ECI value
      /// </param>
      /// <returns> {@link CharacterSetECI} representing ECI of given value, or null if it is legal but
      /// unsupported
      /// </returns>
      /// <throws>  IllegalArgumentException if ECI value is invalid </throws>
      public static CharacterSetECI getCharacterSetECIByValue(int value)
      {
         if (value < 0 || value >= 900)
         {
            return null;
         }
         return VALUE_TO_ECI[value];
      }

      /// <param name="name">character set ECI encoding name
      /// </param>
      /// <returns> {@link CharacterSetECI} representing ECI for character encoding, or null if it is legal
      /// but unsupported
      /// </returns>
      public static CharacterSetECI getCharacterSetECIByName(String name)
      {
         return NAME_TO_ECI[name.ToUpper()];
      }
   }
}