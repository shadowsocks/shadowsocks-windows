using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Obfs
{
    class HttpSimpleObfs : ObfsBase
    {
        public HttpSimpleObfs(string method)
            : base(method)
        {
            has_sent_header = false;
            has_recv_header = false;
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
                {"http_simple", new int[]{}},
        };
        private bool has_sent_header;
        private bool has_recv_header;
        private Random random = new Random();

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
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

        public override bool ClientEncode(byte[] encryptdata, int datalength, byte[] outdata, out int outlength)
        {
            if (has_sent_header)
            {
                Array.Copy(encryptdata, 0, outdata, 0, datalength);
                outlength = datalength;
                return false;
            }
            else
            {
                byte[] headdata;
                if (datalength > 64)
                {
                    headdata = new byte[random.Next(1, 64)];
                }
                else
                {
                    headdata = new byte[datalength];
                }
                Array.Copy(encryptdata, 0, headdata, 0, headdata.Length);
                string http_buf =
                    "GET /" + data2urlencode(headdata, headdata.Length) + " HTTP/1.1\r\n"
                    + "Host: " + Host + ":" + Port.ToString() + "\r\n"
                    + "User-Agent: Mozilla/5.0 (Windows NT 6.3; WOW64; rv:40.0) Gecko/20100101 Firefox/40.0\r\n"
                    + "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\n"
                    + "Accept-Language: en-US,en;q=0.8\r\n"
                    + "Accept-Encoding: gzip, deflate\r\n"
                    + "DNT: 1\r\n"
                    + "Connection: keep-alive\r\n"
                    + "\r\n";
                for (int i = 0; i < http_buf.Length; ++i)
                {
                    outdata[i] = (byte)http_buf[i];
                }
                if (headdata.Length < datalength)
                {
                    Array.Copy(encryptdata, headdata.Length, outdata, http_buf.Length, datalength - headdata.Length);
                }
                outlength = http_buf.Length + datalength - headdata.Length;
                has_sent_header = true;
                return false;
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

        public override bool ClientDecode(byte[] encryptdata, int datalength, byte[] outdata, out int outlength)
        {
            if (has_recv_header)
            {
                Array.Copy(encryptdata, 0, outdata, 0, datalength);
                outlength = datalength;
                return false;
            }
            else
            {
                int pos = FindSubArray(encryptdata, datalength, new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' });
                if (pos > 0)
                {
                    outlength = datalength - (pos + 4);
                    Array.Copy(encryptdata, pos + 4, outdata, 0, outlength);
                    has_recv_header = true;
                }
                else
                {
                    /*
                    Array.Copy(encryptdata, 0, outdata, 0, datalength);
                    string s = "";
                    for (int i = 0; i < datalength; ++i)
                    {
                        s += (char)encryptdata[i];
                    }
                    //*/
                    outlength = 0;
                }
                return false;
            }
        }

        public override void Dispose()
        {
            //GET / HTTP/1.1\r\nHost: 192.168.0.100:1025\r\nUser-Agent: Mozilla/5.0 (Windows NT 6.3; WOW64; rv:40.0) Gecko/20100101 Firefox/40.0\r\nAccept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\nAccept-Language: en-US,en;q=0.8\r\nAccept-Encoding: gzip, deflate\r\nDNT: 1\r\nConnection: keep-alive\r\n\r\n
            //HTTP/1.1 200 OK\r\nServer: openresty\r\nDate: Mon, 07 Sep 2015 10:18:21 GMT\r\nContent-Type: text/plain; charset=utf-8\r\nTransfer-Encoding: chunked\r\nConnection: keep-alive\r\nKeep-Alive: timeout=20\r\nVary: Accept-Encoding\r\nContent-Encoding: gzip\r\n\r\n17\r\n\u001f\u008b\b\0\0\0\0\0\0\u0003ËÏæ\u0002\0}\u000e\u0016Ú\u0003\0\0\0\r\n0\r\n\r\n
        }
    }
}
