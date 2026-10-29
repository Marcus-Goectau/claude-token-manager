using System.Text.Json.Serialization;

namespace ClaudeTokenManager.Models;

// The token itself lives in Credential Manager under the profile Id.
public sealed class Profile
{
    public const int ExpiringSoonDays = 7;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Notes { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public string TokenHint { get; set; } = "";

    // Date only; the token is treated as valid through the end of this day.
    public DateOnly? ExpiresOn { get; set; }

    [JsonIgnore]
    public int? DaysUntilExpiry =>
        ExpiresOn is { } d ? d.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber : null;

    [JsonIgnore]
    public bool IsExpired => DaysUntilExpiry < 0;

    [JsonIgnore]
    public bool IsExpiringSoon => DaysUntilExpiry is >= 0 and <= ExpiringSoonDays;

    [JsonIgnore]
    public string ExpiryText => DaysUntilExpiry switch
    {
        null => "",
        < 0 => $"{ExpiresOn:d} (expired)",
        0 => $"{ExpiresOn:d} (today)",
        1 => $"{ExpiresOn:d} (tomorrow)",
        <= ExpiringSoonDays and var n => $"{ExpiresOn:d} (in {n} days)",
        _ => $"{ExpiresOn:d}",
    };
}
