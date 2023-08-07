using ArtHoarderArchiveService.Archive.DAL.Entities;

namespace ArtHoarderArchiveService.Archive.Parsers;

public sealed class UniversalParser // LordParser
{
    public static bool IsSupportedLink(Uri uri) => ParserFactory.IsSupportedLink(uri);
    private readonly IParsHandler _parsHandler;
    private readonly Dictionary<string, Parser> _parsers = new();

    internal UniversalParser(IParsHandler parsHandler)
    {
        _parsHandler = parsHandler;
    }

    internal Task LightUpdateGalleryAsync(
        IProgressWriter progressWriter, Uri galleryUri, string directoryName, CancellationToken cancellationToken)
    {
        var parser = GetParser(galleryUri);
        if (parser == null)
        {
            progressWriter.WriteLog($"Not found parser for {galleryUri.Host}", LogLevel.Error);
            return Task.CompletedTask;
        }

        return parser.LightUpdateGalleryAsync(progressWriter, galleryUri, directoryName, cancellationToken);
    }
    
    internal Task<List<Uri>>? GetSubscriptions(Uri uri, CancellationToken cancellationToken)
    {
        return GetParser(uri)?.TryGetSubscriptionsAsync(uri, cancellationToken);
    }

    internal string? TryGetUserName(Uri uri)
    {
        return GetParser(uri)?.TryGetUserName(uri);
    }

    internal bool CheckLink(Uri uri)
    {
        var parser = GetParser(uri);
        return parser != null && parser.CheckLink(uri);
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

    public Task ScheduledUpdateGalleryAsync(IProgressWriter progressWriter,
        ScheduledGalleryUpdateInfo scheduledGalleryUpdateInfo, CancellationToken cancellationToken,
        string directoryName)
    {
        var parser = GetParser(scheduledGalleryUpdateInfo.GalleryUri);
        if (parser == null)
        {
            progressWriter.WriteLog($"Not found parser for {scheduledGalleryUpdateInfo.Host}", LogLevel.Error);
            return Task.CompletedTask;
        }

        return parser.ScheduledUpdateGalleryAsync(progressWriter, scheduledGalleryUpdateInfo, cancellationToken,
            directoryName);
    }
}
