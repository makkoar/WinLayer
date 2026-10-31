namespace WinLayer.Service;

/// <summary>Объявления системного канала драйвера.</summary>
internal static partial class NativeMethods
{
    /// <summary>Открывает управляющее устройство.</summary>
    /// <param name="fileName">Путь устройства.</param>
    /// <param name="desiredAccess">Запрошенные права.</param>
    /// <param name="shareMode">Режим совместного доступа.</param>
    /// <param name="securityAttributes">Атрибуты безопасности.</param>
    /// <param name="creationDisposition">Режим открытия.</param>
    /// <param name="flagsAndAttributes">Флаги открытия.</param>
    /// <param name="templateFile">Шаблонный дескриптор.</param>
    /// <returns>Дескриптор устройства; ошибка доступна через Marshal.GetLastPInvokeError.</returns>
    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode, nint securityAttributes, uint creationDisposition, uint flagsAndAttributes, nint templateFile);

    /// <summary>Отправляет буферизованный запрос устройству.</summary>
    /// <param name="device">Дескриптор устройства.</param>
    /// <param name="controlCode">Код операции.</param>
    /// <param name="input">Буфер запроса.</param>
    /// <param name="inputSize">Длина запроса.</param>
    /// <param name="output">Буфер ответа.</param>
    /// <param name="outputSize">Ёмкость ответа.</param>
    /// <param name="bytesReturned">Фактическая длина ответа.</param>
    /// <param name="overlapped">Параметры асинхронной операции.</param>
    /// <returns>Признак успешного выполнения системного вызова.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeviceIoControl(SafeFileHandle device, uint controlCode, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] input, uint inputSize, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] output, uint outputSize, out uint bytesReturned, nint overlapped);
}
