using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Controller;
using Shadowsocks.Encryption;
using System.Threading;
using System.Collections.Generic;

namespace test
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestCompareVersion()
        {
            Assert.IsTrue(UpdateChecker.CompareVersion("2.3.1.0", "2.3.1") == 0);
            Assert.IsTrue(UpdateChecker.CompareVersion("1.2", "1.3") < 0);
            Assert.IsTrue(UpdateChecker.CompareVersion("1.3", "1.2") > 0);
            Assert.IsTrue(UpdateChecker.CompareVersion("1.3", "1.3") == 0);
            Assert.IsTrue(UpdateChecker.CompareVersion("1.2.1", "1.2") > 0);
            Assert.IsTrue(UpdateChecker.CompareVersion("2.3.1", "2.4") < 0);
            Assert.IsTrue(UpdateChecker.CompareVersion("1.3.2", "1.3.1") > 0);
        }

        private void RunEncryptionRound(IEncryptor encryptor, IEncryptor decryptor)
        {
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
        public void TestPolarSSLEncryption()
        {
            // run it once before the multi-threading test to initialize global tables
            RunSinglePolarSSLEncryptionThread();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(RunSinglePolarSSLEncryptionThread));
                threads.Add(t);
                t.Start();
            }
            foreach (Thread t in threads)
            {
                t.Join();
            }
            Assert.IsFalse(encryptionFailed);
        }

        private void RunSinglePolarSSLEncryptionThread()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    IEncryptor encryptor;
                    IEncryptor decryptor;
                    encryptor = new MbedTLSEncryptor("aes-256-cfb", "barfoo!");
                    decryptor = new MbedTLSEncryptor("aes-256-cfb", "barfoo!");
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
                    encryptor = new MbedTLSEncryptor("rc4-md5", "barfoo!");
                    decryptor = new MbedTLSEncryptor("rc4-md5", "barfoo!");
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
                    encryptor = new SodiumEncryptor("salsa20", "barfoo!");
                    decryptor = new SodiumEncryptor("salsa20", "barfoo!");
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
