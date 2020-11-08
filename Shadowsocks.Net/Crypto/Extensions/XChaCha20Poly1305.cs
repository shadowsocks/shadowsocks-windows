using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities;
using System;

namespace Shadowsocks.Net.Crypto.Extensions
{
    public sealed class XChaCha20Poly1305 : IAeadCipher
    {
        private enum State
        {
            Uninitialized = 0,
            EncInit = 1,
            EncAad = 2,
            EncData = 3,
            EncFinal = 4,
            DecInit = 5,
            DecAad = 6,
            DecData = 7,
            DecFinal = 8,
        }

        private const int BufSize = 64;
        private const int KeySize = 32;
        private const int NonceSize = 24;
        private const int MacSize = 16;
        private static readonly byte[] Zeroes = new byte[MacSize - 1];

        private const ulong AadLimit = ulong.MaxValue;
        private const ulong DataLimit = ((1UL << 32) - 1) * 64;

        private readonly XChaCha20Engine mChacha20;
        private readonly IMac mPoly1305;

        private readonly byte[] mKey = new byte[KeySize];
        private readonly byte[] mNonce = new byte[NonceSize];
        private readonly byte[] mBuf = new byte[BufSize + MacSize];
        private readonly byte[] mMac = new byte[MacSize];

        private byte[] mInitialAad;

        private ulong mAadCount;
        private ulong mDataCount;
        private State mState = State.Uninitialized;
        private int mBufPos;

        public XChaCha20Poly1305() : this(new Poly1305())
        {
        }

        public XChaCha20Poly1305(IMac poly1305)
        {
            if (null == poly1305)
            {
                throw new ArgumentNullException(nameof(poly1305));
            }

            if (MacSize != poly1305.GetMacSize())
            {
                throw new ArgumentException("must be a 128-bit MAC", nameof(poly1305));
            }

            mChacha20 = new XChaCha20Engine();
            mPoly1305 = poly1305;
        }

        public string AlgorithmName => @"XChaCha20Poly1305";

        public void Init(bool forEncryption, ICipherParameters parameters)
        {
            KeyParameter initKeyParam;
            byte[] initNonce;
            ICipherParameters chacha20Params;

            if (parameters is AeadParameters aeadParams)
            {
                var macSizeBits = aeadParams.MacSize;
                if (MacSize * 8 != macSizeBits)
                {
                    throw new ArgumentException("Invalid value for MAC size: " + macSizeBits);
                }

                initKeyParam = aeadParams.Key;
                initNonce = aeadParams.GetNonce();
                chacha20Params = new ParametersWithIV(initKeyParam, initNonce);

                mInitialAad = aeadParams.GetAssociatedText();
            }
            else if (parameters is ParametersWithIV ivParams)
            {
                initKeyParam = (KeyParameter)ivParams.Parameters;
                initNonce = ivParams.GetIV();
                chacha20Params = ivParams;

                mInitialAad = null;
            }
            else
            {
                throw new ArgumentException("invalid parameters passed to ChaCha20Poly1305", nameof(parameters));
            }

            // Validate key
            if (null == initKeyParam)
            {
                if (State.Uninitialized == mState)
                {
                    throw new ArgumentException("Key must be specified in initial init");
                }
            }
            else
            {
                if (KeySize != initKeyParam.GetKey().Length)
                {
                    throw new ArgumentException("Key must be 256 bits");
                }
            }

            // Validate nonce
            if (null == initNonce || NonceSize != initNonce.Length)
            {
                throw new ArgumentException("Nonce must be 192 bits");
            }

            // Check for encryption with reused nonce
            if (State.Uninitialized != mState && forEncryption && Arrays.AreEqual(mNonce, initNonce))
            {
                if (null == initKeyParam || Arrays.AreEqual(mKey, initKeyParam.GetKey()))
                {
                    throw new ArgumentException("cannot reuse nonce for ChaCha20Poly1305 encryption");
                }
            }

            if (null != initKeyParam)
            {
                Array.Copy(initKeyParam.GetKey(), 0, mKey, 0, KeySize);
            }

            Array.Copy(initNonce, 0, mNonce, 0, NonceSize);

            mChacha20.Init(true, chacha20Params);

            mState = forEncryption ? State.EncInit : State.DecInit;

            Reset(true, false);
        }

        public int GetOutputSize(int len)
        {
            var total = Math.Max(0, len) + mBufPos;

            switch (mState)
            {
                case State.DecInit:
                case State.DecAad:
                case State.DecData:
                    return Math.Max(0, total - MacSize);
                case State.EncInit:
                case State.EncAad:
                case State.EncData:
                    return total + MacSize;
                default:
                    throw new InvalidOperationException();
            }
        }

        public int GetUpdateOutputSize(int len)
        {
            var total = Math.Max(0, len) + mBufPos;

            switch (mState)
            {
                case State.DecInit:
                case State.DecAad:
                case State.DecData:
                    total = Math.Max(0, total - MacSize);
                    break;
                case State.EncInit:
                case State.EncAad:
                case State.EncData:
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return total - total % BufSize;
        }

        public void ProcessAadByte(byte input)
        {
            CheckAad();

            mAadCount = IncrementCount(mAadCount, 1, AadLimit);
            mPoly1305.Update(input);
        }

        public void ProcessAadBytes(byte[] inBytes, int inOff, int len)
        {
            if (null == inBytes)
            {
                throw new ArgumentNullException(nameof(inBytes));
            }

            if (inOff < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(inOff));
            }

            if (len < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(len));
            }

            Check.DataLength(inBytes, inOff, len, "input buffer too short");

            CheckAad();

            if (len > 0)
            {
                mAadCount = IncrementCount(mAadCount, (uint)len, AadLimit);
                mPoly1305.BlockUpdate(inBytes, inOff, len);
            }
        }

        public int ProcessByte(byte input, byte[] outBytes, int outOff)
        {
            CheckData();

            switch (mState)
            {
                case State.DecData:
                {
                    mBuf[mBufPos] = input;
                    if (++mBufPos == mBuf.Length)
                    {
                        mPoly1305.BlockUpdate(mBuf, 0, BufSize);
                        ProcessData(mBuf, 0, BufSize, outBytes, outOff);
                        Array.Copy(mBuf, BufSize, mBuf, 0, MacSize);
                        mBufPos = MacSize;
                        return BufSize;
                    }

                    return 0;
                }
                case State.EncData:
                {
                    mBuf[mBufPos] = input;
                    if (++mBufPos == BufSize)
                    {
                        ProcessData(mBuf, 0, BufSize, outBytes, outOff);
                        mPoly1305.BlockUpdate(outBytes, outOff, BufSize);
                        mBufPos = 0;
                        return BufSize;
                    }

                    return 0;
                }
                default:
                    throw new InvalidOperationException();
            }
        }

        public int ProcessBytes(byte[] inBytes, int inOff, int len, byte[] outBytes, int outOff)
        {
            if (null == inBytes)
            {
                throw new ArgumentNullException(nameof(inBytes));
            }

            if (null == outBytes)
            {
                throw new ArgumentNullException(nameof(outBytes));
            }

            if (inOff < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(inOff));
            }

            if (len < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(len));
            }

            Check.DataLength(inBytes, inOff, len, "input buffer too short");
            if (outOff < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(outOff));
            }

            CheckData();

            var resultLen = 0;

            switch (mState)
            {
                case State.DecData:
                {
                    for (var i = 0; i < len; ++i)
                    {
                        mBuf[mBufPos] = inBytes[inOff + i];
                        if (++mBufPos == mBuf.Length)
                        {
                            mPoly1305.BlockUpdate(mBuf, 0, BufSize);
                            ProcessData(mBuf, 0, BufSize, outBytes, outOff + resultLen);
                            Array.Copy(mBuf, BufSize, mBuf, 0, MacSize);
                            mBufPos = MacSize;
                            resultLen += BufSize;
                        }
                    }
                    break;
                }
                case State.EncData:
                {
                    if (mBufPos != 0)
                    {
                        while (len > 0)
                        {
                            --len;
                            mBuf[mBufPos] = inBytes[inOff++];
                            if (++mBufPos == BufSize)
                            {
                                ProcessData(mBuf, 0, BufSize, outBytes, outOff);
                                mPoly1305.BlockUpdate(outBytes, outOff, BufSize);
                                mBufPos = 0;
                                resultLen = BufSize;
                                break;
                            }
                        }
                    }

                    while (len >= BufSize)
                    {
                        ProcessData(inBytes, inOff, BufSize, outBytes, outOff + resultLen);
                        mPoly1305.BlockUpdate(outBytes, outOff + resultLen, BufSize);
                        inOff += BufSize;
                        len -= BufSize;
                        resultLen += BufSize;
                    }

                    if (len > 0)
                    {
                        Array.Copy(inBytes, inOff, mBuf, 0, len);
                        mBufPos = len;
                    }
                    break;
                }
                default:
                    throw new InvalidOperationException();
            }

            return resultLen;
        }

        public int DoFinal(byte[] outBytes, int outOff)
        {
            if (null == outBytes)
            {
                throw new ArgumentNullException(nameof(outBytes));
            }

            if (outOff < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(outOff));
            }

            CheckData();

            Array.Clear(mMac, 0, MacSize);

            int resultLen;

            switch (mState)
            {
                case State.DecData:
                {
                    if (mBufPos < MacSize)
                    {
                        throw new InvalidCipherTextException("data too short");
                    }

                    resultLen = mBufPos - MacSize;

                    Check.OutputLength(outBytes, outOff, resultLen, "output buffer too short");

                    if (resultLen > 0)
                    {
                        mPoly1305.BlockUpdate(mBuf, 0, resultLen);
                        ProcessData(mBuf, 0, resultLen, outBytes, outOff);
                    }

                    FinishData(State.DecFinal);

                    if (!Arrays.ConstantTimeAreEqual(MacSize, mMac, 0, mBuf, resultLen))
                    {
                        throw new InvalidCipherTextException("mac check in ChaCha20Poly1305 failed");
                    }

                    break;
                }
                case State.EncData:
                {
                    resultLen = mBufPos + MacSize;

                    Check.OutputLength(outBytes, outOff, resultLen, "output buffer too short");

                    if (mBufPos > 0)
                    {
                        ProcessData(mBuf, 0, mBufPos, outBytes, outOff);
                        mPoly1305.BlockUpdate(outBytes, outOff, mBufPos);
                    }

                    FinishData(State.EncFinal);

                    Array.Copy(mMac, 0, outBytes, outOff + mBufPos, MacSize);
                    break;
                }
                default:
                    throw new InvalidOperationException();
            }

            Reset(false, true);

            return resultLen;
        }

        public byte[] GetMac()
        {
            return Arrays.Clone(mMac);
        }

        public void Reset()
        {
            Reset(true, true);
        }

        private void CheckAad()
        {
            switch (mState)
            {
                case State.DecInit:
                    mState = State.DecAad;
                    break;
                case State.EncInit:
                    mState = State.EncAad;
                    break;
                case State.DecAad:
                case State.EncAad:
                    break;
                case State.EncFinal:
                    throw new InvalidOperationException("ChaCha20Poly1305 cannot be reused for encryption");
                default:
                    throw new InvalidOperationException();
            }
        }

        private void CheckData()
        {
            switch (mState)
            {
                case State.DecInit:
                case State.DecAad:
                    FinishAad(State.DecData);
                    break;
                case State.EncInit:
                case State.EncAad:
                    FinishAad(State.EncData);
                    break;
                case State.DecData:
                case State.EncData:
                    break;
                case State.EncFinal:
                    throw new InvalidOperationException("ChaCha20Poly1305 cannot be reused for encryption");
                default:
                    throw new InvalidOperationException();
            }
        }

        private void FinishAad(State nextState)
        {
            PadMac(mAadCount);

            mState = nextState;
        }

        private void FinishData(State nextState)
        {
            PadMac(mDataCount);

            var lengths = new byte[16];
            Pack.UInt64_To_LE(mAadCount, lengths, 0);
            Pack.UInt64_To_LE(mDataCount, lengths, 8);
            mPoly1305.BlockUpdate(lengths, 0, 16);

            mPoly1305.DoFinal(mMac, 0);

            mState = nextState;
        }

        private ulong IncrementCount(ulong count, uint increment, ulong limit)
        {
            if (count > limit - increment)
            {
                throw new InvalidOperationException("Limit exceeded");
            }

            return count + increment;
        }

        private void InitMac()
        {
            var firstBlock = new byte[64];
            try
            {
                mChacha20.ProcessBytes(firstBlock, 0, 64, firstBlock, 0);
                mPoly1305.Init(new KeyParameter(firstBlock, 0, 32));
            }
            finally
            {
                Array.Clear(firstBlock, 0, 64);
            }
        }

        private void PadMac(ulong count)
        {
            var partial = (int)count % MacSize;
            if (0 != partial)
            {
                mPoly1305.BlockUpdate(Zeroes, 0, MacSize - partial);
            }
        }

        private void ProcessData(byte[] inBytes, int inOff, int inLen, byte[] outBytes, int outOff)
        {
            Check.OutputLength(outBytes, outOff, inLen, "output buffer too short");

            mChacha20.ProcessBytes(inBytes, inOff, inLen, outBytes, outOff);

            mDataCount = IncrementCount(mDataCount, (uint)inLen, DataLimit);
        }

        private void Reset(bool clearMac, bool resetCipher)
        {
            Array.Clear(mBuf, 0, mBuf.Length);

            if (clearMac)
            {
                Array.Clear(mMac, 0, mMac.Length);
            }

            mAadCount = 0UL;
            mDataCount = 0UL;
            mBufPos = 0;

            switch (mState)
            {
                case State.DecInit:
                case State.EncInit:
                    break;
                case State.DecAad:
                case State.DecData:
                case State.DecFinal:
                    mState = State.DecInit;
                    break;
                case State.EncAad:
                case State.EncData:
                case State.EncFinal:
                    mState = State.EncFinal;
                    return;
                default:
                    throw new InvalidOperationException();
            }

            if (resetCipher)
            {
                mChacha20.Reset();
            }

            InitMac();

            if (null != mInitialAad)
            {
                ProcessAadBytes(mInitialAad, 0, mInitialAad.Length);
            }
        }
    }
}
