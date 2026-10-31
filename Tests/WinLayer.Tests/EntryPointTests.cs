namespace WinLayer.Tests;

/// <summary>Проверяет запуск и завершение исполняемых компонентов.</summary>
public sealed class EntryPointTests
{
    /// <summary>Проверяет сборку хоста, работу CLI и корректную остановку службы.</summary>
    [Fact(DisplayName = "Точки входа службы и CLI выполняют диагностику и завершаются")]
    public async Task ServiceAndCliEntryPointsRun()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        TaskCompletionSource<IHost> created = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentBag<IDisposable> subscriptions = [];
        using IDisposable listeners = DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(listener =>
        {
            if (listener.Name == "Microsoft.Extensions.Hosting")
            {
                subscriptions.Add(listener.Subscribe(new CallbackObserver<KeyValuePair<string, object?>>(item =>
                {
                    if (item.Key == "HostBuilt" && item.Value is IHost host)
                        created.TrySetResult(host);
                })));
            }
        }));

        Task service = InvokeMain<Task>(typeof(WinLayer.Service.Program), (object)Array.Empty<string>());
        IHost host = await created.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
        try
        {
            Assert.Equal("WinLayer", host.Services.GetRequiredService<IOptions<WindowsServiceLifetimeOptions>>().Value.ServiceName);
            Assert.IsType<DriverNativeApi>(host.Services.GetRequiredService<IDriverNativeApi>());
            Assert.IsType<DriverClient>(host.Services.GetRequiredService<IDriverClient>());
            DiagnosticResponse expected = await new ControlClient().QueryAsync(token);
            Assert.Equal(expected.Status == DiagnosticStatus.Success ? 0 : 2, await InvokeMain<Task<int>>(typeof(WinLayer.Cli.Program)));
        }
        finally
        {
            host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
            await service.WaitAsync(TimeSpan.FromSeconds(5), token);
            foreach (IDisposable subscription in subscriptions)
                subscription.Dispose();
        }
    }

    /// <summary>Вызывает исходную точку входа исполняемой сборки.</summary>
    /// <typeparam name="T">Тип результата точки входа.</typeparam>
    /// <param name="program">Тип точки входа.</param>
    /// <param name="arguments">Аргументы метода.</param>
    /// <returns>Результат вызова точки входа.</returns>
    internal static T InvokeMain<T>(Type program, params object[] arguments) =>
        (T)program.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, arguments)!;
}
