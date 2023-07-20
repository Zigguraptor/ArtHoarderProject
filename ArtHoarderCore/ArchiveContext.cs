using System.Linq.Expressions;
using System.Text.Json;
using ArtHoarderCore.DAL;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Infrastructure.Enums;
using ArtHoarderCore.Managers;
using ArtHoarderCore.Serializable;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore;

public class ArchiveContext : IDisposable
{
    private readonly FileStream _mainFile;
    private readonly object _filesAccessSyncObj = new();
    public readonly string WorkDirectory;
    private ArchiveMainFile _cachedArchiveMainFile;

    private string MainFilePath => Path.Combine(WorkDirectory, Constants.ArchiveMainFilePath);

    public ArchiveContext(string workDirectory)
    {
        WorkDirectory = workDirectory;
        _mainFile = File.Open(MainFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        _cachedArchiveMainFile = ReadArchiveFile();
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

        return TrySaveChanges(context);
    }

    public bool TryAddNewGallery(Uri uri, string ownerName)
    {
        if (TryAddGalleryProfile(uri, ownerName)) return true;
        return !TryAddNewUser(ownerName) || TryAddGalleryProfile(uri, ownerName);
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

    private static bool TrySaveChanges(DbContext dbContext)
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
