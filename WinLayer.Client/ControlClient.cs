namespace WinLayer.Client;

/// <summary>Обращается к диагностическому каналу локальной службы.</summary>
public sealed class ControlClient : IControlClient
{
    /// <inheritdoc />
    public async Task<DiagnosticResponse> QueryAsync(CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        using NamedPipeClientStream pipe = new(".", DiagnosticProtocol.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous, System.Security.Principal.TokenImpersonationLevel.Identification);
        await pipe.ConnectAsync(timeout.Token).ConfigureAwait(false);
        await pipe.WriteAsync(DiagnosticProtocol.CreateRequest(), timeout.Token).ConfigureAwait(false);
        byte[] response = new byte[DiagnosticProtocol.ResponseSize];
        await pipe.ReadExactlyAsync(response, timeout.Token).ConfigureAwait(false);
        return DiagnosticProtocol.DecodeResponse(response);
    }
}
