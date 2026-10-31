namespace WinLayer.Tests;

/// <summary>Проверяет обработку системных ошибок и ответов управляющего устройства.</summary>
public sealed class DriverClientTests
{
    /// <summary>Проверяет сохранение ошибки открытия и освобождение дескриптора.</summary>
    [Fact(DisplayName = "Ошибка открытия устройства сохраняется в ответе")]
    public void OpenErrorIsPreserved()
    {
        StubDriverNativeApi native = new(5, 0, []);
        Assert.Equal(new(DiagnosticStatus.DriverUnavailable, NativeError: 5), new DriverClient(native).Query());
        Assert.True(native.Handle!.IsClosed);
    }

    /// <summary>Проверяет сохранение ошибки IOCTL.</summary>
    [Fact(DisplayName = "Ошибка IOCTL сохраняется и дескриптор освобождается")]
    public void QueryErrorIsPreserved()
    {
        StubDriverNativeApi native = new(0, 31, []);
        Assert.Equal(new(DiagnosticStatus.DriverUnavailable, NativeError: 31), new DriverClient(native).Query());
        Assert.True(native.Handle!.IsClosed);
    }

    /// <summary>Проверяет отклонение неполного и повреждённого ответа драйвера.</summary>
    /// <param name="length">Длина повреждённого ответа.</param>
    [Theory(DisplayName = "Неполный или повреждённый ответ драйвера отклоняется")]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(32)]
    public void InvalidDriverResponseIsRejected(int length)
    {
        StubDriverNativeApi native = new(0, 0, new byte[length]);
        Assert.Equal(new(DiagnosticStatus.DriverProtocolMismatch), new DriverClient(native).Query());
        Assert.True(native.Handle!.IsClosed);
    }

    /// <summary>Проверяет передачу проверенного ответа драйвера.</summary>
    [Fact(DisplayName = "Корректный ответ драйвера передаётся без изменений")]
    public void ValidDriverResponseIsReturned()
    {
        DiagnosticResponse expected = new(DiagnosticStatus.Success, 1, 1, 64);
        StubDriverNativeApi native = new(0, 0, DiagnosticProtocol.EncodeResponse(expected));
        Assert.Equal(expected, new DriverClient(native).Query());
        Assert.True(native.Handle!.IsClosed);
    }

    /// <summary>Проверяет настоящие вызовы Win32 без установки драйвера.</summary>
    [Fact(DisplayName = "Системный адаптер сообщает об отсутствующем устройстве и недопустимом IOCTL")]
    public void NativeAdapterReportsWindowsErrors()
    {
        DriverNativeApi native = new();
        using SafeFileHandle missing = native.Open(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), out uint openError);
        Assert.True(missing.IsInvalid);
        Assert.Equal(2u, openError);
        string path = Path.GetTempFileName();
        try
        {
            using SafeFileHandle file = native.Open(path, out _);
            Assert.False(file.IsInvalid);
            Assert.False(native.Query(file, DiagnosticProtocol.CreateRequest(), new byte[DiagnosticProtocol.ResponseSize], out uint returned, out uint queryError));
            Assert.Equal(0u, returned);
            Assert.NotEqual(0u, queryError);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
