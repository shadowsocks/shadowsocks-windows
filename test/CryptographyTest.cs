using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Encryption;
using Shadowsocks.Encryption.Stream;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Shadowsocks.Test
{
    [TestClass]
    public class CryptographyTest
    {

        [TestMethod]
        public void TestMD5()
        {
            for (int len = 1; len < 64; len++)
            {
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] bytes = new byte[len];
                var random = new Random();
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
            int outLen = 0;
            int outLen2 = 0;
            var random = new Random();
            random.NextBytes(plain);
            encryptor.Encrypt(plain, plain.Length, cipher, out outLen);
            decryptor.Decrypt(cipher, outLen, plain2, out outLen2);
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

        private static bool encryptionFailed = false;
        private static object locker = new object();

        [TestMethod]
        public void TestBouncyCastleAEADEncryption()
        {
            encryptionFailed = false;
            // run it once before the multi-threading test to initialize global tables
            RunSingleBouncyCastleAEADEncryptionThread();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(RunSingleBouncyCastleAEADEncryptionThread)); threads.Add(t);
                t.Start();
            }
            foreach (Thread t in threads)
            {
                t.Join();
            }
            RNG.Close();
            Assert.IsFalse(encryptionFailed);
        }

        private void RunSingleBouncyCastleAEADEncryptionThread()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var random = new Random();
                    IEncryptor encryptor;
                    IEncryptor decryptor;
                    encryptor = new Encryption.AEAD.AEADBouncyCastleEncryptor("aes-256-gcm", "barfoo!");
                    encryptor.AddrBufLength = 1 + 4 + 2;// ADDR_ATYP_LEN + 4 + ADDR_PORT_LEN;
                    decryptor = new Encryption.AEAD.AEADBouncyCastleEncryptor("aes-256-gcm", "barfoo!");
                    decryptor.AddrBufLength = 1 + 4 + 2;// ADDR_ATYP_LEN + 4 + ADDR_PORT_LEN;
                    RunEncryptionRound(encryptor, decryptor);
                }
            }
            catch
            {
                encryptionFailed = true;
                throw;
            }
        }

        [TestMethod]
        public void TesNaClAEADEncryption()
        {
            encryptionFailed = false;
            // run it once before the multi-threading test to initialize global tables
            RunSingleNaClAEADEncryptionThread();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(RunSingleNaClAEADEncryptionThread)); threads.Add(t);
                t.Start();
            }
            foreach (Thread t in threads)
            {
                t.Join();
            }
            RNG.Close();
            Assert.IsFalse(encryptionFailed);
        }

        private void RunSingleNaClAEADEncryptionThread()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var random = new Random();
                    IEncryptor encryptor;
                    IEncryptor decryptor;
                    encryptor = new Encryption.AEAD.AEADNaClEncryptor("chacha20-ietf-poly1305", "barfoo!");
                    encryptor.AddrBufLength = 1 + 4 + 2;// ADDR_ATYP_LEN + 4 + ADDR_PORT_LEN;
                    decryptor = new Encryption.AEAD.AEADNaClEncryptor("chacha20-ietf-poly1305", "barfoo!");
                    decryptor.AddrBufLength = 1 + 4 + 2;// ADDR_ATYP_LEN + 4 + ADDR_PORT_LEN;
                    RunEncryptionRound(encryptor, decryptor);
                }
            }
            catch
            {
                encryptionFailed = true;
                throw;
            }
        }

        [TestMethod]
        public void TestNativeEncryption()
        {
            encryptionFailed = false;
            // run it once before the multi-threading test to initialize global tables
            RunSingleNativeEncryptionThread();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(RunSingleNativeEncryptionThread));
                threads.Add(t);
                t.Start();
            }
            foreach (Thread t in threads)
            {
                t.Join();
            }
            RNG.Close();
            Assert.IsFalse(encryptionFailed);
        }

        private void RunSingleNativeEncryptionThread()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    IEncryptor encryptorN;
                    IEncryptor decryptorN;
                    encryptorN = new StreamRc4NativeEncryptor("rc4-md5", "barfoo!");
                    decryptorN = new StreamRc4NativeEncryptor("rc4-md5", "barfoo!");
                    RunEncryptionRound(encryptorN, decryptorN);
                }
            }
            catch
            {
                encryptionFailed = true;
                throw;
            }
        }
    }
}