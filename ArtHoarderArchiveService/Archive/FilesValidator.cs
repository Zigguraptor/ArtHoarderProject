using System.IO.Hashing;
using ArtHoarderArchiveService.Archive.DAL;
using ArtHoarderArchiveService.Archive.Managers;

namespace ArtHoarderArchiveService.Archive;

internal static class FilesValidator
{
    internal static FileStateSet GetFileStateSet(string workDirectory)
    {
        var filesHashes = GetFilesMetaDictionary(workDirectory);
        var systemFolder = Path.Combine(workDirectory, Constants.MetaFilesDirectory);
        HashSet<string> unregisteredFiles =
            new(Directory.EnumerateFiles(workDirectory, "*", SearchOption.AllDirectories));
        //Removing system files
        unregisteredFiles.Remove(Path.Combine(workDirectory, Constants.ArchiveMainFilePath));
        unregisteredFiles.RemoveWhere(s => s.StartsWith(systemFolder));

        HashSet<string> missingFiles = new();
        HashSet<string> changedFiles = new();

        var xxHash64 = new XxHash64();
        foreach (var pair in filesHashes)
        {
            if (!unregisteredFiles.Remove(pair.Key))
            {
                missingFiles.Add(pair.Key);
                continue;
            }

            using var stream = File.OpenRead(pair.Key);
            stream.Position = 0;
            xxHash64.Append(stream);

            if (!xxHash64.GetHashAndReset().SequenceEqual(pair.Value))
            {
                changedFiles.Add(pair.Key);
            }
        }

        return new FileStateSet(unregisteredFiles, missingFiles, changedFiles);
    }

    private static Dictionary<string, byte[]> GetFilesMetaDictionary(string workDirectory)
    {
        using var dbContext = new MainDbContext(workDirectory);
        return dbContext.FilesMetaInfos.ToDictionary(metaInfo => metaInfo.LocalFilePath,
            metaInfo => metaInfo.XxHash);
    }
}
