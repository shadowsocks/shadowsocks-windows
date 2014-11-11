#define CASE2

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Controller;
using Shadowsocks.Encrypt;
using System.Threading;
using System.Collections.Generic;

namespace test
{
    [TestClass]
    public class UnitTest
    {
        static IEncryptor encryptor;
        static IEncryptor decryptor;

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

        [TestMethod]
        public void TestEncryption()
        {
            //Test init encryptor in main thread but use in other thread situation
#if CASE1
            encryptor = new PolarSSLEncryptor("aes-256-cfb", "barfoo!");
            decryptor = new PolarSSLEncryptor("aes-256-cfb", "barfoo!");
#endif
#if CASE2
            encryptor = new OpenSSLEncryptor("aes-256-cfb", "barfoo!");
            decryptor = new OpenSSLEncryptor("aes-256-cfb", "barfoo!");
#endif
            // run it once before the multi-threading test to initialize global tables
            RunSingleEncryptionThread();

            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(RunSingleEncryptionThread));
                threads.Add(t);
                t.Start();
            }
            foreach (Thread t in threads)
            {
                t.Join();
            }
            Assert.IsFalse(encryptionFailed);
        }

        private static bool encryptionFailed = false;
        private static object locker = new object();
        private static object locker2 = new object();

        private void RunSingleEncryptionThread()
        {
            try
            {
                    for (int i = 0; i < 100; i++)
                    {
                        var random = new Random();

                        lock (locker)
                        {
                            //encryptor = new PolarSSLEncryptor("aes-256-cfb", "barfoo!");
                            //decryptor = new PolarSSLEncryptor("aes-256-cfb", "barfoo!");
                            //encryptor = new OpenSSLEncryptor("aes-256-cfb", "barfoo!");
                            //decryptor = new OpenSSLEncryptor("aes-256-cfb", "barfoo!");
                        }
                        byte[] plain = new byte[16384];
                        byte[] cipher = new byte[plain.Length + 16];
                        byte[] plain2 = new byte[plain.Length + 16];
                        int outLen = 0;
                        int outLen2 = 0;
                        random.NextBytes(plain);
                        lock (locker)
                        {
                            encryptor.Encrypt(plain, plain.Length, cipher, out outLen);
                            decryptor.Decrypt(cipher, outLen, plain2, out outLen2);
                            Assert.AreEqual(plain.Length, outLen2);
                            encryptor.Encrypt(plain, 1000, cipher, out outLen);
                            decryptor.Decrypt(cipher, outLen, plain2, out outLen2);
                            Assert.AreEqual(1000, outLen2);
                            encryptor.Encrypt(plain, 12333, cipher, out outLen);                      
                            decryptor.Decrypt(cipher, outLen, plain2, out outLen2);
                        }
                        Assert.AreEqual(12333, outLen2);
                        for (int j = 0; j < plain.Length; j++)
                        {
                            Assert.AreEqual(plain[j], plain2[j]);
                        }
                    }
            }
            catch
            {
                encryptionFailed = true;
            }
        }
    }
}
