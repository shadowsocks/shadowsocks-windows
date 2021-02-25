using Shadowsocks.Protocol;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.CLI
{
    internal class Program
    {
        private static Task<int> Main(string[] args)
        {
            var clientCommand = new Command("client", "Shadowsocks client.");
            clientCommand.AddAlias("c");
            clientCommand.AddOption(new Option<string?>("--listen", "The address and port to listen on for both SOCKS5 and HTTP proxy."));
            clientCommand.AddOption(new Option<string?>("--listen-socks", "The address and port to listen on for SOCKS5 proxy."));
            clientCommand.AddOption(new Option<string?>("--listen-http", "The address and port to listen on for HTTP proxy."));
            clientCommand.AddOption(new Option<string>("--server-address", "Address of the remote Shadowsocks server to connect to."));
            clientCommand.AddOption(new Option<int>("--server-port", "Port of the remote Shadowsocks server to connect to."));
            clientCommand.AddOption(new Option<string>("--method", "Encryption method to use for the remote Shadowsocks server."));
            clientCommand.AddOption(new Option<string?>("--password", "Password to use for the remote Shadowsocks server."));
            clientCommand.AddOption(new Option<string?>("--key", "Encryption key (NOT password!) to use for the remote Shadowsocks server."));
            clientCommand.AddOption(new Option<string?>("--plugin", "Plugin binary path."));
            clientCommand.AddOption(new Option<string?>("--plugin-opts", "Plugin options."));
            clientCommand.AddOption(new Option<string?>("--plugin-args", "Plugin startup arguments."));
            clientCommand.Handler = CommandHandler.Create(
                async (string? listen, string? listenSocks, string? listenHttp, string serverAddress, int serverPort, string method, string? password, string? key, string? plugin, string? pluginOpts, string? pluginArgs) =>
                {
                    // TODO
                    var localEP = IPEndPoint.Parse(listenSocks);
                    var remoteEp = new DnsEndPoint(serverAddress, serverPort);
                    byte[]? mainKey = null;
                    if (!string.IsNullOrEmpty(key))
                        mainKey = Encoding.UTF8.GetBytes(key);
                    var tcpPipeListener = new TcpPipeListener(localEP);
                    tcpPipeListener.Start(localEP, remoteEp, method, password, mainKey).Wait();
                });

            var serverCommand = new Command("server", "Shadowsocks server.");
            serverCommand.AddAlias("s");
            serverCommand.Handler = CommandHandler.Create(
                () =>
                {
                    Console.WriteLine("Not implemented.");
                });

            var utilitiesCommand = new Command("utilities", "Shadowsocks-related utilities.");
            utilitiesCommand.AddAlias("u");
            utilitiesCommand.AddAlias("util");
            utilitiesCommand.AddAlias("utils");

            var rootCommand = new RootCommand("CLI for Shadowsocks server and client implementation in C#.")
            {
                clientCommand,
                serverCommand,
                utilitiesCommand,
            };

            Console.OutputEncoding = Encoding.UTF8;
            return rootCommand.InvokeAsync(args);
        }
    }
}
