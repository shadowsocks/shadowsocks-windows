using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Util
{
    class CRC32
    {
        protected static ulong[] Crc32Table = CreateCRC32Table();
        //生成CRC32码表  
        public static ulong[] CreateCRC32Table()
        {
            ulong Crc;
            Crc32Table = new ulong[256];
            int i, j;
            for (i = 0; i < 256; i++)
            {
                Crc = (ulong)i;
                for (j = 8; j > 0; j--)
                {
                    if ((Crc & 1) == 1)
                        Crc = (Crc >> 1) ^ 0xEDB88320;
                    else
                        Crc >>= 1;
                }
                Crc32Table[i] = Crc;
            }
            return Crc32Table;
        }

        //获取字符串的CRC32校验值
        public static ulong CalcCRC32(byte[] input, int len, ulong value = 0xffffffff)
        {
            return CalcCRC32(input, 0, len, value);
        }
        public static ulong CalcCRC32(byte[] input, int index, int len, ulong value = 0xffffffff)
        {
            byte[] buffer = input;
            for (int i = index; i < len; i++)
            {
                value = (value >> 8) ^ Crc32Table[(value & 0xFF) ^ buffer[i]];
            }
            return value ^ 0xffffffff;
        }

        public static void SetCRC32(byte[] buffer)
        {
            SetCRC32(buffer, 0, buffer.Length);
        }

        public static void SetCRC32(byte[] buffer, int length)
        {
            SetCRC32(buffer, 0, length);
        }
        public static void SetCRC32(byte[] buffer, int index, int length)
        {
            ulong crc = ~CalcCRC32(buffer, index, length - 4);
            buffer[length - 1] = (byte)(crc >> 24);
            buffer[length - 2] = (byte)(crc >> 16);
            buffer[length - 3] = (byte)(crc >> 8);
            buffer[length - 4] = (byte)(crc);
        }

        public static bool CheckCRC32(byte[] buffer, int length)
        {
            ulong crc = CalcCRC32(buffer, length);
            if (crc != 0xffffffffu)
                return false;
            return true;
        }
    }
    class Adler32
    {
        public static ulong CalcAdler32(byte[] input, int len)
        {
            ulong a = 1;
            ulong b = 0;
            for (int i = 0; i < len; i++)
            {
                a += input[i];
                b += a;
            }
            a %= 65521;
            b %= 65521;
            return (b << 16) + a;
        }

        public static bool CheckAdler32(byte[] input, int len)
        {
            ulong adler32 = CalcAdler32(input, len - 4);
            int checksum = (input[len - 1] << 24) | (input[len - 2] << 16) | (input[len - 3] << 8) | input[len - 4];
            return (int)adler32 == checksum;
        }

        public static bool CheckAdler32(byte[] input, int len, uint xor)
        {
            ulong adler32 = CalcAdler32(input, len - 4) ^ xor;
            int checksum = (input[len - 1] << 24) | (input[len - 2] << 16) | (input[len - 3] << 8) | input[len - 4];
            return (int)adler32 == checksum;
        }
    }
}
