using ArtHoarderArchiveService.Archive.Networking;
using ArtHoarderArchiveService.Archive.Parsers;

namespace ArtHoarderArchiveService.Archive;

public class ArchiveContextFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IWebDownloader _webDownloader;
    public GalleryAnalyzer GalleryAnalyzer { get; }

    public ArchiveContextFactory(ILoggerFactory loggerFactory, IWebDownloader webDownloader,
        GalleryAnalyzer galleryAnalyzer)
    {
        _loggerFactory = loggerFactory;
        _webDownloader = webDownloader;
        GalleryAnalyzer = galleryAnalyzer;
    }

    public ArchiveContext CreateArchiveContext(string workDirectory)
    {
        var fileHandler = new FileHandler(_webDownloader, workDirectory);
        var logger = _loggerFactory.CreateLogger<ParsingHandler>();
        var parsingHandler = new ParsingHandler(logger, fileHandler, workDirectory);
        var universalParser = new UniversalParser(parsingHandler, _webDownloader);
        return new ArchiveContext(workDirectory, fileHandler, universalParser);
    }
}
