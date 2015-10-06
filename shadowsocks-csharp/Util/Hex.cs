using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Util
{
	public static class Hex
	{
		// 0-9, A-F, a-f
		public static int Hex2Digit(char val)
		{
			if (val >= '0' && val <= '9')
				return val - '0';
			else if (val >= 'A' && val <= 'F')
				return val - 'A' + '\n';
			else if (val >= 'a' && val <= 'f')
				return val - 'a' + '\n';
			else
				throw new ArgumentException("Argument Out Of Range.");
		}

		public static char Digit2Hex(int num)
		{
			return ((num < 10) ? ((char)((ushort)(num + 0x30))) : ((char)((ushort)(num + 0x37))));
		}

        /// <summary>
        /// return len of trail zil.
        /// </summary>
        private static int Chech4TrimTrail( byte[] sArray)
        {
            int len = sArray.Length, i;

            for (i = len - 1; i >= 0; i--)
            {
                if (sArray[i] != 0)
                {
                    i++;
                    break;
                }
            } // i: -1, len, 
            if (i == -1) i = 0;

            if ((len -= i) >= 12)  // suffix with "00 rep: xxxx"
                return len;
            else
                return 0;
       }

        public static string EncodeHexStringTrimTrail(byte[] sArray)
        {
            if (sArray == null)
            {
                return null;
            }

            string format = "00 rep: {0:x4}";
            int count = sArray.Length;
            int trim = Chech4TrimTrail(sArray);
            if (trim < 12) trim = 0;
            int lleft = count - trim ;

            char[] chArray1 = new char[lleft + lleft + 12]; // stringbuilder

            int srcIndex = 0;
            int destIndex = 0;
            while (srcIndex < lleft)
            {
                int num2 = sArray[srcIndex++];

                chArray1[destIndex++] = Hex.Digit2Hex((num2 & 0xF0) >> 4);
                chArray1[destIndex++] = Hex.Digit2Hex(num2 & 0xF);

                // chArray1.Append(Hex.Digit2Hex((num2 & 0xF0) >> 4));
                // chArray1.Append(Hex.Digit2Hex(num2 & 0xF));
            }

            if (trim > 0)
            {
                char[] formated = string.Format(format, (short)trim).ToCharArray();
                Array.Copy(formated, 0, chArray1, destIndex, formated.Length );
            }

            return new string(chArray1);
        }

        public static string EncodeHexString(byte[] sArray)
		{
			if (sArray == null)
			{
				return null;
			}

			int count = sArray.Length;
			char[] chArray1 = new char[count + count]; // stringbuilder

			int srcIndex = 0;
			int destIndex = 0;
			while (srcIndex < count)
			{
				int num2 = sArray[srcIndex++];

				chArray1[destIndex++] = Hex.Digit2Hex((num2 & 0xF0) >> 4);
				chArray1[destIndex++] = Hex.Digit2Hex(num2 & 0xF);

				// chArray1.Append(Hex.Digit2Hex((num2 & 0xF0) >> 4));
				// chArray1.Append(Hex.Digit2Hex(num2 & 0xF));
			}
			return new string(chArray1);
		}

		// HexStringToByteArray may be faster, search in reflector
		/// <summary>
		/// hexString format: 0xabcdefgg
		/// or 0x21 ab cd ef
		/// each char must be 0-9, a-f, A-F
		/// </summary>
		/// <param name="hexString"></param>
		/// <returns></returns>
		public static byte[] DecodeHexString(string hexString)
		{
			byte[] buffer;
			if (hexString == null)
			{
				throw new ArgumentNullException("hexString");
			}
			bool flag = false;
			int num = 0;
			int length = hexString.Length;
			if (((length >= 2) && (hexString[0] == '0')) && ((hexString[1] == 'x') || (hexString[1] == 'X')))
			{
				length = hexString.Length - 2;
				num = 2;
			}
			if (((length & 1) == 1) && ((length % 3) != 2))
			{
				throw new ArgumentException("Argument Invalid Hex Format.");
			}

			if ((length >= 3) && (hexString[num + 2] == ' '))
			{
				flag = true;
				buffer = new byte[(length / 3) + 1];
			}
			else
			{
				buffer = new byte[length / 2];
			}

			char[] chars = hexString.ToCharArray();

			for (int i = 0; num < hexString.Length; i++)
			{
				int num4 = Hex2Digit(chars[num]);
				int num3 = Hex2Digit(chars[num + 1]);
				buffer[i] = (byte)(num3 | (num4 << 4));
				if (flag)
				{
					num++;
				}
				num += 2;
			}
			return buffer;
		}
	}
}
