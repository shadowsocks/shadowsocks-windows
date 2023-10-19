using CryptoBase.SymmetricCryptos.AEADCryptos;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto;

public class AeadAesGcmCrypto(CryptoParameter parameter) : AeadCrypto(parameter)
{
    public override void Init(byte[] key, byte[] iv) => crypto = AEADCryptoCreate.AesGcm(key);
}