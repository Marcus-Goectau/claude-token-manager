using System.ComponentModel;
using System.Diagnostics;

namespace ClaudeTokenManager.Services;

public static class ClaudeSessions
{
    // Counts Claude Code CLI processes. The Claude desktop app also runs as claude.exe, from WindowsApps, so it's skipped.
    public static int CountRunning()
    {
        var count = 0;
        foreach (var process in Process.GetProcessesByName("claude"))
        {
            using (process)
            {
                try
                {
                    var path = process.MainModule?.FileName;
                    if (path != null && !path.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase))
                        count++;
                }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
                {
                    // Exited, or owned by another user.
                }
            }
        }
        return count;
    }
}
