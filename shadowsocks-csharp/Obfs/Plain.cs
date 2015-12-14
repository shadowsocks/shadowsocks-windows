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
                {"plain", new int[]{0, 0, 0}},
                {"origin", new int[]{0, 0, 0}},
        };

        public static List<string> SupportedObfs()
        {
            return new List<string>(_obfs.Keys);
        }

        public override Dictionary<string, int[]> GetObfs()
        {
            return _obfs;
        }

        public override byte[] ClientEncode(byte[] encryptdata, int datalength, out int outlength)
        {
            outlength = datalength;
            SentLength += outlength;
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
