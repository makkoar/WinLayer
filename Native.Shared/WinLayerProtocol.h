#pragma once

// Фиксированный ABI: только 32-битные целые, little-endian, без адресов памяти.
#define WL_MAGIC 0x31594C57u
#define WL_PROTOCOL_VERSION 1u
#define WL_QUERY_OPERATION 1u
#define WL_CAPABILITY_DIAGNOSTICS 1u
#define WL_DEVICE_TYPE 0x8000u
#define WL_IOCTL_QUERY 0x80006000u
#define WL_PIPE_PATH L"\\\\.\\pipe\\WinLayer.Control.v1"

#define WL_STATUS_SUCCESS 0u
#define WL_STATUS_INVALID_REQUEST 1u
#define WL_STATUS_UNSUPPORTED_PROTOCOL 2u
#define WL_STATUS_DRIVER_UNAVAILABLE 3u
#define WL_STATUS_DRIVER_PROTOCOL_MISMATCH 4u
#define WL_STATUS_INTERNAL_ERROR 5u

typedef struct WL_REQUEST
{
    unsigned int Magic;
    unsigned int Version;
    unsigned int Size;
    unsigned int Operation;
} WL_REQUEST;

typedef struct WL_RESPONSE
{
    unsigned int Magic;
    unsigned int Version;
    unsigned int Size;
    unsigned int Status;
    unsigned int DriverVersion;
    unsigned int Capabilities;
    unsigned int Architecture;
    unsigned int NativeError;
} WL_RESPONSE;

typedef char WL_UINT_MUST_BE_32_BITS[(sizeof(unsigned int) == 4) ? 1 : -1];
typedef char WL_REQUEST_MUST_BE_16_BYTES[(sizeof(WL_REQUEST) == 16) ? 1 : -1];
typedef char WL_RESPONSE_MUST_BE_32_BYTES[(sizeof(WL_RESPONSE) == 32) ? 1 : -1];
