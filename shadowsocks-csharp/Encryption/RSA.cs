using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Shadowsocks.Encryption
{
    class RSA
    {
        public static bool SignatureVerify(string p_strKeyPublic, byte[] rgb, byte[] rgbSignature)
        {
            try
            {
                RSACryptoServiceProvider key = new RSACryptoServiceProvider();
                key.FromXmlString(p_strKeyPublic);
                RSAPKCS1SignatureDeformatter deformatter = new RSAPKCS1SignatureDeformatter(key);
                deformatter.SetHashAlgorithm("SHA512");
                if (deformatter.VerifySignature(rgb, rgbSignature))
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
