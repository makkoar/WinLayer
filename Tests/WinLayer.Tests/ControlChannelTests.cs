namespace WinLayer.Tests;

/// <summary>Проверяет обмен по настоящему локальному каналу.</summary>
public sealed class ControlChannelTests
{
    /// <summary>Проверяет обрыв клиента до начала ожидания сервером.</summary>
    [Fact(DisplayName = "Ранний обрыв подключения не оставляет сервер в бесконечном цикле ошибок")]
    public async Task DisconnectBeforeAcceptRequestsPipeRecreation()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string name = "WinLayer.Tests." + Guid.NewGuid().ToString("N");
        using NamedPipeServerStream pipe = new(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        using (NamedPipeClientStream client = new(".", name, PipeDirection.InOut, PipeOptions.Asynchronous))
            await client.ConnectAsync(token);

        RecordingLogger logger = new();
        using ControlServer server = new(new StubDriverClient(new(DiagnosticStatus.DriverUnavailable)), logger);
        await server.ServeAsync(pipe, token).WaitAsync(TimeSpan.FromSeconds(5), token);
        Assert.IsType<IOException>(await logger.Warning.Task.WaitAsync(TimeSpan.FromSeconds(5), token), exactMatch: false);
    }

    /// <summary>Проверяет повторные подключения клиента и остановку ожидающего сервера.</summary>
    [Fact(DisplayName = "Клиент получает ответы при повторных подключениях к службе")]
    public async Task RepeatedQueriesSucceed()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        DiagnosticResponse expected = new(DiagnosticStatus.Success, 1, 1, 64);
        using ControlServer server = new(new StubDriverClient(expected), NullLogger<ControlServer>.Instance);
        await server.StartAsync(token);
        try
        {
            ControlClient client = new();
            for (int index = 0; index < 3; index++)
                Assert.Equal(expected, await client.QueryAsync(token));
        }
        finally
        {
            await server.StopAsync(token);
        }
    }

    /// <summary>Проверяет приём запроса по частям и отказ для несовместимых данных.</summary>
    [Fact(DisplayName = "Служба собирает запрос по частям и отклоняет повреждённый заголовок")]
    public async Task FragmentedInvalidRequestIsRejected()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using ControlServer server = new(new StubDriverClient(new(DiagnosticStatus.Success, 1, 1, 64)), NullLogger<ControlServer>.Instance);
        await server.StartAsync(token);
        try
        {
            using NamedPipeClientStream pipe = new(".", DiagnosticProtocol.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(token);
            byte[] request = DiagnosticProtocol.CreateRequest();
            request[0] = 0;
            await pipe.WriteAsync(request.AsMemory(0, 7), token);
            await pipe.WriteAsync(request.AsMemory(7), token);
            byte[] response = new byte[DiagnosticProtocol.ResponseSize];
            await pipe.ReadExactlyAsync(response, token);
            Assert.Equal(new(DiagnosticStatus.InvalidRequest), DiagnosticProtocol.DecodeResponse(response));
        }
        finally
        {
            await server.StopAsync(token);
        }
    }

    /// <summary>Проверяет восстановление после оборванного или зависшего запроса.</summary>
    /// <param name="stall">Признак зависания клиента вместо закрытия соединения.</param>
    [Theory(DisplayName = "Служба принимает новый запрос после обрыва или таймаута клиента")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BrokenClientDoesNotBlockNextRequest(bool stall)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        DiagnosticResponse expected = new(DiagnosticStatus.DriverUnavailable, NativeError: 2);
        RecordingLogger logger = new();
        using ControlServer server = new(new StubDriverClient(expected), logger);
        await server.StartAsync(token);
        try
        {
            using (NamedPipeClientStream pipe = new(".", DiagnosticProtocol.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                await pipe.ConnectAsync(token);
                await pipe.WriteAsync(new byte[] { 1 }, token);
                if (stall)
                    Assert.IsType<OperationCanceledException>(await logger.Warning.Task.WaitAsync(TimeSpan.FromSeconds(5), token), exactMatch: false);
            }

            Exception? warning = await logger.Warning.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
            if (!stall)
                Assert.IsType<IOException>(warning, exactMatch: false);

            try
            {
                Assert.Equal(expected, await new ControlClient().QueryAsync(token));
            }
            finally
            {
                Assert.False(server.ExecuteTask!.IsFaulted, server.ExecuteTask.Exception?.ToString());
            }
        }
        finally
        {
            await server.StopAsync(token);
        }
    }

    /// <summary>Проверяет немедленную отмену клиента до подключения.</summary>
    [Fact(DisplayName = "Отмена клиентского запроса прекращает ожидание службы")]
    public async Task ClientCancellationIsRespected()
    {
        using CancellationTokenSource canceled = new();
        await canceled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ControlClient().QueryAsync(canceled.Token));
    }

    /// <summary>Проверяет отказ клиента при усечённом ответе службы.</summary>
    [Fact(DisplayName = "Клиент отклоняет усечённый ответ канала")]
    public async Task TruncatedResponseIsRejected()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using NamedPipeServerStream pipe = new(DiagnosticProtocol.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.FirstPipeInstance);
        Task<DiagnosticResponse> query = new ControlClient().QueryAsync(token);
        await pipe.WaitForConnectionAsync(token);
        byte[] request = new byte[DiagnosticProtocol.RequestSize];
        await pipe.ReadExactlyAsync(request, token);
        Assert.Equal(DiagnosticProtocol.CreateRequest(), request);
        await pipe.WriteAsync(new byte[] { 1, 2, 3 }, token);
        await pipe.DisposeAsync();
        await Assert.ThrowsAsync<EndOfStreamException>(() => query);
    }

    /// <summary>Проверяет отсутствие ожидания у заранее остановленного сервера.</summary>
    [Fact(DisplayName = "Отменённый сервер не начинает принимать запросы")]
    public async Task CanceledServerDoesNotWait()
    {
        using CancellationTokenSource canceled = new();
        await canceled.CancelAsync();
        using ControlServer server = new(new StubDriverClient(new(DiagnosticStatus.DriverUnavailable)), NullLogger<ControlServer>.Instance);
        MethodInfo execute = typeof(ControlServer).GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Task operation = (Task)execute.Invoke(server, [canceled.Token])!;
        await operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(operation.IsCompletedSuccessfully);
        using NamedPipeServerStream pipe = new("WinLayer.Tests." + Guid.NewGuid().ToString("N"));
        await server.ServeAsync(pipe, canceled.Token);
        Assert.False(pipe.IsConnected);
    }

    /// <summary>Проверяет ACL канала с известным и отсутствующим владельцем.</summary>
    /// <param name="includeOwner">Добавить ли владельца процесса.</param>
    [Theory(DisplayName = "ACL канала запрещает сеть и разрешает локальную диагностику")]
    [InlineData(false)]
    [InlineData(true)]
    public void PipeSecurityIsLocal(bool includeOwner)
    {
        SecurityIdentifier? owner = includeOwner ? new("S-1-5-21-1-2-3-1001") : null;
        PipeSecurity security = ControlServer.CreatePipeSecurity(owner);
        Assert.True(security.AreAccessRulesProtected);
        PipeAccessRule[] rules = [.. security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<PipeAccessRule>()];
        Assert.Contains(rules, rule => rule.IdentityReference.Value == "S-1-5-2" && rule.AccessControlType == AccessControlType.Deny && rule.PipeAccessRights == PipeAccessRights.FullControl);
        Assert.Contains(rules, rule => rule.IdentityReference.Value == "S-1-5-11" && rule.AccessControlType == AccessControlType.Allow && rule.PipeAccessRights == (PipeAccessRights.ReadWrite | PipeAccessRights.Synchronize));
        Assert.Contains(rules, rule => rule.IdentityReference.Value == "S-1-5-18" && rule.PipeAccessRights == PipeAccessRights.FullControl);
        Assert.Contains(rules, rule => rule.IdentityReference.Value == "S-1-5-32-544" && rule.PipeAccessRights == PipeAccessRights.FullControl);
        Assert.Equal(includeOwner, rules.Any(rule => rule.IdentityReference.Equals(owner)));
    }
}
