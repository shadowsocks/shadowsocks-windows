using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities;
using System;

namespace Shadowsocks.Net.Crypto.Extensions
{
    /// <summary>
    /// Implementation of Daniel J. Bernstein's Salsa20 stream cipher, Snuffle 2005
    /// </summary>
    public class MySalsa20Engine : IStreamCipher
    {
        public static readonly int DefaultRounds = 20;

        /** Constants */
        private const int StateSize = 16; // 16, 32 bit ints = 64 bytes

        private static readonly uint[] TauSigma = Pack.LE_To_UInt32(Strings.ToAsciiByteArray("expand 16-byte k" + "expand 32-byte k"), 0, 8);

        internal static void PackTauOrSigma(int keyLength, uint[] state, int stateOffset)
        {
            var tsOff = (keyLength - 16) / 4;
            state[stateOffset] = TauSigma[tsOff];
            state[stateOffset + 1] = TauSigma[tsOff + 1];
            state[stateOffset + 2] = TauSigma[tsOff + 2];
            state[stateOffset + 3] = TauSigma[tsOff + 3];
        }

        protected int rounds;

        /*
		 * variables to hold the state of the engine
		 * during encryption and decryption
		 */
        private int index = 0;
        internal uint[] engineState = new uint[StateSize]; // state
        internal uint[] x = new uint[StateSize]; // internal buffer
        private byte[] keyStream = new byte[StateSize * 4]; // expanded state, 64 bytes
        private bool initialised = false;

        /*
		 * internal counter
		 */
        private uint cW0, cW1, cW2;

        /// <summary>
        /// Creates a Salsa20 engine with a specific number of rounds.
        /// </summary>
        /// <param name="rounds">the number of rounds (must be an even number).</param>
        protected MySalsa20Engine(int rounds)
        {
            if (rounds <= 0 || (rounds & 1) != 0)
            {
                throw new ArgumentException("'rounds' must be a positive, even number");
            }

            this.rounds = rounds;
        }

        public virtual void Init(
            bool forEncryption,
            ICipherParameters parameters)
        {
            /* 
			 * Salsa20 encryption and decryption is completely
			 * symmetrical, so the 'forEncryption' is 
			 * irrelevant. (Like 90% of stream ciphers)
			 */

            var ivParams = parameters as ParametersWithIV;
            if (ivParams == null)
            {
                throw new ArgumentException(AlgorithmName + " Init requires an IV", "parameters");
            }

            var iv = ivParams.GetIV();
            if (iv == null || iv.Length != NonceSize)
            {
                throw new ArgumentException(AlgorithmName + " requires exactly " + NonceSize + " bytes of IV");
            }

            var keyParam = ivParams.Parameters;
            if (keyParam == null)
            {
                if (!initialised)
                {
                    throw new InvalidOperationException(AlgorithmName + " KeyParameter can not be null for first initialisation");
                }

                SetKey(null, iv);
            }
            else if (keyParam is KeyParameter keyParameter)
            {
                SetKey(keyParameter.GetKey(), iv);
            }
            else
            {
                throw new ArgumentException(AlgorithmName + " Init parameters must contain a KeyParameter (or null for re-init)");
            }

            Reset();
            initialised = true;
        }

        protected virtual int NonceSize => 8;

        public virtual string AlgorithmName
        {
            get
            {
                var name = @"Salsa20";
                if (rounds != DefaultRounds)
                {
                    name += $"/{rounds}";
                }
                return name;
            }
        }

        public virtual byte ReturnByte(
            byte input)
        {
            if (LimitExceeded())
            {
                throw new MaxBytesExceededException("2^70 byte limit per IV; Change IV");
            }

            if (index == 0)
            {
                GenerateKeyStream(keyStream);
                AdvanceCounter();
            }

            var output = (byte)(keyStream[index] ^ input);
            index = (index + 1) & 63;

            return output;
        }

        protected virtual void AdvanceCounter()
        {
            if (++engineState[8] == 0)
            {
                ++engineState[9];
            }
        }

        public virtual void ProcessBytes(
            byte[] inBytes,
            int inOff,
            int len,
            byte[] outBytes,
            int outOff)
        {
            if (!initialised)
            {
                throw new InvalidOperationException(AlgorithmName + " not initialised");
            }

            Check.DataLength(inBytes, inOff, len, "input buffer too short");
            Check.OutputLength(outBytes, outOff, len, "output buffer too short");

            if (LimitExceeded((uint)len))
            {
                throw new MaxBytesExceededException("2^70 byte limit per IV would be exceeded; Change IV");
            }

            for (var i = 0; i < len; i++)
            {
                if (index == 0)
                {
                    GenerateKeyStream(keyStream);
                    AdvanceCounter();
                }
                outBytes[i + outOff] = (byte)(keyStream[index] ^ inBytes[i + inOff]);
                index = (index + 1) & 63;
            }
        }

        public virtual void Reset()
        {
            index = 0;
            ResetLimitCounter();
            ResetCounter();
        }

        protected virtual void ResetCounter()
        {
            engineState[8] = engineState[9] = 0;
        }

        protected virtual void SetKey(byte[] keyBytes, byte[] ivBytes)
        {
            if (keyBytes != null)
            {
                if ((keyBytes.Length != 16) && (keyBytes.Length != 32))
                {
                    throw new ArgumentException(AlgorithmName + " requires 128 bit or 256 bit key");
                }

                var tsOff = (keyBytes.Length - 16) / 4;
                engineState[0] = TauSigma[tsOff];
                engineState[5] = TauSigma[tsOff + 1];
                engineState[10] = TauSigma[tsOff + 2];
                engineState[15] = TauSigma[tsOff + 3];

                // Key
                Pack.LE_To_UInt32(keyBytes, 0, engineState, 1, 4);
                Pack.LE_To_UInt32(keyBytes, keyBytes.Length - 16, engineState, 11, 4);
            }

            // IV
            Pack.LE_To_UInt32(ivBytes, 0, engineState, 6, 2);
        }

        protected virtual void GenerateKeyStream(byte[] output)
        {
            SalsaCore(rounds, engineState, x);
            Pack.UInt32_To_LE(x, output, 0);
        }

        internal static void SalsaCore(int rounds, uint[] input, uint[] x)
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
                throw new ArgumentException("Number of rounds must be even");
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
                x04 ^= R((x00 + x12), 7);
                x08 ^= R((x04 + x00), 9);
                x12 ^= R((x08 + x04), 13);
                x00 ^= R((x12 + x08), 18);
                x09 ^= R((x05 + x01), 7);
                x13 ^= R((x09 + x05), 9);
                x01 ^= R((x13 + x09), 13);
                x05 ^= R((x01 + x13), 18);
                x14 ^= R((x10 + x06), 7);
                x02 ^= R((x14 + x10), 9);
                x06 ^= R((x02 + x14), 13);
                x10 ^= R((x06 + x02), 18);
                x03 ^= R((x15 + x11), 7);
                x07 ^= R((x03 + x15), 9);
                x11 ^= R((x07 + x03), 13);
                x15 ^= R((x11 + x07), 18);

                x01 ^= R((x00 + x03), 7);
                x02 ^= R((x01 + x00), 9);
                x03 ^= R((x02 + x01), 13);
                x00 ^= R((x03 + x02), 18);
                x06 ^= R((x05 + x04), 7);
                x07 ^= R((x06 + x05), 9);
                x04 ^= R((x07 + x06), 13);
                x05 ^= R((x04 + x07), 18);
                x11 ^= R((x10 + x09), 7);
                x08 ^= R((x11 + x10), 9);
                x09 ^= R((x08 + x11), 13);
                x10 ^= R((x09 + x08), 18);
                x12 ^= R((x15 + x14), 7);
                x13 ^= R((x12 + x15), 9);
                x14 ^= R((x13 + x12), 13);
                x15 ^= R((x14 + x13), 18);
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

        /**
		 * Rotate left
		 *
		 * @param   x   value to rotate
		 * @param   y   amount to rotate x
		 *
		 * @return  rotated x
		 */
        internal static uint R(uint x, int y)
        {
            return (x << y) | (x >> (32 - y));
        }

        private void ResetLimitCounter()
        {
            cW0 = 0;
            cW1 = 0;
            cW2 = 0;
        }

        private bool LimitExceeded()
        {
            if (++cW0 == 0)
            {
                if (++cW1 == 0)
                {
                    return (++cW2 & 0x20) != 0;          // 2^(32 + 32 + 6)
                }
            }

            return false;
        }

        /*
		 * this relies on the fact len will always be positive.
		 */
        private bool LimitExceeded(
            uint len)
        {
            var old = cW0;
            cW0 += len;
            if (cW0 < old)
            {
                if (++cW1 == 0)
                {
                    return (++cW2 & 0x20) != 0;          // 2^(32 + 32 + 6)
                }
            }

            return false;
        }
    }
}
