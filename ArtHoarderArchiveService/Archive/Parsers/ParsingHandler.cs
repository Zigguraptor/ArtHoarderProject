﻿using ArtHoarderArchiveService.Archive.DAL;
using ArtHoarderArchiveService.Archive.DAL.Entities;
using ArtHoarderArchiveService.Archive.Networking;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderArchiveService.Archive.Parsers;

internal class ParsingHandler : IParsHandler
{
    private readonly ILogger<ParsingHandler> _logger;
    private readonly IWebDownloader _webDownloader;
    private readonly FileHandler _fileHandler;
    private readonly string _workDirectory;

    public ParsingHandler(ILogger<ParsingHandler> logger, IWebDownloader webDownloader, FileHandler fileHandler,
        string workDirectory)
    {
        _logger = logger;
        _webDownloader = webDownloader;
        _fileHandler = fileHandler;
        _workDirectory = workDirectory;
    }

    public Uri? GetLastSubmissionUri(Uri galleryUri)
    {
        using var context = new MainDbContext(_workDirectory);
        return context.GalleryProfiles.Find(galleryUri)?.LastSubmission;
    }

    public virtual bool RegisterGalleryProfile(GalleryProfile galleryProfile, string? saveFolder,
        CancellationToken cancellationToken)
    {
        using var context = new MainDbContext(_workDirectory);
        var localGalleryProfile = context.GalleryProfiles.Find(galleryProfile.Uri);
        if (localGalleryProfile == null)
        {
            context.GalleryProfiles.Add(galleryProfile);
            return TrySaveChanges(context);
        }

        if (galleryProfile.IconFileUri != null)
        {
            var responseMessage = _webDownloader.Get(galleryProfile.IconFileUri, cancellationToken);
            var stream = responseMessage.Content.ReadAsStream(cancellationToken);
            var fileName = galleryProfile.IconFileUri.AbsoluteUri.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];

            var fileMetaInfo =
                _fileHandler.SaveFileIfNotExists(stream, _workDirectory, saveFolder, fileName, cancellationToken);
            galleryProfile.IconFileGuid = fileMetaInfo.Guid;

            localGalleryProfile.Update(galleryProfile);
            return TrySaveChanges(context);
        }

        localGalleryProfile.Update(galleryProfile);
        return TrySaveChanges(context);
    }

    public virtual void RegisterSubmission(ParsedSubmission? parsedSubmission, string? saveFolder,
        CancellationToken cancellationToken)
    {
        if (parsedSubmission == null)
            return;

        using var context = new MainDbContext(_workDirectory);
        var localSubmission = context.Submissions.Include(s => s.FileMetaInfos)
            .FirstOrDefault(s => s.Uri == parsedSubmission.Uri);

        if (localSubmission == null)
        {
            localSubmission = new Submission(parsedSubmission)
            {
                FileMetaInfos = new List<FileMetaInfo>()
            };
            context.Submissions.Add(localSubmission);
        }
        else
        {
            localSubmission.Update(parsedSubmission);
        }

        if (!TrySaveChanges(context)) return;
        if (parsedSubmission.SubmissionFileUris.Count <= 0) return;

        var fileMetaInfos = ProcessFiles(parsedSubmission, saveFolder, cancellationToken);
        UpdateFileMetaInfos(localSubmission.FileMetaInfos, fileMetaInfos);
        TrySaveChanges(context);
    }

    private FileMetaInfo[] ProcessFiles(ParsedSubmission parsedSubmission, string? saveFolder,
        CancellationToken cancellationToken)
    {
        var fileUris = parsedSubmission.SubmissionFileUris;
        var fileMetaInfos = new FileMetaInfo[fileUris.Count];

        for (var i = 0; i < fileUris.Count; i++)
        {
            var fileUri = fileUris[i];
            var responseMessage = _webDownloader.Get(fileUri, cancellationToken);
            var stream = responseMessage.Content.ReadAsStream(cancellationToken);
            var fileName = fileUri.AbsoluteUri.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
            var fileMetaInfo =
                _fileHandler.SaveFileIfNotExists(stream, _workDirectory, saveFolder, fileName, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return fileMetaInfos;
            fileMetaInfos[i] = fileMetaInfo;
        }

        return fileMetaInfos;
    }

    private static void UpdateFileMetaInfos(ICollection<FileMetaInfo> localFileMetaInfos,
        IEnumerable<FileMetaInfo> newFileMetaInfos)
    {
        foreach (var fileMetaInfo in newFileMetaInfos)
        {
            var localInfo = localFileMetaInfos.FirstOrDefault(i => i.Guid == fileMetaInfo.Guid);
            if (localInfo != null)
            {
                localInfo.Update(fileMetaInfo);
            }
            else
            {
                localFileMetaInfos.Add(fileMetaInfo);
            }
        }
    }

    public DateTime? LastFullUpdate(Uri galleryUri)
    {
        using var context = new MainDbContext(_workDirectory);
        return context.GalleryProfiles.Find(galleryUri)?.LastFullUpdateTime;
    }

    public void UpdateLastSuccessfulSubmission(Uri galleryUri, Uri successfulSubmission)
    {
        using var context = new MainDbContext(_workDirectory);
        var galleryProfile = context.GalleryProfiles.Find(galleryUri);
        if (galleryProfile == null) throw new Exception("Так быть не должно."); //TODO

        galleryProfile.LastSubmission = successfulSubmission;
        context.Update(galleryProfile);
        TrySaveChanges(context);
    }

    public void RegScheduledGalleryUpdateInfo(ScheduledGalleryUpdateInfo scheduledGalleryUpdateInfo)
    {
        using var context = new CacheDbContext(_workDirectory);
        if (context.ScheduledUpdateGalleries.Any(i => i.GalleryUri == scheduledGalleryUpdateInfo.GalleryUri))
            context.ScheduledUpdateGalleries.Update(scheduledGalleryUpdateInfo);
        else
            context.ScheduledUpdateGalleries.Add(scheduledGalleryUpdateInfo);

        TrySaveChanges(context);
    }

    private bool TrySaveChanges(DbContext dbContext)
    {
        try
        {
            dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development")
                throw;

            LogError(e.ToString());
            return false;
        }

        return true;
    }

    protected virtual void LogWarning(string message)
    {
        Console.WriteLine("OLD LOGGER: " + message);
        // Logger.WarningLog($"[{GetType()}] {message}");
    }

    protected virtual void LogError(string message)
    {
        Console.WriteLine("OLD LOGGER: " + message);
        // Logger.ErrorLog($"[{GetType()}] {message}");
    }
}
