namespace WinLayer.App;

/// <summary>Точка входа панели WinLayer.</summary>
internal static class Program
{
    /// <summary>Запускает панель диагностики.</summary>
    /// <param name="args">Аргументы запуска панели.</param>
    [STAThread]
    private static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Настраивает платформу интерфейса.</summary>
    /// <returns>Конфигурация приложения Avalonia.</returns>
    private static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<Application>()
        .UsePlatformDetect();
}
