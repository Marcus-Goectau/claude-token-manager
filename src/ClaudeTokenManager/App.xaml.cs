using System.IO;
using System.Windows;
using ClaudeTokenManager.Models;
using ClaudeTokenManager.Services;

namespace ClaudeTokenManager;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private EventWaitHandle? _showSignal;
    private TrayIcon? _tray;
    private FileSystemWatcher? _watcher;
    private MainWindow? _window;

    public ProfileStore Store { get; } = new();

    public Profile? ActiveProfile { get; private set; }

    public event EventHandler? ActiveChanged;

    public static new App Current => (App)Application.Current;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(true, @"Local\ClaudeTokenManager", out var isFirst);
        if (!isFirst)
        {
            // Ask the running instance to open its window, then exit.
            if (EventWaitHandle.TryOpenExisting(@"Local\ClaudeTokenManager.Show", out var signal))
                signal.Set();
            Shutdown();
            return;
        }

        _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\ClaudeTokenManager.Show");
        ThreadPool.RegisterWaitForSingleObject(_showSignal,
            (_, _) => Dispatcher.BeginInvoke(ShowManager), null, Timeout.Infinite, executeOnlyOnce: false);

        try
        {
            Store.Load();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not read profiles.json: {ex.Message}", "Claude Token Manager",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        Store.Changed += (_, _) => RefreshActive();

        _tray = new TrayIcon(this);
        WatchSettings();
        RefreshActive();

        if (Store.Profiles.Count == 0 || e.Args.Contains("--show"))
            ShowManager();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _watcher?.Dispose();
        _tray?.Dispose();
        _showSignal?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    public void ShowManager()
    {
        if (_window == null)
        {
            _window = new MainWindow(this);
            _window.Closed += (_, _) => _window = null;
            _window.Show();
        }
        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    public bool Activate(Profile profile)
    {
        try
        {
            var token = CredentialVault.Read(profile.Id);
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show($"No token is stored for \"{profile.Name}\". Edit the profile and enter it again.",
                    "Claude Token Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            ClaudeSettings.ApplyToken(token);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Switching to \"{profile.Name}\" failed: {ex.Message}", "Claude Token Manager",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        RefreshActive();
        _tray?.Notify($"Switched to {profile.Name}. Restart Claude Code sessions to pick it up.");
        return true;
    }

    public void RefreshActive()
    {
        ActiveProfile = Store.FindByToken(ClaudeSettings.ReadToken());
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    private void WatchSettings()
    {
        Directory.CreateDirectory(ClaudeSettings.ClaudeDir);
        _watcher = new FileSystemWatcher(ClaudeSettings.ClaudeDir, "settings.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        FileSystemEventHandler handler = (_, _) => Dispatcher.BeginInvoke(RefreshActive);
        _watcher.Changed += handler;
        _watcher.Created += handler;
        _watcher.Renamed += (_, _) => Dispatcher.BeginInvoke(RefreshActive);
        _watcher.EnableRaisingEvents = true;
    }
}
