namespace WinLayer.Client;

/// <summary>Предоставляет сведения о состоянии инфраструктуры.</summary>
public interface IControlClient
{
    /// <summary>Получает фактическое состояние драйвера через службу.</summary>
    /// <param name="cancellationToken">Отмена операции вызывающей стороной.</param>
    /// <returns>Проверенный ответ службы.</returns>
    Task<DiagnosticResponse> QueryAsync(CancellationToken cancellationToken = default);
}
