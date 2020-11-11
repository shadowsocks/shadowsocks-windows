using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.IO;

namespace Shadowsocks.Net.Crypto.Extensions
{
    /**
	* implements a Cipher-FeedBack (CFB) mode on top of a simple cipher.
	*/
    public class MyCfbBlockCipher
        : IBlockCipher
    {
        private byte[] IV;
        private byte[] cfbV;
        private byte[] cfbOutV;
        private byte[] buff;
        private int _offset;
        private bool encrypting;

        private readonly int blockSize;
        private readonly IBlockCipher cipher;

        /**
		* Basic constructor.
		*
		* @param cipher the block cipher to be used as the basis of the
		* feedback mode.
		* @param blockSize the block size in bits (note: a multiple of 8)
		*/
        public MyCfbBlockCipher(
            IBlockCipher cipher,
            int bitBlockSize)
        {
            this.cipher = cipher;
            blockSize = bitBlockSize / 8;
            IV = new byte[cipher.GetBlockSize()];
            cfbV = new byte[cipher.GetBlockSize()];
            cfbOutV = new byte[cipher.GetBlockSize()];
            buff = new byte[cipher.GetBlockSize()];
            _offset = 0;
        }
        /**
		* return the underlying block cipher that we are wrapping.
		*
		* @return the underlying block cipher that we are wrapping.
		*/
        public IBlockCipher GetUnderlyingCipher()
        {
            return cipher;
        }
        /**
		* Initialise the cipher and, possibly, the initialisation vector (IV).
		* If an IV isn't passed as part of the parameter, the IV will be all zeros.
		* An IV which is too short is handled in FIPS compliant fashion.
		*
		* @param forEncryption if true the cipher is initialised for
		*  encryption, if false for decryption.
		* @param param the key and other data required by the cipher.
		* @exception ArgumentException if the parameters argument is
		* inappropriate.
		*/
        public void Init(
            bool forEncryption,
            ICipherParameters parameters)
        {
            encrypting = forEncryption;
            if (parameters is ParametersWithIV ivParam)
            {
                var iv = ivParam.GetIV();
                var diff = IV.Length - iv.Length;
                Array.Copy(iv, 0, IV, diff, iv.Length);
                Array.Clear(IV, 0, diff);

                parameters = ivParam.Parameters;
            }
            Reset();

            // if it's null, key is to be reused.
            if (parameters != null)
            {
                cipher.Init(true, parameters);
            }
        }

        /**
		* return the algorithm name and mode.
		*
		* @return the name of the underlying algorithm followed by "/CFB"
		* and the block size in bits.
		*/
        public string AlgorithmName => $@"{cipher.AlgorithmName}/CFB{blockSize * 8}";

        public bool IsPartialBlockOkay => true;

        /**
		* return the block size we are operating at.
		*
		* @return the block size we are operating at (in bytes).
		*/
        public int GetBlockSize()
        {
            return blockSize;
        }

        /**
		* Process one block of input from the array in and write it to
		* the out array.
		*
		* @param in the array containing the input data.
		* @param inOff offset into the in array the data starts at.
		* @param out the array the output data will be copied into.
		* @param outOff the offset into the out array the output will start at.
		* @exception DataLengthException if there isn't enough data in in, or
		* space in out.
		* @exception InvalidOperationException if the cipher isn't initialised.
		* @return the number of bytes processed and produced.
		*/
        private int ProcessBlock(
            byte[] input,
            int inOff,
            byte[] output,
            int outOff,
            bool change)
        {
            return encrypting
                ? EncryptBlock(input, inOff, output, outOff, change)
                : DecryptBlock(input, inOff, output, outOff, change);
        }

        public int ProcessBlock(byte[] inBuf, int inOff, byte[] outBuf, int outOff)
        {
            using var m = new MemoryStream(inBuf, inOff, inBuf.Length);
            var tmp = new byte[blockSize];
            var o = new byte[outBuf.Length - outOff + blockSize * 8];
            using var outStream = new MemoryStream(o);

            var ptr = _offset;
            int read;
            while ((read = m.Read(buff, _offset, buff.Length - _offset)) > 0)
            {
                if (read + _offset < buff.Length)
                {
                    var len = ProcessBlock(buff, 0, tmp, 0, false);
                    outStream.Write(tmp, 0, len);
                    _offset += read;
                    break;
                }
                else
                {
                    var len = ProcessBlock(buff, 0, tmp, 0, true);
                    outStream.Write(tmp, 0, len);
                    _offset = 0;
                }
            }

            outStream.Seek(ptr, SeekOrigin.Begin);
            var res = inBuf.Length;
            outStream.Read(outBuf, outOff, res);
            return res;
        }

        /**
		* Do the appropriate processing for CFB mode encryption.
		*
		* @param in the array containing the data to be encrypted.
		* @param inOff offset into the in array the data starts at.
		* @param out the array the encrypted data will be copied into.
		* @param outOff the offset into the out array the output will start at.
		* @exception DataLengthException if there isn't enough data in in, or
		* space in out.
		* @exception InvalidOperationException if the cipher isn't initialised.
		* @return the number of bytes processed and produced.
		*/
        public int EncryptBlock(
            byte[] input,
            int inOff,
            byte[] outBytes,
            int outOff,
            bool change)
        {
            if (inOff + blockSize > input.Length)
            {
                throw new DataLengthException("input buffer too short");
            }
            if (outOff + blockSize > outBytes.Length)
            {
                throw new DataLengthException("output buffer too short");
            }
            cipher.ProcessBlock(cfbV, 0, cfbOutV, 0);
            //
            // XOR the cfbV with the plaintext producing the ciphertext
            //
            for (var i = 0; i < blockSize; i++)
            {
                outBytes[outOff + i] = (byte)(cfbOutV[i] ^ input[inOff + i]);
            }
            //
            // change over the input block.
            //
            if (change)
            {
                Array.Copy(cfbV, blockSize, cfbV, 0, cfbV.Length - blockSize);
                Array.Copy(outBytes, outOff, cfbV, cfbV.Length - blockSize, blockSize);
            }
            return blockSize;
        }
        /**
		* Do the appropriate processing for CFB mode decryption.
		*
		* @param in the array containing the data to be decrypted.
		* @param inOff offset into the in array the data starts at.
		* @param out the array the encrypted data will be copied into.
		* @param outOff the offset into the out array the output will start at.
		* @exception DataLengthException if there isn't enough data in in, or
		* space in out.
		* @exception InvalidOperationException if the cipher isn't initialised.
		* @return the number of bytes processed and produced.
		*/
        public int DecryptBlock(
            byte[] input,
            int inOff,
            byte[] outBytes,
            int outOff,
            bool change)
        {
            if (inOff + blockSize > input.Length)
            {
                throw new DataLengthException("input buffer too short");
            }
            if (outOff + blockSize > outBytes.Length)
            {
                throw new DataLengthException("output buffer too short");
            }
            cipher.ProcessBlock(cfbV, 0, cfbOutV, 0);
            //
            // change over the input block.
            //
            if (change)
            {
                Array.Copy(cfbV, blockSize, cfbV, 0, cfbV.Length - blockSize);
                Array.Copy(input, inOff, cfbV, cfbV.Length - blockSize, blockSize);
            }
            //
            // XOR the cfbV with the ciphertext producing the plaintext
            //
            for (var i = 0; i < blockSize; i++)
            {
                outBytes[outOff + i] = (byte)(cfbOutV[i] ^ input[inOff + i]);
            }
            return blockSize;
        }
        /**
		* reset the chaining vector back to the IV and reset the underlying
		* cipher.
		*/
        public void Reset()
        {
            Array.Copy(IV, 0, cfbV, 0, IV.Length);
            _offset = 0;
            cipher.Reset();
        }
    }
}
