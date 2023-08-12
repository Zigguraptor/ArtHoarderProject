using System.Text.Json;
using ArtHoarderArchiveService.Archive.Networking;
using ArtHoarderArchiveService.Archive.Parsers.Settings;
using ArtHoarderArchiveService.PipeCommunications;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ArtHoarderArchiveService.Archive.Parsers;

internal static class ParserFactory
{
    public static readonly SortedDictionary<string, Type> SupportedTypes = new();
    public static List<ParserSettings> ParsersSettingsList { get; private set; } = null!;
    public static string? UnsupportedTypesReport { get; private set; }
    private static readonly object ConfigsUpdateSyncRoot = new();

    static ParserFactory()
    {
        Directory.CreateDirectory(Constants.ParsersConfigs);
        //Don't forget to update prop "SupportedTypes" when adding types!!!
        SupportedTypes.Add("W", typeof(ParserTypeWSettings));
        ReloadParsesSettings();
    }

    public static void ReloadParsesSettings()
    {
        lock (ConfigsUpdateSyncRoot)
        {
            ParsersSettingsList = new List<ParserSettings>();
            UnsupportedTypesReport = "";
            foreach (var fileName in Directory.GetFiles(Constants.ParsersConfigs))
                RegParserSettings(null, fileName);

            if (UnsupportedTypesReport.Length == 0)
                UnsupportedTypesReport = null;
        }
    }

    public static void ReloadParsesSettings(IMessager messager)
    {
        lock (ConfigsUpdateSyncRoot)
        {
            ParsersSettingsList = new List<ParserSettings>();
            UnsupportedTypesReport = "";
            var files = Directory.GetFiles(Constants.ParsersConfigs);
            var subBar = messager.CreateNewProgressBar("Loading configs", files.Length);
            foreach (var fileName in files)
            {
                subBar.UpdateBar(fileName);
                RegParserSettings(subBar, fileName);
            }

            if (UnsupportedTypesReport.Length == 0)
                UnsupportedTypesReport = null;
        }
    }

    private static void RegParserSettings(IProgressWriter? progressWriter, string path)
    {
        var parserSettings = DeserializeParserSettings(progressWriter, path);
        if (parserSettings != null)
            ParsersSettingsList.Add(parserSettings);
    }

    private static ParserSettings? DeserializeParserSettings(IProgressWriter? progressWriter, string path)
    {
        string jsonContent;
        try
        {
            jsonContent = File.ReadAllText(path);
        }
        catch
        {
            progressWriter?.Write("File reading error. " + path);
            return null;
        }

        try
        {
            var rootElement = JsonDocument.Parse(jsonContent).RootElement;
            var objectType = rootElement.GetProperty("ParserType").GetString();

            ParserSettings? parserSettings = null;
            switch (objectType)
            {
                case "W":
                    parserSettings = JsonSerializer.Deserialize<ParserTypeWSettings>(jsonContent);
                    break;
                default:
                    progressWriter?.Write($"Unsupported type {objectType} config path: {path}");
                    break;
            }

            return parserSettings;
        }
        catch
        {
            progressWriter?.Write("Deserialization error " + path);
            return null;
        }
    }

    public static Parser? Create(IParsHandler parsHandler, IWebDownloader webDownloader, Uri uri)
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
            "W" => new ParserTypeW(parsHandler, webDownloader, (ParserTypeWSettings)settings), //TODO handle cast ex
            _ => throw new Exception(
                $"Not found parser type. Check the spelling of the settings file" +
                $" or update the version of the program. SupportedTypes: {SupportedTypes.Aggregate("", (current, s) => current + s.Key + ", ")}.")
        };
    }

    public static bool IsSupportedLink(Uri uri)
    {
        return ParsersSettingsList.Any(s => s.Host == uri.Host);
    }

    public static void ImportParserConfig(IMessager messager, ParserSettings importedSettings)
    {
        try
        {
            foreach (var fileName in Directory.GetFiles(Constants.ParsersConfigs))
            {
                var parserSettings = DeserializeParserSettings(null, fileName);
                if (parserSettings?.Host != importedSettings.Host) continue;
                var word = "";
                if (importedSettings.Version > parserSettings.Version)
                    word = "(new)";
                else if (importedSettings.Version < parserSettings.Version)
                    word = "(old)";

                messager.WriteLine(
                    $"The config for {parserSettings.Host} already exists.\n  Loaded version {parserSettings.Version}\nImported version {importedSettings.Version} {word}");
                if (!messager.Confirmation("Replace?")) return;
                File.Delete(fileName);
                using var fileStream = File.Create(fileName);
                JsonSerializer.Serialize(fileStream, (object)importedSettings);
                return;
            }

            var newFileName = importedSettings.Host + importedSettings.Version.ToString("O");
            var newPath = Path.Combine(Constants.ParsersConfigs, newFileName + ".parsercfg");
            if (File.Exists(newPath))
            {
                for (var i = 0; i < 100; i++)
                {
                    newPath = Path.Combine(Constants.ParsersConfigs, newFileName + $"({i})" + ".parsercfg");
                    if (File.Exists(newPath)) continue;
                    using var fileStream = File.Create(newPath);
                    JsonSerializer.Serialize(fileStream, (object)importedSettings);
                    return;
                }

                messager.WriteLine($"Name error. Check {Constants.ParsersConfigs}");
            }
            else
            {
                using var fileStream = File.Create(newPath);
                JsonSerializer.Serialize(fileStream, (object)importedSettings);
            }
        }
        catch
        {
            messager.WriteLine("Saving error");
        }

        ReloadParsesSettings();
    }

    public static void ImportParserConfig(IMessager messager, string cfgPath)
    {
        if (!File.Exists(cfgPath))
        {
            messager.WriteLine("File not exists.");
            return;
        }

        var importedSettings = DeserializeParserSettings(null, cfgPath);
        if (importedSettings == null)
        {
            messager.WriteLine("Deserialize error. The imported config is incorrect.");
            return;
        }

        try
        {
            foreach (var fileName in Directory.GetFiles(Constants.ParsersConfigs))
            {
                var parserSettings = DeserializeParserSettings(null, fileName);
                if (parserSettings?.Host != importedSettings.Host) continue;
                var word = "";
                if (importedSettings.Version > parserSettings.Version)
                    word = "(new)";
                else if (importedSettings.Version < parserSettings.Version)
                    word = "(old)";

                messager.WriteLine(
                    $"The config for {parserSettings.Host} already exists.\nLoaded version {parserSettings.Version}\n Imported version {importedSettings.Version} {word}");
                if (messager.Confirmation("Replace?"))
                    File.Copy(cfgPath, fileName, true);
                else
                    return;
            }

            var newFileName = importedSettings.Host + importedSettings.Version.ToString("O");
            var newPath = Path.Combine(Constants.ParsersConfigs, newFileName + ".parsercfg");
            if (File.Exists(newPath))
            {
                for (var i = 0; i < 100; i++)
                {
                    newPath = Path.Combine(Constants.ParsersConfigs, newFileName + $"({i})" + ".parsercfg");
                    if (File.Exists(newPath)) continue;
                    File.Copy(cfgPath, newPath);
                    return;
                }

                messager.WriteLine($"Name error. Check {Constants.ParsersConfigs}");
            }
            else
            {
                File.Copy(cfgPath, newPath);
            }
        }
        catch
        {
            messager.WriteLine("File import error. " + cfgPath);
        }
    }
}
