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
            int rand_len = datalength > 1300 ? 1 : random.Next(64) + 1;
            outlength = rand_len + datalength + 6;
            if (datalength > 0)
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
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
                    Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
            int rand_len = (datalength >= 1300 ? 0 : (datalength > 400 ? random.Next(128) : random.Next(1024))) + 1;
            outlength = rand_len + datalength + 6;
            if (datalength > 0)
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
            int rand_len = (datalength > 400 ? random.Next(128) : random.Next(1024)) + 1;
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
            int ogn_datalength = datalength;
            if (!has_sent_header)
            {
                int headsize = GetHeadSize(plaindata, 30);
                int _datalength = Math.Min(random.Next(32) + headsize, datalength);
                int outlen;
                PackAuthData(data, _datalength, packdata, out outlen);
                has_sent_header = true;
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
                Util.Utils.SetArrayMinSize(ref outdata, outlength + outlen);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
                datalength -= unit_len;
                byte[] newdata = new byte[datalength];
                Array.Copy(data, unit_len, newdata, 0, newdata.Length);
                data = newdata;
            }
            if (datalength > 0 || ogn_datalength == -1)
            {
                int outlen;
                if (ogn_datalength == -1)
                    datalength = 0;
                PackData(data, datalength, packdata, out outlen);
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
                    Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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

    public class AuthSHA1V3 : VerifySimpleBase
    {
        public AuthSHA1V3(string method)
            : base(method)
        {
            has_sent_header = false;
            has_recv_header = false;
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
            {"auth_sha1_v3", new int[]{1, 0, 1}},
        };

        protected bool has_sent_header;
        protected bool has_recv_header;
        protected static RNGCryptoServiceProvider g_random = new RNGCryptoServiceProvider();
        protected const string SALT = "auth_sha1_v3";

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
            int rand_len = (datalength > 1200 ? 0 : (datalength > 400 ? random.Next(256) : random.Next(512))) + 1;
            outlength = rand_len + datalength + 6;
            if (datalength > 0)
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
            int rand_len = (datalength > 400 ? random.Next(128) : random.Next(1024)) + 1;
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
                    authData.connectionID = (UInt32)BitConverter.ToInt32(authData.clientID, 0) % 0xFFFFFD; // random.Next(0x1000000);
                }
                authData.connectionID += 1;
                Array.Copy(authData.clientID, 0, outdata, data_offset + 4, 4);
                Array.Copy(BitConverter.GetBytes(authData.connectionID), 0, outdata, data_offset + 8, 4);
            }
            UInt64 utc_time_second = (UInt64)Math.Floor(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
            UInt32 utc_time = (UInt32)(utc_time_second);
            Array.Copy(BitConverter.GetBytes(utc_time), 0, outdata, data_offset, 4);

            Array.Copy(data, 0, outdata, data_offset + 12, datalength);
            outdata[0] = (byte)(outlength >> 8);
            outdata[1] = (byte)(outlength);
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

            byte[] salt = System.Text.Encoding.UTF8.GetBytes(SALT);
            byte[] crcdata = new byte[salt.Length + Server.key.Length + 2];
            salt.CopyTo(crcdata, 0);
            Server.key.CopyTo(crcdata, salt.Length);
            crcdata[crcdata.Length - 2] = outdata[0];
            crcdata[crcdata.Length - 1] = outdata[1];
            ulong crc32 = Util.CRC32.CalcCRC32(crcdata, (int)crcdata.Length);
            BitConverter.GetBytes((uint)crc32).CopyTo(outdata, 2);

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
            int ogn_datalength = datalength;
            if (!has_sent_header)
            {
                int headsize = GetHeadSize(plaindata, 30);
                int _datalength = Math.Min(random.Next(32) + headsize, datalength);
                int outlen;
                PackAuthData(data, _datalength, packdata, out outlen);
                has_sent_header = true;
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
                datalength -= unit_len;
                byte[] newdata = new byte[datalength];
                Array.Copy(data, unit_len, newdata, 0, newdata.Length);
                data = newdata;
            }
            if (datalength > 0 || ogn_datalength == -1)
            {
                int outlen;
                if (ogn_datalength == -1)
                    datalength = 0;
                PackData(data, datalength, packdata, out outlen);
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
                    Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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

    public class AuthSHA1V4 : VerifySimpleBase
    {
        public AuthSHA1V4(string method)
            : base(method)
        {
            has_sent_header = false;
            has_recv_header = false;
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
            {"auth_sha1_v4", new int[]{1, 0, 1}},
        };

        protected bool has_sent_header;
        protected bool has_recv_header;
        protected static RNGCryptoServiceProvider g_random = new RNGCryptoServiceProvider();
        protected const string SALT = "auth_sha1_v4";

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
            int rand_len = (datalength > 1200 ? 0 : (datalength > 400 ? random.Next(256) : random.Next(512))) + 1;
            outlength = rand_len + datalength + 8;
            if (datalength > 0)
                Array.Copy(data, 0, outdata, rand_len + 4, datalength);
            outdata[0] = (byte)(outlength >> 8);
            outdata[1] = (byte)(outlength);
            ulong crc32 = Util.CRC32.CalcCRC32(outdata, 2);
            BitConverter.GetBytes((ushort)crc32).CopyTo(outdata, 2);
            if (rand_len < 128)
            {
                outdata[4] = (byte)(rand_len);
            }
            else
            {
                outdata[4] = 0xFF;
                outdata[5] = (byte)(rand_len >> 8);
                outdata[6] = (byte)(rand_len);
            }
            ulong adler = Util.Adler32.CalcAdler32(outdata, outlength - 4);
            BitConverter.GetBytes((uint)adler).CopyTo(outdata, outlength - 4);
        }

        public void PackAuthData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            int rand_len = (datalength > 400 ? random.Next(128) : random.Next(1024)) + 1;
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
                    authData.connectionID = (UInt32)BitConverter.ToInt32(authData.clientID, 0) % 0xFFFFFD; // random.Next(0x1000000);
                }
                authData.connectionID += 1;
                Array.Copy(authData.clientID, 0, outdata, data_offset + 4, 4);
                Array.Copy(BitConverter.GetBytes(authData.connectionID), 0, outdata, data_offset + 8, 4);
            }
            UInt64 utc_time_second = (UInt64)Math.Floor(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
            UInt32 utc_time = (UInt32)(utc_time_second);
            Array.Copy(BitConverter.GetBytes(utc_time), 0, outdata, data_offset, 4);

            Array.Copy(data, 0, outdata, data_offset + 12, datalength);
            outdata[0] = (byte)(outlength >> 8);
            outdata[1] = (byte)(outlength);
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

            byte[] salt = System.Text.Encoding.UTF8.GetBytes(SALT);
            byte[] crcdata = new byte[salt.Length + Server.key.Length + 2];
            salt.CopyTo(crcdata, 2);
            Server.key.CopyTo(crcdata, salt.Length + 2);
            crcdata[0] = outdata[0];
            crcdata[1] = outdata[1];
            ulong crc32 = Util.CRC32.CalcCRC32(crcdata, (int)crcdata.Length);
            BitConverter.GetBytes((uint)crc32).CopyTo(outdata, 2);

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
            int ogn_datalength = datalength;
            if (!has_sent_header)
            {
                int headsize = GetHeadSize(plaindata, 30);
                int _datalength = Math.Min(random.Next(32) + headsize, datalength);
                int outlen;
                PackAuthData(data, _datalength, packdata, out outlen);
                has_sent_header = true;
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
                datalength -= unit_len;
                byte[] newdata = new byte[datalength];
                Array.Copy(data, unit_len, newdata, 0, newdata.Length);
                data = newdata;
            }
            if (datalength > 0 || ogn_datalength == -1)
            {
                int outlen;
                if (ogn_datalength == -1)
                    datalength = 0;
                PackData(data, datalength, packdata, out outlen);
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
            while (recv_buf_len > 4)
            {
                ulong crc32 = Util.CRC32.CalcCRC32(recv_buf, 2);
                if ((uint)((recv_buf[3] << 8) | recv_buf[2]) != ((uint)crc32 & 0xffff))
                {
                    throw new ObfsException("ClientPostDecrypt data error");
                }
                int len = (recv_buf[0] << 8) + recv_buf[1];
                if (len >= 8192 || len < 7)
                {
                    throw new ObfsException("ClientPostDecrypt data error");
                }
                if (len > recv_buf_len)
                    break;

                if (Util.Adler32.CheckAdler32(recv_buf, len))
                {
                    int pos = recv_buf[4];
                    if (pos < 255)
                    {
                        pos += 4;
                    }
                    else
                    {
                        pos = ((recv_buf[5] << 8) | recv_buf[6]) + 4;
                    }
                    int outlen = len - pos - 4;
                    Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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

    public class AuthAES128 : VerifySimpleBase
    {
        public AuthAES128(string method)
            : base(method)
        {
            has_sent_header = false;
            has_recv_header = false;
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
            {"auth_aes128", new int[]{1, 0, 1}},
        };

        protected bool has_sent_header;
        protected bool has_recv_header;
        protected static RNGCryptoServiceProvider g_random = new RNGCryptoServiceProvider();
        protected const string SALT = "auth_aes128";

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
            int rand_len = (datalength > 1200 ? 0 : (datalength > 400 ? random.Next(256) : random.Next(512))) + 1;
            outlength = rand_len + datalength + 8;
            if (datalength > 0)
                Array.Copy(data, 0, outdata, rand_len + 4, datalength);
            outdata[0] = (byte)(outlength >> 8);
            outdata[1] = (byte)(outlength);
            ulong crc32 = Util.CRC32.CalcCRC32(outdata, 2);
            BitConverter.GetBytes((ushort)crc32).CopyTo(outdata, 2);
            if (rand_len < 128)
            {
                outdata[4] = (byte)(rand_len);
            }
            else
            {
                outdata[4] = 0xFF;
                outdata[5] = (byte)(rand_len >> 8);
                outdata[6] = (byte)(rand_len);
            }
            ulong adler = Util.Adler32.CalcAdler32(outdata, outlength - 4);
            BitConverter.GetBytes((uint)adler).CopyTo(outdata, outlength - 4);
        }

        public void PackAuthData(byte[] data, int datalength, byte[] outdata, out int outlength)
        {
            int rand_len = (datalength > 400 ? random.Next(128) : random.Next(1024));
            int data_offset = rand_len + 16 + 10;
            outlength = data_offset + datalength + 4;
            byte[] encrypt = new byte[26];
            byte[] encrypt_data = new byte[32];
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
                    authData.connectionID = (UInt32)BitConverter.ToInt32(authData.clientID, 0) % 0xFFFFFD; // random.Next(0x1000000);
                }
                authData.connectionID += 1;
                Array.Copy(authData.clientID, 0, encrypt, 4, 4);
                Array.Copy(BitConverter.GetBytes(authData.connectionID), 0, encrypt, 8, 4);
            }
            UInt64 utc_time_second = (UInt64)Math.Floor(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
            UInt32 utc_time = (UInt32)(utc_time_second);
            Array.Copy(BitConverter.GetBytes(utc_time), 0, encrypt, 0, 4);
            encrypt[12] = (byte)(outlength >> 8);
            encrypt[13] = (byte)(outlength);
            encrypt[14] = (byte)(rand_len >> 8);
            encrypt[15] = (byte)(rand_len);

            Encryption.IEncryptor encryptor = Encryption.EncryptorFactory.GetEncryptor("aes-128-cbc", SALT);
            int enc_outlen;
            encryptor.SetIV(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            encryptor.Encrypt(encrypt, 16, encrypt_data, out enc_outlen);
            encryptor.Dispose();
            Array.Copy(encrypt_data, 16, encrypt, 0, 16);

            byte[] key = new byte[Server.iv.Length + Server.key.Length];
            Server.iv.CopyTo(key, 0);
            Server.key.CopyTo(key, Server.iv.Length);

            HMACSHA1 sha1 = new HMACSHA1(key);
            byte[] sha1data = sha1.ComputeHash(encrypt, 0, 16);
            Array.Copy(sha1data, 0, encrypt, 16, 10);

            encrypt.CopyTo(outdata, 0);
            Array.Copy(data, 0, outdata, data_offset, datalength);

            ulong adler = Util.Adler32.CalcAdler32(outdata, outlength - 4);
            BitConverter.GetBytes((uint)adler).CopyTo(outdata, outlength - 4);
        }

        public override byte[] ClientPreEncrypt(byte[] plaindata, int datalength, out int outlength)
        {
            byte[] outdata = new byte[datalength + datalength / 10 + 32];
            byte[] packdata = new byte[9000];
            byte[] data = plaindata;
            outlength = 0;
            const int unit_len = 8100;
            int ogn_datalength = datalength;
            if (!has_sent_header)
            {
                int headsize = GetHeadSize(plaindata, 30);
                int _datalength = Math.Min(random.Next(32) + headsize, datalength);
                int outlen;
                PackAuthData(data, _datalength, packdata, out outlen);
                has_sent_header = true;
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
                Array.Copy(packdata, 0, outdata, outlength, outlen);
                outlength += outlen;
                datalength -= unit_len;
                byte[] newdata = new byte[datalength];
                Array.Copy(data, unit_len, newdata, 0, newdata.Length);
                data = newdata;
            }
            if (datalength > 0 || ogn_datalength == -1)
            {
                int outlen;
                if (ogn_datalength == -1)
                    datalength = 0;
                PackData(data, datalength, packdata, out outlen);
                Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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
            while (recv_buf_len > 4)
            {
                byte[] crcdata = new byte[2];
                crcdata[crcdata.Length - 2] = recv_buf[0];
                crcdata[crcdata.Length - 1] = recv_buf[1];
                ulong crc32 = Util.CRC32.CalcCRC32(crcdata, (int)crcdata.Length);
                if ((uint)((recv_buf[3] << 8) | recv_buf[2]) != ((uint)crc32 & 0xffff))
                {
                    throw new ObfsException("ClientPostDecrypt data error");
                }
                int len = (recv_buf[0] << 8) + recv_buf[1];
                if (len >= 8192 || len < 7)
                {
                    throw new ObfsException("ClientPostDecrypt data error");
                }
                if (len > recv_buf_len)
                    break;

                if (Util.Adler32.CheckAdler32(recv_buf, len))
                {
                    int pos = recv_buf[4];
                    if (pos < 255)
                    {
                        pos += 4;
                    }
                    else
                    {
                        pos = ((recv_buf[5] << 8) | recv_buf[6]) + 4;
                    }
                    int outlen = len - pos - 4;
                    Util.Utils.SetArrayMinSize2(ref outdata, outlength + outlen);
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

        public override byte[] ClientUdpPreEncrypt(byte[] plaindata, int datalength, out int outlength)
        {
            byte[] outdata = new byte[datalength + 4];
            outlength = datalength + 4;
            Array.Copy(plaindata, 0, outdata, 0, datalength);
            ulong adler = Util.Adler32.CalcAdler32(outdata, datalength);
            BitConverter.GetBytes((uint)adler).CopyTo(outdata, datalength);
            return outdata;
        }

        public override byte[] ClientUdpPostDecrypt(byte[] plaindata, int datalength, out int outlength)
        {
            if (Util.Adler32.CheckAdler32(plaindata, datalength))
            {
                outlength = datalength - 4;
                return plaindata;
            }
            outlength = 0;
            return plaindata;
        }

        public override void Dispose()
        {
        }
    }
}
