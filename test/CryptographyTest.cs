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
                string md5str3 = Convert.ToBase64String(MbedTLS.MD5(bytes));
                Assert.IsTrue(md5str == md5str2);
                Assert.IsTrue(md5str == md5str3);
            }
        }

        [TestMethod]
        public void TestHKDF()
        {
            int keylen = 32;
            int saltlen = 32;
            byte[] master = new byte[keylen];
            byte[] info = new byte[8];
            byte[] salt = new byte[saltlen];
            Random r = new Random();
            r.NextBytes(master);
            r.NextBytes(info);
            r.NextBytes(salt);
            string bchkdf = Convert.ToBase64String(CryptoUtils.HKDF(keylen, master, salt, info));
            string mbhkdf = Convert.ToBase64String(MbedTLShkdf(keylen, master, salt, info));

            Assert.IsTrue(bchkdf == mbhkdf);
        }

        private byte[] MbedTLShkdf(int keylen, byte[] master, byte[] salt, byte[] info)
        {
            byte[] sessionKey = new byte[keylen];
            MbedTLS.hkdf(salt, salt.Length, master, keylen, info, info.Length, sessionKey, keylen);
            return sessionKey;
        }

        [TestMethod]
        public void TestSodiumIncrement()
        {
            byte[] full255 = new byte[32];
            for (int i = 0; i < full255.Length; i++)
            {
                full255[i] = 255;
            }

            byte[] zero255 = new byte[32];
            zero255[0] = 255;

            byte[] rd = new byte[32];
            new Random().NextBytes(rd);
            RunSodiumIncrement(full255);
            RunSodiumIncrement(zero255);
            RunSodiumIncrement(rd);

        }

        private void RunSodiumIncrement(byte[] data)
        {
            byte[] data2 = new byte[data.Length];
            data.CopyTo(data2, 0);
            CryptoUtils.SodiumIncrement(data);
            Sodium.sodium_increment(data2, data2.Length);

            for (int i = 0; i < data.Length; i++)
            {
                Assert.AreEqual(data[i], data2[i]);
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
                    IEncryptor encryptorO, encryptorN, encryptorN2;
                    IEncryptor decryptorO, decryptorN, decryptorN2;
                    encryptorO = new StreamOpenSSLEncryptor("rc4-md5", "barfoo!");
                    decryptorO = new StreamOpenSSLEncryptor("rc4-md5", "barfoo!");
                    encryptorN = new StreamNativeEncryptor("rc4-md5", "barfoo!");
                    encryptorN2 = new StreamNativeEncryptor("rc4-md5", "barfoo!");
                    decryptorN = new StreamNativeEncryptor("rc4-md5", "barfoo!");
                    decryptorN2 = new StreamNativeEncryptor("rc4-md5", "barfoo!");
                    RunEncryptionRound(encryptorN, decryptorN);
                    RunEncryptionRound(encryptorO, decryptorN2);
                    RunEncryptionRound(encryptorN2, decryptorO);
                }
            }
            catch
            {
                encryptionFailed = true;
                throw;
            }
        }

        [TestMethod]
        public void TestOpenSSLAEADEncryption()
        {
            encryptionFailed = false;
            // run it once before the multi-threading test to initialize global tables
            RunSingleOpenSSLAEADEncryptionThread();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(RunSingleOpenSSLAEADEncryptionThread));
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

        private void RunSingleOpenSSLAEADEncryptionThread()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var random = new Random();
                    IEncryptor encryptor;
                    IEncryptor decryptor;
                    encryptor = new Encryption.AEAD.AEADOpenSSLEncryptor("aes-256-gcm", "barfoo!");
                    encryptor.AddrBufLength = 1 + 4 + 2;// ADDR_ATYP_LEN + 4 + ADDR_PORT_LEN;
                    decryptor = new Encryption.AEAD.AEADOpenSSLEncryptor("aes-256-gcm", "barfoo!");
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
        public void TestMBedTLSAEADEncryption()
        {
            encryptionFailed = false;
            // run it once before the multi-threading test to initialize global tables
            RunSingleMBedTLSAEADEncryptionThread();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(RunSingleMBedTLSAEADEncryptionThread));
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

        private void RunSingleMBedTLSAEADEncryptionThread()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var random = new Random();
                    IEncryptor encryptor;
                    IEncryptor decryptor;
                    encryptor = new Encryption.AEAD.AEADMbedTLSEncryptor("aes-256-gcm", "barfoo!");
                    encryptor.AddrBufLength = 1 + 4 + 2;// ADDR_ATYP_LEN + 4 + ADDR_PORT_LEN;
                    decryptor = new Encryption.AEAD.AEADMbedTLSEncryptor("aes-256-gcm", "barfoo!");
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
    }
}