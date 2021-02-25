using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol.Socks5
{
    public class Socks5Service : IStreamService
    {
        public Socks5Service()
        {

        }

        public Socks5Service(Dictionary<string, string> passwords)
        {
            enablePassword = true;
            this.passwords = passwords;
        }

        private readonly bool enablePassword;
        private readonly Dictionary<string, string> passwords = new Dictionary<string, string>();
        public static int ReadTimeout = 120000;

        public async Task<bool> IsMyClient(IDuplexPipe pipe)
        {
            var result = await pipe.Input.ReadAsync();
            pipe.Input.AdvanceTo(result.Buffer.Start);
            var buffer = result.Buffer;
            if (buffer.Length < 3) return false;
            if (buffer.First.Span[0] != 5) return false;
            if (buffer.First.Span[1] == 0) return false;
            // ver 5, has auth method
            return true;
        }

        public async Task<IDuplexPipe> Handle(IDuplexPipe pipe)
        {
            var pmp = new ProtocolMessagePipe(pipe);
            var hs = await pmp.ReadAsync<Socks5VersionIdentifierMessage>();

            var selected = Socks5Message.AuthNoAcceptable;
            if (enablePassword)
            {
                foreach (var a in Util.GetArray(hs.Auth))
                {
                    if (a == Socks5Message.AuthUserPass)
                    {
                        selected = Socks5Message.AuthUserPass;
                        break;
                    }

                    if (a == Socks5Message.AuthNone)
                    {
                        selected = Socks5Message.AuthNone;
                    }
                }
            }
            else
            {
                if (Util.GetArray(hs.Auth).Any(a => a == Socks5Message.AuthNone))
                {
                    selected = Socks5Message.AuthNone;
                }
            }

            await pmp.WriteAsync(new Socks5MethodSelectionMessage()
            {
                SelectedAuth = selected,
            });
            switch (selected)
            {
                case Socks5Message.AuthNoAcceptable:
                default:
                    await pipe.Output.CompleteAsync();
                    return null;

                case Socks5Message.AuthNone:
                    break;

                case Socks5Message.AuthUserPass:
                    var token = await pmp.ReadAsync<Socks5UserPasswordRequestMessage>();
                    var user = Encoding.UTF8.GetString(token.User.Span);
                    var password = Encoding.UTF8.GetString(token.Password.Span);
                    var ar = new Socks5UserPasswordResponseMessage();
                    var success =
                        passwords.TryGetValue(user, out var expectPassword)
                        && expectPassword == password;
                    ar.Success = success;
                    await pmp.WriteAsync(ar);
                    if (!success)
                    {
                        await pipe.Output.CompleteAsync();
                        return null;
                    }

                    break;
            }

            var req = await pmp.ReadAsync<Socks5RequestMessage>();
            var resp = new Socks5ReplyMessage();
            switch (req.Command)
            {
                case Socks5Message.CmdBind:
                case Socks5Message.CmdUdpAssociation: // not support yet
                    resp.Reply = Socks5Message.ReplyCommandNotSupport;
                    break;

                case Socks5Message.CmdConnect:
                    Console.WriteLine(req.EndPoint);
                    // TODO: route and dial outbound


                    resp.Reply = Socks5Message.ReplySucceed;
                    break;
            }
            // TODO: write response, hand out connection
            await pmp.WriteAsync(resp);
            if (req.Command != Socks5Message.CmdConnect) return null;

            return pipe;
        }
    }
}
