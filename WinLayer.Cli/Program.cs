namespace WinLayer.Cli;

/// <summary>Точка входа консольной диагностики.</summary>
internal static class Program
{
    /// <summary>Проверяет канал службы и драйвера.</summary>
    /// <returns>Ноль при ответе драйвера, два при его недоступности, один при ошибке службы.</returns>
    private static Task<int> Main() => RunAsync(new ControlClient(), Console.Out, Console.Error);

    /// <summary>Выводит результат диагностики службы и драйвера.</summary>
    /// <param name="client">Клиент диагностического канала.</param>
    /// <param name="output">Поток результата диагностики.</param>
    /// <param name="error">Поток сообщений об ошибках.</param>
    /// <returns>Код завершения диагностики.</returns>
    internal static async Task<int> RunAsync(IControlClient client, TextWriter output, TextWriter error)
    {
        string message;
        try
        {
            DiagnosticResponse response = await client.QueryAsync().ConfigureAwait(false);
            await output.WriteLineAsync($"Состояние: {response.Status}; драйвер: {response.DriverVersion}; архитектура: {response.Architecture}; код Windows: {response.NativeError}.").ConfigureAwait(false);
            await output.WriteLineAsync("Виртуализация файлов и реестра пока не реализована.").ConfigureAwait(false);
            return response.Status == DiagnosticStatus.Success ? 0 : 2;
        }
        catch (OperationCanceledException)
        {
            message = "Служба не ответила за три секунды.";
        }
        catch (IOException)
        {
            message = "Ошибка канала службы или формата ответа.";
        }
        catch (UnauthorizedAccessException)
        {
            message = "Нет доступа к каналу службы.";
        }

        await error.WriteLineAsync(message).ConfigureAwait(false);
        return 1;
    }
}
