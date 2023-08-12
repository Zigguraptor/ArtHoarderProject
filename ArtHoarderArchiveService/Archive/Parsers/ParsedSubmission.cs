namespace ArtHoarderArchiveService.Archive.Parsers;

public class ParsedSubmission
{
    public ParsedSubmission(Uri uri, Uri sourceGalleryUri, List<Uri> submissionFileUris)
    {
        Uri = uri;
        SourceGalleryUri = sourceGalleryUri;
        SubmissionFileUris = submissionFileUris;
    }

    public Uri Uri { get; init; }
    public Uri SourceGalleryUri { get; init; }
    public List<Uri> SubmissionFileUris { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Tags { get; init; }

    public DateTime? PublicationTime { get; init; }
}
