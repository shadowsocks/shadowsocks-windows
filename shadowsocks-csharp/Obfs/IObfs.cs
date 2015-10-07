using System;

namespace Shadowsocks.Obfs
{
    public interface IObfs : IDisposable
    {
        byte[] ClientPreEncrypt(byte[] plaindata, int datalength, out int outlength);
        bool ClientEncode(byte[] encryptdata, int datalength, byte[] outdata, out int outlength);
        bool ClientDecode(byte[] encryptdata, int datalength, byte[] outdata, out int outlength); // return true if need to send data
        byte[] ClientPostDecrypt(byte[] plaindata, int datalength, out int outlength);
        void SetHost(string host, int port);
    }
}
