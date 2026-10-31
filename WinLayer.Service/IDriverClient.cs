namespace WinLayer.Service;

/// <summary>Предоставляет диагностические сведения драйвера.</summary>
internal interface IDriverClient
{
    /// <summary>Проверяет доступность и совместимость драйвера.</summary>
    /// <returns>Сведения о драйвере либо явная ошибка канала.</returns>
    DiagnosticResponse Query();
}
