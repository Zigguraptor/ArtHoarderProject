using ArtHoarderArchiveService.Archive.DAL;
using ArtHoarderArchiveService.Archive.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderArchiveService.Archive.Parsers;

internal class ParsingHandler : IParsHandler
{
    private readonly ILogger<ParsingHandler> _logger;
    private readonly FileHandler _fileHandler;
    private readonly string _workDirectory;

    public ParsingHandler(ILogger<ParsingHandler> logger, FileHandler fileHandler, string workDirectory)
    {
        _logger = logger;
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
            var tuple = _fileHandler.CheckOrSaveFile(_workDirectory, saveFolder, galleryProfile.IconFileUri,
                cancellationToken);
            galleryProfile.IconFileGuid = tuple.fileMetaInfo.Guid;

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
        var localSubmission = context.Submissions.Find(parsedSubmission.Uri);
        if (localSubmission == null)
        {
            context.Submissions.Add(new Submission(parsedSubmission));
            TrySaveChanges(context);
            return;
        }

        if (parsedSubmission.SubmissionFileUris.Count > 0)
        {
            if (parsedSubmission.SubmissionFileUris.Count == 1)
            {
                var tuple = _fileHandler.CheckOrSaveFile(_workDirectory, saveFolder,
                    parsedSubmission.SubmissionFileUris[0], cancellationToken);
                AddOrUpdateSubmissionFileLink(parsedSubmission.Uri, tuple.fileMetaInfo.Guid,
                    parsedSubmission.SubmissionFileUris[0]);
            }

            var response =
                _fileHandler.CheckOrSaveFiles(_workDirectory, saveFolder, parsedSubmission.SubmissionFileUris,
                    cancellationToken);
            foreach (var valueTuple in response)
                AddOrUpdateSubmissionFileLink(parsedSubmission.Uri, valueTuple.fileMetaInfo.Guid, valueTuple.fileUri);
        }

        localSubmission.Update(parsedSubmission);
        TrySaveChanges(context);

        void AddOrUpdateSubmissionFileLink(Uri uri, Guid guid, Uri fileUri)
        {
            var local = context.SubmissionFileMetaInfos.Find(uri, guid);
            if (local == null)
            {
                context.SubmissionFileMetaInfos.Add(new SubmissionFileMetaInfo(uri, guid, fileUri));
            }
            else
            {
                if (local.FileUri.ToString() != fileUri.ToString())
                    local.FileUri = fileUri;
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
        context.Update(scheduledGalleryUpdateInfo);
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
            LogError(e.ToString());
            return false;
        }

        return true;
    }

    protected virtual void LogWarning(string message)
    {
        Console.WriteLine("OLD LOGGER: "+ message);
        // Logger.WarningLog($"[{GetType()}] {message}");
    }

    protected virtual void LogError(string message)
    {
        Console.WriteLine("OLD LOGGER: "+ message);
        // Logger.ErrorLog($"[{GetType()}] {message}");
    }
}
