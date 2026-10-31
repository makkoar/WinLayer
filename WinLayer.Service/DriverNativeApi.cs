namespace WinLayer.Service;

/// <summary>Обращается к устройству через Win32.</summary>
internal sealed class DriverNativeApi : IDriverNativeApi
{
    /// <inheritdoc />
    public SafeFileHandle Open(string path, out uint error)
    {
        SafeFileHandle device = NativeMethods.CreateFile(path, 0x80000000, 3, nint.Zero, 3, 0x80, nint.Zero);
        error = (uint)Marshal.GetLastPInvokeError();
        return device;
    }

    /// <inheritdoc />
    public bool Query(SafeFileHandle device, byte[] request, byte[] response, out uint returned, out uint error)
    {
        bool success = NativeMethods.DeviceIoControl(device, DiagnosticProtocol.QueryIoControlCode, request, (uint)request.Length, response, (uint)response.Length, out returned, nint.Zero);
        error = (uint)Marshal.GetLastPInvokeError();
        return success;
    }
}
