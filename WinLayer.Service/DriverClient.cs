namespace WinLayer.Service;

/// <summary>Выполняет диагностический IOCTL управляющего драйвера.</summary>
/// <param name="native">Системный канал устройства.</param>
internal sealed class DriverClient(IDriverNativeApi native) : IDriverClient
{
    /// <inheritdoc />
    public DiagnosticResponse Query()
    {
        using SafeFileHandle device = native.Open(DiagnosticProtocol.DevicePath, out uint error);
        if (device.IsInvalid)
            return new(DiagnosticStatus.DriverUnavailable, NativeError: error);

        byte[] request = DiagnosticProtocol.CreateRequest();
        byte[] response = new byte[DiagnosticProtocol.ResponseSize];
        if (!native.Query(device, request, response, out uint returned, out error))
            return new(DiagnosticStatus.DriverUnavailable, NativeError: error);

        if (returned != response.Length)
            return new(DiagnosticStatus.DriverProtocolMismatch);

        try
        {
            return DiagnosticProtocol.DecodeResponse(response);
        }
        catch (InvalidDataException)
        {
            return new(DiagnosticStatus.DriverProtocolMismatch);
        }
    }
}
