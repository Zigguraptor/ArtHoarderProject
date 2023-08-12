namespace ArtHoarderArchiveService.Archive.Parsers.Settings;

internal abstract class ParserSettings
{
    [AutoSet] public string ParserType { get; set; }
    public string Host { get; set; }
    [AutoSet] public DateOnly Version { get; set; }
}
