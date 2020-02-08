using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Controller.Service;
using Shadowsocks.Model;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace Shadowsocks.Test
{
    [TestClass]
    public class Sip003PluginTest
    {
        string fake_plugin = "ftp";

        [TestMethod]
        public void TestSip003Plugin_NoPlugin()
        {


            Sip003Plugin NoPlugin = Sip003Plugin.CreateIfConfigured(
                new Server
                {
                    server = "192.168.100.1",
                    server_port = 8888,
                    password = "test",
                    method = "bf-cfb"
                },
                false);

            RunPluginSupportTest(
                NoPlugin,
                "",
                "",
                "",
                "192.168.100.1",
                8888);
        }

        [TestMethod]
        public void TestSip003Plugin_Plugin()
        {
            Sip003Plugin Plugin = Sip003Plugin.CreateIfConfigured(
                new Server
                {
                    server = "192.168.100.1",
                    server_port = 8888,
                    password = "test",
                    method = "bf-cfb",
                    plugin = fake_plugin
                },
                false);

            RunPluginSupportTest(
                Plugin,
                fake_plugin,
                "",
                "",
                "192.168.100.1",
                8888);
        }

        [TestMethod]
        public void TestSip003Plugin_PluginWithOpts()
        {
            Sip003Plugin PluginWithOpts = Sip003Plugin.CreateIfConfigured(
                new Server
                {
                    server = "192.168.100.1",
                    server_port = 8888,
                    password = "test",
                    method = "bf-cfb",
                    plugin = fake_plugin,
                    plugin_opts = "_option"
                },
                false);

            RunPluginSupportTest(
                PluginWithOpts,
                fake_plugin,
                "_option",
                "",
                "192.168.100.1",
                8888);
        }

        [TestMethod]
        public void TestSip003Plugin_PluginWithArgs()
        {
            Sip003Plugin PluginWithArgs = Sip003Plugin.CreateIfConfigured(
                new Server
                {
                    server = "192.168.100.1",
                    server_port = 8888,
                    password = "test",
                    method = "bf-cfb",
                    plugin = fake_plugin,
                    plugin_args = "_test"
                },
                false);

            RunPluginSupportTest(
                PluginWithArgs,
                fake_plugin,
                "",
                "_test",
                "192.168.100.1",
                8888);
        }

        [TestMethod]
        public void TestSip003Plugin_PluginWithOptsAndArgs()
        {
            Sip003Plugin PluginWithOptsAndArgs = Sip003Plugin.CreateIfConfigured(
                new Server
                {
                    server = "192.168.100.1",
                    server_port = 8888,
                    password = "test",
                    method = "bf-cfb",
                    plugin = fake_plugin,
                    plugin_opts = "_option",
                    plugin_args = "_test"
                },
                false);

            RunPluginSupportTest(
                PluginWithOptsAndArgs,
                fake_plugin,
                "_option",
                "_test",
                "192.168.100.1",
                8888);
        }

        [TestMethod]
        public void TestSip003Plugin_PluginWithArgsReplaced()
        {
            Sip003Plugin PluginWithArgsReplaced = Sip003Plugin.CreateIfConfigured(
                new Server
                {
                    server = "192.168.100.1",
                    server_port = 8888,
                    password = "test",
                    method = "bf-cfb",
                    plugin = fake_plugin,
                    plugin_args = "_test,%SS_REMOTE_HOST%"
                },
                false);

            RunPluginSupportTest(
                PluginWithArgsReplaced,
                fake_plugin,
                "",
                "_test,192.168.100.1",
                "192.168.100.1",
                8888);
        }

        [TestMethod]
        public void TestSip003Plugin_PluginWithOptsAndArgsReplaced()
        {
            Sip003Plugin PluginWithOptsAndArgsReplaced = Sip003Plugin.CreateIfConfigured(
                new Server
                {
                    server = "192.168.100.1",
                    server_port = 8888,
                    password = "test",
                    method = "bf-cfb",
                    plugin = fake_plugin,
                    plugin_opts = "_option",
                    plugin_args = "_test,%SS_REMOTE_HOST%,%SS_PLUGIN_OPTIONS%"
                },
                false);

            RunPluginSupportTest(
                PluginWithOptsAndArgsReplaced,
                fake_plugin,
                "_option",
                "_test,192.168.100.1,_option",
                "192.168.100.1",
                8888);
        }

        private static void RunPluginSupportTest(Sip003Plugin plugin, string pluginName, string pluginOpts, string pluginArgs, string serverAddress, int serverPort)
        {

            if (string.IsNullOrWhiteSpace(pluginName))
            {
                return;
            }

            plugin.StartIfNeeded();

            Process[] processes = Process.GetProcessesByName(pluginName);
            Assert.AreEqual(processes.Length, 1);
            Process p = processes[0];


            System.Collections.Specialized.StringDictionary penv = ProcessEnvironment.ReadEnvironmentVariables(p);
            string pcmd = ProcessEnvironment.GetCommandLine(p).Trim();
            pcmd = pcmd.IndexOf(' ') >= 0 ? pcmd.Substring(pcmd.IndexOf(' ') + 1) : "";

            Assert.AreEqual(penv["SS_REMOTE_HOST"], serverAddress);
            Assert.AreEqual(penv["SS_REMOTE_PORT"], serverPort.ToString());
            Assert.AreEqual(penv["SS_LOCAL_HOST"], IPAddress.Loopback.ToString());

            Assert.IsTrue(int.TryParse(penv["SS_LOCAL_PORT"], out int _ignored));

            Assert.AreEqual(penv["SS_PLUGIN_OPTIONS"], pluginOpts);
            Assert.AreEqual(pcmd, pluginArgs);


            plugin.Dispose();
            for (int i = 0; i < 50; i++)
            {
                if (Process.GetProcessesByName(pluginName).Length == 0)
                {
                    return;
                }

                Thread.Sleep(50);
            }
        }
    }
}
