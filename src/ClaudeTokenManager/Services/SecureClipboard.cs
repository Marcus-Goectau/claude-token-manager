using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ClaudeTokenManager.Services;

// Copies a secret without it landing in clipboard history or cloud clipboard, and clears it after a timeout.
public static class SecureClipboard
{
    public static readonly TimeSpan ClearAfter = TimeSpan.FromSeconds(30);

    private static DispatcherTimer? _timer;

    public static void SetSecret(string secret)
    {
        var data = new DataObject();
        data.SetData(DataFormats.UnicodeText, secret);
        data.SetData("ExcludeClipboardContentFromMonitorProcessing", new MemoryStream(new byte[4]));
        data.SetData("CanIncludeInClipboardHistory", new MemoryStream(new byte[4]));
        data.SetData("CanUploadToCloudClipboard", new MemoryStream(new byte[4]));
        Clipboard.SetDataObject(data, copy: true);

        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = ClearAfter };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            try
            {
                // Leave the clipboard alone if the user has copied something else since.
                if (Clipboard.ContainsText() && Clipboard.GetText() == secret)
                    Clipboard.Clear();
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Another app holds the clipboard open.
            }
        };
        _timer.Start();
    }
}
