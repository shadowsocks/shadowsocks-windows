using Shadowsocks.Net.Crypto.AEAD;

namespace Shadowsocks.Net.Crypto
{
    public static class TCPParameter
    {
        // each recv size.
        public const int RecvSize = 2048;

        // overhead of one chunk, reserved for AEAD ciphers
        //                                /* two tags */
        public const int ChunkOverheadSize = 16 * 2  + AEADCrypto.ChunkLengthBytes;

        // max chunk size
        public const uint MaxChunkSize = AEADCrypto.ChunkLengthMask + AEADCrypto.ChunkLengthBytes + 16 * 2;

        // In general, the ciphertext length, we should take overhead into account
        public const int BufferSize = RecvSize + (int)MaxChunkSize + 32 /* max salt len */;
    }
}
