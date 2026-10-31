namespace WinLayer.Tests;

/// <summary>Проверяет совместимость и отбраковку сообщений двоичного протокола.</summary>
public sealed class DiagnosticProtocolTests
{
    /// <summary>Проверяет байтовый ABI запроса независимо от разрядности процесса.</summary>
    [Fact(DisplayName = "Проверяет байтовый ABI запроса независимо от разрядности процесса")]
    public void RequestMatchesNativeAbi()
    {
        Assert.Equal("574C5931010000001000000001000000", Convert.ToHexString(DiagnosticProtocol.CreateRequest()));
        Assert.Equal(0x80006000u, DiagnosticProtocol.QueryIoControlCode);
        Assert.Equal(DiagnosticStatus.Success, DiagnosticProtocol.ValidateRequest(DiagnosticProtocol.CreateRequest()));
    }

    /// <summary>Проверяет чтение зафиксированного ответа нативного драйвера.</summary>
    [Fact(DisplayName = "Проверяет чтение зафиксированного ответа нативного драйвера")]
    public void NativeResponseIsDecoded()
    {
        byte[] bytes = Convert.FromHexString("574C593101000000200000000000000001000000010000004000000000000000");
        DiagnosticResponse response = DiagnosticProtocol.DecodeResponse(bytes);
        Assert.Equal(new(DiagnosticStatus.Success, 1, 1, 64), response);
        Assert.Equal(bytes, DiagnosticProtocol.EncodeResponse(response));
    }

    /// <summary>Проверяет отклонение неполных и избыточных запросов.</summary>
    /// <param name="length">Неверная длина сообщения.</param>
    [Theory(DisplayName = "Проверяет отклонение неполных и избыточных запросов")]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public void InvalidRequestLengthIsRejected(int length) =>
        Assert.Equal(DiagnosticStatus.InvalidRequest, DiagnosticProtocol.ValidateRequest(new byte[length]));

    /// <summary>Проверяет различение повреждённого и неподдерживаемого запроса.</summary>
    /// <param name="offset">Смещение изменяемого поля.</param>
    /// <param name="value">Неподдерживаемое значение.</param>
    /// <param name="expected">Ожидаемый результат.</param>
    [Theory(DisplayName = "Проверяет различение повреждённого и неподдерживаемого запроса")]
    [InlineData(0, 0u, DiagnosticStatus.InvalidRequest)]
    [InlineData(4, 2u, DiagnosticStatus.UnsupportedProtocol)]
    [InlineData(8, 32u, DiagnosticStatus.InvalidRequest)]
    [InlineData(12, 99u, DiagnosticStatus.UnsupportedProtocol)]
    public void InvalidRequestFieldIsRejected(int offset, uint value, DiagnosticStatus expected)
    {
        byte[] bytes = DiagnosticProtocol.CreateRequest();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
        Assert.Equal(expected, DiagnosticProtocol.ValidateRequest(bytes));
    }

    /// <summary>Проверяет отбраковку несовместимых и противоречивых ответов.</summary>
    /// <param name="offset">Смещение поля.</param>
    /// <param name="value">Неверное значение.</param>
    [Theory(DisplayName = "Проверяет отбраковку несовместимых и противоречивых ответов")]
    [InlineData(0, 0u)]
    [InlineData(4, 2u)]
    [InlineData(8, 16u)]
    [InlineData(12, 99u)]
    [InlineData(12, 3u)]
    [InlineData(16, 0u)]
    [InlineData(20, 3u)]
    [InlineData(24, 32u)]
    [InlineData(28, 5u)]
    public void InvalidResponseFieldIsRejected(int offset, uint value)
    {
        byte[] bytes = Convert.FromHexString("574C593101000000200000000000000001000000010000004000000000000000");
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
        Assert.Throws<InvalidDataException>(() => DiagnosticProtocol.DecodeResponse(bytes));
    }

    /// <summary>Проверяет запрет усечённых и избыточных ответов.</summary>
    /// <param name="length">Неверная длина сообщения.</param>
    [Theory(DisplayName = "Проверяет запрет усечённых и избыточных ответов")]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(33)]
    public void InvalidResponseLengthIsRejected(int length) =>
        Assert.Throws<InvalidDataException>(() => DiagnosticProtocol.DecodeResponse(new byte[length]));

    /// <summary>Проверяет передачу ошибки доступа без фиктивных сведений о драйвере.</summary>
    [Fact(DisplayName = "Проверяет передачу ошибки доступа без фиктивных сведений о драйвере")]
    public void DriverAccessErrorIsPreserved()
    {
        DiagnosticResponse expected = new(DiagnosticStatus.DriverUnavailable, NativeError: 5);
        Assert.Equal(expected, DiagnosticProtocol.DecodeResponse(DiagnosticProtocol.EncodeResponse(expected)));
    }

    /// <summary>Проверяет отсутствие сведений о драйвере в ответе с ошибкой.</summary>
    /// <param name="offset">Смещение недопустимого поля сведений.</param>
    [Theory(DisplayName = "Ответ с ошибкой не может содержать версию, возможности или архитектуру драйвера")]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(24)]
    public void ErrorResponseCannotContainDriverMetadata(int offset)
    {
        byte[] bytes = DiagnosticProtocol.EncodeResponse(new(DiagnosticStatus.DriverUnavailable, NativeError: 5));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), 1);
        Assert.Throws<InvalidDataException>(() => DiagnosticProtocol.DecodeResponse(bytes));
    }
}
