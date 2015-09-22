using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Obfs
{
    public class Plain : ObfsBase
    {
        public Plain(string method)
            : base(method)
        {
        }
        private static Dictionary<string, int[]> _obfs = new Dictionary<string, int[]> {
                {"plain", new int[]{}},
        };

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
        }

        public override bool ClientEncode(byte[] encryptdata, int datalength, byte[] outdata, out int outlength)
        {
            Array.Copy(encryptdata, 0, outdata, 0, datalength);
            outlength = datalength;
            return false;
        }
        public override bool ClientDecode(byte[] encryptdata, int datalength, byte[] outdata, out int outlength)
        {
            Array.Copy(encryptdata, 0, outdata, 0, datalength);
            outlength = datalength;
            return false;
        }

        public override void Dispose()
        {
        }
    }
}
