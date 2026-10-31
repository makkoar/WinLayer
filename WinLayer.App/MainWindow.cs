namespace WinLayer.App;

/// <summary>Показывает состояние диагностической инфраструктуры.</summary>
internal sealed class MainWindow : Window
{
    private readonly IControlClient client;
    private readonly TextBlock status = new() { Text = "Состояние ещё не проверено.", TextWrapping = TextWrapping.Wrap };
    private readonly Button refresh = new() { Content = "Проверить связь" };

    /// <summary>Создаёт диагностическое окно.</summary>
    public MainWindow() : this(new ControlClient())
    {
    }

    /// <summary>Создаёт диагностическое окно с указанным каналом службы.</summary>
    /// <param name="client">Клиент диагностического канала.</param>
    internal MainWindow(IControlClient client)
    {
        this.client = client;
        Title = "WinLayer — прототип";
        Width = 640;
        Height = 360;
        MinWidth = 480;
        MinHeight = 300;
        refresh.Click += OnRefresh;
        Content = new StackPanel
        {
            Margin = new Thickness(28),
            Spacing = 20,
            Children =
            {
                new TextBlock { Text = "WinLayer", FontSize = 30, FontWeight = FontWeight.SemiBold },
                new TextBlock
                {
                    Text = "Диагностика службы и драйвера. Перенаправление файлов, реестра и запуск в контейнере пока не реализованы.",
                    TextWrapping = TextWrapping.Wrap
                },
                status,
                refresh
            }
        };
    }

    /// <summary>Обновляет сведения о канале управления.</summary>
    /// <param name="sender">Источник команды.</param>
    /// <param name="args">Параметры события кнопки.</param>
    private async void OnRefresh(object? sender, RoutedEventArgs args)
    {
        refresh.IsEnabled = false;
        status.Text = "Проверка связи…";
        try
        {
            DiagnosticResponse response = await client.QueryAsync();
            status.Text = response.Status == DiagnosticStatus.Success
                ? $"Служба и драйвер отвечают. Версия драйвера: {response.DriverVersion}, архитектура: x{response.Architecture}. Доступна только диагностика."
                : $"Служба отвечает. Состояние драйвера: {response.Status}; код Windows: {response.NativeError}.";
        }
        catch (OperationCanceledException)
        {
            status.Text = "Служба не ответила за три секунды. Проверьте её запуск.";
        }
        catch (IOException)
        {
            status.Text = "Не удалось получить корректный ответ службы.";
        }
        catch (UnauthorizedAccessException)
        {
            status.Text = "Нет доступа к диагностическому каналу службы.";
        }
        finally
        {
            refresh.IsEnabled = true;
        }
    }
}
