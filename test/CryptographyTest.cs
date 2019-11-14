using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Encryption;
using System.Threading;
using System.Collections.Generic;
using Shadowsocks.Encryption.Stream;
using System.Diagnostics;

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
                string md5str2 = Convert.ToBase64String(MbedTLS.MD5(bytes));
                Assert.IsTrue(md5str == md5str2);
            }
        }

        private void RunEncryptionRound(IEncryptor encryptor, IEncryptor decryptor)
        {
            RNG.Reload();
            byte[] plain = new byte[16384];
            byte[] cipher = new byte[plain.Length + 16];
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
        public void TestMbedTLSEncryption()
        {
            encryptionFailed = false;
            // run it once before the multi-threading test to initialize global tables
            RunSingleMbedTLSEncryptionThread();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(RunSingleMbedTLSEncryptionThread));
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

        private void RunSingleMbedTLSEncryptionThread()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    IEncryptor encryptor;
                    IEncryptor decryptor;
                    encryptor = new StreamMbedTLSEncryptor("aes-256-cfb", "barfoo!");
                    decryptor = new StreamMbedTLSEncryptor("aes-256-cfb", "barfoo!");
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
        public void TestRC4Encryption()
        {
            encryptionFailed = false;
            // run it once before the multi-threading test to initialize global tables
            RunSingleRC4EncryptionThread();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(RunSingleRC4EncryptionThread));
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

        private void RunSingleRC4EncryptionThread()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var random = new Random();
                    IEncryptor encryptor;
                    IEncryptor decryptor;
                    encryptor = new StreamMbedTLSEncryptor("rc4-md5", "barfoo!");
                    decryptor = new StreamMbedTLSEncryptor("rc4-md5", "barfoo!");
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
        public void TestSodiumEncryption()
        {
            encryptionFailed = false;
            // run it once before the multi-threading test to initialize global tables
            RunSingleSodiumEncryptionThread();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(RunSingleSodiumEncryptionThread));
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

        private void RunSingleSodiumEncryptionThread()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var random = new Random();
                    IEncryptor encryptor;
                    IEncryptor decryptor;
                    encryptor = new StreamSodiumEncryptor("salsa20", "barfoo!");
                    decryptor = new StreamSodiumEncryptor("salsa20", "barfoo!");
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
        public void TestOpenSSLEncryption()
        {
            encryptionFailed = false;
            // run it once before the multi-threading test to initialize global tables
            RunSingleOpenSSLEncryptionThread();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(RunSingleOpenSSLEncryptionThread));
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

        private void RunSingleOpenSSLEncryptionThread()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var random = new Random();
                    IEncryptor encryptor;
                    IEncryptor decryptor;
                    encryptor = new StreamOpenSSLEncryptor("aes-256-cfb", "barfoo!");
                    decryptor = new StreamOpenSSLEncryptor("aes-256-cfb", "barfoo!");
                    RunEncryptionRound(encryptor, decryptor);
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
