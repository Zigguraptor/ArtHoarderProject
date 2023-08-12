using System.Text.Json;
using ArtHoarderArchiveService.Archive.DAL;
using ArtHoarderArchiveService.Archive.Infrastructure.Enums;
using ArtHoarderArchiveService.Archive.Serializable;

namespace ArtHoarderArchiveService.Archive;

internal static class ArchiveInitialization
{
    public static CreationCode CreateArchive(string workDirectory)
    {
        if (File.Exists(Path.Combine(workDirectory, Constants.ArchiveMainFilePath)))
            return CreationCode.AlreadyExists;

        CreateSystemFolders(workDirectory);
        //Храним mainFileStream ради болкиовки, до конца инициализации.
        using var mainFileStream = InitArchiveMainFile(workDirectory);
        InitMainDb(workDirectory);
        return CreationCode.Ok;
    }

    private static void CreateSystemFolders(string workDirectory)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(workDirectory, Constants.MainDbPath)) ??
                                  workDirectory);
        Directory.CreateDirectory(
            Path.GetDirectoryName(Path.Combine(workDirectory, Constants.ChangesAuditDbPath)) ?? workDirectory);

        Directory.CreateDirectory(Path.Combine(workDirectory, Constants.DownloadedMediaDirectory));
        Directory.CreateDirectory(Path.Combine(workDirectory, Constants.PHashDbDirectory));
    }

    private static FileStream InitArchiveMainFile(string workDirectory)
    {
        var mainFileStream = File.Open(Path.Combine(workDirectory, Constants.ArchiveMainFilePath), FileMode.Create,
            FileAccess.ReadWrite, FileShare.None);
        var mainFile = new ArchiveMainFile
        {
            ArchiveRootName = workDirectory,
            LastAccess = Time.NowUtcDataTime()
        };
        JsonSerializer.Serialize(mainFileStream, mainFile);
        return mainFileStream;
    }

    private static void InitMainDb(string workDirectory)
    {
        using var
            context = new MainDbContext(
                workDirectory); //Там вложенный контекст для аудита. По тому директория, а не путь
    }
}
