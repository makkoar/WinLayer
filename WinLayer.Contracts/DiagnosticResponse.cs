namespace WinLayer.Contracts;

/// <summary>Сведения о доступности драйвера.</summary>
/// <param name="Status">Результат запроса.</param>
/// <param name="DriverVersion">Версия диагностического драйвера; ноль при его недоступности.</param>
/// <param name="Capabilities">Битовая маска реализованных возможностей.</param>
/// <param name="Architecture">Разрядность драйвера; ноль при его недоступности.</param>
/// <param name="NativeError">Код ошибки Windows; ноль при отсутствии системной ошибки.</param>
public sealed record DiagnosticResponse(DiagnosticStatus Status, uint DriverVersion = 0, uint Capabilities = 0, uint Architecture = 0, uint NativeError = 0);
