using System;

namespace Shadowsocks.Encryption
{
    public enum CipherFamily
    {
        Plain,
        Table,

        AesGcm,

        AesCfb,
        AesCtr,

        Chacha20,
        Chacha20Poly1305,
        XChacha20Poly1305,

        Rc4,
        Rc4Md5,
    }

    public enum CipherStandardState
    {
        InUse,
        Deprecated,
        Hidden,
    }

    public class CipherParameter
    {
        public int KeySize;
    }
    public class StreamCipherParameter : CipherParameter
    {
        public int IvSize;
    }

    public class AEADCipherParameter : CipherParameter
    {
        public int SaltSize;
        public int TagSize;
        public int NonceSize;
    }

    public class CipherInfo
    {
        public string Name;
        public CipherFamily Type;
        public CipherParameter CipherParameter;

        public CipherStandardState StandardState = CipherStandardState.InUse;

        #region Stream ciphers
        public CipherInfo(string name, int keySize, int ivSize, CipherFamily type)
        {
            Type = type;
            Name = name;
            StandardState = CipherStandardState.Hidden;
            CipherParameter = new StreamCipherParameter
            {
                KeySize = keySize,
                IvSize = ivSize,
            };
        }

        #endregion

        #region AEAD ciphers
        public CipherInfo(string name, int keySize, int saltSize, int nonceSize, int tagSize, CipherFamily type)
        {
            Type = type;
            Name = name;

            CipherParameter = new AEADCipherParameter
            {
                KeySize = keySize,
                SaltSize = saltSize,
                NonceSize = nonceSize,
                TagSize = tagSize,
            };
        }
        #endregion
    }
}