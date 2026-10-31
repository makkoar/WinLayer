#include "WinLayerRuntime.h"

/// <summary>Возвращает оставшееся время общего запроса.</summary>
/// <param name="deadline">Предельное значение счётчика миллисекунд.</param>
/// <returns>Оставшиеся миллисекунды либо ноль.</returns>
static DWORD WlRemaining(ULONGLONG deadline)
{
    ULONGLONG now = GetTickCount64();
    return now < deadline ? static_cast<DWORD>(deadline - now) : 0;
}

/// <summary>Передаёт полный буфер через канал с общим сроком ожидания.</summary>
/// <param name="pipe">Дескриптор канала.</param>
/// <param name="buffer">Буфер чтения или записи.</param>
/// <param name="length">Размер буфера.</param>
/// <param name="write">Признак записи.</param>
/// <param name="deadline">Предельное время завершения.</param>
/// <returns>Код ошибки Windows или ERROR_SUCCESS.</returns>
static DWORD WlTransfer(HANDLE pipe, void* buffer, DWORD length, bool write, ULONGLONG deadline)
{
    OVERLAPPED operation = {};
    operation.hEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    if (operation.hEvent == nullptr)
        return GetLastError();

    DWORD offset = 0;
    DWORD error = ERROR_SUCCESS;
    while (offset < length)
    {
        DWORD remaining = WlRemaining(deadline);
        if (remaining == 0)
        {
            error = ERROR_TIMEOUT;
            break;
        }

        (void)ResetEvent(operation.hEvent);
        DWORD transferred = 0;
        BYTE* position = static_cast<BYTE*>(buffer) + offset;
        BOOL completed = write
            ? WriteFile(pipe, position, length - offset, &transferred, &operation)
            : ReadFile(pipe, position, length - offset, &transferred, &operation);
        if (!completed)
        {
            error = GetLastError();
            if (error == ERROR_IO_PENDING)
            {
                if (GetOverlappedResultEx(pipe, &operation, &transferred, remaining, FALSE))
                    error = ERROR_SUCCESS;
                else
                {
                    error = GetLastError();
                    // Ожидаем завершения отмены до освобождения OVERLAPPED и буфера.
                    (void)CancelIoEx(pipe, &operation);
                    (void)GetOverlappedResult(pipe, &operation, &transferred, TRUE);
                    if (error == WAIT_TIMEOUT)
                        error = ERROR_TIMEOUT;
                }
            }
            if (error != ERROR_SUCCESS)
                break;
        }
        if (transferred == 0)
        {
            error = ERROR_BROKEN_PIPE;
            break;
        }
        offset += transferred;
    }
    (void)CloseHandle(operation.hEvent);
    return error;
}

/// <summary>Проверяет согласованность ответа службы.</summary>
/// <param name="response">Полученный ответ.</param>
/// <returns>Признак корректного сообщения.</returns>
static bool WlValidResponse(const WL_RESPONSE& response)
{
    if (response.Magic != WL_MAGIC || response.Version != WL_PROTOCOL_VERSION ||
        response.Size != sizeof(response) || response.Status > WL_STATUS_INTERNAL_ERROR)
        return false;

    if (response.Status == WL_STATUS_SUCCESS)
        return response.DriverVersion != 0 && response.Capabilities == WL_CAPABILITY_DIAGNOSTICS &&
            response.Architecture == 64 && response.NativeError == 0;

    return response.DriverVersion == 0 && response.Capabilities == 0 && response.Architecture == 0;
}

/// <summary>Запрашивает диагностику драйвера через локальную службу.</summary>
/// <param name="response">Буфер полного ответа.</param>
/// <param name="responseSize">Размер буфера.</param>
/// <returns>Код результата транспортной операции Windows.</returns>
DWORD __cdecl WinLayerQuery(WL_RESPONSE* response, DWORD responseSize)
{
    if (response == nullptr || responseSize != sizeof(WL_RESPONSE))
        return ERROR_INVALID_PARAMETER;

    *response = {};
    ULONGLONG deadline = GetTickCount64() + 3000;
    HANDLE pipe;
    for (;;)
    {
        pipe = CreateFileW(WL_PIPE_PATH, GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_EXISTING,
            FILE_FLAG_OVERLAPPED | SECURITY_SQOS_PRESENT | SECURITY_IDENTIFICATION, nullptr);
        if (pipe != INVALID_HANDLE_VALUE)
            break;

        DWORD error = GetLastError();
        if (error != ERROR_PIPE_BUSY)
            return error;
        DWORD remaining = WlRemaining(deadline);
        if (remaining == 0)
            return ERROR_TIMEOUT;
        if (!WaitNamedPipeW(WL_PIPE_PATH, remaining))
            return GetLastError();
    }

    WL_REQUEST request = { WL_MAGIC, WL_PROTOCOL_VERSION, sizeof(WL_REQUEST), WL_QUERY_OPERATION };
    WL_RESPONSE received = {};
    DWORD error = WlTransfer(pipe, &request, sizeof(request), true, deadline);
    if (error == ERROR_SUCCESS)
        error = WlTransfer(pipe, &received, sizeof(received), false, deadline);
    (void)CloseHandle(pipe);

    if (error != ERROR_SUCCESS)
        return error;
    if (!WlValidResponse(received))
        return ERROR_INVALID_DATA;

    *response = received;
    return ERROR_SUCCESS;
}
