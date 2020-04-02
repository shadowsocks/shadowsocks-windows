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
        Random random = new Random();
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

        private void RunEncryptionRound(IEncryptor encryptor, IEncryptor decryptor)
        {
            RNG.Reload();
            byte[] plain = new byte[16384];
            byte[] cipher = new byte[plain.Length + 100];// AEAD with IPv4 address type needs +100
            byte[] plain2 = new byte[plain.Length + 16];

            //random = new Random();

            random.NextBytes(plain);
            encryptor.Encrypt(plain, plain.Length, cipher, out int outLen);
            decryptor.Decrypt(cipher, outLen, plain2, out int outLen2);
            Assert.AreEqual(plain.Length, outLen2);
            for (int j = 0; j < plain.Length; j++)
            {
                Assert.AreEqual(plain[j], plain2[j]);
            }
            encryptor.Encrypt(plain, 1000, cipher, out outLen);
            decryptor.Decrypt(cipher, outLen, plain2, out outLen2);
            Assert.AreEqual(1000, outLen2);
            for (int j = 0; j < outLen2; j++)
            {
                Assert.AreEqual(plain[j], plain2[j]);
            }
            encryptor.Encrypt(plain, 12333, cipher, out outLen);
            decryptor.Decrypt(cipher, outLen, plain2, out outLen2);
            Assert.AreEqual(12333, outLen2);
            for (int j = 0; j < outLen2; j++)
            {
                Assert.AreEqual(plain[j], plain2[j]);
            }
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
                encryptor.AddressBufferLength = 1 + 4 + 2;// ADDR_ATYP_LEN + 4 + ADDR_PORT_LEN;
                decryptor.AddressBufferLength = 1 + 4 + 2;// ADDR_ATYP_LEN + 4 + ADDR_PORT_LEN;


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
            RNG.Close();
            Assert.IsFalse(encryptionFailed);
        }

        [TestMethod]
        public void TestBouncyCastleAEADEncryption()
        {
            TestEncryptionMethod(typeof(AEADBouncyCastleEncryptor), "aes-128-gcm");
            TestEncryptionMethod(typeof(AEADBouncyCastleEncryptor), "aes-192-gcm");
            TestEncryptionMethod(typeof(AEADBouncyCastleEncryptor), "aes-256-gcm");
        }

        [TestMethod]
        public void TestAesGcmNativeAEADEncryption()
        {
            TestEncryptionMethod(typeof(AEADAesGcmNativeEncryptor), "aes-128-gcm");
            TestEncryptionMethod(typeof(AEADAesGcmNativeEncryptor), "aes-192-gcm");
            TestEncryptionMethod(typeof(AEADAesGcmNativeEncryptor), "aes-256-gcm");
        }

        [TestMethod]
        public void TestNaClAEADEncryption()
        {
            TestEncryptionMethod(typeof(AEADNaClEncryptor), "chacha20-ietf-poly1305");
            TestEncryptionMethod(typeof(AEADNaClEncryptor), "xchacha20-ietf-poly1305");
        }

        [TestMethod]
        public void TestNativeEncryption()
        {
            TestEncryptionMethod(typeof(StreamTableNativeEncryptor), "plain");
            TestEncryptionMethod(typeof(StreamRc4NativeEncryptor), "rc4");
            TestEncryptionMethod(typeof(StreamRc4NativeEncryptor), "rc4-md5");
        }

        [TestMethod]
        public void TestNativeTableEncryption()
        {
            // Too slow, run once to save CPU
            var enc = new StreamTableNativeEncryptor("table", "barfoo!");
            var dec = new StreamTableNativeEncryptor("table", "barfoo!");
            RunEncryptionRound(enc, dec);
        }
    }
}