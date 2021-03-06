using Shadowsocks.Protocol;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using System.Text;
using System.Threading;
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

            var convertConfigCommand = new Command("convert-config", "Convert between different config formats. Supported formats: SIP002 links, SIP008 delivery JSON, and V2Ray JSON (outbound only).");
            convertConfigCommand.AddOption(new Option<string[]?>("--from-urls", "URL conversion sources. Multiple URLs are supported. Supported protocols are ss:// and https://."));
            convertConfigCommand.AddOption(new Option<string[]?>("--from-sip008-json", "SIP008 JSON conversion sources. Multiple JSON files are supported."));
            convertConfigCommand.AddOption(new Option<string[]?>("--from-v2ray-json", "V2Ray JSON conversion sources. Multiple JSON files are supported."));
            convertConfigCommand.AddOption(new Option<bool>("--prefix-group-name", "Whether to prefix group name to server names after conversion."));
            convertConfigCommand.AddOption(new Option<bool>("--to-urls", "Convert to ss:// links and print."));
            convertConfigCommand.AddOption(new Option<string?>("--to-sip008-json", "Convert to SIP008 JSON and save to the specified path."));
            convertConfigCommand.AddOption(new Option<string?>("--to-v2ray-json", "Convert to V2Ray JSON and save to the specified path."));
            convertConfigCommand.Handler = CommandHandler.Create(
                async (string[]? fromUrls, string[]? fromSip008Json, string[]? fromV2rayJson, bool prefixGroupName, bool toUrls, string? toSip008Json, string? toV2rayJson, CancellationToken cancellationToken) =>
                {
                    var configConverter = new ConfigConverter(prefixGroupName);

                    try
                    {
                        if (fromUrls != null)
                        {
                            var uris = new List<Uri>();
                            foreach (var url in fromUrls)
                            {
                                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                                    uris.Add(uri);
                                else
                                    Console.WriteLine($"Invalid URL: {url}");
                            }
                            await configConverter.FromUrls(uris, cancellationToken);
                        }
                        if (fromSip008Json != null)
                            await configConverter.FromSip008Json(fromSip008Json, cancellationToken);
                        if (fromV2rayJson != null)
                            await configConverter.FromV2rayJson(fromV2rayJson, cancellationToken);

                        if (toUrls)
                        {
                            var uris = configConverter.ToUrls();
                            foreach (var uri in uris)
                                Console.WriteLine(uri.AbsoluteUri);
                        }
                        if (!string.IsNullOrEmpty(toSip008Json))
                            await configConverter.ToSip008Json(toSip008Json, cancellationToken);
                        if (!string.IsNullOrEmpty(toV2rayJson))
                            await configConverter.ToV2rayJson(toV2rayJson, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                });

            var utilitiesCommand = new Command("utilities", "Shadowsocks-related utilities.")
            {
                convertConfigCommand,
            };
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
