using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle;
using System.IO;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace Shadowsocks.Controller.Service
{
    class RSAHandler
    {
        public static void GenerateKeyPair()
        {
            RsaKeyPairGenerator keyGenerator = new RsaKeyPairGenerator();
            RsaKeyGenerationParameters param = new RsaKeyGenerationParameters(
                Org.BouncyCastle.Math.BigInteger.ValueOf(3),
                new Org.BouncyCastle.Security.SecureRandom(),
                1024,
                25);
            keyGenerator.Init(param);
            AsymmetricCipherKeyPair keyPair = keyGenerator.GenerateKeyPair();
            AsymmetricKeyParameter publicKey = keyPair.Public;
            AsymmetricKeyParameter privateKey = keyPair.Private;
            if (!Directory.Exists(System.Environment.CurrentDirectory + "\\keys"))
            {
                Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\keys");
            }
            if (File.Exists(System.Environment.CurrentDirectory + "\\keys\\local.private.pem"))
            {
                File.Delete(System.Environment.CurrentDirectory + "\\keys\\local.private.pem");
            }
            if (File.Exists(System.Environment.CurrentDirectory + "\\keys\\local.public.pem"))
            {
                File.Delete(System.Environment.CurrentDirectory + "\\keys\\local.public.pem");
            }
            FileStream fsPrivate = new FileStream(System.Environment.CurrentDirectory + "\\keys\\local.private.pem", FileMode.Create);
            FileStream fsPublic = new FileStream(System.Environment.CurrentDirectory + "\\keys\\local.public.pem", FileMode.Create);
            StreamWriter swPrivate = new StreamWriter(fsPrivate);
            StreamWriter swPublic = new StreamWriter(fsPublic);
            using (TextWriter textWriter = new StringWriter())
            {
                var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(textWriter);
                pemWriter.WriteObject(keyPair.Private);
                swPrivate.Write(textWriter.ToString());
                swPrivate.Flush();
                swPrivate.Close();
                fsPrivate.Close();
            }
            using (TextWriter textWriter = new StringWriter())
            {
                var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(textWriter);
                pemWriter.WriteObject(keyPair.Public);
                swPublic.Write(textWriter.ToString());
                swPublic.Flush();
                swPublic.Close();
                fsPublic.Close();
            }
        }
        /// <summary>
        /// 数据加密处理
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string EncryptRSA(string text, string publicKey)
        {
            string value = "";
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    #region 分段加密 解决加密明文过长问题

                    text = Convert.ToBase64String(Encoding.GetEncoding("utf-8").GetBytes(text));
                    text = text.Replace("\r", "").Replace("\n", "");
                    int len = 117;
                    int m = text.Length / len;
                    if (m * len != text.Length)
                    {
                        m = m + 1;
                    }
                    for (int i = 0; i < m; i++)
                    {
                        string temp = "";
                        if (i < m - 1)
                        {
                            temp = text.Substring(i * len, len);//(i + 1) * len
                        }
                        else
                        {
                            temp = text.Substring(i * len);
                        }
                        temp = Convert.ToBase64String(Encrypt(Encoding.GetEncoding("utf-8").GetBytes(temp), publicKey));
                        temp = temp.Replace("\r", "").Replace("\n", "");
                        value += temp;
                    }

                    #endregion
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }


            return value;
        }
        /// <summary>
        /// 数据解密处理
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string DecryptRSA(string text, string privateKey)
        {
            string value = "";
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    #region 分段解密 解决加密密文过长问题
                    int len = 172;
                    int m = text.Length / len;
                    if (m * len != text.Length)
                    {
                        m = m + 1;
                    }

                    for (int i = 0; i < m; i++)
                    {
                        string temp = "";

                        if (i < m - 1)
                        {
                            temp = text.Substring(i * len, len);
                        }
                        else
                        {
                            temp = text.Substring(i * len);
                        }
                        temp = decode64(temp);

                        temp = Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(temp), privateKey));
                        value += temp;

                    }
                    #endregion
                    value = decode64(value);
                    value = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message);
            }
            return value;
        }

        /// <summary>
        /// RSA加密操作 Encrypt方法
        /// </summary>
        /// <param name="DataToEncrypt"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        private static byte[] Encrypt(byte[] DataToEncrypt, string publicKey)
        {
            try
            {
                PemReader r = new PemReader(new StreamReader(publicKey));
                Object readObject = r.ReadObject();
                AsymmetricKeyParameter pubKey = (AsymmetricKeyParameter)readObject;
                IBufferedCipher c = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
                c.Init(true, pubKey);
                byte[] outBytes = c.DoFinal(DataToEncrypt);
                return outBytes;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// RSA解密操作 Decrypt方法
        /// </summary>
        /// <param name="input"></param>
        /// <param name="privateKey"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private static byte[] Decrypt(byte[] input, string privateKey)
        {
            try
            {
                AsymmetricCipherKeyPair keyPair;
                using (var reader = File.OpenText(privateKey))
                    keyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();
                AsymmetricKeyParameter priKey = keyPair.Private;
                IBufferedCipher c = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
                c.Init(false, priKey);
                byte[] outBytes = c.DoFinal(input);
                return outBytes;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 解决PHP Base64编码后回车问题 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static String decode64(String text)
        {
            String value = "";

            try
            {
                text = string.IsNullOrEmpty(text) ? "" : text;

                int len = 64;
                int m = text.Length / len;
                if (m * len != text.Length)
                {
                    m = m + 1;
                }

                for (int i = 0; i < m; i++)
                {
                    String temp = "";

                    if (i < m - 1)
                    {
                        temp = text.Substring(i * len, len);
                        value += temp + "\r\n";
                    }
                    else
                    {
                        temp = text.Substring(i * len);
                        value += temp;
                    }
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return value;
        }
    }

    /// <summary>
    /// BouncyCastle 密钥类
    /// </summary>
    public class Password : IPasswordFinder
    {
        private char[] password;

        public Password(char[] word)
        {
            this.password = (char[])word.Clone();
        }

        public char[] GetPassword()
        {
            return (char[])password.Clone();
        }
    }
}
