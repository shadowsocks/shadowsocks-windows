using CryptoBase.SymmetricCryptos.AEADCryptos;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto
{
    public class AeadChaCha20Poly1305Crypto : AeadCrypto
    {
        public AeadChaCha20Poly1305Crypto(CryptoParameter parameter) : base(parameter)
        {
        }

        public override void Init(byte[] key, byte[] iv) => crypto = AEADCryptoCreate.ChaCha20Poly1305(key);
    }
}
