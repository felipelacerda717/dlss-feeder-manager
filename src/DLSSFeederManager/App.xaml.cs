using System.Windows;
using DLSSFeederManager.Services;

namespace DLSSFeederManager;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length > 0 && e.Args[0] == "--apply-update")
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            await UpdateBootstrap.ApplyAsync(e.Args.Skip(1).ToArray());
            Shutdown();
            return;
        }

        var completion = UpdateBootstrap.ParseCompletion(e.Args);
        var window = new MainWindow();
        MainWindow = window;
        window.Show();

        if (completion is not null)
            await UpdateBootstrap.CompleteAsync(completion);
    }
}
