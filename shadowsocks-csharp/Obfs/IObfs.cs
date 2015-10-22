using System;

namespace Shadowsocks.Obfs
{
    public class ServerInfo
    {
        public string host;
        public int port;
        public int tcp_mss;
        public string param;
        public object data;

        public ServerInfo(string host, int port, int tcp_mss, string param, object data)
        {
            this.host = host;
            this.port = port;
            this.tcp_mss = tcp_mss;
            this.param = param;
            this.data = data;
        }
    }
    public interface IObfs : IDisposable
    {
        byte[] ClientPreEncrypt(byte[] plaindata, int datalength, out int outlength);
        byte[] ClientEncode(byte[] encryptdata, int datalength, out int outlength);
        byte[] ClientDecode(byte[] encryptdata, int datalength, out int outlength, out bool needsendback);
        byte[] ClientPostDecrypt(byte[] plaindata, int datalength, out int outlength);
        object InitData();
        void SetServerInfo(ServerInfo serverInfo);
    }
}
