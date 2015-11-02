
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
        public virtual void SetServerInfo(ServerInfo serverInfo)
        {
            Server = serverInfo;
        }
        public int GetHeadSize(byte[] plaindata, int defaultValue)
        {
            if (plaindata == null || plaindata.Length < 2)
                return defaultValue;
            if (plaindata[0] == 1)
                return 7;
            if (plaindata[0] == 4)
                return 19;
            if (plaindata[0] == 3)
                return 4 + plaindata[1];
            return defaultValue;
        }
    }
}
