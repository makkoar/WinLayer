#include <stdio.h>
#include "../WinLayer.Runtime/WinLayerRuntime.h"

/// <summary>Проверяет путь DLL → служба → драйвер для текущей разрядности.</summary>
/// <returns>Ноль при ответе драйвера, два при его недоступности, один при ошибке транспорта.</returns>
int main()
{
    (void)SetConsoleOutputCP(CP_UTF8);
    WL_RESPONSE response = {};
    DWORD error = WinLayerQuery(&response, sizeof(response));
    if (error != ERROR_SUCCESS)
    {
        (void)printf("Ошибка канала службы: %lu.\n", error);
        return 1;
    }

    (void)printf("DLL x%u; состояние: %u; драйвер: %u; архитектура: %u; код Windows: %u.\n",
        static_cast<unsigned int>(sizeof(void*) * 8), response.Status, response.DriverVersion,
        response.Architecture, response.NativeError);
    (void)printf("Виртуализация файлов и реестра пока не реализована.\n");
    return response.Status == WL_STATUS_SUCCESS ? 0 : 2;
}
