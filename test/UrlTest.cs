using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Controller;
using System.Threading;
using System.Collections.Generic;
using Shadowsocks.Model;
using System.Diagnostics;

namespace Shadowsocks.Test
{
    [TestClass]
    public class UrlTest
    {
        Server server1, server1WithRemark, server1WithPlugin, server1WithPluginAndRemark;
        string server1CanonUrl, server1WithRemarkCanonUrl, server1WithPluginCanonUrl, server1WithPluginAndRemarkCanonUrl;

        Server server2, server2WithRemark, server2WithPlugin, server2WithPluginAndRemark;
        string server2CanonUrl, server2WithRemarkCanonUrl, server2WithPluginCanonUrl, server2WithPluginAndRemarkCanonUrl;


        [TestInitialize]
        public void PrepareTestData()
        {
            server1 = new Server
            {
                server = "192.168.100.1",
                server_port = 8888,
                password = "test",
                method = "bf-cfb"
            };
            server1CanonUrl = "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xMDAuMTo4ODg4";

            // server2 has base64 padding
            server2 = new Server
            {
                server = "192.168.1.1",
                server_port = 8388,
                password = "test",
                method = "bf-cfb"
            };
            server2CanonUrl = "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xLjE6ODM4OA==";

            server1WithRemark = new Server
            {
                server = server1.server,
                server_port = server1.server_port,
                password = server1.password,
                method = server1.method,
                remarks = "example-server"
            };
            server1WithRemarkCanonUrl = "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xMDAuMTo4ODg4#example-server";

            server2WithRemark = new Server
            {
                server = server2.server,
                server_port = server2.server_port,
                password = server2.password,
                method = server2.method,
                remarks = "example-server"
            };

            server2WithRemarkCanonUrl = "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xLjE6ODM4OA==#example-server";

            server1WithPlugin = new Server
            {
                server = server1.server,
                server_port = server1.server_port,
                password = server1.password,
                method = server1.method,
                plugin = "obfs-local",
                plugin_opts = "obfs=http;obfs-host=google.com"
            };
            server1WithPluginCanonUrl =
                "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888/?plugin=obfs-local%3bobfs%3dhttp%3bobfs-host%3dgoogle.com";

            server2WithPlugin = new Server
            {
                server = server2.server,
                server_port = server2.server_port,
                password = server2.password,
                method = server2.method,
                plugin = "obfs-local",
                plugin_opts = "obfs=http;obfs-host=google.com"
            };
            server2WithPluginCanonUrl =
                "ss://YmYtY2ZiOnRlc3Q@192.168.1.1:8388/?plugin=obfs-local%3bobfs%3dhttp%3bobfs-host%3dgoogle.com";

            server1WithPluginAndRemark = new Server
            {
                server = server1.server,
                server_port = server1.server_port,
                password = server1.password,
                method = server1.method,
                plugin = server1WithPlugin.plugin,
                plugin_opts = server1WithPlugin.plugin_opts,
                remarks = server1WithRemark.remarks
            };
            server1WithPluginAndRemarkCanonUrl =
                "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888/?plugin=obfs-local%3bobfs%3dhttp%3bobfs-host%3dgoogle.com#example-server";

            server2WithPluginAndRemark = new Server
            {
                server = server2.server,
                server_port = server2.server_port,
                password = server2.password,
                method = server2.method,
                plugin = server2WithPlugin.plugin,
                plugin_opts = server2WithPlugin.plugin_opts,
                remarks = server2WithRemark.remarks
            };
            server2WithPluginAndRemarkCanonUrl =
                "ss://YmYtY2ZiOnRlc3Q@192.168.1.1:8388/?plugin=obfs-local%3bobfs%3dhttp%3bobfs-host%3dgoogle.com#example-server";
        }

        [TestMethod]
        public void TestParseUrl_Server1()
        {
            RunParseShadowsocksUrlTest(
                string.Join(
                    "\r\n",
                    server1CanonUrl,
                    "\r\n",
                    "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xMDAuMTo4ODg4/",
                    server1WithRemarkCanonUrl,
                    "ss://YmYtY2ZiOnRlc3RAMTkyLjE2OC4xMDAuMTo4ODg4/#example-server"),
                new[]
                {
                    server1,
                    server1,
                    server1WithRemark,
                    server1WithRemark
                });

            RunParseShadowsocksUrlTest(
                string.Join(
                    "\r\n",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888",
                    "\r\n",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888/",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888#example-server",
                    "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888/#example-server",
                    server1WithPluginCanonUrl,
                    server1WithPluginAndRemarkCanonUrl,
                    "ss://YmYtY2ZiOnRlc3Q@192.168.100.1:8888/?plugin=obfs-local%3bobfs%3dhttp%3bobfs-host%3dgoogle.com&unsupported=1#example-server"),
                new[]
                {
                    server1,
                    server1,
                    server1WithRemark,
                    server1WithRemark,
                    server1WithPlugin,
                    server1WithPluginAndRemark,
                    server1WithPluginAndRemark
                });
        }



        [TestMethod]
        public void TestParseUrl_Server2()
        {
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
        }


        [TestMethod]
        public void TestUrlGenerate()
        { 
            var generateUrlCases = new Dictionary<string, Server>
            {
                [server1CanonUrl] = server1,
                [server1WithRemarkCanonUrl] = server1WithRemark,
                [server1WithPluginCanonUrl] = server1WithPlugin,
                [server1WithPluginAndRemarkCanonUrl] = server1WithPluginAndRemark
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
