using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Shadowsocks.Obfs
{
    class HttpSimpleObfs : ObfsBase
    {
        public HttpSimpleObfs(string method)
            : base(method)
        {
            has_sent_header = false;
            //has_recv_header = false;
            raw_trans_sent = false;
            raw_trans_recv = false;
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
                {"tls_simple", new int[]  {0, 1, 0}}, //modify original protocol, wrap protocol, obfs param
                {"http_simple", new int[] {0, 1, 1}},
                {"http2_simple", new int[]{0, 1, 1}},
                {"random_head", new int[] {0, 1, 0}},
        };
        private static string[] _request_path = new string[]
        {
            "", "",
            "login.php?redir=", "",
            "register.php?code=", "",
            "?keyword=", "",
            "search?src=typd&q=", "&lang=en",
            "s?ie=utf-8&f=8&rsv_bp=1&rsv_idx=1&ch=&bar=&wd=", "&rn=",
            "post.php?id=", "&goto=view.php",
        };

        private static string[] _request_useragent = new string[]
        {
            "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:40.0) Gecko/20100101 Firefox/40.0",
            "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:40.0) Gecko/20100101 Firefox/44.0",
            "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/535.11 (KHTML, like Gecko) Ubuntu/11.10 Chromium/27.0.1453.93 Chrome/27.0.1453.93 Safari/537.36",
            "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:35.0) Gecko/20100101 Firefox/35.0",
            "Mozilla/5.0 (compatible; WOW64; MSIE 10.0; Windows NT 6.2)",
            "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US) AppleWebKit/533.20.25 (KHTML, like Gecko) Version/5.0.4 Safari/533.20.27",
            "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.3; Trident/7.0; .NET4.0E; .NET4.0C)",
            "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko",
            "Mozilla/5.0 (Linux; Android 4.4; Nexus 5 Build/BuildID) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/30.0.0.0 Mobile Safari/537.36",
            "Mozilla/5.0 (iPad; CPU OS 5_0 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko) Version/5.1 Mobile/9A334 Safari/7534.48.3",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 5_0 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko) Version/5.1 Mobile/9A334 Safari/7534.48.3",
        };
        private static int _useragent_index = new Random().Next(_request_useragent.Length);

        private bool has_sent_header;
        //private bool has_recv_header;
        private bool raw_trans_sent;
        private bool raw_trans_recv;
        private List<byte[]> data_buffer = new List<byte[]>();
        private Random random = new Random();

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
        }

        public override Dictionary<string, int[]> GetObfs()
        {
            return _obfs;
        }

        private string data2urlencode(byte[] encryptdata, int datalength)
        {
            string ret = "";
            for (int i = 0; i < datalength; ++i)
            {
                ret += "%" + encryptdata[i].ToString("x2");
            }
            return ret;
        }

        public override byte[] ClientEncode(byte[] encryptdata, int datalength, out int outlength)
        {
            if (raw_trans_sent)
            {
                SentLength += datalength;
                outlength = datalength;
                return encryptdata;
            }
            else
            {
                byte[] outdata = new byte[datalength + 4096];
                byte[] headdata;
                if (Method == "tls_simple" || Method == "random_head")
                {
                    if (has_sent_header)
                    {
                        outlength = 0;
                        if (datalength > 0)
                        {
                            byte[] data = new byte[datalength];
                            Array.Copy(encryptdata, 0, data, 0, datalength);
                            data_buffer.Add(data);
                        }
                        else
                        {
                            foreach (byte[] data in data_buffer)
                            {
                                Array.Copy(data, 0, outdata, outlength, data.Length);
                                SentLength += data.Length;
                                outlength += data.Length;
                            }
                            data_buffer.Clear();
                            raw_trans_sent = true;
                        }
                    }
                    else if (Method == "random_head")
                    {
                        int size = random.Next(96) + 8;
                        byte[] rnd = new byte[size];
                        random.NextBytes(rnd);
                        Util.CRC32.SetCRC32(rnd);
                        rnd.CopyTo(outdata, 0);
                        outlength = rnd.Length;

                        byte[] data = new byte[datalength];
                        Array.Copy(encryptdata, 0, data, 0, datalength);
                        data_buffer.Add(data);
                    }
                    else
                    {
                        byte[] rnd = new byte[32];
                        random.NextBytes(rnd);
                        List<byte> ssl_buf = new List<byte>();
                        string str_buf = "000016c02bc02fc00ac009c013c01400330039002f0035000a0100006fff01000100000a00080006001700180019000b0002010000230000337400000010002900270568322d31360568322d31350568322d313402683208737064792f332e3108687474702f312e31000500050100000000000d001600140401050106010201040305030603020304020202";
                        // "\x00\x00\x16\xc0+\xc0/\xc0\n\xc0\t\xc0\x13\xc0\x14\x003\x009\x00/\x005\x00\n\x01\x00\x00o\xff\x01\x00\x01\x00\x00\n\x00\x08\x00\x06\x00\x17\x00\x18\x00\x19\x00\x0b\x00\x02\x01\x00\x00#\x00\x003t\x00\x00\x00\x10\x00)\x00'\x05h2-16\x05h2-15\x05h2-14\x02h2\x08spdy/3.1\x08http/1.1\x00\x05\x00\x05\x01\x00\x00\x00\x00\x00\r\x00\x16\x00\x14\x04\x01\x05\x01\x06\x01\x02\x01\x04\x03\x05\x03\x06\x03\x02\x03\x04\x02\x02\x02";
                        foreach (byte b in rnd)
                        {
                            ssl_buf.Add(b);
                        }
                        for (int i = 0; i < str_buf.Length; i += 2)
                        {
                            byte c = (byte)(str_buf[i] | str_buf[i + 1] * 16);
                            ssl_buf.Add(c);
                        }
                        // client version
                        ssl_buf.Insert(0, (byte)1);
                        ssl_buf.Insert(0, (byte)3);
                        // length
                        ssl_buf.Insert(0, (byte)(ssl_buf.Count % 256));
                        ssl_buf.Insert(0, (byte)((ssl_buf.Count - 1) / 256));
                        ssl_buf.Insert(0, (byte)0);
                        ssl_buf.Insert(0, (byte)1); // client hello
                        // length
                        ssl_buf.Insert(0, (byte)(ssl_buf.Count % 256));
                        ssl_buf.Insert(0, (byte)((ssl_buf.Count - 1) / 256));
                        //
                        ssl_buf.Insert(0, (byte)0x1);
                        ssl_buf.Insert(0, (byte)0x3);
                        ssl_buf.Insert(0, (byte)0x16);
                        for (int i = 0; i < ssl_buf.Count; ++i)
                        {
                            outdata[i] = (byte)ssl_buf[i];
                        }
                        outlength = ssl_buf.Count;

                        byte[] data = new byte[datalength];
                        Array.Copy(encryptdata, 0, data, 0, datalength);
                        data_buffer.Add(data);
                    }
                }
                else if (Method == "http_simple")
                {
                    int headsize = Server.iv.Length + Server.head_len;
                    if (datalength - headsize > 64)
                    {
                        headdata = new byte[headsize + random.Next(0, 64)];
                    }
                    else
                    {
                        headdata = new byte[datalength];
                    }
                    Array.Copy(encryptdata, 0, headdata, 0, headdata.Length);
                    int request_path_index = new Random().Next(_request_path.Length / 2) * 2;
                    string host = Server.host;
                    string custom_head = "";
                    if (Server.param.Length > 0)
                    {
                        string[] custom_heads = Server.param.Split(new char[] { '#' }, 2);
                        string param = Server.param;
                        if (custom_heads.Length > 1)
                        {
                            custom_head = custom_heads[1];
                            param = custom_heads[0];
                        }
                        string[] hosts = param.Split(',');
                        host = hosts[random.Next(hosts.Length)];
                    }
                    string http_buf =
                        "GET /" + _request_path[request_path_index] + data2urlencode(headdata, headdata.Length) + _request_path[request_path_index + 1] + " HTTP/1.1\r\n"
                        + "Host: " + host + (Server.port == 80 ? "" : ":" + Server.port.ToString()) + "\r\n";
                    if (custom_head.Length > 0)
                    {
                        http_buf += custom_head.Replace("\\n", "\r\n") + "\r\n\r\n";
                    }
                    else
                    {
                        http_buf +=
                        "User-Agent: " + _request_useragent[_useragent_index] + "\r\n"
                        + "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\n"
                        + "Accept-Language: en-US,en;q=0.8\r\n"
                        + "Accept-Encoding: gzip, deflate\r\n"
                        + "DNT: 1\r\n"
                        + "Connection: keep-alive\r\n"
                        + "\r\n";
                    }
                    for (int i = 0; i < http_buf.Length; ++i)
                    {
                        outdata[i] = (byte)http_buf[i];
                    }
                    if (headdata.Length < datalength)
                    {
                        Array.Copy(encryptdata, headdata.Length, outdata, http_buf.Length, datalength - headdata.Length);
                    }
                    SentLength += headdata.Length;
                    outlength = http_buf.Length + datalength - headdata.Length;
                    raw_trans_sent = true;
                }
                else if (Method == "http2_simple")
                {
                    string host = Server.host;
                    if (Server.param.Length > 0)
                    {
                        string[] hosts = Server.param.Split(',');
                        host = hosts[random.Next(hosts.Length)];
                    }
                    string http_buf = "GET / HTTP/1.1\r\n"
                    + "Host: " + host + (Server.port == 80 ? "" : ":" + Server.port.ToString()) + "\r\n"
                    + "Connection: Upgrade, HTTP2-Settings\r\n"
                    + "Upgrade: h2c\r\n"
                    + "HTTP2-Settings: " + Convert.ToBase64String(encryptdata, 0, datalength).Replace('+', '-').Replace('/', '_') + "\r\n"
                    + "\r\n";
                    for (int i = 0; i < http_buf.Length; ++i)
                    {
                        outdata[i] = (byte)http_buf[i];
                    }
                    SentLength += datalength;
                    outlength = http_buf.Length;
                    raw_trans_sent = true;
                }
                else
                {
                    outlength = 0;
                }
                has_sent_header = true;
                return outdata;
            }
        }

        private int FindSubArray(byte[] array, int length, byte[] subArray)
        {
            for (int pos = 0; pos < length; ++pos)
            {
                int offset = 0;
                for (; offset < subArray.Length; ++offset)
                {
                    if (array[pos + offset] != subArray[offset])
                        break;
                }
                if (offset == subArray.Length)
                {
                    return pos;
                }
            }
            return -1;
        }

        public override byte[] ClientDecode(byte[] encryptdata, int datalength, out int outlength, out bool needsendback)
        {
            if (raw_trans_recv)
            {
                outlength = datalength;
                needsendback = false;
                return encryptdata;
            }
            else
            {
                byte[] outdata = new byte[datalength];
                if (Method == "tls_simple" || Method == "random_head")
                {
                    outlength = 0;
                    raw_trans_recv = true;
                    needsendback = true;
                    return encryptdata;
                }
                else
                {
                    int pos = FindSubArray(encryptdata, datalength, new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' });
                    if (pos > 0)
                    {
                        outlength = datalength - (pos + 4);
                        Array.Copy(encryptdata, pos + 4, outdata, 0, outlength);
                        //has_recv_header = true;
                        raw_trans_recv = true;
                    }
                    else
                    {
                        outlength = 0;
                    }
                    needsendback = false;
                }
                return outdata;
            }
        }

        public override void Dispose()
        {
        }
    }
    public class TlsAuthData
    {
        public byte[] clientID;
    }

    class TlsAuthObfs : ObfsBase
    {
        public TlsAuthObfs(string method)
            : base(method)
        {
            has_sent_header = false;
            //has_recv_header = false;
            raw_trans_sent = false;
            raw_trans_recv = false;
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
                {"tls1.0_session_auth", new int[]  {0, 1, 0}},
        };

        private bool has_sent_header;
        //private bool has_recv_header;
        private bool raw_trans_sent;
        private bool raw_trans_recv;
        private List<byte[]> data_buffer = new List<byte[]>();
        protected static RNGCryptoServiceProvider g_random = new RNGCryptoServiceProvider();
        private Random random = new Random();

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
            return new TlsAuthData();
        }

        protected void hmac_sha1(byte[] data, int length)
        {
            byte[] key = new byte[Server.key.Length + 32];
            Server.key.CopyTo(key, 0);
            ((TlsAuthData)this.Server.data).clientID.CopyTo(key, Server.key.Length);

            HMACSHA1 sha1 = new HMACSHA1(key);
            byte[] sha1data = sha1.ComputeHash(data, 0, length - 10);

            Array.Copy(sha1data, 0, data, length - 10, 10);
        }

        public void PackAuthData(byte[] outdata)
        {
            int outlength = 32;
            {
                byte[] randomdata = new byte[18];
                g_random.GetBytes(randomdata);
                randomdata.CopyTo(outdata, 4);
            }
            TlsAuthData authData = (TlsAuthData)this.Server.data;
            lock (authData)
            {
                if (authData.clientID == null)
                {
                    authData.clientID = new byte[32];
                    g_random.GetBytes(authData.clientID);
                }
            }
            UInt64 utc_time_second = (UInt64)Math.Floor(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
            UInt32 utc_time = (UInt32)(utc_time_second);
            byte[] time_bytes = BitConverter.GetBytes(utc_time);
            Array.Reverse(time_bytes);
            Array.Copy(time_bytes, 0, outdata, 0, 4);

            hmac_sha1(outdata, outlength);
        }

        public override byte[] ClientEncode(byte[] encryptdata, int datalength, out int outlength)
        {
            if (raw_trans_sent)
            {
                SentLength += datalength;
                outlength = datalength;
                return encryptdata;
            }
            byte[] outdata = new byte[datalength + 4096];
            if (has_sent_header)
            {
                outlength = 0;
                if (datalength > 0)
                {
                    byte[] data = new byte[datalength];
                    Array.Copy(encryptdata, 0, data, 0, datalength);
                    data_buffer.Add(data);
                }
                else
                {
                    {
                        byte[] hmac_data = new byte[43];
                        byte[] rnd = new byte[22];
                        random.NextBytes(rnd);

                        byte[] handshake_finish = System.Text.Encoding.ASCII.GetBytes("\x14\x03\x01\x00\x01\x01" + "\x16\x03\x01\x00\x20");
                        handshake_finish.CopyTo(hmac_data, 0);
                        rnd.CopyTo(hmac_data, handshake_finish.Length);

                        hmac_sha1(hmac_data, hmac_data.Length);

                        data_buffer.Insert(0, hmac_data);
                        SentLength -= hmac_data.Length;
                    }
                    foreach (byte[] data in data_buffer)
                    {
                        Array.Copy(data, 0, outdata, outlength, data.Length);
                        SentLength += data.Length;
                        outlength += data.Length;
                    }
                    data_buffer.Clear();
                    raw_trans_sent = true;
                }
            }
            else
            {
                byte[] rnd = new byte[32];
                PackAuthData(rnd);
                List<byte> ssl_buf = new List<byte>();
                string str_buf = "0016c02bc02fc00ac009c013c01400330039002f0035000a0100006fff01000100000a00080006001700180019000b0002010000230000337400000010002900270568322d31360568322d31350568322d313402683208737064792f332e3108687474702f312e31000500050100000000000d001600140401050106010201040305030603020304020202";
                foreach (byte b in rnd)
                {
                    ssl_buf.Add(b);
                }
                ssl_buf.Add(32);
                foreach (byte b in ((TlsAuthData)this.Server.data).clientID)
                {
                    ssl_buf.Add(b);
                }
                for (int i = 0; i < str_buf.Length; i += 2)
                {
                    byte c = (byte)(str_buf[i] | str_buf[i + 1] * 16);
                    ssl_buf.Add(c);
                }
                // client version
                ssl_buf.Insert(0, (byte)1); // version
                ssl_buf.Insert(0, (byte)3);
                // length
                ssl_buf.Insert(0, (byte)(ssl_buf.Count % 256));
                ssl_buf.Insert(0, (byte)((ssl_buf.Count - 1) / 256));
                ssl_buf.Insert(0, (byte)0);
                ssl_buf.Insert(0, (byte)1); // client hello
                // length
                ssl_buf.Insert(0, (byte)(ssl_buf.Count % 256));
                ssl_buf.Insert(0, (byte)((ssl_buf.Count - 1) / 256));
                //
                ssl_buf.Insert(0, (byte)0x1); // version
                ssl_buf.Insert(0, (byte)0x3);
                ssl_buf.Insert(0, (byte)0x16);
                for (int i = 0; i < ssl_buf.Count; ++i)
                {
                    outdata[i] = (byte)ssl_buf[i];
                }
                outlength = ssl_buf.Count;

                byte[] data = new byte[datalength];
                Array.Copy(encryptdata, 0, data, 0, datalength);
                data_buffer.Add(data);
            }
            has_sent_header = true;
            return outdata;
        }

        public override byte[] ClientDecode(byte[] encryptdata, int datalength, out int outlength, out bool needsendback)
        {
            if (raw_trans_recv)
            {
                outlength = datalength;
                needsendback = false;
                return encryptdata;
            }
            else
            {
                byte[] outdata = new byte[datalength];
                if (datalength < 11 + 32 + 1 + 32)
                {
                    throw new ObfsException("ClientDecode data error");
                }
                else
                {
                    byte[] hmacsha1 = new byte[10];
                    byte[] data = new byte[32];
                    Array.Copy(encryptdata, 11, data, 0, 22);
                    hmac_sha1(data, data.Length);
                    Array.Copy(data, 22, hmacsha1, 0, 10);

                    if (Util.Utils.FindStr(encryptdata, 11 + 32 + 1 + 32, hmacsha1) != 11 + 22)
                    {
                        throw new ObfsException("ClientDecode data error: wrong sha1");
                    }
                }
                outlength = 0;
                raw_trans_recv = true;
                needsendback = true;
                return encryptdata;
            }
        }

        public override void Dispose()
        {
        }
    }
}
