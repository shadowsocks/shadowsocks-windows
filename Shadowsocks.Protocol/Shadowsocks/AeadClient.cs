using Shadowsocks.Protocol.Shadowsocks.Crypto;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol.Shadowsocks
{
    public class AeadClient : IStreamClient
    {
        private CryptoParameter cryptoParameter;
        private readonly byte[] mainKey;

        /// <summary>
        /// ss-subkey
        /// </summary>
        private static ReadOnlySpan<byte> _ssSubKeyInfo => new byte[]
        {
            0x73, 0x73, 0x2d, 0x73, 0x75, 0x62, 0x6b, 0x65, 0x79
        };

        public AeadClient(CryptoParameter parameter, string password)
        {
            cryptoParameter = parameter;
            mainKey = CryptoUtils.SSKDF(password, parameter.KeySize);
            if (!parameter.IsAead)
                throw new NotSupportedException($"Unsupported method.");
        }

        public AeadClient(CryptoParameter parameter, byte[] key)
        {
            cryptoParameter = parameter;
            mainKey = key;
        }

        public Task Connect(EndPoint destination, IDuplexPipe client, IDuplexPipe server) =>
            // destination is ignored, this is just a converter
            Task.WhenAll(ConvertUplink(client, server), ConvertDownlink(client, server));

        public async Task ConvertUplink(IDuplexPipe client, IDuplexPipe server)
        {
            using var up = cryptoParameter.GetCrypto();
            var pmp = new ProtocolMessagePipe(server);
            var salt = new SaltMessage(16, true);
            await pmp.WriteAsync(salt);

            var key = new byte[cryptoParameter.KeySize];
            HKDF.DeriveKey(HashAlgorithmName.SHA1, mainKey, key, salt.Salt.Span, _ssSubKeyInfo);
            up.Init(key, null);
            Memory<byte> nonce = new byte[cryptoParameter.NonceSize];
            nonce.Span.Fill(0);
            // TODO write salt with data
            while (true)
            {
                var result = await client.Input.ReadAsync();
                if (result.IsCanceled || result.IsCompleted) return;

                // TODO compress into one chunk when possible

                foreach (var item in result.Buffer)
                {
                    foreach (var i in SplitBigChunk(item))
                    {
                        await pmp.WriteAsync(new AeadBlockMessage(up, nonce, cryptoParameter)
                        {
                            // in send routine, Data is readonly
                            Data = MemoryMarshal.AsMemory(i),
                        });
                    }
                }
                client.Input.AdvanceTo(result.Buffer.End);
            }
        }

        public async Task ConvertDownlink(IDuplexPipe client, IDuplexPipe server)
        {
            using var down = cryptoParameter.GetCrypto();

            var pmp = new ProtocolMessagePipe(server);
            var salt = await pmp.ReadAsync(new SaltMessage(cryptoParameter.KeySize));

            var key = new byte[cryptoParameter.KeySize];
            HKDF.DeriveKey(HashAlgorithmName.SHA1, mainKey, key, salt.Salt.Span, _ssSubKeyInfo);
            down.Init(key, null);
            Memory<byte> nonce = new byte[cryptoParameter.NonceSize];
            nonce.Span.Fill(0);

            while (true)
            {
                try
                {
                    var block = await pmp.ReadAsync(new AeadBlockMessage(down, nonce, cryptoParameter));
                    await client.Output.WriteAsync(block.Data);
                    client.Output.Advance(block.Data.Length);
                }
                catch (FormatException)
                {
                    return;
                }
            }
        }

        public List<ReadOnlyMemory<byte>> SplitBigChunk(ReadOnlyMemory<byte> mem)
        {
            var l = new List<ReadOnlyMemory<byte>>(mem.Length / 0x3fff + 1);
            while (mem.Length > 0x3fff)
            {

                l.Add(mem.Slice(0, 0x3fff));
                mem = mem.Slice(0x4000);
            }
            l.Add(mem);
            return l;
        }
    }
}
