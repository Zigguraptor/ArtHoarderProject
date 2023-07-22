namespace ArtHoarderCore.Parsers.Settings;

internal interface IParserSettings
{
    public string ParserType { get; init; }
    public string Host { get; init; }
}