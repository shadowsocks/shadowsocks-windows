using System;

namespace Shadowsocks.Net.Crypto.Extensions
{
    /// <summary>
    /// Implementation of Daniel J. Bernstein's ChaCha stream cipher.
    /// </summary>
    public class MyChaChaEngine : MySalsa20Engine
    {
        /// <summary>
        /// Creates a ChaCha engine with a specific number of rounds.
        /// </summary>
        /// <param name="rounds">the number of rounds (must be an even number).</param>
        protected MyChaChaEngine(int rounds) : base(rounds) { }

        public override string AlgorithmName => $@"ChaCha{rounds}";

        protected override void AdvanceCounter()
        {
            if (++engineState[12] == 0)
            {
                ++engineState[13];
            }
        }

        protected override void ResetCounter()
        {
            engineState[12] = engineState[13] = 0;
        }

        protected override void SetKey(byte[] keyBytes, byte[] ivBytes)
        {
            if (keyBytes != null)
            {
                if (keyBytes.Length != 16 && keyBytes.Length != 32)
                {
                    throw new ArgumentException($@"{AlgorithmName} requires 128 bit or 256 bit key");
                }

                PackTauOrSigma(keyBytes.Length, engineState, 0);

                // Key
                Pack.LE_To_UInt32(keyBytes, 0, engineState, 4, 4);
                Pack.LE_To_UInt32(keyBytes, keyBytes.Length - 16, engineState, 8, 4);
            }

            // IV
            Pack.LE_To_UInt32(ivBytes, 0, engineState, 14, 2);
        }

        protected override void GenerateKeyStream(byte[] output)
        {
            ChachaCore(rounds, engineState, x);
            Pack.UInt32_To_LE(x, output, 0);
        }

        /// <summary>
        /// ChaCha function.
        /// </summary>
        /// <param name="rounds">The number of ChaCha rounds to execute</param>
        /// <param name="input">The input words.</param>
        /// <param name="x">The ChaCha state to modify.</param>
        private static void ChachaCore(int rounds, uint[] input, uint[] x)
        {
            if (input.Length != 16)
            {
                throw new ArgumentException();
            }

            if (x.Length != 16)
            {
                throw new ArgumentException();
            }

            if (rounds % 2 != 0)
            {
                throw new ArgumentException(@"Number of rounds must be even");
            }

            var x00 = input[0];
            var x01 = input[1];
            var x02 = input[2];
            var x03 = input[3];
            var x04 = input[4];
            var x05 = input[5];
            var x06 = input[6];
            var x07 = input[7];
            var x08 = input[8];
            var x09 = input[9];
            var x10 = input[10];
            var x11 = input[11];
            var x12 = input[12];
            var x13 = input[13];
            var x14 = input[14];
            var x15 = input[15];

            for (var i = rounds; i > 0; i -= 2)
            {
                x00 += x04;
                x12 = R(x12 ^ x00, 16);
                x08 += x12;
                x04 = R(x04 ^ x08, 12);
                x00 += x04;
                x12 = R(x12 ^ x00, 8);
                x08 += x12;
                x04 = R(x04 ^ x08, 7);
                x01 += x05;
                x13 = R(x13 ^ x01, 16);
                x09 += x13;
                x05 = R(x05 ^ x09, 12);
                x01 += x05;
                x13 = R(x13 ^ x01, 8);
                x09 += x13;
                x05 = R(x05 ^ x09, 7);
                x02 += x06;
                x14 = R(x14 ^ x02, 16);
                x10 += x14;
                x06 = R(x06 ^ x10, 12);
                x02 += x06;
                x14 = R(x14 ^ x02, 8);
                x10 += x14;
                x06 = R(x06 ^ x10, 7);
                x03 += x07;
                x15 = R(x15 ^ x03, 16);
                x11 += x15;
                x07 = R(x07 ^ x11, 12);
                x03 += x07;
                x15 = R(x15 ^ x03, 8);
                x11 += x15;
                x07 = R(x07 ^ x11, 7);
                x00 += x05;
                x15 = R(x15 ^ x00, 16);
                x10 += x15;
                x05 = R(x05 ^ x10, 12);
                x00 += x05;
                x15 = R(x15 ^ x00, 8);
                x10 += x15;
                x05 = R(x05 ^ x10, 7);
                x01 += x06;
                x12 = R(x12 ^ x01, 16);
                x11 += x12;
                x06 = R(x06 ^ x11, 12);
                x01 += x06;
                x12 = R(x12 ^ x01, 8);
                x11 += x12;
                x06 = R(x06 ^ x11, 7);
                x02 += x07;
                x13 = R(x13 ^ x02, 16);
                x08 += x13;
                x07 = R(x07 ^ x08, 12);
                x02 += x07;
                x13 = R(x13 ^ x02, 8);
                x08 += x13;
                x07 = R(x07 ^ x08, 7);
                x03 += x04;
                x14 = R(x14 ^ x03, 16);
                x09 += x14;
                x04 = R(x04 ^ x09, 12);
                x03 += x04;
                x14 = R(x14 ^ x03, 8);
                x09 += x14;
                x04 = R(x04 ^ x09, 7);
            }

            x[0] = x00 + input[0];
            x[1] = x01 + input[1];
            x[2] = x02 + input[2];
            x[3] = x03 + input[3];
            x[4] = x04 + input[4];
            x[5] = x05 + input[5];
            x[6] = x06 + input[6];
            x[7] = x07 + input[7];
            x[8] = x08 + input[8];
            x[9] = x09 + input[9];
            x[10] = x10 + input[10];
            x[11] = x11 + input[11];
            x[12] = x12 + input[12];
            x[13] = x13 + input[13];
            x[14] = x14 + input[14];
            x[15] = x15 + input[15];
        }
    }
}
