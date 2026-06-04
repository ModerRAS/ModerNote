using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Modernote.Client;
using Modernote.Service;

namespace Modernote.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var service = new ModernoteService();
            var client = new ModernoteClient(new EmbeddedTransport(service));
            var runtime = new DesktopRuntime(client);
            var mainWindow = new MainWindow();
            mainWindow.Initialize(client, runtime);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
