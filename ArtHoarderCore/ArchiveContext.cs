using System.IO.Hashing;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text.Json;
using ArtHoarderCore.DAL;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Infrastructure.Enums;
using ArtHoarderCore.Managers;
using ArtHoarderCore.Networking;
using ArtHoarderCore.Parsers;
using ArtHoarderCore.Serializable;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore;

public class ArchiveContext : IDisposable
{
    private const int FileNameLimit = 100;
    private readonly FileStream _mainFile;
    private readonly object _filesAccessSyncObj = new();
    public readonly string WorkDirectory;
    private ArchiveMainFile _cachedArchiveMainFile;
    private readonly UniversalParser _universalParser;
    private readonly PerceptualHashing _perceptualHashing;

    private string MainFilePath => Path.Combine(WorkDirectory, Constants.ArchiveMainFilePath);

    public ArchiveContext(string workDirectory)
    {
        WorkDirectory = workDirectory;
        _mainFile = File.Open(MainFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        _cachedArchiveMainFile = ReadArchiveFile();
        _universalParser = new UniversalParser(new ParsingHandler(this, new Logger(WorkDirectory, "ParsingHandler")));
        _perceptualHashing = new PerceptualHashing(WorkDirectory);
    }

    #region publicMethods

    public static CreationCode CreateArchive(string workDirectory) =>
        ArchiveInitialization.CreateArchive(workDirectory);

    public FileStateSet GetFileStateSet()
    {
        lock (_filesAccessSyncObj)
        {
            return FilesValidator.GetFileStateSet(WorkDirectory);
        }
    }

    #region Update

    public Task UpdateAllGalleriesAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<bool> UpdateUserAsync(string userName, CancellationToken cancellationToken)
    {
        using var context = new MainDbContext(WorkDirectory);
        var galleryProfiles = context.GalleryProfiles.Where(g => g.OwnerName == userName).ToList();
        if (galleryProfiles.Count == 0) return false;

        var tasks = galleryProfiles.Select(profile =>
            UpdateGalleryAsync(profile.Uri, cancellationToken));
        await Task.WhenAll(tasks);

        return true;
    }

    public async Task UpdateGalleriesAsync(ICollection<ProfileInfo> galleries, CancellationToken cancellationToken)
    {
        // reporter.SetProgressStage("Galleries Updating");
        // reporter.SetProgressBar(0, galleries.Length);

        foreach (var gallery in galleries)
        {
            // reporter.Progress();
            await UpdateGalleryAsync(gallery.Uri, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<bool> UpdateGalleryAsync(Uri galleryUri, CancellationToken cancellationToken,
        string? directoryName = null)
    {
        using var context = new MainDbContext(WorkDirectory);
        var galleryProfile = context.GalleryProfiles.FirstOrDefault(g => g.Uri == galleryUri);
        if (galleryProfile is null)
            return false;

        directoryName ??= galleryProfile.OwnerName;
        return await _universalParser.UpdateGallery(galleryUri, directoryName, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region MainDbManipulations

    //Add area
    public bool TryAddNewUser(string name)
    {
        using var context = new MainDbContext(WorkDirectory);
        var time = Time.GetCurrentDateTime();
        context.Users.Add(new User
        {
            Name = name,
            FirstSaveTime = time,
            LastUpdateTime = time
        });

        return TrySaveDbChanges(context);
    }

    public bool TryAddNewGallery(Uri uri, string ownerName)
    {
        if (TryAddGalleryProfile(uri, ownerName)) return true;
        return !TryAddNewUser(ownerName) || TryAddGalleryProfile(uri, ownerName);
    }

    //Get area
    public List<User> GetUsers()
    {
        using var context = new MainDbContext(WorkDirectory);
        return context.Users.ToList();
    }

    public List<GalleryProfile> GetGalleryProfiles()
    {
        using var context = new MainDbContext(WorkDirectory);
        return context.GalleryProfiles.ToList();
    }

    public List<ProfileInfo> GetGalleryProfileInfos(Expression<Func<ProfileInfo, bool>> where)
    {
        using var context = new MainDbContext(WorkDirectory);
        return context.DisplayedGalleries.Where(where).ToList();
    }

    public List<Submission> GetSubmissions()
    {
        using var context = new MainDbContext(WorkDirectory);
        return context.Submissions
            .Include(s => s.FileMetaInfos)
            .Include(s => s.SourceGallery)
            .ToList();
    }

    public List<Submission> GetSubmissions(Expression<Func<Submission, bool>> where)
    {
        using var context = new MainDbContext(WorkDirectory);
        return context.Submissions.Where(where)
            .Include(s => s.FileMetaInfos)
            .Include(s => s.SourceGallery)
            .ToList();
    }

    #endregion

    #endregion

    #region InternalFilesManipulations

    internal List<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)> CheckOrSaveFilesAsync(
        string? localDirectoryName, List<Uri> uris)
    {
        localDirectoryName ??= Constants.DefaultOtherDirectory;

        var result = new List<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)>();
        foreach (var uri in uris) // Parallel foreach?
            result.Add(CheckOrSaveFile(localDirectoryName, uri));

        return result;
    }

    internal (FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders) CheckOrSaveFile(
        string? localDirectoryName, Uri uri)
    {
        localDirectoryName ??= Constants.DefaultOtherDirectory;

        var xxHash64 = new XxHash64();

        var responseMessage = WebDownloader.GetAsync(uri).Result;
        using var dbContext = new MainDbContext(WorkDirectory);
        using var stream = responseMessage.Content.ReadAsStream();

        stream.Position = 0;
        xxHash64.Append(stream);

        var fileMetaInfo =
            dbContext.FilesMetaInfos.FirstOrDefault(fileInfo => fileInfo.XxHash == xxHash64.GetCurrentHash());
        if (fileMetaInfo != null)
            return (fileMetaInfo, uri, responseMessage.Headers);

        var localPath = uri.AbsoluteUri.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        localPath = Path.Combine(WorkDirectory, Constants.DownloadedMediaDirectory, localDirectoryName, localPath);

        stream.Position = 0;
        localPath = SaveFile(stream, localPath);

        var guid = Guid.NewGuid();
        fileMetaInfo = new FileMetaInfo
        {
            Guid = guid,
            LocalFilePath = localPath,
            XxHash = xxHash64.GetCurrentHash(),
            FirstSaveTime = Time.GetCurrentDateTime()
        };
        dbContext.FilesMetaInfos.Add(fileMetaInfo);
        TrySaveDbChanges(dbContext);

        stream.Position = 0;
        _perceptualHashing.CalculateHashes(guid, stream);

        return (fileMetaInfo, uri, responseMessage.Headers);
    }

    #endregion

    #region PrivateFilesManipulations

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

    #endregion


    private bool TryAddGalleryProfile(Uri uri, string ownerName)
    {
        using var context = new MainDbContext(WorkDirectory);
        var time = Time.GetCurrentDateTime();

        var owner = context.Users.Find(ownerName);
        if (owner == null)
            return false;

        context.GalleryProfiles.Add(new GalleryProfile
        {
            Uri = uri,
            Resource = uri.Host,
            Owner = owner,
            FirstSaveTime = time,
            LastUpdateTime = time
        });
        return TrySaveDbChanges(context);
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

    private ArchiveMainFile ReadArchiveFile()
    {
        lock (_mainFile)
        {
            var archiveMainFile = JsonSerializer.Deserialize<ArchiveMainFile>(_mainFile);
            if (archiveMainFile == null)
                throw new Exception("Archive main file cannot be read");
            return archiveMainFile;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_mainFile)
            {
                _mainFile.Dispose();
            }
        }
    }
}
