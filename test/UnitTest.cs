using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Controller;
using Shadowsocks.Encryption;
using GlobalHotKey;
using System.Windows.Input;
using System.Threading;
using System.Collections.Generic;
using Shadowsocks.Controller.Hotkeys;
using Shadowsocks.Encryption.Stream;
using Shadowsocks.Model;

namespace test
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestCompareVersion()
        {
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("2.3.1.0", "2.3.1") == 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("1.2", "1.3") < 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("1.3", "1.2") > 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("1.3", "1.3") == 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("1.2.1", "1.2") > 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("2.3.1", "2.4") < 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("1.3.2", "1.3.1") > 0);
        }

        [TestMethod]
        public void TestHotKey2Str()
        {
            Assert.AreEqual("Ctrl+A", HotKeys.HotKey2Str(Key.A, ModifierKeys.Control));
            Assert.AreEqual("Ctrl+Alt+D2", HotKeys.HotKey2Str(Key.D2, (ModifierKeys.Alt | ModifierKeys.Control)));
            Assert.AreEqual("Ctrl+Alt+Shift+NumPad7", HotKeys.HotKey2Str(Key.NumPad7, (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)));
            Assert.AreEqual("Ctrl+Alt+Shift+F6", HotKeys.HotKey2Str(Key.F6, (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)));
            Assert.AreNotEqual("Ctrl+Shift+Alt+F6", HotKeys.HotKey2Str(Key.F6, (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)));
        }

        [TestMethod]
        public void TestStr2HotKey()
        {
            Assert.IsTrue(HotKeys.Str2HotKey("Ctrl+A").Equals(new HotKey(Key.A, ModifierKeys.Control)));
            Assert.IsTrue(HotKeys.Str2HotKey("Ctrl+Alt+A").Equals(new HotKey(Key.A, (ModifierKeys.Control | ModifierKeys.Alt))));
            Assert.IsTrue(HotKeys.Str2HotKey("Ctrl+Shift+A").Equals(new HotKey(Key.A, (ModifierKeys.Control | ModifierKeys.Shift))));
            Assert.IsTrue(HotKeys.Str2HotKey("Ctrl+Alt+Shift+A").Equals(new HotKey(Key.A, (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))));
            HotKey testKey0 = HotKeys.Str2HotKey("Ctrl+Alt+Shift+A");
            Assert.IsTrue(testKey0 != null && testKey0.Equals(new HotKey(Key.A, (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))));
            HotKey testKey1 = HotKeys.Str2HotKey("Ctrl+Alt+Shift+F2");
            Assert.IsTrue(testKey1 != null && testKey1.Equals(new HotKey(Key.F2, (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))));
            HotKey testKey2 = HotKeys.Str2HotKey("Ctrl+Shift+Alt+D7");
            Assert.IsTrue(testKey2 != null && testKey2.Equals(new HotKey(Key.D7, (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))));
            HotKey testKey3 = HotKeys.Str2HotKey("Ctrl+Shift+Alt+NumPad7");
            Assert.IsTrue(testKey3 != null && testKey3.Equals(new HotKey(Key.NumPad7, (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))));
        }

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
        public void ParseAndGenerateShadowsocksUrl()
        {
            var server = new Server
            {
                server = "192.168.100.1",
                server_port = 8888,
                password = "test",
                method = "bf-cfb"
            };
            var serverCanonUrl = "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xMDAuMTo4ODg4";

            var server2 = new Server
            {
                server = "192.168.1.1",
                server_port = 8388,
                password = "test",
                method = "bf-cfb"
            };
            var server2CanonUrl = "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xLjE6ODM4OA==";

            var serverWithRemark = new Server
            {
                server = server.server,
                server_port = server.server_port,
                password = server.password,
                method = server.method,
                remarks = "example-server"
            };
            var serverWithRemarkCanonUrl = "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xMDAuMTo4ODg4#example-server";

            var server2WithRemark = new Server
            {
                server = server2.server,
                server_port = server2.server_port,
                password = server2.password,
                method = server2.method,
                remarks = "example-server"
            };
            var server2WithRemarkCanonUrl = "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xLjE6ODM4OA==#example-server";

            var serverWithPlugin = new Server
            {
                server = server.server,
                server_port = server.server_port,
                password = server.password,
                method = server.method,
                plugin = "obfs-local",
                plugin_opts = "obfs=http;obfs-host=google.com"
            };
            var serverWithPluginCanonUrl =
                "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888/?plugin=obfs-local%3bobfs%3dhttp%3bobfs-host%3dgoogle.com";

            var server2WithPlugin = new Server
            {
                server = server2.server,
                server_port = server2.server_port,
                password = server2.password,
                method = server2.method,
                plugin = "obfs-local",
                plugin_opts = "obfs=http;obfs-host=google.com"
            };
            var server2WithPluginCanonUrl =
                "ss://YmYtY2ZiOnRlc3Q@192.168.1.1:8388/?plugin=obfs-local%3bobfs%3dhttp%3bobfs-host%3dgoogle.com";

            var serverWithPluginAndRemark = new Server
            {
                server = server.server,
                server_port = server.server_port,
                password = server.password,
                method = server.method,
                plugin = serverWithPlugin.plugin,
                plugin_opts = serverWithPlugin.plugin_opts,
                remarks = serverWithRemark.remarks
            };
            var serverWithPluginAndRemarkCanonUrl =
                "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888/?plugin=obfs-local%3bobfs%3dhttp%3bobfs-host%3dgoogle.com#example-server";

            var server2WithPluginAndRemark = new Server
            {
                server = server2.server,
                server_port = server2.server_port,
                password = server2.password,
                method = server2.method,
                plugin = server2WithPlugin.plugin,
                plugin_opts = server2WithPlugin.plugin_opts,
                remarks = server2WithRemark.remarks
            };
            var server2WithPluginAndRemarkCanonUrl =
                "ss://YmYtY2ZiOnRlc3Q@192.168.1.1:8388/?plugin=obfs-local%3bobfs%3dhttp%3bobfs-host%3dgoogle.com#example-server";


            RunParseShadowsocksUrlTest(
                string.Join(
                    "\r\n",
                    serverCanonUrl,
                    "\r\n",
                    "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xMDAuMTo4ODg4/",
                    serverWithRemarkCanonUrl,
                    "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xMDAuMTo4ODg4/#example-server"),
                new[]
                {
                    server,
                    server,
                    serverWithRemark,
                    serverWithRemark
                });

            RunParseShadowsocksUrlTest(
                string.Join(
                    "\r\n",
                    server2CanonUrl,
                    "\r\n",
                    "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xLjE6ODM4OA==/",
                    server2WithRemarkCanonUrl,
                    "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xLjE6ODM4OA==/#example-server"),
                new[]
                {
                    server2,
                    server2,
                    server2WithRemark,
                    server2WithRemark
                });

            RunParseShadowsocksUrlTest(
                string.Join(
                    "\r\n",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888",
                    "\r\n",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888/",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888#example-server",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888/#example-server",
                    serverWithPluginCanonUrl,
                    serverWithPluginAndRemarkCanonUrl,
                    "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888/?plugin=obfs-local%3bobfs%3dhttp%3bobfs-host%3dgoogle.com&unsupported=1#example-server"),
                new[]
                {
                    server,
                    server,
                    serverWithRemark,
                    serverWithRemark,
                    serverWithPlugin,
                    serverWithPluginAndRemark,
                    serverWithPluginAndRemark
                });

            RunParseShadowsocksUrlTest(
                string.Join(
                    "\r\n",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.1.1:8388",
                    "\r\n",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.1.1:8388/",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.1.1:8388#example-server",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.1.1:8388/#example-server",
                    server2WithPluginCanonUrl,
                    server2WithPluginAndRemarkCanonUrl,
                    "ss://YmYtY2ZiOnRlc3Q@192.168.1.1:8388/?plugin=obfs-local%3bobfs%3dhttp%3bobfs-host%3dgoogle.com&unsupported=1#example-server"),
                new[]
                {
                    server2,
                    server2,
                    server2WithRemark,
                    server2WithRemark,
                    server2WithPlugin,
                    server2WithPluginAndRemark,
                    server2WithPluginAndRemark
                });

            var generateUrlCases = new Dictionary<string, Server>
            {
                [serverCanonUrl] = server,
                [serverWithRemarkCanonUrl] = serverWithRemark,
                [serverWithPluginCanonUrl] = serverWithPlugin,
                [serverWithPluginAndRemarkCanonUrl] = serverWithPluginAndRemark
            };
            RunGenerateShadowsocksUrlTest(generateUrlCases);
        }

        private static void RunParseShadowsocksUrlTest(string testCase, IReadOnlyList<Server> expected)
        {
            var actual = Server.GetServers(testCase);
            if (actual.Count != expected.Count)
            {
                Assert.Fail("Wrong number of configs. Expected: {0}. Actual: {1}", expected.Count, actual.Count);
            }

            for (int i = 0; i < expected.Count; i++)
            {
                var expectedServer = expected[i];
                var actualServer = actual[i];

                Assert.AreEqual(expectedServer.server, actualServer.server);
                Assert.AreEqual(expectedServer.server_port, actualServer.server_port);
                Assert.AreEqual(expectedServer.password, actualServer.password);
                Assert.AreEqual(expectedServer.method, actualServer.method);
                Assert.AreEqual(expectedServer.plugin, actualServer.plugin);
                Assert.AreEqual(expectedServer.plugin_opts, actualServer.plugin_opts);
                Assert.AreEqual(expectedServer.remarks, actualServer.remarks);
                Assert.AreEqual(expectedServer.timeout, actualServer.timeout);
            }
        }

        private static void RunGenerateShadowsocksUrlTest(IReadOnlyDictionary<string, Server> testCases)
        {
            foreach (var testCase in testCases)
            {
                string expected = testCase.Key;
                Server config = testCase.Value;

                var actual = ShadowsocksController.GetServerURL(config);
                Assert.AreEqual(expected, actual);
            }
        }
    }
}
