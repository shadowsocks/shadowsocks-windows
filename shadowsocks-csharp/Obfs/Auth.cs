using System;
using System.Collections.Generic;
using Shadowsocks.Controller;

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
            int rand_len = random.Next(16) + 1;
            outlength = rand_len + datalength + 6 + 12;
            lock ((AuthData)this.Server.data)
            {
                if (((AuthData)this.Server.data).connectionID > 0xFF000000)
                {
                    ((AuthData)this.Server.data).clientID = null;
                }
                if (((AuthData)this.Server.data).clientID == null)
                {
                    ((AuthData)this.Server.data).clientID = new byte[4];
                    random.NextBytes(((AuthData)this.Server.data).clientID);
                    ((AuthData)this.Server.data).connectionID = (UInt32)random.Next(0x1000000);
                }
                ((AuthData)this.Server.data).connectionID += 1;
                Array.Copy(((AuthData)this.Server.data).clientID, 0, outdata, rand_len + 4 + 2, 4);
                Array.Copy(BitConverter.GetBytes(((AuthData)this.Server.data).connectionID), 0, outdata, rand_len + 8 + 2, 4);
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
                    //len = (recv_buf[0] << 8) + recv_buf[1];
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
}
