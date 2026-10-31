namespace WinLayer.Tests;

/// <summary>Проверяет жизненный цикл панели и результаты пользовательской команды.</summary>
public sealed class ApplicationTests
{
    /// <summary>Проверяет запуск панели, обновление состояния и завершение интерфейса.</summary>
    [Fact(DisplayName = "Панель запускается, показывает ответы и ошибки, восстанавливает кнопку и закрывается")]
    public async Task DesktopLifecycleAndRefreshWork()
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Thread thread = new(() => RunDesktop(completion)) { IsBackground = true, Name = "Проверка панели WinLayer" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
    }

    /// <summary>Проверяет панель в её собственном потоке интерфейса.</summary>
    /// <param name="completion">Завершение проверки.</param>
    private static void RunDesktop(TaskCompletionSource completion)
    {
        Exception? failure = null;
        Dispatcher.UIThread.Post(async () =>
        {
            IClassicDesktopStyleApplicationLifetime lifetime = (IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current!.ApplicationLifetime!;
            lifetime.MainWindow!.Hide();
            try
            {
                Assert.Equal("WinLayer — прототип", lifetime.MainWindow.Title);
                await CheckRefreshAsync(new(DiagnosticStatus.Success, 1, 1, 64), null, "Служба и драйвер отвечают");
                await CheckRefreshAsync(new(DiagnosticStatus.DriverUnavailable, NativeError: 2), null, "код Windows: 2");
                await CheckRefreshAsync(null, new OperationCanceledException(), "Служба не ответила");
                await CheckRefreshAsync(null, new IOException("Повреждённый ответ."), "корректный ответ");
                await CheckRefreshAsync(null, new UnauthorizedAccessException("Доступ запрещён."), "Нет доступа");
                new WinLayer.App.Application().OnFrameworkInitializationCompleted();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                lifetime.Shutdown();
            }
        });

        try
        {
            _ = EntryPointTests.InvokeMain<object?>(typeof(WinLayer.App.Program), (object)Array.Empty<string>());
            if (failure is not null)
                completion.SetException(failure);
            else
                completion.SetResult();
        }
        catch (Exception exception)
        {
            completion.SetException(exception);
        }
    }

    /// <summary>Проверяет состояние кнопки и текст результата команды.</summary>
    /// <param name="response">Ответ службы при успехе транспорта.</param>
    /// <param name="error">Ошибка транспорта.</param>
    /// <param name="expectedText">Ожидаемый фрагмент текста.</param>
    private static async Task CheckRefreshAsync(DiagnosticResponse? response, Exception? error, string expectedText)
    {
        TaskCompletionSource<DiagnosticResponse> query = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubControlClient client = new(_ => query.Task);
        WinLayer.App.MainWindow window = new(client);
        StackPanel content = Assert.IsType<StackPanel>(window.Content);
        Button button = Assert.Single(content.Children.OfType<Button>());
        TextBlock status = content.Children.OfType<TextBlock>().Last();
        Assert.Equal("Состояние ещё не проверено.", status.Text);
        TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        button.PropertyChanged += (_, args) =>
        {
            if (args.Property == Button.IsEnabledProperty && button.IsEnabled)
                finished.TrySetResult();
        };

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.False(button.IsEnabled);
        Assert.Equal("Проверка связи…", status.Text);
        if (error is not null)
            query.SetException(error);
        else
            query.SetResult(response!);

        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(button.IsEnabled);
        Assert.Contains(expectedText, status.Text);
        window.Close();
    }
}
