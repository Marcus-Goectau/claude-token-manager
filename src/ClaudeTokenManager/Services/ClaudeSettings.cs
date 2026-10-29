using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClaudeTokenManager.Services;

public static class ClaudeSettings
{
    private const string TokenKey = "ANTHROPIC_AUTH_TOKEN";

    public static string ClaudeDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");

    public static string FilePath { get; } = Path.Combine(ClaudeDir, "settings.json");

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonDocumentOptions ReadOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string? ReadToken()
    {
        if (!File.Exists(FilePath)) return null;
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(FilePath), documentOptions: ReadOptions);
            return root?["env"]?[TokenKey]?.GetValue<string>();
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or IOException)
        {
            return null;
        }
    }

    // Only env.ANTHROPIC_AUTH_TOKEN changes; everything else is preserved.
    public static void ApplyToken(string token)
    {
        Directory.CreateDirectory(ClaudeDir);

        JsonObject root;
        if (File.Exists(FilePath))
        {
            var text = File.ReadAllText(FilePath);
            root = string.IsNullOrWhiteSpace(text)
                ? new JsonObject()
                : JsonNode.Parse(text, documentOptions: ReadOptions) as JsonObject
                  ?? throw new InvalidDataException("settings.json is not a JSON object.");
        }
        else
        {
            root = new JsonObject();
        }

        if (root["env"] is not JsonObject env)
        {
            env = new JsonObject();
            root["env"] = env;
        }
        env[TokenKey] = token;

        var tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, root.ToJsonString(WriteOptions));
        if (File.Exists(FilePath))
            File.Replace(tmp, FilePath, FilePath + ".bak");
        else
            File.Move(tmp, FilePath);
    }
}
