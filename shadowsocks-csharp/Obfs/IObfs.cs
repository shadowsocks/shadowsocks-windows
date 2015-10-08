using System;

namespace Shadowsocks.Obfs
{
    public interface IObfs : IDisposable
    {
        byte[] ClientPreEncrypt(byte[] plaindata, int datalength, out int outlength);
        byte[] ClientEncode(byte[] encryptdata, int datalength, out int outlength);
        byte[] ClientDecode(byte[] encryptdata, int datalength, out int outlength, out bool needsendback);
        byte[] ClientPostDecrypt(byte[] plaindata, int datalength, out int outlength);
        void SetHost(string host, int port);
    }
}
