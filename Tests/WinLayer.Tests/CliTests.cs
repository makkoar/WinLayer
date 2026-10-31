namespace WinLayer.Tests;

/// <summary>Проверяет коды завершения и сообщения консольной диагностики.</summary>
public sealed class CliTests
{
    /// <summary>Проверяет, что внутренняя ошибка не выдаётся за отказ канала.</summary>
    [Fact(DisplayName = "CLI не скрывает неожиданные внутренние ошибки")]
    public async Task UnexpectedFailureIsNotHidden()
    {
        InvalidOperationException expected = new("Нарушен внутренний контракт клиента.");
        StubControlClient client = new(_ => Task.FromException<DiagnosticResponse>(expected));
        using StringWriter output = new();
        using StringWriter error = new();
        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => WinLayer.Cli.Program.RunAsync(client, output, error)));
        Assert.Equal(string.Empty, output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    /// <summary>Проверяет вывод состояния драйвера.</summary>
    /// <param name="available">Доступность драйвера.</param>
    [Theory(DisplayName = "CLI различает доступный и недоступный драйвер")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DriverStatusIsPrinted(bool available)
    {
        DiagnosticResponse response = available ? new(DiagnosticStatus.Success, 1, 1, 64) : new(DiagnosticStatus.DriverUnavailable, NativeError: 2);
        StubControlClient client = new(_ => Task.FromResult(response));
        using StringWriter output = new();
        using StringWriter error = new();
        Assert.Equal(available ? 0 : 2, await WinLayer.Cli.Program.RunAsync(client, output, error));
        Assert.Contains($"Состояние: {response.Status}", output.ToString());
        Assert.Contains($"архитектура: {response.Architecture}", output.ToString());
        Assert.Contains($"код Windows: {response.NativeError}", output.ToString());
        Assert.Contains("пока не реализована", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    /// <summary>Проверяет понятное сообщение для каждого отказа канала.</summary>
    /// <param name="failure">Вид отказа.</param>
    /// <param name="message">Ожидаемое сообщение.</param>
    [Theory(DisplayName = "CLI объясняет таймаут, повреждение ответа и отказ в доступе")]
    [InlineData(0, "Служба не ответила")]
    [InlineData(1, "Ошибка канала")]
    [InlineData(2, "Нет доступа")]
    public async Task ChannelFailureIsExplained(int failure, string message)
    {
        Exception exception = failure switch
        {
            0 => new OperationCanceledException(),
            1 => new IOException("Повреждённый ответ."),
            _ => new UnauthorizedAccessException("Доступ запрещён.")
        };
        StubControlClient client = new(_ => Task.FromException<DiagnosticResponse>(exception));
        using StringWriter output = new();
        using StringWriter error = new();
        Assert.Equal(1, await WinLayer.Cli.Program.RunAsync(client, output, error));
        Assert.Equal(string.Empty, output.ToString());
        Assert.Contains(message, error.ToString());
    }
}
