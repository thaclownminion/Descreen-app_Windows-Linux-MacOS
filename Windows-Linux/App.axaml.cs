using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Descreen;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var timerManager = new TimerManager();
            var mainVm = new MainViewModel(timerManager);

            desktop.MainWindow = new MainWindow { DataContext = mainVm };

            // Wire break overlay
            timerManager.OnBreakStart = () =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var overlay = new BreakOverlayWindow(timerManager);
                    overlay.Show();
                });

            timerManager.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
