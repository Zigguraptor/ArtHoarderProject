using ArtHoarderArchiveService.Archive.DAL.Entities;
using ArtHoarderArchiveService.Archive.Infrastructure;
using ArtHoarderArchiveService.Archive.Networking;
using ArtHoarderArchiveService.PipeCommunications;

namespace ArtHoarderArchiveService.Archive.Parsers;

public sealed class UniversalParser : IUniversalParser // LordParser
{
    public static bool IsSupportedLink(Uri uri) => ParserFactory.IsSupportedLink(uri);
    private readonly IParsHandler _parsHandler;
    private readonly IWebDownloader _webDownloader;
    private readonly Dictionary<string, Parser> _parsers = new();

    internal UniversalParser(IParsHandler parsHandler, IWebDownloader webDownloader)
    {
        _parsHandler = parsHandler;
        _webDownloader = webDownloader;
    }

    public Task LightUpdateGalleryAsync(
        IProgressWriter progressWriter, Uri galleryUri, string? directoryName, CancellationToken cancellationToken)
    {
        var parser = GetParser(galleryUri);
        if (parser == null)
        {
            progressWriter.WriteLog($"Not found parser for {galleryUri.Host}", LogLevel.Error);
            return Task.CompletedTask;
        }

        return parser.LightUpdateGalleryAsync(progressWriter, galleryUri, directoryName, cancellationToken);
    }

    public List<Uri>? GetSubscriptions(Uri uri, CancellationToken cancellationToken)
    {
        return GetParser(uri)?.TryGetSubscriptions(uri, cancellationToken);
    }

    public string? TryGetUserName(Uri uri)
    {
        return GetParser(uri)?.TryGetUserName(uri);
    }

    private Parser? GetParser(Uri uri)
    {
        if (_parsers.TryGetValue(uri.Host, out var parser))
            return parser;

        parser = ParserFactory.Create(_parsHandler, _webDownloader, uri);
        if (parser == null)
            return null;

        _parsers.Add(uri.Host, parser);
        return parser;
    }

    public Task ScheduledUpdateGalleryAsync(IProgressWriter progressWriter,
        ScheduledGalleryUpdateInfo scheduledGalleryUpdateInfo, CancellationToken cancellationToken,
        string? directoryName)
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
