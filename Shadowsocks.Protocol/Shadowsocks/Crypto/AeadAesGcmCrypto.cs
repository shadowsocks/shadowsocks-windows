using CryptoBase.SymmetricCryptos.AEADCryptos;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto
{
    public class AeadAesGcmCrypto : AeadCrypto
    {
        public AeadAesGcmCrypto(CryptoParameter parameter) : base(parameter)
        {
        }

        public override void Init(byte[] key, byte[] iv) => crypto = AEADCryptoCreate.AesGcm(key);
    }
}
