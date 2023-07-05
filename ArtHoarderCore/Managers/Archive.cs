using System.Linq.Expressions;
using System.Text.Json;
using ArtHoarderCore.DAL;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.HashAlgs;
using ArtHoarderCore.Infrastructure;
using ArtHoarderCore.Parsers;
using ArtHoarderCore.Serializable;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore.Managers;

public sealed class Archive
{
    public readonly string WorkDirectory;
    public readonly string ArchiveName;
    private readonly UniversalParser _universalParser;
    private readonly IFilesManager _filesManager;
    private readonly Logger _logger;
    public string TempFolder => Path.Combine(WorkDirectory, Constants.Temp);
    public string MetaDataFolder => Path.Combine(WorkDirectory, Constants.MetaFilesDirectory);

    public IEnumerable<string> PHashAlgorithmsInDb =>
        Directory.EnumerateFiles(WorkDirectory, "*.db", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)!;

    public HashSet<string> UnregisteredFiles => _filesManager.UnregisteredFiles;

    public List<FileMetaInfo> MissingFiles
    {
        get
        {
            using var context = new MainDbContext(WorkDirectory);
            return context.FilesMetaInfos.Where(info => _filesManager.MissingFiles.Contains(info.LocalFilePath))
                .ToList();
        }
    }

    public List<FileMetaInfo> ChangedFiles
    {
        get
        {
            using var context = new MainDbContext(WorkDirectory);
            return context.FilesMetaInfos.Where(info => _filesManager.ChangedFiles.Contains(info.LocalFilePath))
                .ToList();
        }
    }

    public List<IImageHasher> EnabledImageHashMakers => _filesManager.ImageHashMakers;

    public Archive(ProgressReporter reporter, string archivePath, string archiveName)
    {
        WorkDirectory = archivePath;
        var mainFilePath = Path.Combine(archivePath, Constants.ArchiveMainFilePath);
        if (File.Exists(mainFilePath))
        {
            ArchiveName = ReadArchiveFile(mainFilePath);
        }
        else
        {
            ArchiveName = archiveName;
            InitArchiveMainFile(mainFilePath);
        }

        CreateSystemFolders(WorkDirectory);
        _logger = new Logger(archivePath, "Archive manager");
        InitMainDb(WorkDirectory);

        var parsersLogger = new Logger(WorkDirectory, "Parsers");
        _filesManager = new FilesManager(WorkDirectory, mainFilePath, _logger)
        {
            ImageHashMakers = new List<IImageHasher> { new DctHash() } //Add standard hash algorithms. 
        };
        _universalParser = new UniversalParser(new ParsHandler(_filesManager, parsersLogger));
    }

    public Archive(ProgressReporter reporter, string archivePath)
    {
        WorkDirectory = archivePath;
        var mainFilePath = Path.Combine(archivePath, Constants.ArchiveMainFilePath);
        if (!File.Exists(mainFilePath))
        {
            ArchiveName = "Archive";
            InitArchiveMainFile(mainFilePath);
        }
        else
        {
            ArchiveName = ReadArchiveFile(mainFilePath);
        }

        CreateSystemFolders(archivePath);
        _logger = new Logger(archivePath, "Archive manager");
        InitMainDb(archivePath);

        var parsersLogger = new Logger(archivePath, "Parsers");
        _filesManager = new FilesManager(archivePath, mainFilePath, _logger)
        {
            ImageHashMakers = new List<IImageHasher> { new DctHash() } //Add standard hash algorithms. 
        };
        _universalParser = new UniversalParser(new ParsHandler(_filesManager, parsersLogger));
    }

    #region Init

    private static string ReadArchiveFile(string path)
    {
        if (!File.Exists(path)) throw new Exception("Archive main file not found");

        using var stream = File.Open(path, FileMode.Open);
        var qwe = JsonSerializer.Deserialize<ArchiveMainFile>(stream);
        if (qwe == null)
            throw new Exception("Archive main file cannot be read");

        return qwe.ArchiveName;
    }

    private void InitArchiveMainFile(string path)
    {
        CreateSystemFolders(WorkDirectory);
        var mainFile = new ArchiveMainFile
        {
            ArchiveName = ArchiveName
        };
        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, mainFile);
    }

    private void InitMainDb(string workDirectory)
    {
        using var context = new MainDbContext(workDirectory);
        try
        {
            context.Database.ExecuteSqlRaw
            (@"CREATE VIEW `View_DisplayedGalleries` AS
    SELECT GalleryProfiles.UserName, GalleryProfiles.OwnerName, GalleryProfiles.Uri
     FROM `GalleryProfiles`;");
        }
        catch
        {
            // ignored
        }
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

    #endregion

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

        return TrySaveChanges(context);
    }

    public bool TryAddNewGallery(Uri uri, string ownerName)
    {
        if (TryAddGalleryProfile(uri, ownerName)) return true;
        return !TryAddNewUser(ownerName) || TryAddGalleryProfile(uri, ownerName);
    }

    public async Task<bool> UpdateGalleryAsync(Uri uri, string ownerName, ProgressReporter reporter)
    {
        await using var context = new MainDbContext(WorkDirectory);

        if (!context.GalleryProfiles.Any(g => g.Uri == uri))
        {
            TryAddNewUser(ownerName);
            if (!TryAddNewGallery(uri, ownerName))
            {
                return false;
            }
        }

        await _universalParser.UpdateGalleryAsync(uri, ownerName, reporter);
        return true;
    }

    public async Task UpdateGalleriesAsync(ProfileInfo[] galleries, ProgressReporter reporter)
    {
        reporter.SetProgressStage("Galleries Updating");
        reporter.SetProgressBar(0, galleries.Length);

        foreach (var gallery in galleries)
        {
            reporter.Progress();
            await _universalParser.UpdateGalleryAsync(gallery.Uri, gallery.OwnerName, reporter).ConfigureAwait(false);
        }
    }

    public List<FullSubmissionInfo> GetSubmissions(Expression<Func<Submission, bool>> where)
    {
        using var context = new MainDbContext(WorkDirectory);
        var submissions = context.Submissions.Where(where);

        var result = new List<FullSubmissionInfo>();
        foreach (var submission in submissions)
        {
            result.Add(CombineFullSubmissionInfo(context, submission));
        }

        return result;
    }

    private FullSubmissionInfo CombineFullSubmissionInfo(MainDbContext context, Submission submission)
    {
        var fileMetaInfos = new List<FileMetaInfo>();
        var linksToFiles = context.SubmissionFileMetaInfos.Where(info => info.SubmissionUri == submission.Uri);
        foreach (var linksToFile in linksToFiles)
        {
            var fileMetaInfo = context.FilesMetaInfos.Find(linksToFile.FileGuid);
            if (fileMetaInfo != null)
                fileMetaInfos.Add(fileMetaInfo);
        }

        var galleryProfile = context.GalleryProfiles.Find(submission.SourceGalleryUri);
        return new FullSubmissionInfo(galleryProfile!.OwnerName, galleryProfile.UserName, submission, fileMetaInfos);
    }

    public Task UpdateAllGalleriesAsync()
    {
        throw new NotImplementedException();
    }

    public List<User> GetUsers()
    {
        using var context = new MainDbContext(WorkDirectory);
        return context.Users.ToList();
    }

    public string? TryGetUserName(Uri uri)
    {
        return _universalParser.TryGetUserName(uri);
    }

    public Task<IEnumerable<Uri>>? TryGetSubscriptionsAsync(Uri uri, ProgressReporter reporter)
    {
        return _universalParser.GetSubscriptions(uri, reporter);
    }

    public bool CheckLink(Uri uri)
    {
        return _universalParser.CheckLink(uri);
    }

    public List<ProfileInfo> GetProfiles()
    {
        using var context = new MainDbContext(WorkDirectory);
        return context.DisplayedGalleries.ToList();
    }

    public List<ProfileInfo> GetProfiles(Expression<Func<ProfileInfo, bool>> where)
    {
        using var context = new MainDbContext(WorkDirectory);
        return context.DisplayedGalleries.Where(where).ToList();
    }

    public List<FullSubmissionInfo> GetSubmissionsByHash(string hashName, Guid fileGuid, int range)
    {
        throw new NotImplementedException();
    }

    public List<FullSubmissionInfo> GetFullSubmissionInfosSortedByHash(Expression<Func<FullSubmissionInfo, bool>> where,
        string hashName)
    {
        using var context = new MainDbContext(WorkDirectory);
        throw new NotImplementedException();
    }

    public IEnumerable<FileMetaInfo> GetFilesInfo(Expression<Func<FileMetaInfo, bool>> where)
    {
        using var context = new MainDbContext(WorkDirectory);
        var response = context.FilesMetaInfos.Where(where);
        return response.ToList();
    }

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
        return TrySaveChanges(context);
    }

    private bool TrySaveChanges(DbContext dbContext)
    {
        try
        {
            dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            _logger.ErrorLog(e.ToString());
            return false;
        }

        return true;
    }
}
