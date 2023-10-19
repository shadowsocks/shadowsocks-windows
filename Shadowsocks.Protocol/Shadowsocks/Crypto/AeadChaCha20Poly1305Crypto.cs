using CryptoBase;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto;

public class AeadChaCha20Poly1305Crypto(CryptoParameter parameter) : AeadCrypto(parameter)
{
    public override void Init(byte[] key, byte[] iv) => crypto = AEADCryptoCreate.ChaCha20Poly1305(key);
}