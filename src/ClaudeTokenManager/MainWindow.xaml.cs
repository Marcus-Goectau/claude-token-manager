using System.Windows;
using System.Windows.Input;
using ClaudeTokenManager.Models;
using ClaudeTokenManager.Services;

namespace ClaudeTokenManager;

public partial class MainWindow : Window
{
    private readonly App _app;
    private bool _loading;

    private sealed record Row(Profile Profile, bool IsActive)
    {
        public string ActiveMark => IsActive ? "*" : "";
        public string Name => Profile.Name;
        public string Hint => Profile.TokenHint;
        public string Notes => Profile.Notes;
        public string Expiry => Profile.ExpiryText;
        public string ExpiryState => Profile.IsExpired ? "Expired" : Profile.IsExpiringSoon ? "Soon" : "";
    }

    public MainWindow(App app)
    {
        _app = app;
        InitializeComponent();

        _loading = true;
        StartupCheck.IsChecked = StartupRegistration.IsEnabled;
        _loading = false;

        _app.ActiveChanged += OnActiveChanged;
        Closed += (_, _) => _app.ActiveChanged -= OnActiveChanged;
        Refresh();
    }

    private Profile? Selected => (ProfileList.SelectedItem as Row)?.Profile;

    private void OnActiveChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        var selectedId = Selected?.Id;
        var rows = _app.Store.Profiles
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(p => new Row(p, p == _app.ActiveProfile))
            .ToList();
        ProfileList.ItemsSource = rows;
        ProfileList.SelectedItem = rows.FirstOrDefault(r => r.Profile.Id == selectedId);

        ActiveText.Text = _app.ActiveProfile switch
        {
            null => "Active: none of your saved profiles",
            { IsExpired: true } a => $"Active: {a.Name} (expired {a.ExpiresOn:d})",
            { IsExpiringSoon: true } a => $"Active: {a.Name} (expires {a.ExpiryText})",
            var a => $"Active: {a.Name}",
        };

        var current = ClaudeSettings.ReadToken();
        ImportBanner.Visibility = !string.IsNullOrEmpty(current) && _app.ActiveProfile == null
            ? Visibility.Visible
            : Visibility.Collapsed;

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var has = Selected != null;
        EditButton.IsEnabled = has;
        DeleteButton.IsEnabled = has;
        CopyButton.IsEnabled = has;
        ActivateButton.IsEnabled = has && Selected != _app.ActiveProfile;
    }

    private void ProfileList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        UpdateButtons();

    private void ProfileList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Selected != null) EditProfile(Selected);
    }

    private void Add_Click(object sender, RoutedEventArgs e) => AddProfile(null);

    private void Import_Click(object sender, RoutedEventArgs e) => AddProfile(ClaudeSettings.ReadToken());

    private void AddProfile(string? token)
    {
        var editor = new ProfileEditor(null, token) { Owner = this };
        if (editor.ShowDialog() != true) return;
        Run(() => _app.Store.Add(editor.ProfileName, editor.ProfileNotes, editor.ExpiresOn, editor.Token!));
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Selected != null) EditProfile(Selected);
    }

    private void EditProfile(Profile profile)
    {
        var editor = new ProfileEditor(profile, null) { Owner = this };
        if (editor.ShowDialog() != true) return;

        var wasActive = profile == _app.ActiveProfile;
        var tokenChanged = !string.IsNullOrEmpty(editor.Token) && TokenUtil.Hash(editor.Token) != profile.TokenHash;
        Run(() => _app.Store.Update(profile, editor.ProfileName, editor.ProfileNotes, editor.ExpiresOn, editor.Token));

        // A rotated key for the active profile should land in settings.json right away.
        if (wasActive && tokenChanged) _app.Activate(profile);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } profile) return;
        var answer = MessageBox.Show(this,
            $"Delete \"{profile.Name}\"? The token is removed from Credential Manager. settings.json is not changed.",
            "Delete profile", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Yes) Run(() => _app.Store.Remove(profile));
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } profile) return;
        Run(() =>
        {
            var token = CredentialVault.Read(profile.Id);
            if (string.IsNullOrEmpty(token))
                MessageBox.Show(this, "No token is stored for this profile.", Title);
            else
                SecureClipboard.SetSecret(token);
        });
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        if (Selected != null) _app.Activate(Selected);
    }

    private void Startup_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        Run(() => StartupRegistration.SetEnabled(StartupCheck.IsChecked == true));
    }

    private void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Claude Token Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        Refresh();
    }
}
