using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Obfs
{
    public abstract class ObfsBase: IObfs
    {
        protected ObfsBase(string method)
        {
            Method = method;
        }

        protected string Method;
        protected string Host;
        protected int Port;

        public abstract bool ClientEncode(byte[] encryptdata, int datalength, byte[] outdata, out int outlength);
        public abstract bool ClientDecode(byte[] encryptdata, int datalength, byte[] outdata, out int outlength);
        public abstract void Dispose();
        public void SetHost(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}
