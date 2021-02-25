using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol.Socks5
{
    class Socks5Client : IStreamClient
    {
        NetworkCredential _credential;

        public Socks5Client(NetworkCredential credential = null)
        {
            _credential = credential;
        }

        public async Task Connect(EndPoint destination, IDuplexPipe client, IDuplexPipe server)
        {
            var pmp = new ProtocolMessagePipe(server);

            await pmp.WriteAsync(new Socks5VersionIdentifierMessage
            {
                Auth = _credential == null ? new [] { Socks5Message.AuthNone } : new [] { Socks5Message.AuthNone, Socks5Message.AuthUserPass }
            });

            var msm = await pmp.ReadAsync<Socks5MethodSelectionMessage>();
            switch (msm.SelectedAuth)
            {
                case Socks5Message.AuthNone:
                    break;
                case Socks5Message.AuthUserPass:
                    Debug.Assert(_credential != null);
                    var name = _credential.UserName;
                    var password = _credential.Password;

                    await pmp.WriteAsync(new Socks5UserPasswordRequestMessage
                    {
                        User = Encoding.UTF8.GetBytes(name),
                        Password = Encoding.UTF8.GetBytes(password),
                    });

                    var upResp = await pmp.ReadAsync<Socks5UserPasswordResponseMessage>();
                    if (!upResp.Success) throw new UnauthorizedAccessException("Wrong username / password");

                    break;
                default:
                    throw new NotSupportedException("Server not support our authencation method");
            }
            await pmp.WriteAsync(new Socks5RequestMessage
            {
                Command = Socks5Message.CmdConnect,
                EndPoint = destination,
            });

            var reply = await pmp.ReadAsync<Socks5ReplyMessage>();

            if (reply.Reply != Socks5Message.ReplySucceed) throw new Exception();

            await DuplexPipe.CopyDuplexPipe(client, server);
        }
    }
}
