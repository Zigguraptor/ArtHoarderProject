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

    public FileMetaInfo SaveFileIfNotExists(ReadOnlySpan<byte> readOnlySpan, string workDirectory,
        string? relativeDirectoryName,
        string fileName, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return null!;

        var hash = XxHash64.Hash(readOnlySpan);

        if (cancellationToken.IsCancellationRequested) return null!;

        using var dbContext = new MainDbContext(workDirectory);
        var fileMetaInfo = dbContext.FilesMetaInfos.FirstOrDefault(fileInfo => fileInfo.XxHash == hash);
        if (fileMetaInfo != null)
            return fileMetaInfo;

        relativeDirectoryName ??= Constants.DefaultOtherDirectory;
        var localPath = Path.Combine(workDirectory, Constants.DownloadedMediaDirectory, relativeDirectoryName);

        localPath = SaveFile(localPath, fileName, readOnlySpan);
        fileMetaInfo = CreateFileMetaInfo(localPath, hash);
        dbContext.FilesMetaInfos.Add(fileMetaInfo);
        TrySaveDbChanges(dbContext);
        _perceptualHashing.CalculateHashes(fileMetaInfo.Guid, readOnlySpan);

        return fileMetaInfo;
    }

    private FileMetaInfo CreateFileMetaInfo(string path, byte[] hash)
    {
        return new FileMetaInfo
        {
            Guid = Guid.NewGuid(),
            LocalFilePath = path,
            XxHash = hash,
            FirstSaveTime = Time.NowUtcDataTime()
        };
    }

    private static string SaveFile(string directory, string fileName, ReadOnlySpan<byte> readOnlySpan)
    {
        Directory.CreateDirectory(directory);
        var localPath = GetFreeFilePath(directory, fileName);
        using var fileStream = File.Create(localPath);
        fileStream.Write(readOnlySpan);
        return localPath;
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
