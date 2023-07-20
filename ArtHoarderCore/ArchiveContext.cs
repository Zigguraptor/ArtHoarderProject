﻿using System.IO.Hashing;
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

    #region MainDbManipulations

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

    public string? TryGetUserName(Uri uri)
    {
        return _universalParser.TryGetUserName(uri);
    }

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

    internal async Task<List<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)>> CheckOrSaveFilesAsync(
        string? localDirectoryName, List<Uri> uris)
    {
        localDirectoryName ??= Constants.DefaultOtherDirectory;

        var result = new List<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)>();
        foreach (var uri in uris) // Parallel foreach?
            result.Add(await CheckOrSaveFileAsync(localDirectoryName, uri));

        return result;
    }

    internal async Task<(FileMetaInfo fileMetaInfo, Uri fileUri, HttpHeaders httpHeaders)> CheckOrSaveFileAsync(
        string? localDirectoryName,
        Uri uri)
    {
        localDirectoryName ??= Constants.DefaultOtherDirectory;

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
        TrySaveDbChanges(dbContext);

        stream.Position = 0;
        _perceptualHashing.CalculateHashes(guid, stream);

        return (fileMetaInfo, uri, responseMessage.Headers);
    }

    #endregion

    #region PrivateFilesManipulations

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
