using CryptoBase.SymmetricCryptos.AEADCryptos;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto;

public class AeadXChaCha20Poly1305Crypto(CryptoParameter parameter) : AeadCrypto(parameter)
{
    public override void Init(byte[] key, byte[] iv) => crypto = AEADCryptoCreate.XChaCha20Poly1305(key);
}