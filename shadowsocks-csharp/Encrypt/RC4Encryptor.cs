
namespace shadowsocks_csharp.Encrypt
{
    public class Rc4Encryptor
        : EncryptorBase
    {
        public Rc4Encryptor(string method, string password)
            : base(method, password)
        {
            byte[] hash = GetPasswordHash();
            _encryptTable = EncryptInitalize(hash);
            _decryptTable = EncryptInitalize(hash);
        }

        public override byte[] Encrypt(byte[] buf, int length)
        {
            return EncryptOutput(enc_ctx, _encryptTable, buf, length);
        }

        public override byte[] Decrypt(byte[] buf, int length)
        {
            return EncryptOutput(dec_ctx, _decryptTable, buf, length);
        }

        private readonly byte[] _encryptTable = new byte[256];
        private readonly byte[] _decryptTable = new byte[256];

        private Context enc_ctx = new Context();
        private Context dec_ctx = new Context();

        private byte[] EncryptOutput(Context ctx, byte[] s, byte[] data, int length)
        {
            byte[] result = new byte[length];
            for (int n = 0; n < length; n++)
            {
                byte b = data[n];

                ctx.Index1 = (ctx.Index1 + 1) & 255;
                ctx.Index2 = (ctx.Index2 + s[ctx.Index1]) & 255;

                Swap(s, ctx.Index1, ctx.Index2);

                result[n] = (byte)(b ^ s[(s[ctx.Index1] + s[ctx.Index2]) & 255]);
            }
            return result;
        }

        private byte[] EncryptInitalize(byte[] key)
        {
            var s = new byte[256];

            for (int i = 0; i < 256; i++)
            {
                s[i] = (byte)i;
            }

            for (int i = 0, j = 0; i < 256; i++)
            {
                j = (j + key[i % key.Length] + s[i]) & 255;

                Swap(s, i, j);
            }

            return s;
        }

        private static void Swap(byte[] s, int i, int j)
        {
            byte c = s[i];

            s[i] = s[j];
            s[j] = c;
        }

        class Context
        {
            public int Index1;
            public int Index2;
        }
    }
}
