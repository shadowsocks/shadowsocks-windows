using Org.BouncyCastle.Utilities;
using System;

namespace Shadowsocks.Net.Crypto.Extensions
{
    public class XChaCha20Engine : MyChaChaEngine
    {
        public XChaCha20Engine() : base(20)
        {
        }

        protected override int NonceSize => 24;

        public override string AlgorithmName => @"XChaCha20";

        private static readonly uint[] Sigma = Pack.LE_To_UInt32(Strings.ToAsciiByteArray("expand 32-byte k"), 0, 4);

        protected override void SetKey(byte[] keyBytes, byte[] ivBytes)
        {
            base.SetKey(keyBytes, ivBytes);

            if (keyBytes == null || keyBytes.Length != 32)
            {
                throw new ArgumentException($@"{AlgorithmName} requires a 256 bit key");
            }

            if (ivBytes == null || ivBytes.Length != NonceSize)
            {
                throw new ArgumentException($@"{AlgorithmName} requires a 192 bit nonce");
            }

            var nonceInt = Pack.LE_To_UInt32(ivBytes, 0, 6);

            var chachaKey = HChaCha20Internal(keyBytes, nonceInt);
            SetSigma(engineState);
            SetKey(engineState, chachaKey);
            engineState[12] = 1; // Counter
            engineState[13] = 0;
            engineState[14] = nonceInt[4];
            engineState[15] = nonceInt[5];
        }

        private static uint[] HChaCha20Internal(byte[] key, uint[] nonceInt)
        {
            var x = new uint[16];
            var intKey = Pack.LE_To_UInt32(key, 0, 8);

            SetSigma(x);
            SetKey(x, intKey);
            SetIntNonce(x, nonceInt);
            DoubleRound(x);
            Array.Copy(x, 12, x, 4, 4);
            return x;
        }

        private static void SetSigma(uint[] state)
        {
            Array.Copy(Sigma, 0, state, 0, Sigma.Length);
        }

        private static void SetKey(uint[] state, uint[] key)
        {
            Array.Copy(key, 0, state, 4, 8);
        }

        private static void SetIntNonce(uint[] state, uint[] nonce)
        {
            Array.Copy(nonce, 0, state, 12, 4);
        }

        private static void QuarterRound(uint[] x, uint a, uint b, uint c, uint d)
        {
            x[a] += x[b];
            x[d] = R(x[d] ^ x[a], 16);
            x[c] += x[d];
            x[b] = R(x[b] ^ x[c], 12);
            x[a] += x[b];
            x[d] = R(x[d] ^ x[a], 8);
            x[c] += x[d];
            x[b] = R(x[b] ^ x[c], 7);
        }

        private static void DoubleRound(uint[] state)
        {
            for (var i = 0; i < 10; ++i)
            {
                QuarterRound(state, 0, 4, 8, 12);
                QuarterRound(state, 1, 5, 9, 13);
                QuarterRound(state, 2, 6, 10, 14);
                QuarterRound(state, 3, 7, 11, 15);
                QuarterRound(state, 0, 5, 10, 15);
                QuarterRound(state, 1, 6, 11, 12);
                QuarterRound(state, 2, 7, 8, 13);
                QuarterRound(state, 3, 4, 9, 14);
            }
        }
    }
}
