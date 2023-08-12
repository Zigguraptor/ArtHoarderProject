using System.Text.Json;

namespace ArtHoarderArchiveService;

public class AppSettings
{
    public int DefaultConnectionLimiter { get; set; } = 2000;
    public Dictionary<string, int>? ConnectionLimiters { get; set; }

    public static void CreateDefault(string path)
    {
        using var fileStream = File.Create(path);
        var options = new JsonSerializerOptions { WriteIndented = true };
        JsonSerializer.Serialize(fileStream, new AppSettings(), options);
    }
}
