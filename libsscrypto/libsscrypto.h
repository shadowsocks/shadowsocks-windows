// libsscrypto.h

#pragma once

#pragma comment(lib, "Ws2_32")
#pragma comment(lib, "libsodium")
#pragma comment(lib, "mbedTLS")

#if _WIN32 || _WIN64
#if _WIN64
#define ENVIRONMENT64
#else
#define ENVIRONMENT32
#endif
#endif

#include <string>
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>

#include "..\3rd\libsodium\src\libsodium\include\sodium\randombytes.h"
#include "..\3rd\libsodium\src\libsodium\include\sodium\crypto_stream_salsa20.h"
#include "..\3rd\libsodium\src\libsodium\include\sodium\crypto_stream_chacha20.h"
#include "..\3rd\mbedtls\include\mbedtls\md.h"
#include "..\3rd\mbedtls\include\mbedtls\aes.h"
#include "..\3rd\mbedtls\include\mbedtls\arc4.h"
#include "..\3rd\mbedtls\include\mbedtls\md5.h"
