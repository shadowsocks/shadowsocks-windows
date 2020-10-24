using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Encryption;
using Shadowsocks.Encryption.Stream;
using Shadowsocks.Encryption.AEAD;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Shadowsocks.Test
{
    [TestClass]
    public class CryptographyTest
    {
        readonly Random random = new Random();


        [TestMethod]
        public void TestMD5()
        {
            for (int len = 1; len < 64; len++)
            {
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] bytes = new byte[len];
                random.NextBytes(bytes);
                string md5str = Convert.ToBase64String(md5.ComputeHash(bytes));
                string md5str2 = Convert.ToBase64String(CryptoUtils.MD5(bytes));
                Assert.IsTrue(md5str == md5str2);
            }
        }

        #region Encryptor test tools
        private void SingleEncryptionTestCase(IEncryptor encryptor, IEncryptor decryptor, int length)
        {
            RNG.Reload();
            byte[] plain = new byte[length];
            byte[] cipher = new byte[plain.Length + 100];// AEAD with IPv4 address type needs +100
            byte[] plain2 = new byte[plain.Length + 16];

            random.NextBytes(plain);
            int outLen = encryptor.Encrypt(plain, cipher);
            int outLen2 = decryptor.Decrypt(plain2, cipher.AsSpan(0, outLen));
            //encryptor.Encrypt(plain, length, cipher, out int outLen);
            //decryptor.Decrypt(cipher, outLen, plain2, out int outLen2);
            Assert.AreEqual(length, outLen2);
            TestUtils.ArrayEqual<byte>(plain.AsSpan(0, length).ToArray(), plain2.AsSpan(0, length).ToArray());
        }

        const string password = "barfoo!";

        private void RunSingleEncryptionThread(Type enc, Type dec, string method)
        {
            var ector = enc.GetConstructor(new Type[] { typeof(string), typeof(string) });
            var dctor = dec.GetConstructor(new Type[] { typeof(string), typeof(string) });
            try
            {
                IEncryptor encryptor = (IEncryptor)ector.Invoke(new object[] { method, password });
                IEncryptor decryptor = (IEncryptor)dctor.Invoke(new object[] { method, password });
                
                for (int i = 0; i < 16; i++)
                {
                    RunEncryptionRound(encryptor, decryptor);
                }
            }
            catch
            {
                encryptionFailed = true;
                throw;
            }
        }
        private static bool encryptionFailed = false;

        private void TestEncryptionMethod(Type enc, string method)
        {
            TestEncryptionMethod(enc, enc, method);
        }
        private void TestEncryptionMethod(Type enc, Type dec, string method)
        {
            encryptionFailed = false;

            // run it once before the multi-threading test to initialize global tables
            RunSingleEncryptionThread(enc, dec, method);
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 8; i++)
            {
                Thread t = new Thread(new ThreadStart(() => RunSingleEncryptionThread(enc, dec, method))); threads.Add(t);
                t.Start();
            }
            foreach (Thread t in threads)
            {
                t.Join();
            }
            Assert.IsFalse(encryptionFailed);
        }
        #endregion

        // encryption test cases
        private void RunEncryptionRound(IEncryptor encryptor, IEncryptor decryptor)
        {
            SingleEncryptionTestCase(encryptor, decryptor, 7);      // for not aligned data
            SingleEncryptionTestCase(encryptor, decryptor, 1000);
            SingleEncryptionTestCase(encryptor, decryptor, 12333);
            SingleEncryptionTestCase(encryptor, decryptor, 16384);
        }

        [TestMethod]
        public void TestAEADAesGcmNativeEncryption()
        {
            TestEncryptionMethod(typeof(AEADAesGcmNativeEncryptor), "aes-128-gcm");
            TestEncryptionMethod(typeof(AEADAesGcmNativeEncryptor), "aes-192-gcm");
            TestEncryptionMethod(typeof(AEADAesGcmNativeEncryptor), "aes-256-gcm");
        }

        [TestMethod]
        public void TestAEADNaClEncryption()
        {
            TestEncryptionMethod(typeof(AEADNaClEncryptor), "chacha20-ietf-poly1305");
            TestEncryptionMethod(typeof(AEADNaClEncryptor), "xchacha20-ietf-poly1305");
        }

        [TestMethod]
        public void TestStreamNativeEncryption()
        {
            TestEncryptionMethod(typeof(StreamPlainNativeEncryptor), "plain");
            TestEncryptionMethod(typeof(StreamRc4NativeEncryptor), "rc4");
            TestEncryptionMethod(typeof(StreamRc4NativeEncryptor), "rc4-md5");
        }

        [TestMethod]
        public void TestStreamAesCfbBouncyCastleEncryption()
        {
            TestEncryptionMethod(typeof(StreamAesCfbBouncyCastleEncryptor), "aes-128-cfb");
            TestEncryptionMethod(typeof(StreamAesCfbBouncyCastleEncryptor), "aes-192-cfb");
            TestEncryptionMethod(typeof(StreamAesCfbBouncyCastleEncryptor), "aes-256-cfb");
        }
        [TestMethod]
        public void TestStreamChachaNaClEncryption()
        {
            TestEncryptionMethod(typeof(StreamChachaNaClEncryptor), "chacha20-ietf");
        }
    }
}