using Shadowsocks.Protocol.Shadowsocks.Crypto;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol.Shadowsocks;

internal class ShadowsocksClient : IStreamClient
{
    private readonly IStreamClient _shadow;
    private readonly PayloadProtocolClient _socks = new();
    private readonly PipePair _pipe = new();

    public ShadowsocksClient(string method, string password)
    {
        var param = CryptoProvider.GetCrypto(method);
        if (param.IsAead)
        {
            _shadow = new AeadClient(param, password);
        }
        else
        {
            _shadow = new UnsafeClient(param, password);
        }
    }

    public ShadowsocksClient(string method, byte[] key)
    {
        var param = CryptoProvider.GetCrypto(method);
        _shadow = new AeadClient(param, key);
    }

    public Task Connect(EndPoint destination, IDuplexPipe client, IDuplexPipe server)
    {
        var tShadow = _shadow.Connect(null, _pipe.UpSide, server);
        var tSocks = _socks.Connect(destination, client, _pipe.UpSide);

        return Task.WhenAll(tShadow, tSocks);
    }
}