namespace WinLayer.Contracts;

/// <summary>Результат диагностического запроса к инфраструктуре.</summary>
public enum DiagnosticStatus : uint
{
    /// <summary>Драйвер ответил на запрос.</summary>
    Success = 0,

    /// <summary>Формат запроса некорректен.</summary>
    InvalidRequest = 1,

    /// <summary>Версия или операция не поддерживается.</summary>
    UnsupportedProtocol = 2,

    /// <summary>Канал драйвера недоступен.</summary>
    DriverUnavailable = 3,

    /// <summary>Ответ драйвера несовместим с протоколом.</summary>
    DriverProtocolMismatch = 4,

    /// <summary>Не удалось выполнить системную операцию.</summary>
    InternalError = 5
}
