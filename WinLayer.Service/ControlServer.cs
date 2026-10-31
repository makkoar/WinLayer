namespace WinLayer.Service;

/// <summary>Обслуживает ограниченный локальный диагностический канал.</summary>
/// <param name="driver">Канал драйвера.</param>
/// <param name="logger">Журнал службы.</param>
internal sealed class ControlServer(IDriverClient driver, ILogger<ControlServer> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Диагностический канал WinLayer запущен. Виртуализация пока не реализована.");
        while (!stoppingToken.IsCancellationRequested)
        {
            using NamedPipeServerStream pipe = CreatePipe();
            await ServeAsync(pipe, stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>Обрабатывает подключения серверного экземпляра канала.</summary>
    /// <param name="pipe">Серверный экземпляр канала.</param>
    /// <param name="stoppingToken">Сигнал остановки службы.</param>
    /// <remarks>При обрыве до завершения подключения экземпляр передаётся вызывающей стороне для пересоздания.</remarks>
    internal async Task ServeAsync(NamedPipeServerStream pipe, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool connected = false;
            try
            {
                await pipe.WaitForConnectionAsync(stoppingToken).ConfigureAwait(false);
                connected = true;
                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(2));
                byte[] request = new byte[DiagnosticProtocol.RequestSize];
                await pipe.ReadExactlyAsync(request, timeout.Token).ConfigureAwait(false);
                DiagnosticStatus status = DiagnosticProtocol.ValidateRequest(request);
                DiagnosticResponse response = status == DiagnosticStatus.Success ? driver.Query() : new(status);
                await pipe.WriteAsync(DiagnosticProtocol.EncodeResponse(response), timeout.Token).ConfigureAwait(false);
                // Ожидаем закрытия клиента, чтобы Disconnect не отбросил ещё не прочитанный ответ.
                byte[] closing = new byte[1];
                _ = await pipe.ReadAsync(closing, timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException exception)
            {
                logger.LogWarning(exception, "Клиент превысил время диагностического запроса.");
            }
            catch (IOException exception)
            {
                logger.LogWarning(exception, "Ошибка диагностического канала: код {Code}.", exception.HResult);
                if (!connected)
                    return;
            }
            finally
            {
                if (connected)
                    pipe.Disconnect();
            }
        }
    }

    /// <summary>Создаёт канал с доступом только для локальных клиентов.</summary>
    /// <returns>Серверный экземпляр канала.</returns>
    private static NamedPipeServerStream CreatePipe()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        PipeSecurity security = CreatePipeSecurity(identity.User);
        return NamedPipeServerStreamAcl.Create(DiagnosticProtocol.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.FirstPipeInstance, DiagnosticProtocol.RequestSize, DiagnosticProtocol.ResponseSize, security);
    }

    /// <summary>Определяет права доступа к локальному каналу.</summary>
    /// <param name="user">Владелец процесса, если идентификатор доступен.</param>
    /// <returns>Правила доступа к серверному экземпляру канала.</returns>
    internal static PipeSecurity CreatePipeSecurity(SecurityIdentifier? user)
    {
        PipeSecurity security = new();
        security.SetAccessRuleProtection(true, false);
        security.AddAccessRule(new(new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));
        security.AddAccessRule(new(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        if (user is not null)
            security.AddAccessRule(new(user, PipeAccessRights.FullControl, AccessControlType.Allow));

        PipeAccessRights clientRights = PipeAccessRights.ReadWrite | PipeAccessRights.Synchronize;
        security.AddAccessRule(new(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), clientRights, AccessControlType.Allow));
        return security;
    }
}
