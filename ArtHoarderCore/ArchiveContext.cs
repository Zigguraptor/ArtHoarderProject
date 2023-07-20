using System.IO.Hashing;
using System.Text.Json;
using ArtHoarderCore.DAL;
using ArtHoarderCore.Managers;
using ArtHoarderCore.Serializable;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore;

public class ArchiveContext
{
    private readonly string _workDirectory;
    public string ArchiveName { get; private set; } = null!;
    private string MainFilePath => Path.Combine(_workDirectory, Constants.ArchiveMainFilePath);
    private NewFileManager _filesManager;
    
    private HashSet<string>? _unregisteredFiles;
    private HashSet<string>? _missingFiles;
    private HashSet<string>? _changedFiles;

    public ArchiveContext(string workDirectory, string? archiveName = null)
    {
        _workDirectory = workDirectory;
        _filesManager = new NewFileManager(workDirectory);
        EnsureCreated(archiveName);
    }

    #region Init

    private void EnsureCreated(string? archiveName)
    {
        if (File.Exists(MainFilePath))
        {
            ReadArchiveFile();
        }
        else
        {
            ArchiveName = archiveName ?? throw new AggregateException("Archive Name is missing (null)");
            InitArchiveMainFile();
        }

        CreateSystemFolders(_workDirectory);
        InitMainDb();
    }

    private void ReadArchiveFile()
    {
        if (!File.Exists(MainFilePath)) throw new Exception("Archive main file not found");

        using var stream = File.Open(MainFilePath, FileMode.Open);
        var archiveMainFile = JsonSerializer.Deserialize<ArchiveMainFile>(stream);
        if (archiveMainFile == null)
            throw new Exception("Archive main file cannot be read");
        ArchiveName = archiveMainFile.ArchiveName;
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

    private void InitArchiveMainFile()
    {
        CreateSystemFolders(_workDirectory);
        var mainFile = new ArchiveMainFile
        {
            ArchiveName = ArchiveName
        };
        using var stream = File.Create(MainFilePath);
        JsonSerializer.Serialize(stream, mainFile);
    }

    private void InitMainDb()
    {
        using var
            context = new MainDbContext(
                _workDirectory); //Там вложенный контекст для аудита. По тому директория, а не путь
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

    #endregion

    #region FilesManagment

    public Task ValidateFiles()
    {
        var filesHashes =  
        var systemFolder = Path.Combine(_workDirectory, Constants.MetaFilesDirectory);
        _unregisteredFiles =
            new HashSet<string>(Directory.EnumerateFiles(_workDirectory, "*", SearchOption.AllDirectories));
        _unregisteredFiles.Remove(MainFilePath);
        _unregisteredFiles.RemoveWhere(s => s.StartsWith(systemFolder));
        _missingFiles = new HashSet<string>();
        _changedFiles = new HashSet<string>();
        
        var xxHash64 = new XxHash64();
        foreach (var pair in filesHashes)
        {
            if (!_unregisteredFiles.Remove(pair.Key))
            {
                _missingFiles.Add(pair.Key);
                continue;
            }

            using var stream = File.OpenRead(pair.Key);
            stream.Position = 0;
            xxHash64.Append(stream);

            if (!xxHash64.GetHashAndReset().SequenceEqual(pair.Value))
            {
                _changedFiles.Add(pair.Key);
            }
        }

        return Task.CompletedTask;
    }

    #endregion
}
