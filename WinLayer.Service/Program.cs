namespace WinLayer.Service;

/// <summary>Точка входа службы управления WinLayer.</summary>
internal static class Program
{
    /// <summary>Запускает службу или её консольный диагностический режим.</summary>
    /// <param name="args">Параметры стандартного хоста .NET.</param>
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
        builder.Services.AddWindowsService();
        builder.Services.Configure<WindowsServiceLifetimeOptions>(options => options.ServiceName = "WinLayer");
        builder.Services.AddSingleton<IDriverNativeApi, DriverNativeApi>();
        builder.Services.AddSingleton<IDriverClient, DriverClient>();
        builder.Services.AddHostedService<ControlServer>();
        using IHost host = builder.Build();
        await host.RunAsync().ConfigureAwait(false);
    }
}
