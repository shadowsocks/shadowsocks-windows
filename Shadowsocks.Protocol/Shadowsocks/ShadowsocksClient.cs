using Shadowsocks.Protocol.Shadowsocks.Crypto;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol.Shadowsocks
{
    internal class ShadowsocksClient : IStreamClient
    {
        private readonly IStreamClient shadow;
        private readonly PayloadProtocolClient socks = new PayloadProtocolClient();
        private readonly PipePair p = new PipePair();

        public ShadowsocksClient(string method, string password)
        {
            var param = CryptoProvider.GetCrypto(method);
            if (param.IsAead)
            {
                shadow = new AeadClient(param, password);
            }
            else
            {
                shadow = new UnsafeClient(param, password);
            }
        }

        public ShadowsocksClient(string method, byte[] key)
        {
            var param = CryptoProvider.GetCrypto(method);
            shadow = new AeadClient(param, key);
        }

        public Task Connect(EndPoint destination, IDuplexPipe client, IDuplexPipe server)
        {
            var tShadow = shadow.Connect(null, p.UpSide, server);
            var tSocks = socks.Connect(destination, client, p.UpSide);

            return Task.WhenAll(tShadow, tSocks);
        }
    }
}
