using System;

namespace Shadowsocks.Model
{
    [Serializable]
    public class KeywordPattern
    {
        public string keyword;
        public string regex;
        public string proxy;
    }
}
