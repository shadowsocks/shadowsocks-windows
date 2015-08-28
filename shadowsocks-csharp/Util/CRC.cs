using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Util
{
    class CRC32
    {
        static protected ulong[] Crc32Table;
        //生成CRC32码表  
        static public void CreateCRC32Table()
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
        }

        //获取字符串的CRC32校验值
        static public ulong CalcCRC32(byte[] input, int len, ulong value = 0xffffffff)
        {
            //生成码表
            if (Crc32Table == null)
                CreateCRC32Table();
            byte[] buffer = input;
            for (int i = 0; i < len; i++)
            {
                value = (value >> 8) ^ Crc32Table[(value & 0xFF) ^ buffer[i]];
            }
            return value ^ 0xffffffff;
        }

        public static void SetCRC32(byte[] buffer)
        {
            ulong crc = ~CalcCRC32(buffer, buffer.Length - 4);
            buffer[buffer.Length - 1] = (byte)(crc >> 24);
            buffer[buffer.Length - 2] = (byte)(crc >> 16);
            buffer[buffer.Length - 3] = (byte)(crc >> 8);
            buffer[buffer.Length - 4] = (byte)(crc);
        }

        public byte[] CheckCRC32(byte[] buffer)
        {
            ulong crc = ~CalcCRC32(buffer, buffer.Length);
            if (crc != 0xffffffff00000000u)
                return null;
            byte[] ret = new byte[buffer.Length - 4];
            Array.Copy(buffer, ret, buffer.Length - 4);
            return ret;
        }
    }
}
