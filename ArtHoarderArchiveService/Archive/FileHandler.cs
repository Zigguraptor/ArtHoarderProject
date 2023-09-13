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

    public List<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)> CheckOrSaveFiles(
        string workDirectory, string? localDirectoryName, List<Uri> uris, CancellationToken cancellationToken)
    {
        localDirectoryName ??= Constants.DefaultOtherDirectory;

        var result = new List<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)>();
        foreach (var uri in uris) // Parallel foreach?
            result.Add(CheckOrSaveFile(workDirectory, localDirectoryName, uri, cancellationToken));

        return result;
    }

    public (FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders) CheckOrSaveFile(
        string workDirectory, string? localDirectoryName, Uri uri, CancellationToken cancellationToken)
    {
        localDirectoryName ??= Constants.DefaultOtherDirectory;

        var xxHash64 = new XxHash64();

        var responseMessage = _webDownloader.Get(uri, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return new ValueTuple<FileMetaInfo, Uri, HttpHeaders>(null!, null!, null!);

        using var dbContext = new MainDbContext(workDirectory);
        using var stream = responseMessage.Content.ReadAsStream();

        stream.Position = 0;
        xxHash64.Append(stream);

        var fileMetaInfo =
            dbContext.FilesMetaInfos.FirstOrDefault(fileInfo => fileInfo.XxHash == xxHash64.GetCurrentHash());
        if (fileMetaInfo != null)
            return (fileMetaInfo, uri, responseMessage.Headers);

        var localPath = uri.AbsoluteUri.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        localPath = Path.Combine(workDirectory, Constants.DownloadedMediaDirectory, localDirectoryName, localPath);

        stream.Position = 0;
        localPath = SaveFile(stream, localPath);

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

        stream.Position = 0;
        _perceptualHashing.CalculateHashes(guid, stream);

        return (fileMetaInfo, uri, responseMessage.Headers);
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
