using System.Linq.Expressions;
using System.Text.Json;
using ArtHoarderArchiveService.Archive.DAL;
using ArtHoarderArchiveService.Archive.DAL.Entities;
using ArtHoarderArchiveService.Archive.Infrastructure;
using ArtHoarderArchiveService.Archive.Infrastructure.Enums;
using ArtHoarderArchiveService.Archive.Serializable;
using ArtHoarderArchiveService.PipeCommunications;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderArchiveService.Archive;

public sealed class ArchiveContext : IDisposable
{
    private readonly FileStream _mainFile;
    private readonly object _filesAccessSyncObj = new();
    private readonly string _workDirectory;
    private readonly IFileHandler _fileHandler;
    private readonly IUniversalParser _universalParser;

    private ArchiveMainFile _cachedArchiveMainFile;

    public ArchiveContext(string workDirectory, FileStream fileStream, IFileHandler fileHandler,
        IUniversalParser universalParser)
    {
        _workDirectory = workDirectory;
        _mainFile = fileStream;
        _fileHandler = fileHandler;
        _universalParser = universalParser;

        _cachedArchiveMainFile = ReadArchiveFile();
    }

    public void Dispose()
    {
        lock (_mainFile) _mainFile.Dispose();
    }

    #region publicMethods

    public static CreationCode CreateArchive(string workDirectory) =>
        ArchiveInitialization.CreateArchive(workDirectory);

    public FileStateSet GetFileStateSet()
    {
        lock (_filesAccessSyncObj)
        {
            return FilesValidator.GetFileStateSet(_workDirectory);
        }
    }

    #region Update

    public async Task UpdateAllGalleriesAsync(IMessager statusWriter, bool oldIncluded,
        CancellationToken cancellationToken)
    {
        statusWriter.WriteLine("Analyze data base...");
        await using var context = new MainDbContext(_workDirectory);
        var groups = context
            .GalleryProfiles
            .OrderBy(p => p.LastNewUpdateTime)
            .GroupBy(p => p.ResourceHost);

        if (oldIncluded)
        {
            var progressBar = statusWriter.CreateNewProgressBar("", groups.Count(),
                "All galleries update stage 1/2(newest)");

            await Parallel.ForEachAsync(groups, cancellationToken,
                (group, _) => LightUpdateGroupAsync(progressBar, group, cancellationToken)).ConfigureAwait(false);

            progressBar = statusWriter.CreateNewProgressBar("", groups.Count(),
                "All galleries update stage 2/2(old)");

            await context.DisposeAsync();

            await using var cache = new CacheDbContext(_workDirectory);
            var scheduledUpdateGalleries = cache
                .ScheduledUpdateGalleries
                .OrderBy(i => i.LastFullUpdate)
                .GroupBy(i => i.Host);

            await Parallel.ForEachAsync(scheduledUpdateGalleries, cancellationToken,
                (infoGroup, _) => ScheduledUpdateGroupAsync(progressBar, infoGroup, cancellationToken));
        }
        else
        {
            var progressBar = statusWriter.CreateNewProgressBar("", groups.Count(),
                "All galleries update. Only new submissions.");

            await Parallel.ForEachAsync(groups, cancellationToken,
                (group, _) => LightUpdateGroupAsync(progressBar, group, cancellationToken)).ConfigureAwait(false);
        }
    }

    private async ValueTask LightUpdateGroupAsync(IProgressWriter progressWriter, IEnumerable<GalleryProfile> group,
        CancellationToken cancellationToken)
    {
        foreach (var gallery in group)
        {
            await LightUpdateGalleryAsync(progressWriter, gallery.Uri, cancellationToken);
            progressWriter.UpdateBar();
        }
    }

    private async ValueTask ScheduledUpdateGroupAsync(IProgressWriter progressWriter,
        IEnumerable<ScheduledGalleryUpdateInfo> group, CancellationToken cancellationToken)
    {
        foreach (var info in group)
        {
            await ScheduledUpdateGalleryAsync(progressWriter, info, cancellationToken);
            progressWriter.UpdateBar();
        }
    }


    public async Task LightUpdateGalleryAsync(IProgressWriter progressWriter, Uri galleryUri,
        CancellationToken cancellationToken,
        string? directoryName = null)
    {
        using var context = new MainDbContext(_workDirectory);
        var galleryProfile =
            await context.GalleryProfiles.FirstOrDefaultAsync(g => g.Uri == galleryUri,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        if (galleryProfile is null)
        {
            progressWriter.Write($"{galleryUri} not found in db.");
            return;
        }

        directoryName ??= galleryProfile.OwnerName;

        await _universalParser
            .LightUpdateGalleryAsync(progressWriter, galleryUri, directoryName, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task ScheduledUpdateGalleryAsync(IProgressWriter statusWriter,
        ScheduledGalleryUpdateInfo scheduledGalleryUpdateInfo, CancellationToken cancellationToken,
        string? directoryName = null)
    {
        directoryName ??= _universalParser.TryGetUserName(scheduledGalleryUpdateInfo.GalleryUri);
        directoryName ??= "Other"; //TODO literal

        return _universalParser.ScheduledUpdateGalleryAsync(statusWriter, scheduledGalleryUpdateInfo, cancellationToken,
            directoryName);
    }

    public async Task<bool> UpdateUserAsync(string userName, bool oldIncluded, CancellationToken cancellationToken)
    {
        // await using var context = new MainDbContext(WorkDirectory);
        // var galleryProfiles = context.GalleryProfiles.Where(g => g.OwnerName == userName).ToList();
        // if (galleryProfiles.Count == 0) return false;
        //
        // var tasks = galleryProfiles.Select(profile =>
        //     UpdateGalleryAsync(profile.Uri, cancellationToken));
        // await Task.WhenAll(tasks);

        return true;
    }

    public async Task UpdateGalleriesAsync(ICollection<ProfileInfo> galleries, bool oldIncluded,
        CancellationToken cancellationToken)
    {
        // // reporter.SetProgressStage("Galleries Updating");
        // // reporter.SetProgressBar(0, galleries.Length);
        //
        // foreach (var gallery in galleries)
        // {
        //     // reporter.Progress();
        //     await UpdateGalleryAsync(gallery.Uri, cancellationToken).ConfigureAwait(false);
        // }
    }

    public async Task UpdateGalleryAsync(Uri galleryUri, CancellationToken cancellationToken,
        string? directoryName = null)
    {
        // using var context = new MainDbContext(WorkDirectory);
        // var galleryProfile = context.GalleryProfiles.FirstOrDefault(g => g.Uri == galleryUri);
        // if (galleryProfile is null)
        //     return false;
        //
        // directoryName ??= galleryProfile.OwnerName;
        // return await _universalParser.UpdateGallery(galleryUri, directoryName, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region MainDbManipulations

    #region Add

    public void TryAddNewUsers(IMessager messager, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (!TryAddNewUser(name))
                messager.WriteLine($"\"{name}\" name already exists.");
        }
    }

    public bool TryAddNewUser(string name)
    {
        using var context = new MainDbContext(_workDirectory);
        var time = Time.NowUtcDataTime();
        context.Users.Add(new User
        {
            Name = name,
            FirstSaveTime = time,
            LastUpdateTime = time
        });

        return TrySaveDbChanges(context);
    }

    public void TryAddNewGalleries(IMessager statusWriter, List<Uri> uris)
    {
        var names = new List<string>(uris.Count);
        if (names == null) throw new ArgumentNullException(nameof(names));
        foreach (var uri in uris)
        {
            names.Add(_universalParser.TryGetUserName(uri) ?? string.Empty); //TODO string.Empty is ok? 
        }

        TryAddNewGalleries(statusWriter, uris, names); //TODO
    }

    public void TryAddNewGalleries(IMessager statusWriter, List<Uri> uris, List<string> ownerNames)
    {
        if (uris.Count != ownerNames.Count)
            throw new ArgumentException(); //TODO

        for (var i = 0; i < uris.Count; i++)
        {
            if (!TryAddNewGallery(uris[i], ownerNames[i]))
                statusWriter.WriteLine($"Gallery \"{uris[i]}\" already exists.");
        }
    }

    public bool TryAddNewGallery(Uri uri, string ownerName)
    {
        if (TryAddGalleryProfile(uri, ownerName)) return true;
        return !TryAddNewUser(ownerName) || TryAddGalleryProfile(uri, ownerName);
    }

    #endregion

    #region Get

    public List<User> GetUsers()
    {
        using var context = new MainDbContext(_workDirectory);
        return context.Users.ToList();
    }

    public List<GalleryProfile> GetGalleryProfiles()
    {
        using var context = new MainDbContext(_workDirectory);
        return context.GalleryProfiles.ToList();
    }

    public List<ProfileInfo> GetGalleryProfileInfos(Expression<Func<ProfileInfo, bool>> where)
    {
        using var context = new MainDbContext(_workDirectory);
        return context.DisplayedGalleries.Where(where).ToList();
    }

    public List<Submission> GetSubmissions()
    {
        using var context = new MainDbContext(_workDirectory);
        return context.Submissions
            .Include(s => s.FileMetaInfos)
            .Include(s => s.SourceGallery)
            .ToList();
    }

    public List<Submission> GetSubmissions(Expression<Func<Submission, bool>> where)
    {
        using var context = new MainDbContext(_workDirectory);
        return context.Submissions.Where(where)
            .Include(s => s.FileMetaInfos)
            .Include(s => s.SourceGallery)
            .ToList();
    }

    public List<FileMetaInfo> GetFileMetaInfoByHashAsync(string hashName, Guid fileGuid)
    {
        if (!PerceptualHashing.GetAvailableAlgorithms().Contains(hashName)) return new List<FileMetaInfo>(0);

        using var pHashDbContext = new PHashDbContext(_workDirectory, hashName);
        var pHashInfo = pHashDbContext.PHashInfos.FirstOrDefault(i => i.FileGuid == fileGuid);
        if (pHashInfo is null)
            return new List<FileMetaInfo>(0);

        var guids = pHashDbContext.PHashInfos
            .Where(i => i.Hash == pHashInfo.Hash && i.FileGuid != fileGuid)
            .Select(info => info.FileGuid)
            .ToList();

        using var mainDbContext = new MainDbContext(_workDirectory);
        return mainDbContext.FilesMetaInfos.Where(i => guids.Contains(i.Guid)).ToList();
    }

    public async Task<List<FileMetaInfo>> GetFilesInfoAsync(Expression<Func<FileMetaInfo, bool>> where)
    {
        await using var context = new MainDbContext(_workDirectory);
        var response = context.FilesMetaInfos.Where(where);
        return await response.ToListAsync();
    }

    #endregion

    #endregion

    #endregion

    private bool TryAddGalleryProfile(Uri uri, string ownerName)
    {
        using var context = new MainDbContext(_workDirectory);
        var time = Time.NowUtcDataTime();

        var owner = context.Users.Find(ownerName);
        if (owner == null)
            return false;

        context.GalleryProfiles.Add(new GalleryProfile
        {
            Uri = uri,
            ResourceHost = uri.Host,
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
}
