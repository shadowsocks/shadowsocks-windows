using System;
using System.Collections.Generic;
using Shadowsocks.Controller;

namespace Shadowsocks.Obfs
{
    public class VerifySimpleObfs : ObfsBase
    {
        public VerifySimpleObfs(string method)
            : base(method)
        {
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
                {"verify_simple", new int[]{}},
        };

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
        }

        private const int RecvBufferSize = 65536 * 2;

        private byte[] recv_buf = new byte[RecvBufferSize];
        private int recv_buf_len = 0;
        private Random random = new Random();

        public void PackData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            int rand_len = random.Next(16) + 1;
            outlength = rand_len + datalength + 6;
            outdata[0] = (byte)(outlength >> 8);
            outdata[1] = (byte)(outlength);
            outdata[2] = (byte)(rand_len);
            Array.Copy(data, 0, outdata, rand_len + 2, datalength);
            Util.CRC32.SetCRC32(outdata, outlength);
        }

        public override byte[] ClientPreEncrypt(byte[] plaindata, int datalength, out int outlength)
        {
            byte[] outdata = new byte[datalength + datalength / 10 + 32];
            byte[] packdata = new byte[9000];
            byte[] data = plaindata;
            outlength = 0;
            const int unit_len = 8100;
            while (datalength > unit_len)
            {
                int outlen;
                PackData(data, unit_len, packdata, out outlen);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
                datalength -= unit_len;
                byte[] newdata = new byte[datalength];
                Array.Copy(data, unit_len, newdata, 0, newdata.Length);
                data = newdata;
            }
            if (datalength > 0)
            {
                int outlen;
                PackData(data, datalength, packdata, out outlen);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
            }
            return outdata;
        }

        public override byte[] ClientEncode(byte[] encryptdata, int datalength, out int outlength)
        {
            outlength = datalength;
            return encryptdata;
        }
        public override byte[] ClientDecode(byte[] encryptdata, int datalength, out int outlength, out bool needsendback)
        {
            outlength = datalength;
            needsendback = false;
            return encryptdata;
        }
        public override byte[] ClientPostDecrypt(byte[] plaindata, int datalength, out int outlength)
        {
            byte[] outdata = new byte[recv_buf_len + datalength];
            Array.Copy(plaindata, 0, recv_buf, recv_buf_len, datalength);
            recv_buf_len += datalength;
            outlength = 0;
            while (recv_buf_len > 2)
            {
                int len = (recv_buf[0] << 8) + recv_buf[1];
                if (len >= 8192)
                {
                    throw new Exception("ClientPostDecrypt data error");
                }
                if (len > recv_buf_len)
                    break;

                if (Util.CRC32.CheckCRC32(recv_buf, len))
                {
                    int pos = recv_buf[2] + 2;
                    int outlen = len - pos - 4;
                    Array.Copy(recv_buf, pos, outdata, outlength, outlen);
                    outlength += outlen;
                    recv_buf_len -= len;
                    Array.Copy(recv_buf, len, recv_buf, 0, recv_buf_len);
                    //len = (recv_buf[0] << 8) + recv_buf[1];
                }
                else
                {
                    throw new Exception("ClientPostDecrypt data uncorrect CRC32");
                }
            }
            return outdata;
        }

        public override void Dispose()
        {
        }
    }
    public class VerifyDeflateObfs : ObfsBase
    {
        public VerifyDeflateObfs(string method)
            : base(method)
        {
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
                {"verify_deflate", new int[]{}},
        };

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
        }

        private const int RecvBufferSize = 65536 * 2;

        private byte[] recv_buf = new byte[RecvBufferSize];
        private int recv_buf_len = 0;
        private Random random = new Random();

        public void PackData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            int outlen;
            byte[] comdata = FileManager.DeflateCompress(data, 0, datalength, out outlen);
            outlength = outlen + 2 + 4;
            outdata[0] = (byte)(outlength >> 8);
            outdata[1] = (byte)(outlength);
            Array.Copy(comdata, 0, outdata, 2, outlen);
            ulong alder = Util.Alder32.CalcAlder32(data, datalength);
            outdata[outlength - 4] = (byte)(alder >> 24);
            outdata[outlength - 3] = (byte)(alder >> 16);
            outdata[outlength - 2] = (byte)(alder >> 8);
            outdata[outlength - 1] = (byte)(alder);
        }

        public override byte[] ClientPreEncrypt(byte[] plaindata, int datalength, out int outlength)
        {
            byte[] outdata = new byte[datalength + datalength / 10 + 32];
            byte[] packdata = new byte[32768];
            byte[] data = plaindata;
            outlength = 0;
            const int unit_len = 32700;
            while (datalength > unit_len)
            {
                int outlen;
                PackData(data, unit_len, packdata, out outlen);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
                datalength -= unit_len;
                byte[] newdata = new byte[datalength];
                Array.Copy(data, unit_len, newdata, 0, newdata.Length);
                data = newdata;
            }
            if (datalength > 0)
            {
                int outlen;
                PackData(data, datalength, packdata, out outlen);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
            }
            return outdata;
        }

        public override byte[] ClientEncode(byte[] encryptdata, int datalength, out int outlength)
        {
            outlength = datalength;
            return encryptdata;
        }
        public override byte[] ClientDecode(byte[] encryptdata, int datalength, out int outlength, out bool needsendback)
        {
            outlength = datalength;
            needsendback = false;
            return encryptdata;
        }
        public override byte[] ClientPostDecrypt(byte[] plaindata, int datalength, out int outlength)
        {
            byte[] outdata = new byte[recv_buf_len + datalength * 2 + 16];
            Array.Copy(plaindata, 0, recv_buf, recv_buf_len, datalength);
            recv_buf_len += datalength;
            outlength = 0;
            while (recv_buf_len > 2)
            {
                int len = (recv_buf[0] << 8) + recv_buf[1];
                if (len >= 32768)
                {
                    throw new Exception("ClientPostDecrypt data error");
                }
                if (len > recv_buf_len)
                    break;

                int outlen;
                byte[] buf = FileManager.DeflateDecompress(recv_buf, 2, len - 6, out outlen);
                if (buf != null)
                {
                    ulong alder = Util.Alder32.CalcAlder32(buf, outlen);
                    if (recv_buf[len - 4] == (byte)(alder >> 24)
                        && recv_buf[len - 3] == (byte)(alder >> 16)
                        && recv_buf[len - 2] == (byte)(alder >> 8)
                        && recv_buf[len - 1] == (byte)(alder))
                    {
                        //pass
                    }
                    else
                    {
                        throw new Exception("ClientPostDecrypt data decompress ERROR");
                    }
                    while (outlength + outlen > outdata.Length)
                    {
                        byte[] newout = new byte[outdata.Length * 2 + 1024];
                        outdata.CopyTo(newout, 0);
                        outdata = newout;
                    }
                    Array.Copy(buf, 0, outdata, outlength, outlen);
                    outlength += outlen;
                    recv_buf_len -= len;
                    Array.Copy(recv_buf, len, recv_buf, 0, recv_buf_len);
                    len = (recv_buf[0] << 8) + recv_buf[1];
                }
                else
                {
                    throw new Exception("ClientPostDecrypt data decompress ERROR");
                }
            }
            return outdata;
        }

        public override void Dispose()
        {
        }
    }
}
