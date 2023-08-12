using ArtHoarderArchiveService.Archive.DAL.Entities;

namespace ArtHoarderArchiveService.Archive.Parsers;

internal interface IParsHandler
{
    Uri? GetLastSubmissionUri(Uri galleryUri);
    bool RegisterGalleryProfile(GalleryProfile galleryProfile, string? saveFolder, CancellationToken cancellationToken);

    void RegisterSubmission(ParsedSubmission? parsedSubmission, string? saveFolder,
        CancellationToken cancellationToken);

    DateTime? LastFullUpdate(Uri galleryUri);
    void UpdateLastSuccessfulSubmission(Uri galleryUri, Uri successfulSubmission);
    void RegScheduledGalleryUpdateInfo(ScheduledGalleryUpdateInfo scheduledGalleryUpdateInfo);
}
