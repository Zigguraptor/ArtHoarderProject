﻿using System.IO.Hashing;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text.Json;
using ArtHoarderArchiveService.Archive.DAL;
using ArtHoarderArchiveService.Archive.DAL.Entities;
using ArtHoarderArchiveService.Archive.Infrastructure.Enums;
using ArtHoarderArchiveService.Archive.Managers;
using ArtHoarderArchiveService.Archive.Networking;
using ArtHoarderArchiveService.Archive.Parsers;
using ArtHoarderArchiveService.Archive.Serializable;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderArchiveService.Archive;

public sealed class ArchiveContext : IDisposable
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

    public async Task UpdateAllGalleriesAsync(IMessageWriter statusWriter, bool oldIncluded,
        CancellationToken cancellationToken)
    {
        statusWriter.Write("Analyze data base...");
        await using var context = new MainDbContext(WorkDirectory);
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

            await using var cache = new CacheDbContext(WorkDirectory);
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
        using var context = new MainDbContext(WorkDirectory);
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
        directoryName ??= GalleryAnalyzer.TryGetUserName(scheduledGalleryUpdateInfo.GalleryUri);
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

    public void TryAddNewUsers(IMessageWriter messageWriter, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (!TryAddNewUser(name))
                messageWriter.Write($"\"{name}\" name already exists.");
        }
    }

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

    public void TryAddNewGalleries(IMessageWriter statusWriter, List<Uri> uris)
    {
        var names = new List<string>(uris.Count);
        if (names == null) throw new ArgumentNullException(nameof(names));
        foreach (var uri in uris)
        {
            names.Add(GalleryAnalyzer.TryGetUserName(uri) ?? string.Empty); //TODO string.Empty is ok? 
        }

        TryAddNewGalleries(statusWriter, uris, names); //TODO
    }

    public void TryAddNewGalleries(IMessageWriter statusWriter, List<Uri> uris, List<string> ownerNames)
    {
        if (uris.Count != ownerNames.Count)
            throw new ArgumentException(); //TODO

        for (var i = 0; i < uris.Count; i++)
        {
            if (!TryAddNewGallery(uris[i], ownerNames[i]))
                statusWriter.Write($"Gallery \"{uris[i]}\" already exists.");
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

    public async Task<List<FileMetaInfo>> GetFileMetaInfoByHashAsync(string hashName, Guid fileGuid)
    {
        if (!_perceptualHashing.GetAvailableAlgorithms().Contains(hashName))
            return new List<FileMetaInfo>(0);

        await using var pHashDbContext = new PHashDbContext(WorkDirectory, hashName);
        var pHashInfo = await pHashDbContext.PHashInfos.FirstOrDefaultAsync(i => i.FileGuid == fileGuid);
        if (pHashInfo is null)
            return new List<FileMetaInfo>(0);

        var guids = await pHashDbContext.PHashInfos
            .Where(i => i.Hash == pHashInfo.Hash && i.FileGuid != fileGuid)
            .Select(info => info.FileGuid)
            .ToListAsync();

        await using var mainDbContext = new MainDbContext(WorkDirectory);
        return await mainDbContext.FilesMetaInfos.Where(i => guids.Contains(i.Guid)).ToListAsync();
    }

    public async Task<List<FileMetaInfo>> GetFilesInfoAsync(Expression<Func<FileMetaInfo, bool>> where)
    {
        await using var context = new MainDbContext(WorkDirectory);
        var response = context.FilesMetaInfos.Where(where);
        return await response.ToListAsync();
    }

    #endregion

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
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
