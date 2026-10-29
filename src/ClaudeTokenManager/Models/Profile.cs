namespace ClaudeTokenManager.Models;

// The token itself lives in Credential Manager under the profile Id.
public sealed class Profile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Notes { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public string TokenHint { get; set; } = "";
}
