namespace ArtHoarderArchiveService.Archive.Parsers;

public class ExtractedBytes
{
    public ExtractedBytes(Uri uri, byte[] bytes)
    {
        Uri = uri;
        Bytes = bytes;
    }

    public Uri Uri { get; }
    public byte[] Bytes { get; }
}
