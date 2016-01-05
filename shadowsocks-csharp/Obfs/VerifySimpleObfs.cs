using System;
using System.IO;
using System.Collections.Generic;
using Shadowsocks.Controller;
using System.Security.Cryptography;

namespace Shadowsocks.Obfs
{
    public class VerifyData
    {
    }

    public abstract class VerifySimpleBase : ObfsBase
    {
        public VerifySimpleBase(string method)
            : base(method)
        {
        }

        protected const int RecvBufferSize = 65536 * 2;

        protected byte[] recv_buf = new byte[RecvBufferSize];
        protected int recv_buf_len = 0;
        protected Random random = new Random();

        public override object InitData()
        {
            return new VerifyData();
        }

        public override void SetServerInfo(ServerInfo serverInfo)
        {
            base.SetServerInfo(serverInfo);
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
    }

    public class VerifySimpleObfs : VerifySimpleBase
    {
        public VerifySimpleObfs(string method)
            : base(method)
        {
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
                {"verify_simple", new int[]{1, 0, 1}},
        };

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
        }

        public override Dictionary<string, int[]> GetObfs()
        {
            return _obfs;
        }

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
                if (outdata.Length < outlength + outlen)
                    Array.Resize(ref outdata, (outlength + outlen) * 2);
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
                if (outdata.Length < outlength + outlen)
                    Array.Resize(ref outdata, (outlength + outlen) * 2);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
            }
            return outdata;
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
                if (len >= 8192 || len < 7)
                {
                    throw new ObfsException("ClientPostDecrypt data error");
                }
                if (len > recv_buf_len)
                    break;

                if (Util.CRC32.CheckCRC32(recv_buf, len))
                {
                    int pos = recv_buf[2] + 2;
                    int outlen = len - pos - 4;
                    if (outlength + outlen > outdata.Length)
                    {
                        Array.Resize(ref outdata, (outlength + outlen) * 2);
                    }
                    Array.Copy(recv_buf, pos, outdata, outlength, outlen);
                    outlength += outlen;
                    recv_buf_len -= len;
                    Array.Copy(recv_buf, len, recv_buf, 0, recv_buf_len);
                }
                else
                {
                    throw new ObfsException("ClientPostDecrypt data uncorrect CRC32");
                }
            }
            return outdata;
        }

        public override void Dispose()
        {
        }
    }
    public class VerifyDeflateObfs : VerifySimpleBase
    {
        public VerifyDeflateObfs(string method)
            : base(method)
        {
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
                {"verify_deflate", new int[]{1, 0, 1}},
        };

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
        }

        public override Dictionary<string, int[]> GetObfs()
        {
            return _obfs;
        }

        public void PackData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            int outlen;
            byte[] comdata = FileManager.DeflateCompress(data, 0, datalength, out outlen);
            outlength = outlen + 2 + 4;
            outdata[0] = (byte)(outlength >> 8);
            outdata[1] = (byte)(outlength);
            Array.Copy(comdata, 0, outdata, 2, outlen);
            ulong adler = Util.Adler32.CalcAdler32(data, datalength);
            outdata[outlength - 4] = (byte)(adler >> 24);
            outdata[outlength - 3] = (byte)(adler >> 16);
            outdata[outlength - 2] = (byte)(adler >> 8);
            outdata[outlength - 1] = (byte)(adler);
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
                if (outdata.Length < outlength + outlen)
                    Array.Resize(ref outdata, (outlength + outlen) * 2);
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
                if (outdata.Length < outlength + outlen)
                    Array.Resize(ref outdata, (outlength + outlen) * 2);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
            }
            return outdata;
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
                if (len >= 32768 || len < 6)
                {
                    throw new ObfsException("ClientPostDecrypt data error");
                }
                if (len > recv_buf_len)
                    break;

                int outlen;
                byte[] buf = FileManager.DeflateDecompress(recv_buf, 2, len - 6, out outlen);
                if (buf != null)
                {
                    ulong alder = Util.Adler32.CalcAdler32(buf, outlen);
                    if (recv_buf[len - 4] == (byte)(alder >> 24)
                        && recv_buf[len - 3] == (byte)(alder >> 16)
                        && recv_buf[len - 2] == (byte)(alder >> 8)
                        && recv_buf[len - 1] == (byte)(alder))
                    {
                        //pass
                    }
                    else
                    {
                        throw new ObfsException("ClientPostDecrypt data decompress ERROR");
                    }
                    if (outlength + outlen > outdata.Length)
                    {
                        Array.Resize(ref outdata, (outlength + outlen) * 2);
                    }
                    Array.Copy(buf, 0, outdata, outlength, outlen);
                    outlength += outlen;
                    recv_buf_len -= len;
                    Array.Copy(recv_buf, len, recv_buf, 0, recv_buf_len);
                    len = (recv_buf[0] << 8) + recv_buf[1];
                }
                else
                {
                    throw new ObfsException("ClientPostDecrypt data decompress ERROR");
                }
            }
            return outdata;
        }

        public override void Dispose()
        {
        }
    }

    public class VerifySHA1Obfs : VerifySimpleBase
    {
        public VerifySHA1Obfs(string method)
            : base(method)
        {
            has_sent_header = false;
            //has_recv_header = false;
            pack_id = 0;
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
                {"verify_sha1", new int[]{1, 0, 1}},
        };

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
        }

        public override Dictionary<string, int[]> GetObfs()
        {
            return _obfs;
        }

        private bool has_sent_header;
        //private bool has_recv_header;
        private uint pack_id;

        public void PackData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            byte[] key = new byte[Server.iv.Length + 4];
            Server.iv.CopyTo(key, 0);
            {
                byte[] id = BitConverter.GetBytes(pack_id);
                pack_id += 1;
                Array.Reverse(id);
                id.CopyTo(key, Server.iv.Length);
            }
            Array.Copy(data, 0, outdata, 12, datalength);

            HMACSHA1 sha1 = new HMACSHA1(key);
            byte[] sha1data = sha1.ComputeHash(data, 0, datalength);
            Array.Copy(sha1data, 0, outdata, 2, 10);
            outdata[0] = (byte)(datalength >> 8);
            outdata[1] = (byte)(datalength & 0xff);
            outlength = datalength + 12;
        }
        public void PackAuthData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            byte[] key = new byte[Server.iv.Length + Server.key.Length];
            Server.iv.CopyTo(key, 0);
            Server.key.CopyTo(key, Server.iv.Length);
            Array.Copy(data, 0, outdata, 0, datalength);
            outdata[0] |= 0x10;

            HMACSHA1 sha1 = new HMACSHA1(key);
            byte[] sha1data = sha1.ComputeHash(outdata, 0, datalength);

            Array.Copy(sha1data, 0, outdata, datalength, 10);
            outlength = datalength + 10;
        }

        public override byte[] ClientPreEncrypt(byte[] plaindata, int datalength, out int outlength)
        {
            byte[] outdata = new byte[datalength + datalength / 10 + 32];
            byte[] packdata = new byte[9000];
            byte[] data = plaindata;
            outlength = 0;
            const int unit_len = 8100;
            if (!has_sent_header)
            {
                int headsize = GetHeadSize(plaindata, 30);
                int _datalength = headsize;
                int outlen;
                PackAuthData(data, _datalength, packdata, out outlen);
                has_sent_header = true;
                if (outdata.Length < outlength + outlen)
                    Array.Resize(ref outdata, (outlength + outlen) * 2);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
                datalength -= _datalength;
                byte[] newdata = new byte[datalength];
                Array.Copy(data, _datalength, newdata, 0, newdata.Length);
                data = newdata;
            }
            while (datalength > unit_len)
            {
                int outlen;
                PackData(data, unit_len, packdata, out outlen);
                if (outdata.Length < outlength + outlen)
                    Array.Resize(ref outdata, (outlength + outlen) * 2);
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
                if (outdata.Length < outlength + outlen)
                    Array.Resize(ref outdata, (outlength + outlen) * 2);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
            }
            return outdata;
        }
        public override byte[] ClientPostDecrypt(byte[] plaindata, int datalength, out int outlength)
        {
            outlength = datalength;
            return plaindata;
        }

        public override byte[] ClientUdpPreEncrypt(byte[] plaindata, int datalength, out int outlength)
        {
            byte[] packdata = new byte[datalength + 10];
            outlength = datalength + 10;
            int _datalength = datalength;
            int outlen;
            PackAuthData(plaindata, datalength, packdata, out outlen);
            return packdata;
        }

        public override void Dispose()
        {
        }
    }

}
