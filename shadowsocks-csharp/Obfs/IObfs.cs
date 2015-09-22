using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Obfs
{
    public interface IObfs : IDisposable
    {
        bool ClientEncode(byte[] encryptdata, int datalength, byte[] outdata, out int outlength);
        bool ClientDecode(byte[] encryptdata, int datalength, byte[] outdata, out int outlength); // return true if need to send data
        void SetHost(string host, int port);
    }
}
