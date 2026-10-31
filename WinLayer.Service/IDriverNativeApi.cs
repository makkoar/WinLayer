namespace WinLayer.Service;

/// <summary>Предоставляет системные операции управляющего устройства.</summary>
internal interface IDriverNativeApi
{
    /// <summary>Открывает устройство для диагностического запроса.</summary>
    /// <param name="path">Путь устройства.</param>
    /// <param name="error">Код последней ошибки Windows.</param>
    /// <returns>Дескриптор устройства, в том числе недействительный при ошибке открытия.</returns>
    SafeFileHandle Open(string path, out uint error);

    /// <summary>Передаёт диагностический запрос устройству.</summary>
    /// <param name="device">Дескриптор устройства.</param>
    /// <param name="request">Запрос двоичного протокола.</param>
    /// <param name="response">Буфер ответа.</param>
    /// <param name="returned">Число записанных байтов.</param>
    /// <param name="error">Код последней ошибки Windows.</param>
    /// <returns>Признак успешного системного вызова.</returns>
    bool Query(SafeFileHandle device, byte[] request, byte[] response, out uint returned, out uint error);
}
