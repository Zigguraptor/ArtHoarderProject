using System.Text.Json;
using ArtHoarderArchiveService.Archive.Serializable;

namespace ArtHoarderArchiveService.Archive.Managers;

internal static class Constants
{
    private const string ConstantsDirectory = @".\Constants";
    private const string ConstantsFilePath = @".\Constants\Consts.txt";

    public const string UnknownFileIconPath =
        @"C:\Users\Lunar\My CSharp Apps\ArtHoarderProject\ArtHoarderCore\Resources\unknown-file.png"; //TODO

    private static ConstantsJson _constants;

    static Constants()
    {
        _constants = ReloadConstants(ConstantsFilePath);
    }

    public static ConstantsJson ReloadConstants(string sourcePath)
    {
        using var stream = File.Open(sourcePath, FileMode.Open);
        return JsonSerializer.Deserialize<ConstantsJson>(stream) ??
               throw new Exception("Missing constants resource file");
    }

    public static string ArchiveMainFilePath => Path.Combine(MetaFilesDirectory, _constants.ArchiveMainFileName);
    public static string MetaFilesDirectory => _constants.MetaFilesDirectory;
    public static string DownloadedMediaDirectory => _constants.DownloadedMediaDirectory;
    public static string DefaultOtherDirectory => _constants.DefaultOtherDirectory;
    public static string LogsDirectory => Path.Combine(MetaFilesDirectory, _constants.LogDirectory);

    public static string ParsersConfigs =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create), "ArtHoarderArchive", "Parsers");

    public static string MainDbPath => Path.Combine(MetaFilesDirectory, _constants.MainDbName);
    public static string ChangesAuditDbPath => Path.Combine(MetaFilesDirectory, _constants.ChangesAuditDbName);
    public static string PHashDbDirectory => Path.Combine(MetaFilesDirectory, _constants.PHashDbsDirectory);

    public static string PerceptualHashingLibs => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _constants.ApplicationName,
        _constants.PerceptualHashingLibs);

    public static string Temp => Path.Combine(MetaFilesDirectory, "Temp");
}
