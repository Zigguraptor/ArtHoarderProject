namespace ArtHoarderArchiveService.Archive.Parsers.Settings;

internal class ParserSettings
{
    public string ParserType { get; set; }
    public string Host { get; set; }
    public Dictionary<string, string> Settings { get; set; }
}