﻿using ArtHoarderCore.DAL;
using ArtHoarderCore.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore.Parsers;

internal class ParsingHandler : IParsHandler
{
    private readonly ArchiveContext _archiveContext;
    public Logger Logger { get; }

    public ParsingHandler(ArchiveContext archiveContext, Logger logger)
    {
        _archiveContext = archiveContext;
        Logger = logger;
    }

    public virtual bool RegisterGalleryProfile(GalleryProfile galleryProfile, string? saveFolder)
    {
        using var context = new MainDbContext(_archiveContext.WorkDirectory);
        var localGalleryProfile = context.GalleryProfiles.Find(galleryProfile.Uri);
        if (localGalleryProfile == null)
        {
            context.GalleryProfiles.Add(galleryProfile);
            return TrySaveChanges(context);
        }

        if (galleryProfile.IconFileUri != null)
        {
            var tuple = _archiveContext.CheckOrSaveFile(saveFolder, galleryProfile.IconFileUri);
            galleryProfile.IconFileGuid = tuple.fileMetaInfo.Guid;

            localGalleryProfile.Update(galleryProfile);
            return TrySaveChanges(context);
        }

        localGalleryProfile.Update(galleryProfile);
        return TrySaveChanges(context);
    }

    public virtual void RegisterSubmission(ParsedSubmission? parsedSubmission, string? saveFolder)
    {
        if (parsedSubmission == null)
            return;

        using var context = new MainDbContext(_archiveContext.WorkDirectory);
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
                var tuple = _archiveContext.CheckOrSaveFile(saveFolder, parsedSubmission.SubmissionFileUris[0]);
                AddOrUpdateSubmissionFileLink(parsedSubmission.Uri, tuple.fileMetaInfo.Guid,
                    parsedSubmission.SubmissionFileUris[0]);
            }

            var response = _archiveContext.CheckOrSaveFilesAsync(saveFolder, parsedSubmission.SubmissionFileUris);
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