namespace WinLayer.Tests;

/// <summary>Возвращает заданный результат клиентского запроса.</summary>
/// <param name="query">Обработка запроса.</param>
internal sealed class StubControlClient(Func<CancellationToken, Task<DiagnosticResponse>> query) : IControlClient
{
    /// <inheritdoc />
    public Task<DiagnosticResponse> QueryAsync(CancellationToken cancellationToken = default) => query(cancellationToken);
}

/// <summary>Возвращает заданный ответ драйвера.</summary>
/// <param name="response">Ответ драйвера.</param>
internal sealed class StubDriverClient(DiagnosticResponse response) : IDriverClient
{
    /// <inheritdoc />
    public DiagnosticResponse Query() => response;
}

/// <summary>Воспроизводит ответы системного канала драйвера.</summary>
/// <param name="openError">Ошибка открытия либо ноль.</param>
/// <param name="queryError">Ошибка запроса либо ноль.</param>
/// <param name="bytes">Байты ответа.</param>
internal sealed class StubDriverNativeApi(uint openError, uint queryError, byte[] bytes) : IDriverNativeApi
{
    /// <summary>Последний выданный дескриптор для проверки освобождения.</summary>
    internal SafeFileHandle? Handle { get; private set; }

    /// <inheritdoc />
    public SafeFileHandle Open(string path, out uint error)
    {
        Assert.Equal(DiagnosticProtocol.DevicePath, path);
        error = openError;
        Handle = new(openError == 0 ? new nint(123) : new nint(-1), false);
        return Handle;
    }

    /// <inheritdoc />
    public bool Query(SafeFileHandle device, byte[] request, byte[] response, out uint returned, out uint error)
    {
        Assert.Same(Handle, device);
        Assert.Equal(DiagnosticProtocol.CreateRequest(), request);
        Assert.Equal(DiagnosticProtocol.ResponseSize, response.Length);
        bytes.CopyTo(response, 0);
        returned = (uint)bytes.Length;
        error = queryError;
        return queryError == 0;
    }
}

/// <summary>Сохраняет предупреждения сервера для проверки восстановления канала.</summary>
internal sealed class RecordingLogger : ILogger<ControlServer>
{
    /// <summary>Первое зарегистрированное предупреждение.</summary>
    internal TaskCompletionSource<Exception?> Warning { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning)
            Warning.TrySetResult(exception);
    }
}

/// <summary>Передаёт события диагностики указанному обработчику.</summary>
/// <typeparam name="T">Тип события.</typeparam>
/// <param name="onNext">Обработчик события.</param>
internal sealed class CallbackObserver<T>(Action<T> onNext) : IObserver<T>
{
    /// <inheritdoc />
    public void OnNext(T value) => onNext(value);

    /// <inheritdoc />
    public void OnCompleted() { }

    /// <inheritdoc />
    public void OnError(Exception error) => throw new InvalidOperationException("Ошибка источника событий тестового хоста.", error);
}
