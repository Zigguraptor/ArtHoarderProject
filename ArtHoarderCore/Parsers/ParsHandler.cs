using ArtHoarderCore.DAL;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Managers;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore.Parsers;

internal class ParsHandler : IParsHandler
{
    private IFilesManager _filesManager;
    public Logger Logger { get; }

    public ParsHandler(IFilesManager filesManager, Logger logger)
    {
        _filesManager = filesManager;
        Logger = logger;
    }

    public virtual async Task<bool> RegisterGalleryProfileAsync(GalleryProfile galleryProfile, string? saveFolder)
    {
        await using var context = new MainDbContext(_filesManager.WorkDirectory);
        var localGalleryProfile = await context.GalleryProfiles.FindAsync(galleryProfile.Uri);
        if (localGalleryProfile == null)
        {
            context.GalleryProfiles.Add(galleryProfile);
            return TrySaveChanges(context);
        }

        if (galleryProfile.IconFileUri != null)
        {
            var tuple = await _filesManager
                .CheckOrSaveFileAsync(saveFolder, galleryProfile.IconFileUri).ConfigureAwait(false);
            galleryProfile.IconFileGuid = tuple.fileMetaInfo.Guid;

            localGalleryProfile.Update(galleryProfile);
            return TrySaveChanges(context);
        }

        localGalleryProfile.Update(galleryProfile);
        return TrySaveChanges(context);
    }

    public virtual async Task RegisterSubmissionAsync(ParsedSubmission? parsedSubmission, string? saveFolder)
    {
        if (parsedSubmission == null)
            return;

        await using var context = new MainDbContext(_filesManager.WorkDirectory);
        var localSubmission = await context.Submissions.FindAsync(parsedSubmission.Uri);
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
                var tuple = await _filesManager.CheckOrSaveFileAsync(saveFolder,
                    parsedSubmission.SubmissionFileUris[0]);
                AddOrUpdateSubmissionFileLink(parsedSubmission.Uri,
                    tuple.fileMetaInfo.Guid, parsedSubmission.SubmissionFileUris[0]);
            }

            var response = await _filesManager.CheckOrSaveFilesAsync(saveFolder, parsedSubmission.SubmissionFileUris);
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
        Logger.WarningLog($"[{GetType()}] {message}");
    }

    protected virtual void LogError(string message)
    {
        Logger.ErrorLog($"[{GetType()}] {message}");
    }
}