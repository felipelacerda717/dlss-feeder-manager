using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using DLSSFeederManager.Models;
using DLSSFeederManager.Services;
using Microsoft.Win32;

namespace DLSSFeederManager;

public partial class MainWindow : Window
{
    private readonly SettingsStore _settingsStore = new();
    private readonly ProfileCatalogService _profiles = new();
    private readonly SourceCatalogService _sources = new();
    private readonly InstallationService _installer;
    private AppSettings _settings = new();

    public MainWindow()
    {
        InitializeComponent();
        _installer = new InstallationService(_profiles, _sources, new FeederDownloader());
        Loaded += Window_Loaded;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = await _settingsStore.LoadAsync();
            LoadSettingsIntoView();
            RefreshProfile();
        }
        catch (Exception exception)
        {
            ShowResult(OperationResult.Fail("Settings could not be loaded.", exception.Message));
        }
    }

    private async void BrowseGame_Click(object sender, RoutedEventArgs e)
    {
        var path = SelectFile("Windows executable|*.exe", GameBox.Text);
        if (path is null)
            return;

        GameBox.Text = path;
        RefreshProfile();
        await SaveViewAsync();
    }

    private async void BrowseRenoDx_Click(object sender, RoutedEventArgs e)
    {
        var path = SelectFile("ReShade add-on|*.addon64", RenoDxBox.Text);
        if (path is null)
            return;
        RenoDxBox.Text = path;
        await SaveViewAsync();
    }

    private async void BrowseDlss_Click(object sender, RoutedEventArgs e)
    {
        var path = SelectFile("nvngx_dlss.dll|nvngx_dlss.dll", DlssBox.Text);
        if (path is null)
            return;
        DlssBox.Text = path;
        await SaveViewAsync();
    }

    private async void BrowseDlssNr_Click(object sender, RoutedEventArgs e)
    {
        var path = SelectFile("nvngx_dlssnr.dll|nvngx_dlssnr.dll", DlssNrBox.Text);
        if (path is null)
            return;
        DlssNrBox.Text = path;
        await SaveViewAsync();
    }

    private async void BrowseImmerse_Click(object sender, RoutedEventArgs e)
    {
        var path = SelectFile(
            "iMMERSE ZIP or LaunchPad|*.zip;MartysMods_LAUNCHPAD.fx|ZIP archive|*.zip|LaunchPad shader|MartysMods_LAUNCHPAD.fx",
            ImmerseBox.Text);
        if (path is null)
            return;
        ImmerseBox.Text = path;
        await SaveViewAsync();
    }

    private void OpenImmerse_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/martymcmodding/iMMERSE");

    private void OpenReShade_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://reshade.me");

    private void OpenRhi_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/RankFTW/RHI/releases");

    private void OpenDlssSwapper_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/beeradmoore/dlss-swapper");

    private void OpenProject_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/felipelacerda717/dlss-feeder-manager");

    private async void Check_Click(object sender, RoutedEventArgs e)
    {
        await SaveViewAsync();
        ShowResult(_installer.Check(_settings));
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        await SaveViewAsync();
        SetBusy(true, "Installing");
        try
        {
            ShowResult(await _installer.InstallAsync(_settings));
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Validate_Click(object sender, RoutedEventArgs e)
    {
        await SaveViewAsync();
        SetBusy(true, "Validating");
        try
        {
            ShowResult(await _installer.ValidateAsync(_settings.GameExecutable));
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        await SaveViewAsync();
        if (MessageBox.Show(
                this,
                "Remove the managed setup and restore the backup?",
                "DLSS Feeder Manager",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        SetBusy(true, "Removing");
        try
        {
            ShowResult(await _installer.RemoveAsync(_settings.GameExecutable));
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RefreshProfile()
    {
        if (!File.Exists(GameBox.Text))
        {
            ProfileText.Text = "No game selected";
            return;
        }

        var profile = _profiles.FindByExecutable(GameBox.Text);
        var release = _sources.GetFeederRelease(profile);
        ProfileText.Text = profile is null
            ? $"Generic experimental mode · x64 D3D11/D3D12 only · DLSS5-Feeder v{release.Version}"
            : $"{profile.Name} · {profile.Status} · x64 D3D11/D3D12 · DLSS5-Feeder v{release.Version}";
    }

    private void LoadSettingsIntoView()
    {
        GameBox.Text = _settings.GameExecutable;
        RenoDxBox.Text = _settings.RenoDxAddon;
        DlssBox.Text = _settings.DlssRuntime;
        DlssNrBox.Text = _settings.DlssNrRuntime;
        ImmerseBox.Text = _settings.ImmerseSource;
    }

    private async Task SaveViewAsync()
    {
        _settings.GameExecutable = GameBox.Text.Trim();
        _settings.RenoDxAddon = RenoDxBox.Text.Trim();
        _settings.DlssRuntime = DlssBox.Text.Trim();
        _settings.DlssNrRuntime = DlssNrBox.Text.Trim();
        _settings.ImmerseSource = ImmerseBox.Text.Trim();
        await _settingsStore.SaveAsync(_settings);
    }

    private void ShowResult(OperationResult result)
    {
        StatusTitle.Text = result.Message;
        StatusTitle.Foreground = result.Success ? Brushes.ForestGreen : Brushes.Firebrick;
        StatusBox.Text = string.Join(Environment.NewLine, result.Details.Select(detail => $"• {detail}"));
    }

    private void SetBusy(bool busy, string? title = null)
    {
        ActionPanel.IsEnabled = !busy;
        if (busy && title is not null)
        {
            StatusTitle.Text = title + "...";
            StatusTitle.Foreground = Brushes.DimGray;
            StatusBox.Text = "Please wait.";
        }
    }

    private static string? SelectFile(string filter, string currentPath)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true
        };

        if (File.Exists(currentPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
            dialog.FileName = Path.GetFileName(currentPath);
        }

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
