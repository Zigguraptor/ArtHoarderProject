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

        localDirectoryName ??= Constants.DefaultOtherDirectory;
        var localPath = Path.Combine(workDirectory, Constants.DownloadedMediaDirectory, localDirectoryName);

        Directory.CreateDirectory(localPath);
        localPath = GetFreeFilePath(localPath, fileName);

        fileStream.Position = 0;
        using var localFileStream = File.Create(localPath);
        fileStream.CopyTo(localFileStream);

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

    private static string GetFreeFilePath(string directory, string fileName)
    {
        var newPath = Path.Combine(directory, fileName);

        if (!File.Exists(newPath)) return newPath;

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        for (var i = 1; File.Exists(newPath) && i < FileNameLimit; i++)
            newPath = Path.Combine(directory, fileNameWithoutExtension + '(' + i + ')' + extension);

        if (!File.Exists(newPath))
            return newPath;

        newPath = Path.Combine(directory, Guid.NewGuid() + extension);
        return newPath;
        // throw new Exception("File naming limit: " + fileName);
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
