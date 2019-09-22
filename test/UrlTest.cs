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
