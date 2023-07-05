using System.IO.Hashing;
using System.Net.Http.Headers;
using ArtHoarderCore.DAL;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Infrastructure;
using ArtHoarderCore.Networking;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore.Managers;

public class FilesManager : IFilesManager
{
    private const int FileNameLimit = 4;
    private readonly string _archiveFilePath;
    private readonly Logger _logger;
    public string WorkDirectory { get; }

    public FilesManager(string workDirectory, string archiveFilePath, Logger logger)
    {
        WorkDirectory = workDirectory;
        _logger = logger;
        _archiveFilePath = archiveFilePath;
    }

    public List<IImageHasher> ImageHashMakers { get; init; } = new();

    private HashSet<string>? _unregisteredFiles;
    private HashSet<string>? _missingFiles;
    private HashSet<string>? _changedFiles;

    public HashSet<string> UnregisteredFiles =>
        _unregisteredFiles ?? new HashSet<string>(0);

    public HashSet<string> MissingFiles =>
        _missingFiles ?? new HashSet<string>(0);

    public HashSet<string> ChangedFiles =>
        _changedFiles ?? new HashSet<string>(0);

    public Task ValidateFilesAsync(Dictionary<string, byte[]> filesHashes, ProgressReporter reporter)
    {
        var systemFolder = Path.Combine(WorkDirectory, Constants.MetaFilesDirectory);
        reporter.Report("Read local files");
        _unregisteredFiles =
            new HashSet<string>(Directory.EnumerateFiles(WorkDirectory, "*", SearchOption.AllDirectories));
        _unregisteredFiles.Remove(_archiveFilePath);
        _unregisteredFiles.RemoveWhere(s => s.StartsWith(systemFolder));
        _missingFiles = new HashSet<string>();
        _changedFiles = new HashSet<string>();

        reporter.Report($"{_unregisteredFiles.Count} files founded");

        var xxHash64 = new XxHash64();
        foreach (var pair in filesHashes)
        {
            if (!_unregisteredFiles.Remove(pair.Key))
            {
                _missingFiles.Add(pair.Key);
                reporter.ReportAndProgress($"Missing {pair.Key}");
                continue;
            }

            using var stream = File.OpenRead(pair.Key);
            stream.Position = 0;
            xxHash64.Append(stream);

            if (!xxHash64.GetHashAndReset().SequenceEqual(pair.Value))
            {
                _changedFiles.Add(pair.Key);
                reporter.ReportAndProgress($"Changed {pair.Key}");
            }

            reporter.ReportAndProgress($"OK {pair.Key}");
        }

        return Task.CompletedTask;
    }

    public async Task<List<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)>> CheckOrSaveFilesAsync(
        string? localDirectoryName, List<Uri> uris)
    {
        localDirectoryName ??= Constants.DefaultOtherFolderName;

        var result = new List<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)>();
        foreach (var uri in uris) // Parallel foreach?
            result.Add(await CheckOrSaveFileAsync(localDirectoryName, uri));

        return result;
    }

    public async Task<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)> CheckOrSaveFileAsync(
        string? localDirectoryName,
        Uri uri)
    {
        localDirectoryName ??= Constants.DefaultOtherFolderName;

        var xxHash64 = new XxHash64();

        var responseMessage = await WebDownloader.GetAsync(uri);
        await using var dbContext = new MainDbContext(WorkDirectory);
        await using var stream = await responseMessage.Content.ReadAsStreamAsync();

        stream.Position = 0;
        await xxHash64.AppendAsync(stream).ConfigureAwait(false);

        var fileMetaInfo =
            dbContext.FilesMetaInfos.FirstOrDefault(fileInfo => fileInfo.XxHash == xxHash64.GetCurrentHash());
        if (fileMetaInfo != null)
            return (fileMetaInfo, uri, responseMessage.Headers);

        var localPath = uri.AbsoluteUri.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        localPath = Path.Combine(WorkDirectory, Constants.DownloadedMediaDirectory, localDirectoryName, localPath);

        stream.Position = 0;
        localPath = await SaveFileAsync(stream, localPath).ConfigureAwait(false);

        var guid = Guid.NewGuid();
        fileMetaInfo = new FileMetaInfo
        {
            Guid = guid,
            LocalFilePath = localPath,
            XxHash = xxHash64.GetCurrentHash(),
            FirstSaveTime = Time.GetCurrentDateTime()
        };
        dbContext.FilesMetaInfos.Add(fileMetaInfo);
        TrySaveChanges(dbContext);

        stream.Position = 0;
        using var image = await Image.LoadAsync<Rgb24>(stream);
        var lowImage = CompressImage(image);

        foreach (var hasher in ImageHashMakers)
            SavePHash(hasher.HashName, guid, hasher.ComputeHash(lowImage));

        return (fileMetaInfo, uri, responseMessage.Headers);
    }

    private double[,] CompressImage(Image<Rgb24> image)
    {
        image.Mutate(x => x.Resize(32, 32));

        var grayImageMatrix = new double[32, 32];
        for (var i = 0; i < 32; i++)
        {
            for (var j = 0; j < 32; j++)
            {
                var pixel = image[i, j];
                grayImageMatrix[i, j] = ((double)pixel.R + pixel.G + pixel.B) / 3d;
            }
        }

        return grayImageMatrix;
    }

    private async Task<string> SaveFileAsync(Stream sourceStream, string path)
    {
        path = GetFreeFileName(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

        await using var localFileStream = File.Create(path);
        await sourceStream.CopyToAsync(localFileStream).ConfigureAwait(false);
        return path;
    }

    private string GetFreeFileName(string startPath)
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

    private void SavePHash(string hashName, Guid fileGuid, byte[] hash)
    {
        using var context = new PHashDbContext(WorkDirectory, hashName);
        if (context.PHashInfos.Find(fileGuid) == null)
            context.PHashInfos.Add(new PHashInfo(fileGuid, hash));
        TrySaveChanges(context);
    }

    private void TrySaveChanges(DbContext dbContext)
    {
        try
        {
            dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            _logger.ErrorLog("[Error in FilesManager] " + e);
        }
    }
}