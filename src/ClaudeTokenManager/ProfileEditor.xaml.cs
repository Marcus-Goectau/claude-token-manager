using System.Windows;
using ClaudeTokenManager.Models;
using ClaudeTokenManager.Services;

namespace ClaudeTokenManager;

public partial class ProfileEditor : Window
{
    private readonly bool _isNew;
    private readonly string? _existingId;

    public string ProfileName => NameBox.Text.Trim();
    public string ProfileNotes => NotesBox.Text.Trim();

    // Null when editing and the token field was left blank.
    public string? Token { get; private set; }

    public ProfileEditor(Profile? profile, string? initialToken)
    {
        InitializeComponent();
        _isNew = profile == null;
        Title = _isNew ? "Add profile" : "Edit profile";

        if (profile != null)
        {
            NameBox.Text = profile.Name;
            NotesBox.Text = profile.Notes;
            TokenLabel.Text = $"API token (current {profile.TokenHint}, leave blank to keep)";
            ShowCheck.Content = "Show current token";
        }
        if (!string.IsNullOrEmpty(initialToken))
            TokenPassword.Password = initialToken;

        _existingId = profile?.Id;
        Loaded += (_, _) => NameBox.Focus();
    }

    private string EnteredToken =>
        (ShowCheck.IsChecked == true ? TokenText.Text : TokenPassword.Password).Trim();

    private void Show_Changed(object sender, RoutedEventArgs e)
    {
        if (ShowCheck.IsChecked == true)
        {
            var entered = TokenPassword.Password;
            if (entered.Length == 0 && _existingId != null)
            {
                try { entered = CredentialVault.Read(_existingId) ?? ""; }
                catch { entered = ""; }
            }
            TokenText.Text = entered;
            TokenText.Visibility = Visibility.Visible;
            TokenPassword.Visibility = Visibility.Collapsed;
        }
        else
        {
            TokenPassword.Password = TokenText.Text;
            TokenPassword.Visibility = Visibility.Visible;
            TokenText.Visibility = Visibility.Collapsed;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileName.Length == 0)
        {
            MessageBox.Show(this, "Enter a name for the profile.", Title);
            return;
        }

        var token = EnteredToken;
        if (_isNew && token.Length == 0)
        {
            MessageBox.Show(this, "Enter the API token.", Title);
            return;
        }

        Token = token.Length == 0 ? null : token;
        DialogResult = true;
    }
}
