#pragma once
#include <Windows.h>
#include "../Native.Shared/WinLayerProtocol.h"

#ifdef WINLAYER_RUNTIME_BUILD
#define WL_RUNTIME_API
#else
#define WL_RUNTIME_API __declspec(dllimport)
#endif

#ifdef __cplusplus
extern "C" {
#endif

/// <summary>Запрашивает диагностику драйвера через локальную службу.</summary>
/// <param name="response">Буфер полного ответа.</param>
/// <param name="responseSize">Размер буфера в байтах; должен совпадать с WL_RESPONSE.</param>
/// <returns>Код ошибки Windows либо ERROR_SUCCESS при получении корректного ответа; состояние драйвера находится в response.</returns>
WL_RUNTIME_API DWORD __cdecl WinLayerQuery(WL_RESPONSE* response, DWORD responseSize);

#ifdef __cplusplus
}
#endif
