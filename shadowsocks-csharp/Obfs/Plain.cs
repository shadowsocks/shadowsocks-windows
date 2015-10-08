using System;
using System.Collections.Generic;

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

        public override byte[] ClientEncode(byte[] encryptdata, int datalength, out int outlength)
        {
            outlength = datalength;
            return encryptdata;
        }
        public override byte[] ClientDecode(byte[] encryptdata, int datalength, out int outlength, out bool needsendback)
        {
            outlength = datalength;
            needsendback = false;
            return encryptdata;
        }

        public override void Dispose()
        {
        }
    }
}
