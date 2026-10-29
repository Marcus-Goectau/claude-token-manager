using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ClaudeTokenManager.Services;

namespace ClaudeTokenManager;

public sealed class TrayIcon : IDisposable
{
    private readonly App _app;
    private readonly NotifyIcon _icon;
    private readonly Icon _image;

    public TrayIcon(App app)
    {
        _app = app;
        _image = CreateIcon();
        _icon = new NotifyIcon
        {
            Icon = _image,
            Text = "Claude Token Manager",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip(),
        };
        _icon.ContextMenuStrip.Opening += (_, _) => BuildMenu();
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) _app.ShowManager();
        };

        _app.ActiveChanged += (_, _) => UpdateTooltip();
        BuildMenu();
    }

    public void Notify(string message) =>
        _icon.ShowBalloonTip(3000, "Claude Token Manager", message, ToolTipIcon.Info);

    private void BuildMenu()
    {
        var menu = _icon.ContextMenuStrip!;
        menu.Items.Clear();

        if (_app.Store.Profiles.Count == 0)
            menu.Items.Add(new ToolStripMenuItem("No profiles yet") { Enabled = false });

        foreach (var profile in _app.Store.Profiles.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            var label = profile switch
            {
                { IsExpired: true } => $"{profile.Name} (expired)",
                { IsExpiringSoon: true } => $"{profile.Name} (expires {profile.ExpiryText})",
                _ => profile.Name,
            };
            // A single & would be read as a keyboard shortcut marker.
            var item = new ToolStripMenuItem(label.Replace("&", "&&"))
            {
                Checked = profile == _app.ActiveProfile,
                ToolTipText = profile.Notes,
            };
            item.Click += (_, _) => _app.Activate(profile);
            menu.Items.Add(item);
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Manage tokens...", null, (_, _) => _app.ShowManager());
        menu.Items.Add("Open settings.json", null, (_, _) => OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => _app.Shutdown());
    }

    private void UpdateTooltip()
    {
        var text = _app.ActiveProfile is { } p ? $"Claude Token Manager - {p.Name}" : "Claude Token Manager";
        // NotifyIcon.Text is limited to 127 characters.
        _icon.Text = text.Length > 127 ? text[..127] : text;
    }

    private static void OpenSettings()
    {
        if (File.Exists(ClaudeSettings.FilePath))
            Process.Start(new ProcessStartInfo(ClaudeSettings.FilePath) { UseShellExecute = true });
    }

    // Pick the frame that matches the tray size at the current DPI.
    private static Icon CreateIcon()
    {
        var info = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
        using var stream = info!.Stream;
        return new Icon(stream, SystemInformation.SmallIconSize);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _image.Dispose();
    }
}
