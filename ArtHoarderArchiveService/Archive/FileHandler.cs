using System.IO.Hashing;
using ArtHoarderArchiveService.Archive.DAL;
using ArtHoarderArchiveService.Archive.DAL.Entities;
using ArtHoarderArchiveService.Archive.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderArchiveService.Archive;

internal class FileHandler : IFileHandler
{
    private const int FileNameLimit = 1000;
    private readonly PerceptualHashing _perceptualHashing;

    public FileHandler(string workDirectory)
    {
        _perceptualHashing = new PerceptualHashing(workDirectory);
    }

    public FileMetaInfo SaveFileIfNotExists(Stream fileStream, string workDirectory, string? localDirectoryName,
        string fileName, CancellationToken cancellationToken)
    {
        localDirectoryName ??= Constants.DefaultOtherDirectory;

        var xxHash64 = new XxHash64();

        if (cancellationToken.IsCancellationRequested)
            return null!;

        using var dbContext = new MainDbContext(workDirectory);

        fileStream.Position = 0;
        xxHash64.Append(fileStream);

        var fileMetaInfo =
            dbContext.FilesMetaInfos.FirstOrDefault(fileInfo => fileInfo.XxHash == xxHash64.GetCurrentHash());
        if (fileMetaInfo != null)
            return fileMetaInfo;

        var localPath = fileName;
        localPath = Path.Combine(workDirectory, Constants.DownloadedMediaDirectory, localDirectoryName, localPath);

        fileStream.Position = 0;
        localPath = SaveFile(fileStream, localPath);

        var guid = Guid.NewGuid();
        fileMetaInfo = new FileMetaInfo
        {
            Guid = guid,
            LocalFilePath = localPath,
            XxHash = xxHash64.GetCurrentHash(),
            FirstSaveTime = Time.NowUtcDataTime()
        };
        dbContext.FilesMetaInfos.Add(fileMetaInfo);
        TrySaveDbChanges(dbContext);

        fileStream.Position = 0;
        _perceptualHashing.CalculateHashes(guid, fileStream);

        return fileMetaInfo;
    }

    private string SaveFile(Stream sourceStream, string path)
    {
        path = GetFreeFileName(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

        using var localFileStream = File.Create(path);
        sourceStream.CopyTo(localFileStream);
        return path;
    }

    private static string GetFreeFileName(string startPath)
    {
        var newPath = startPath;
        for (var i = 1; File.Exists(newPath) && i < FileNameLimit; i++)
        {
            newPath = Path.Combine(Path.GetDirectoryName(startPath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(startPath) + $"({i:D})" + Path.GetExtension(startPath));
        }

        if (!File.Exists(newPath))
            return newPath;

        throw new Exception("File naming limit: " + startPath); //TODO handle this
    }

    private static bool TrySaveDbChanges(DbContext dbContext)
    {
        try
        {
            dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }
}
