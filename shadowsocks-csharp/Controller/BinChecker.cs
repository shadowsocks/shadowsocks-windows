using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Shadowsocks.Encryption;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{
    class BinChecker
    {
        private static string PublicKeyXML = @"<RSAKeyValue><Modulus>v0ZR0OAA8Ml8mJX/Gj1cXwFf9t16fY3fvsijJTbzqUL+XT4WR3hjOJAqirS91KPK3yZwnx+cRIXLmthORq3S07xzYY4ukVgIU4Ya+jDxA4RaToiqOyuA2It46BZR0dQcsGDiFpnVYYIkAdM/0I9q6HvbSMbZkiGI1OB9U8KFsmXdwSwc4jQtN/Ex8BHsMHVyo07FwE7etca/SJR66N2aBAgKthQEHHwAcB2kEMpNYe/kr9Ifvz1hJK8VhDpVJDXmw7cI1/ZJinhUdgnKRXfCBPPafNEBCifNSXgsGGG8yNNjVNKn+k/X3q5UNxTNS5l93d91q02rlpVkGWzaiqtPsQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        private const string UpdateURL = "https://raw.githubusercontent.com/breakwa11/breakwa11.github.io/master/check/" + UpdateChecker.Version + "-" + UpdateChecker.NetVer + ".txt";
        public event EventHandler CheckCodeFound;
        protected string response;
        protected string hash;

        public static bool CheckBin()
        {
            try
            {
                string pubkey_path = Path.Combine(System.Windows.Forms.Application.StartupPath, "key.public");
                try
                {
                    using (FileStream fs = new FileStream(pubkey_path, FileMode.Open, FileAccess.Read))
                    {
                        using (StreamReader r = new StreamReader(fs))
                        {
                            PublicKeyXML = r.ReadToEnd();
                        }
                    }
                }
                catch
                {
                }

                string filePath = Util.Utils.GetExecutablePath() + "_sign";
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader r = new StreamReader(fs))
                    {
                        byte[] sign = System.Convert.FromBase64String(r.ReadToEnd());
                        return RSA.SignatureVerify(PublicKeyXML, Util.Utils.BinarySHA512(), sign);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e != null ? e.ToString() : "", e.Message);
                return false;
            }
        }

        public void CheckUpdate(Configuration config)
        {
            try
            {
                WebClient http = new WebClient();
                http.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.3319.102 Safari/537.36");
                WebProxy proxy = new WebProxy(IPAddress.Loopback.ToString(), config.localPort);
                if (config.authPass != null && config.authPass.Length > 0)
                {
                    proxy.Credentials = new NetworkCredential(config.authUser, config.authPass);
                }
                http.Proxy = proxy;

                http.DownloadStringCompleted += http_DownloadStringCompleted;
                http.DownloadStringAsync(new Uri(UpdateURL));
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public bool needCheck()
        {
            return response == null;
        }

        public string get_version()
        {
            if (response != null && response.Length > hash.Length && response.Substring(0, hash.Length) == hash)
            {
                return response.Substring(hash.Length);
            }
            return null;
        }

        private void http_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (response != e.Result)
                {
                    response = e.Result;
                    hash = Util.Utils.BinarySHA512hex();

                    if (CheckCodeFound != null)
                    {
                        CheckCodeFound(this, new EventArgs());
                    }
                }
            }
            catch (Exception ex)
            {
                if (e.Error != null)
                {
                    Logging.Debug(e.Error.ToString());
                }
                Logging.Debug(ex.ToString());
                if (CheckCodeFound != null)
                {
                    CheckCodeFound(this, new EventArgs());
                }
            }
        }
    }
}
