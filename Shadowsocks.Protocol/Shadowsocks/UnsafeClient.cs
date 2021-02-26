using Shadowsocks.Protocol.Shadowsocks.Crypto;
using System;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol.Shadowsocks
{
    // 'original' shadowsocks encryption layer
    internal class UnsafeClient : IStreamClient
    {
        CryptoParameter parameter;
        string password;

        public UnsafeClient(CryptoParameter parameter, string password)
        {
            this.password = password;
            this.parameter = parameter;
        }

        public Task Connect(EndPoint destination, IDuplexPipe client, IDuplexPipe server) =>
            // destination is ignored, this is just a converter
            Task.WhenAll(ConvertUplink(client, server), ConvertDownlink(client, server));

        public async Task ConvertUplink(IDuplexPipe client, IDuplexPipe server)
        {
            using var up = parameter.GetCrypto();
            var pmp = new ProtocolMessagePipe(server);
            var key = CryptoUtils.SSKDF(password, parameter.KeySize);

            var salt = new SaltMessage(parameter.NonceSize, true);
            await pmp.WriteAsync(salt);

            up.Init(key, salt.Salt.ToArray());
            Memory<byte> nonce = new byte[parameter.NonceSize];
            nonce.Span.Fill(0);
            // TODO write salt with data
            while (true)
            {
                var result = await client.Input.ReadAsync();
                if (result.IsCanceled || result.IsCompleted) return;

                // TODO compress into one chunk when possible

                foreach (var item in result.Buffer)
                {
                    var mem = server.Output.GetMemory(item.Length);
                    var len = up.Encrypt(null, item.Span, mem.Span);
                    server.Output.Advance(len);
                }
                client.Input.AdvanceTo(result.Buffer.End);
            }
        }

        public async Task ConvertDownlink(IDuplexPipe client, IDuplexPipe server)
        {
            using var down = parameter.GetCrypto();

            var pmp = new ProtocolMessagePipe(server);
            var salt = await pmp.ReadAsync(new SaltMessage(parameter.NonceSize));

            var key = CryptoUtils.SSKDF(password, parameter.KeySize);
            down.Init(key, salt.Salt.ToArray());

            while (true)
            {
                while (true)
                {
                    var result = await server.Input.ReadAsync();
                    if (result.IsCanceled || result.IsCompleted) return;

                    // TODO compress into one chunk when possible

                    foreach (var item in result.Buffer)
                    {
                        var mem = client.Output.GetMemory(item.Length);
                        var len = down.Decrypt(null, mem.Span, item.Span);
                        client.Output.Advance(len);
                    }
                    server.Input.AdvanceTo(result.Buffer.End);
                }
            }
        }

    }
}
