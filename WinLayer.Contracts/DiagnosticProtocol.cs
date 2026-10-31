namespace WinLayer.Contracts;

/// <summary>Кодирует сообщения диагностического канала WinLayer.</summary>
/// <remarks>Поля имеют фиксированную длину и порядок little-endian; нативный контракт находится в Native.Shared/WinLayerProtocol.h.</remarks>
public static class DiagnosticProtocol
{
    /// <summary>Сигнатура сообщения WLY1.</summary>
    public const uint Magic = 0x31594C57;

    /// <summary>Текущая версия протокола.</summary>
    public const uint Version = 1;

    /// <summary>Размер запроса в байтах.</summary>
    public const int RequestSize = 16;

    /// <summary>Размер ответа в байтах.</summary>
    public const int ResponseSize = 32;

    /// <summary>Код диагностической операции.</summary>
    public const uint QueryOperation = 1;

    /// <summary>Возможность получения диагностических сведений.</summary>
    public const uint DiagnosticCapability = 1;

    /// <summary>Имя локального канала службы.</summary>
    public const string PipeName = "WinLayer.Control.v1";

    /// <summary>Путь к управляющему устройству драйвера.</summary>
    public const string DevicePath = @"\\.\WinLayerControl";

    /// <summary>IOCTL диагностики: тип 0x8000, функция 0x800, METHOD_BUFFERED, FILE_READ_DATA.</summary>
    public const uint QueryIoControlCode = 0x80006000;

    /// <summary>Создаёт диагностический запрос.</summary>
    /// <returns>Полное сообщение запроса.</returns>
    public static byte[] CreateRequest()
    {
        byte[] buffer = new byte[RequestSize];
        Write(buffer, 0, Magic);
        Write(buffer, 4, Version);
        Write(buffer, 8, RequestSize);
        Write(buffer, 12, QueryOperation);
        return buffer;
    }

    /// <summary>Проверяет формат и поддерживаемую операцию запроса.</summary>
    /// <param name="buffer">Полное сообщение запроса.</param>
    /// <returns>Результат проверки.</returns>
    public static DiagnosticStatus ValidateRequest(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length != RequestSize || Read(buffer, 0) != Magic || Read(buffer, 8) != RequestSize)
            return DiagnosticStatus.InvalidRequest;

        return Read(buffer, 4) == Version && Read(buffer, 12) == QueryOperation
            ? DiagnosticStatus.Success
            : DiagnosticStatus.UnsupportedProtocol;
    }

    /// <summary>Создаёт двоичное представление ответа.</summary>
    /// <param name="response">Сведения о драйвере.</param>
    /// <returns>Полное сообщение ответа.</returns>
    public static byte[] EncodeResponse(DiagnosticResponse response)
    {
        byte[] buffer = new byte[ResponseSize];
        Write(buffer, 0, Magic);
        Write(buffer, 4, Version);
        Write(buffer, 8, ResponseSize);
        Write(buffer, 12, (uint)response.Status);
        Write(buffer, 16, response.DriverVersion);
        Write(buffer, 20, response.Capabilities);
        Write(buffer, 24, response.Architecture);
        Write(buffer, 28, response.NativeError);
        return buffer;
    }

    /// <summary>Проверяет и декодирует ответ.</summary>
    /// <param name="buffer">Полное сообщение ответа.</param>
    /// <returns>Сведения о драйвере.</returns>
    public static DiagnosticResponse DecodeResponse(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length != ResponseSize || Read(buffer, 0) != Magic || Read(buffer, 4) != Version || Read(buffer, 8) != ResponseSize || Read(buffer, 12) > (uint)DiagnosticStatus.InternalError)
            throw new InvalidDataException("Некорректный заголовок ответа WinLayer.");

        DiagnosticResponse response = new((DiagnosticStatus)Read(buffer, 12), Read(buffer, 16), Read(buffer, 20), Read(buffer, 24), Read(buffer, 28));
        if (response.Status == DiagnosticStatus.Success && (response.DriverVersion == 0 || response.Architecture != 64 || response.Capabilities != DiagnosticCapability || response.NativeError != 0))
            throw new InvalidDataException("Ответ драйвера содержит несовместимые сведения о возможностях.");

        if (response.Status != DiagnosticStatus.Success && (response.DriverVersion != 0 || response.Capabilities != 0 || response.Architecture != 0))
            throw new InvalidDataException("Ответ с ошибкой не должен содержать сведения о доступном драйвере.");

        return response;
    }

    /// <summary>Читает поле сообщения.</summary>
    /// <param name="buffer">Сообщение.</param>
    /// <param name="offset">Смещение поля.</param>
    /// <returns>Значение поля.</returns>
    private static uint Read(ReadOnlySpan<byte> buffer, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(buffer[offset..]);

    /// <summary>Записывает поле сообщения.</summary>
    /// <param name="buffer">Сообщение.</param>
    /// <param name="offset">Смещение поля.</param>
    /// <param name="value">Значение поля.</param>
    private static void Write(Span<byte> buffer, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], value);
}
