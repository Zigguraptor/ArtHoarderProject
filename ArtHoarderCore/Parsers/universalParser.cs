using ArtHoarderCore.DAL;
using ArtHoarderCore.Infrastructure;
using ArtHoarderCore.Managers;

namespace ArtHoarderCore.Parsers;

internal sealed class UniversalParser // LordParser
{
    private readonly IParsHandler _parsHandler;
    private readonly Dictionary<string, Parser> _parsers = new();

    public UniversalParser(IParsHandler parsHandler)
    {
        _parsHandler = parsHandler;
    }

    public Task UpdateGalleryAsync(Uri galleryUri, string ownerName, ProgressReporter reporter)
    {
        return GetParser(galleryUri)?.ParsProfileGalleryAsync(galleryUri, ownerName, reporter) ?? Task.CompletedTask;
    }

    public Task<IEnumerable<Uri>>? GetSubscriptions(Uri uri, ProgressReporter reporter)
    {
        return GetParser(uri)?.TryGetSubscriptions(uri, reporter);
    }

    private Parser? GetParser(Uri uri)
    {
        if (_parsers.TryGetValue(uri.Host, out var parser))
            return parser;

        parser = ParserFactory.Create(_parsHandler, uri);
        if (parser == null)
            return null;

        _parsers.Add(uri.Host, parser);
        return parser;
    }
    
    public string? TryGetUserName(Uri uri)
    {
        return GetParser(uri)?.TryGetUserName(uri);
    }

    public bool CheckLink(Uri uri)
    {
        var parser = GetParser(uri);
        return parser != null && parser.CheckLink(uri);
    }
}