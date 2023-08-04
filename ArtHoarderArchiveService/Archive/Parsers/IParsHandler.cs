using ArtHoarderArchiveService.Archive.DAL.Entities;

namespace ArtHoarderArchiveService.Archive.Parsers;

internal interface IParsHandler
{
    Logger Logger { get; }

    Uri? GetLastSubmissionUri(Uri galleryUri);
    bool RegisterGalleryProfile(GalleryProfile galleryProfile, string? saveFolder);
    void RegisterSubmission(ParsedSubmission? parsedSubmission, string? saveFolder);
    DateTime? LastFullUpdate(Uri galleryUri);
    void UpdateLastSuccessfulSubmission(Uri galleryUri, Uri successfulSubmission);
    void RegScheduledGalleryUpdateInfo(ScheduledGalleryUpdateInfo scheduledGalleryUpdateInfo);
}
