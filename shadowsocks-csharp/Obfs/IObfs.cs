using System;

namespace Shadowsocks.Obfs
{
    public class ObfsException : Exception
    {
        public ObfsException(string info)
            : base(info)
        {

        }
    }
    public class ServerInfo
    {
        public string host;
        public int port;
        public string param;
        public object data;
        public int tcp_mss;
        public byte[] iv;
        public byte[] key;
        public int head_len;

        public ServerInfo(string host, int port, string param, object data, byte[] iv, byte[] key, int head_len, int tcp_mss)
        {
            this.host = host;
            this.port = port;
            this.param = param;
            this.data = data;
            this.iv = iv;
            this.key = key;
            this.head_len = head_len;
            this.tcp_mss = tcp_mss;
        }

        public void SetIV(byte[] iv)
        {
            this.iv = iv;
        }
    }
    public interface IObfs : IDisposable
    {
        byte[] ClientPreEncrypt(byte[] plaindata, int datalength, out int outlength);
        byte[] ClientEncode(byte[] encryptdata, int datalength, out int outlength);
        byte[] ClientDecode(byte[] encryptdata, int datalength, out int outlength, out bool needsendback);
        byte[] ClientPostDecrypt(byte[] plaindata, int datalength, out int outlength);
        byte[] ClientUdpPreEncrypt(byte[] plaindata, int datalength, out int outlength);
        byte[] ClientUdpPostDecrypt(byte[] plaindata, int datalength, out int outlength);
        object InitData();
        void SetServerInfo(ServerInfo serverInfo);
        void SetServerInfoIV(byte[] iv);
        long getSentLength();
    }
}
