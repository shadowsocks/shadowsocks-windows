
using System.Collections.Generic;

namespace Shadowsocks.Obfs
{
    public abstract class ObfsBase: IObfs
    {
        protected ObfsBase(string method)
        {
            Method = method;
        }

        protected string Method;
        protected ServerInfo Server;

        public abstract Dictionary<string, int[]> GetObfs();

        public virtual byte[] ClientPreEncrypt(byte[] plaindata, int datalength, out int outlength)
        {
            outlength = datalength;
            return plaindata;
        }
        public abstract byte[] ClientEncode(byte[] encryptdata, int datalength, out int outlength);
        public abstract byte[] ClientDecode(byte[] encryptdata, int datalength, out int outlength, out bool needsendback);
        public virtual byte[] ClientPostDecrypt(byte[] plaindata, int datalength, out int outlength)
        {
            outlength = datalength;
            return plaindata;
        }
        public abstract void Dispose();
        public virtual object InitData()
        {
            return null;
        }
        public void SetHost(ServerInfo serverInfo)
        {
            Server = serverInfo;
        }
    }
}
