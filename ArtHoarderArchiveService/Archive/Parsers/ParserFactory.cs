using System.Text;
using ArtHoarderArchiveService.Archive.Managers;
using ArtHoarderArchiveService.Archive.Parsers.Settings;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ArtHoarderArchiveService.Archive.Parsers;

internal static class ParserFactory
{
    //Don't forget to update prop "SupportedTypes" when adding types!!!
    public static readonly string[] SupportedTypes = { "W" };
    public static List<ParserSettings> ParsersSettingsList { get; private set; } = null!;
    public static HashSet<string>? UnsupportedTypes { get; private set; }

    static ParserFactory()
    {
        ReloadParsesSettings();
    }

    public static void ReloadParsesSettings()
    {
        ParsersSettingsList = new List<ParserSettings>();
        UnsupportedTypes = new HashSet<string>();
        foreach (var fileName in Directory.GetFiles(Constants.ParsersConfigs))
        {
            try
            {
                var settings = JsonSerializer.Deserialize<ParserSettings>(File.ReadAllText(fileName));
                if (settings?.Settings == null) continue;

                if (SupportedTypes.Contains(settings.ParserType))
                {
                    ParsersSettingsList.Add(settings);
                }
                else
                {
                    UnsupportedTypes.Add(settings.ParserType);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        if (UnsupportedTypes.Count == 0)
            UnsupportedTypes = null;

        Console.WriteLine($"{ParsersSettingsList.Count} parsers settings loaded. For hosts: ");

        var sb = new StringBuilder();
        foreach (var settings in ParsersSettingsList)
            sb.Append(settings.Host + '\n');

        Console.WriteLine(sb.ToString());
        Console.WriteLine("___");
    }

    public static Parser? Create(IParsHandler parsHandler, Uri uri)
    {
        var settings = ParsersSettingsList.Find(x => x.Host == uri.Host);
        if (settings == null)
        {
            return null;
            // throw new Exception(
            //     $"Not found parser settings for \"{uri}\". Put the configuration file in a folder {Constants.ParsersConfigs}");
        }

        return settings.ParserType switch
        {
            //Don't forget to update prop "SupportedTypes" when adding types!!!
            "W" => new ParserTypeW(parsHandler, new ParserTypeWSettings(uri.Host, settings.Settings)),
            _ => throw new Exception(
                $"Not found parser type. Check the spelling of the settings file" +
                $" or update the version of the program. SupportedTypes: {SupportedTypes.Aggregate("", (current, s) => current + s + ", ")}.")
        };
    }

    public static bool IsSupportedLink(Uri uri)
    {
        return ParsersSettingsList.Any(s => s.Host == uri.Host);
    }
}
