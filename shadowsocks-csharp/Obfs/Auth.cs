using System;
using System.Collections.Generic;
using Shadowsocks.Controller;
using System.Security.Cryptography;

namespace Shadowsocks.Obfs
{
    public class AuthData : VerifyData
    {
        public byte[] clientID;
        public UInt32 connectionID;
    }

    public class AuthSimple : VerifySimpleBase
    {
        public AuthSimple(string method)
            : base(method)
        {
            has_sent_header = false;
            has_recv_header = false;
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
            {"auth_simple", new int[]{1, 0, 1}},
        };

        protected bool has_sent_header;
        protected bool has_recv_header;
        protected static RNGCryptoServiceProvider g_random = new RNGCryptoServiceProvider();

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
        }

        public override Dictionary<string, int[]> GetObfs()
        {
            return _obfs;
        }

        public override object InitData()
        {
            return new AuthData();
        }

        public void PackData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            int rand_len = random.Next(16) + 1;
            outlength = rand_len + datalength + 6;
            Array.Copy(data, 0, outdata, rand_len + 2, datalength);
            outdata[0] = (byte)(outlength >> 8);
            outdata[1] = (byte)(outlength);
            outdata[2] = (byte)(rand_len);
            Util.CRC32.SetCRC32(outdata, outlength);
        }

        public void PackAuthData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            int rand_len = random.Next(250) + 1;
            outlength = rand_len + datalength + 6 + 12;
            AuthData authData = (AuthData)this.Server.data;
            lock (authData)
            {
                if (authData.connectionID > 0xFF000000)
                {
                    authData.clientID = null;
                }
                if (authData.clientID == null)
                {
                    authData.clientID = new byte[4];
                    g_random.GetBytes(authData.clientID);
                    authData.connectionID = (UInt32)random.Next(0x1000000);
                }
                authData.connectionID += 1;
                Array.Copy(authData.clientID, 0, outdata, rand_len + 4 + 2, 4);
                Array.Copy(BitConverter.GetBytes(authData.connectionID), 0, outdata, rand_len + 8 + 2, 4);
            }
            UInt64 utc_time_second = (UInt64)Math.Floor(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
            UInt32 utc_time = (UInt32)(utc_time_second);
            Array.Copy(BitConverter.GetBytes(utc_time), 0, outdata, rand_len + 2, 4);

            Array.Copy(data, 0, outdata, rand_len + 12 + 2, datalength);
            outdata[0] = (byte)(outlength >> 8);
            outdata[1] = (byte)(outlength);
            outdata[2] = (byte)(rand_len);
            Util.CRC32.SetCRC32(outdata, outlength);
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
                int _datalength = Math.Min(random.Next(32) + headsize, datalength);
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

    public class AuthSHA1 : VerifySimpleBase
    {
        public AuthSHA1(string method)
            : base(method)
        {
            has_sent_header = false;
            has_recv_header = false;
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
            {"auth_sha1", new int[]{1, 0, 1}},
        };

        protected bool has_sent_header;
        protected bool has_recv_header;
        protected static RNGCryptoServiceProvider g_random = new RNGCryptoServiceProvider();

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
        }

        public override Dictionary<string, int[]> GetObfs()
        {
            return _obfs;
        }

        public override object InitData()
        {
            return new AuthData();
        }

        public void PackData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            int rand_len = random.Next(16) + 1;
            outlength = rand_len + datalength + 6;
            Array.Copy(data, 0, outdata, rand_len + 2, datalength);
            outdata[0] = (byte)(outlength >> 8);
            outdata[1] = (byte)(outlength);
            outdata[2] = (byte)(rand_len);
            ulong adler = Util.Adler32.CalcAdler32(outdata, outlength - 4);
            BitConverter.GetBytes((uint)adler).CopyTo(outdata, outlength - 4);
        }

        public void PackAuthData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            int rand_len = random.Next(250) + 1;
            int data_offset = rand_len + 4 + 2;
            outlength = data_offset + datalength + 12 + 10;
            AuthData authData = (AuthData)this.Server.data;
            lock (authData)
            {
                if (authData.connectionID > 0xFF000000)
                {
                    authData.clientID = null;
                }
                if (authData.clientID == null)
                {
                    authData.clientID = new byte[4];
                    g_random.GetBytes(authData.clientID);
                    authData.connectionID = (UInt32)random.Next(0x1000000);
                }
                authData.connectionID += 1;
                Array.Copy(authData.clientID, 0, outdata, data_offset + 4, 4);
                Array.Copy(BitConverter.GetBytes(authData.connectionID), 0, outdata, data_offset + 8, 4);
            }
            UInt64 utc_time_second = (UInt64)Math.Floor(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
            UInt32 utc_time = (UInt32)(utc_time_second);
            Array.Copy(BitConverter.GetBytes(utc_time), 0, outdata, data_offset, 4);

            Array.Copy(data, 0, outdata, data_offset + 12, datalength);
            outdata[4] = (byte)(outlength >> 8);
            outdata[5] = (byte)(outlength);
            outdata[6] = (byte)(rand_len);

            ulong crc32 = Util.CRC32.CalcCRC32(Server.key, (int)Server.key.Length);
            BitConverter.GetBytes((uint)crc32).CopyTo(outdata, 0);

            byte[] key = new byte[Server.iv.Length + Server.key.Length];
            Server.iv.CopyTo(key, 0);
            Server.key.CopyTo(key, Server.iv.Length);

            HMACSHA1 sha1 = new HMACSHA1(key);
            byte[] sha1data = sha1.ComputeHash(outdata, 0, outlength - 10);

            Array.Copy(sha1data, 0, outdata, outlength - 10, 10);
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
                int _datalength = Math.Min(random.Next(32) + headsize, datalength);
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

                if (Util.Adler32.CheckAdler32(recv_buf, len))
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
                    throw new ObfsException("ClientPostDecrypt data uncorrect checksum");
                }
            }
            return outdata;
        }

        public override void Dispose()
        {
        }
    }

    public class AuthSHA1V2 : VerifySimpleBase
    {
        public AuthSHA1V2(string method)
            : base(method)
        {
            has_sent_header = false;
            has_recv_header = false;
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
            {"auth_sha1_v2", new int[]{1, 0, 1}},
        };

        protected bool has_sent_header;
        protected bool has_recv_header;
        protected static RNGCryptoServiceProvider g_random = new RNGCryptoServiceProvider();

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
        }

        public override Dictionary<string, int[]> GetObfs()
        {
            return _obfs;
        }

        public override object InitData()
        {
            return new AuthData();
        }

        public void PackData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            int rand_len = datalength >= 1300 ? 1 : datalength > 400 ? random.Next(128) + 1 : random.Next(1024) + 3;
            outlength = rand_len + datalength + 6;
            Array.Copy(data, 0, outdata, rand_len + 2, datalength);
            outdata[0] = (byte)(outlength >> 8);
            outdata[1] = (byte)(outlength);
            if (rand_len < 128)
            {
                outdata[2] = (byte)(rand_len);
            }
            else
            {
                outdata[2] = 0xFF;
                outdata[3] = (byte)(rand_len >> 8);
                outdata[4] = (byte)(rand_len);
            }
            ulong adler = Util.Adler32.CalcAdler32(outdata, outlength - 4);
            BitConverter.GetBytes((uint)adler).CopyTo(outdata, outlength - 4);
        }

        public void PackAuthData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            int rand_len = datalength > 1300 ? 1 : datalength > 400 ? random.Next(128) + 1 : random.Next(1024) + 3;
            int data_offset = rand_len + 4 + 2;
            outlength = data_offset + datalength + 12 + 10;
            AuthData authData = (AuthData)this.Server.data;
            lock (authData)
            {
                if (authData.connectionID > 0xFF000000)
                {
                    authData.clientID = null;
                }
                if (authData.clientID == null)
                {
                    authData.clientID = new byte[8];
                    g_random.GetBytes(authData.clientID);
                    authData.connectionID = (UInt32)BitConverter.ToInt64(authData.clientID, 0) % 0xFFFFFD; // random.Next(0x1000000);
                }
                authData.connectionID += 1;
                Array.Copy(authData.clientID, 0, outdata, data_offset, 8);
                Array.Copy(BitConverter.GetBytes(authData.connectionID), 0, outdata, data_offset + 8, 4);
            }

            Array.Copy(data, 0, outdata, data_offset + 12, datalength);
            outdata[4] = (byte)(outlength >> 8);
            outdata[5] = (byte)(outlength);
            if (rand_len < 128)
            {
                outdata[6] = (byte)(rand_len);
            }
            else
            {
                outdata[6] = 0xFF;
                outdata[7] = (byte)(rand_len >> 8);
                outdata[8] = (byte)(rand_len);
            }

            byte[] salt = System.Text.Encoding.UTF8.GetBytes("auth_sha1_v2");
            byte[] crcdata = new byte[salt.Length + Server.key.Length];
            salt.CopyTo(crcdata, 0);
            Server.key.CopyTo(crcdata, salt.Length);
            ulong crc32 = Util.CRC32.CalcCRC32(crcdata, (int)crcdata.Length);
            BitConverter.GetBytes((uint)crc32).CopyTo(outdata, 0);

            byte[] key = new byte[Server.iv.Length + Server.key.Length];
            Server.iv.CopyTo(key, 0);
            Server.key.CopyTo(key, Server.iv.Length);

            HMACSHA1 sha1 = new HMACSHA1(key);
            byte[] sha1data = sha1.ComputeHash(outdata, 0, outlength - 10);

            Array.Copy(sha1data, 0, outdata, outlength - 10, 10);
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
                int _datalength = Math.Min(random.Next(32) + headsize, datalength);
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

                if (Util.Adler32.CheckAdler32(recv_buf, len))
                {
                    int pos = recv_buf[2];
                    if (pos < 255)
                    {
                        pos += 2;
                    }
                    else
                    {
                        pos = ((recv_buf[3] << 8) | recv_buf[4]) + 2;
                    }
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
                    throw new ObfsException("ClientPostDecrypt data uncorrect checksum");
                }
            }
            return outdata;
        }

        public override void Dispose()
        {
        }
    }
}
