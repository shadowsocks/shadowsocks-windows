// This is the main DLL file.

#include "stdafx.h"
#include "libsscrypto.h"
using namespace System;

#ifndef GETSIZET//means 'Get siez_t'
    #define GETSIZET(item) (size_t)item->Length
#endif
#ifndef TOUCHAR//means 'To unsigned char'
    #define TOUCHAR(str) (const unsigned char*)(void*)(System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(str)).ToPointer()
#endif

inline unsigned char* B2C(array<System::Byte>^ idx)
{
    pin_ptr<System::Byte> p = &idx[0];
    unsigned char* pp = p;
    return pp;
}

namespace LibSSCrypto
{
    public ref class MbedTLS
    {
        public:
            literal int MD5_CTX_SIZE = 88;

            static void md5_init(IntPtr ctx)
            {
                mbedtls_md5_context *handle = static_cast<mbedtls_md5_context*>(ctx.ToPointer());
                mbedtls_md5_init(handle);
            }

            static void md5_free(IntPtr ctx)
            {
                mbedtls_md5_context *handle = static_cast<mbedtls_md5_context*>(ctx.ToPointer());
                mbedtls_md5_free(handle);
            }

            static void md5_starts(IntPtr ctx)
            {
                mbedtls_md5_context *handle = static_cast<mbedtls_md5_context*>(ctx.ToPointer());
                mbedtls_md5_starts(handle);
            }

            static void md5_update(IntPtr ctx, array<Byte>^ input, UInt32 ilen)
            {
                mbedtls_md5_context *handle = static_cast<mbedtls_md5_context*>(ctx.ToPointer());
                mbedtls_md5_update(handle, B2C(input), ilen);
            }

            static void md5_finish(IntPtr ctx, array<Byte>^ output)
            {
                mbedtls_md5_context *handle = static_cast<mbedtls_md5_context*>(ctx.ToPointer());
                mbedtls_md5_finish(handle, B2C(output));
            }

            static array<Byte>^ MD5(array<Byte>^ input)
            {
                IntPtr ctx = System::Runtime::InteropServices::Marshal::AllocHGlobal(MD5_CTX_SIZE);
                array<Byte>^ output = gcnew array<Byte>(16);
                md5_init(ctx);
                md5_starts(ctx);
                md5_update(ctx, input, GETSIZET(input));
                md5_finish(ctx, output);
                md5_free(ctx);
                System::Runtime::InteropServices::Marshal::FreeHGlobal(ctx);
                return output;
            }
    };

    public ref class PolarSSL
    {
        public:
            literal int AES_CTX_SIZE = 8 + 4 * 68;
            literal int AES_ENCRYPT = 1;
            literal int AES_DECRYPT = 0;
            literal int ARC4_CTX_SIZE = 264;

            static void aes_init(IntPtr ctx)
            {
                mbedtls_aes_context *handle = static_cast<mbedtls_aes_context*>(ctx.ToPointer());
                mbedtls_aes_init(handle);
            }

            static void aes_free(IntPtr ctx)
            {
                mbedtls_aes_context *handle = static_cast<mbedtls_aes_context*>(ctx.ToPointer());
                mbedtls_aes_free(handle);
            }

            static int aes_setkey_enc(IntPtr ctx, array<Byte>^ key, int keysize)
            {
                mbedtls_aes_context *handle = static_cast<mbedtls_aes_context*>(ctx.ToPointer());
                return mbedtls_aes_setkey_enc(handle, B2C(key), keysize);
            }

            static int aes_crypt_cfb128(IntPtr ctx, int mode, int length, size_t iv_off, array<Byte>^ iv, array<Byte>^ input, array<Byte>^ output)
            {
                mbedtls_aes_context *handle = static_cast<mbedtls_aes_context*>(ctx.ToPointer());
                return mbedtls_aes_crypt_cfb128(handle, mode, length, &iv_off, B2C(iv), B2C(input), B2C(output));
            }

            static int aes_crypt_cfb128(IntPtr ctx, int mode, int length, size_t *iv_off, array<Byte>^ iv, array<Byte>^ input, array<Byte>^ output)
            {
                mbedtls_aes_context *handle = static_cast<mbedtls_aes_context*>(ctx.ToPointer());
                return mbedtls_aes_crypt_cfb128(handle, mode, length, iv_off, B2C(iv), B2C(input), B2C(output));
            }

            static void arc4_init(IntPtr ctx)
            {
                mbedtls_arc4_context *handle = static_cast<mbedtls_arc4_context*>(ctx.ToPointer());
                mbedtls_arc4_init(handle);
            }

            static void arc4_free(IntPtr ctx)
            {
                mbedtls_arc4_context *handle = static_cast<mbedtls_arc4_context*>(ctx.ToPointer());
                mbedtls_arc4_free(handle);
            }

            static void arc4_setup(IntPtr ctx, array<Byte>^ key, int keysize)
            {
                mbedtls_arc4_context *handle = static_cast<mbedtls_arc4_context*>(ctx.ToPointer());
                mbedtls_arc4_setup(handle, B2C(key), keysize);
            }

            static int arc4_crypt(IntPtr ctx, int length, array<Byte>^ input, array<Byte>^ output)
            {
                mbedtls_arc4_context *handle = static_cast<mbedtls_arc4_context*>(ctx.ToPointer());
                return mbedtls_arc4_crypt(handle, length, B2C(input), B2C(output));
            }
    };

    public ref class Sodium
    {
        public:
            static int crypto_stream_salsa20_xoric(array<Byte>^ c, array<Byte>^ m, UInt64 mlen, array<Byte>^ n, UInt64 ic, array<Byte>^ k)
            {
                return crypto_stream_salsa20_xor_ic(B2C(c), B2C(m), mlen, B2C(n), ic, B2C(k));
            }

            static int crypto_stream_chacha20_xoric(array<Byte>^ c, array<Byte>^ m, UInt64 mlen, array<Byte>^ n, UInt64 ic, array<Byte>^ k)
            {
                return crypto_stream_chacha20_xor_ic(B2C(c), B2C(m), mlen, B2C(n), ic, B2C(k));
            }

            static int crypto_stream_chacha20_ietf_xoric(array<Byte>^ c, array<Byte>^ m, UInt64 mlen, array<Byte>^ n, UInt32 ic, array<Byte>^ k)
            {
                return crypto_stream_chacha20_ietf_xor_ic(B2C(c), B2C(m), mlen, B2C(n), ic, B2C(k));
            }

            static int ss_sha1_hmac_ex(array<Byte>^ key, UInt32 keylen, array<Byte>^ input, int ioff, UInt32 ilen, array<Byte>^ output)
            {
                return mbedtls_md_hmac(mbedtls_md_info_from_type(MBEDTLS_MD_SHA1), B2C(key), keylen, B2C(input) + ioff, ilen, B2C(output));
            }
    };

    //[System::Runtime::CompilerServices::ExtensionAttribute]
    public ref class MessageDigest abstract sealed
    {
        public:
            //[System::Runtime::CompilerServices::ExtensionAttribute]
            static String^ MD5(String^ input, System::Text::Encoding^ enc)
            {
                array<Byte>^ output = gcnew array<Byte>(16);
                mbedtls_md5(B2C(enc->GetBytes(input)), GETSIZET(input), B2C(output));
                return BitConverter::ToString(output)->Replace("-", "");
            }
            static array<Byte>^ MD5(array<Byte>^ input)
            {
                array<Byte>^ output = gcnew array<Byte>(16);
                mbedtls_md5(B2C(input), GETSIZET(input), B2C(output));
                return output;
            }
            static array<Byte>^ MD5(String^ input)
            {
                array<Byte>^ output = gcnew array<Byte>(16);
                mbedtls_md5(TOUCHAR(input), GETSIZET(input), B2C(output));
                return output;
            }
            static array<Byte>^ HMAC_MD5(array<Byte>^ key, UInt32 keylen, array<Byte>^ input, int ioff, UInt32 ilen)
            {
                array<Byte>^ output = gcnew array<Byte>(16);
                mbedtls_md_hmac(mbedtls_md_info_from_type(MBEDTLS_MD_MD5), B2C(key), keylen, B2C(input) + ioff, ilen, B2C(output));
                return output;
            }
            static array<Byte>^ HMAC_SHA1(array<Byte>^ key, UInt32 keylen, array<Byte>^ input, int ioff, UInt32 ilen)
            {
                array<Byte>^ output = gcnew array<Byte>(20);
                mbedtls_md_hmac(mbedtls_md_info_from_type(MBEDTLS_MD_SHA1), B2C(key), keylen, B2C(input) + ioff, ilen, B2C(output));
                return output;
            }

            static UInt32 GetRandom(UInt32 upper_bound)
            {
                return randombytes_uniform(upper_bound);
            }
    };
}
