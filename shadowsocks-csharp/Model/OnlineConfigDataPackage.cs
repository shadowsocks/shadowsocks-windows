using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shadowsocks.Model
{
    class KeyExchangeResponse
    {
        public string FingerPrint;
        public string ServerPublicKey;
        public string VerifyString;
    }
    class GetConfigRequest
    {
        public string decrypted_string;
        public string verify_string;
    }
    class GetConfigResponse
    {
        public Server[] Config;
        public string VerifyString;
        public string Provider;
    }
}
