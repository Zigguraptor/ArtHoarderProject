namespace ArtHoarderArchiveService.Archive.Parsers;

public class ParsedSubmission
{
    public ParsedSubmission(Uri uri, Uri sourceGalleryUri, ExtractedBytes[] submissionFiles)
    {
        Uri = uri;
        SourceGalleryUri = sourceGalleryUri;
        SubmissionFiles = submissionFiles;
    }

    public Uri Uri { get; init; }
    public Uri SourceGalleryUri { get; init; }
    public ExtractedBytes[] SubmissionFiles { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Tags { get; init; }
    public DateTime? PublicationTime { get; init; }
}
