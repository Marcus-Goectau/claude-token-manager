using System.IO;
using System.Text.Json;
using ClaudeTokenManager.Models;

namespace ClaudeTokenManager.Services;

// Profile metadata only. Tokens are never written here.
public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClaudeTokenManager");

    private static string FilePath => Path.Combine(DataDir, "profiles.json");

    public List<Profile> Profiles { get; private set; } = new();

    public event EventHandler? Changed;

    public void Load()
    {
        if (!File.Exists(FilePath))
        {
            Profiles = new();
            return;
        }
        Profiles = JsonSerializer.Deserialize<List<Profile>>(File.ReadAllText(FilePath)) ?? new();
    }

    public void Save()
    {
        Directory.CreateDirectory(DataDir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(Profiles, JsonOptions));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Profile Add(string name, string notes, string token)
    {
        var profile = new Profile { Name = name, Notes = notes };
        SetToken(profile, token);
        Profiles.Add(profile);
        Save();
        return profile;
    }

    public void Update(Profile profile, string name, string notes, string? newToken)
    {
        profile.Name = name;
        profile.Notes = notes;
        if (!string.IsNullOrEmpty(newToken))
            SetToken(profile, newToken);
        else
            // Keep the credential's display name in sync with the profile name.
            CredentialVault.Save(profile.Id, name, CredentialVault.Read(profile.Id) ?? "");
        Save();
    }

    public void Remove(Profile profile)
    {
        CredentialVault.Delete(profile.Id);
        Profiles.Remove(profile);
        Save();
    }

    public Profile? FindByToken(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        var hash = TokenUtil.Hash(token);
        return Profiles.FirstOrDefault(p => p.TokenHash == hash);
    }

    private static void SetToken(Profile profile, string token)
    {
        CredentialVault.Save(profile.Id, profile.Name, token);
        profile.TokenHash = TokenUtil.Hash(token);
        profile.TokenHint = TokenUtil.Hint(token);
    }
}
