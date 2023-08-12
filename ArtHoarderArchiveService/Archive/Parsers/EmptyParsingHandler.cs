using ArtHoarderArchiveService.Archive.DAL.Entities;

namespace ArtHoarderArchiveService.Archive.Parsers;

public class EmptyParsingHandler : IParsHandler
{
    public Uri? GetLastSubmissionUri(Uri galleryUri)
    {
        throw new Exception("Этот метод не должен быть вызван. Но кто то его вызвал.");
    }

    public bool RegisterGalleryProfile(GalleryProfile galleryProfile, string? saveFolder,
        CancellationToken cancellationToken)
    {
        throw new Exception("Этот метод не должен быть вызван. Но кто то его вызвал.");
    }

    public void RegisterSubmission(ParsedSubmission? parsedSubmission, string? saveFolder,
        CancellationToken cancellationToken)
    {
        throw new Exception("Этот метод не должен быть вызван. Но кто то его вызвал.");
    }

    public DateTime? LastFullUpdate(Uri galleryUri)
    {
        throw new Exception("Этот метод не должен быть вызван. Но кто то его вызвал.");
    }

    public void UpdateLastSuccessfulSubmission(Uri galleryUri, Uri successfulSubmission)
    {
        throw new Exception("Этот метод не должен быть вызван. Но кто то его вызвал.");
    }

    public void RegScheduledGalleryUpdateInfo(ScheduledGalleryUpdateInfo scheduledGalleryUpdateInfo)
    {
        throw new Exception("Этот метод не должен быть вызван. Но кто то его вызвал.");
    }
}
