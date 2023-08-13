using ArtHoarderArchiveService.Archive.Networking;
using ArtHoarderArchiveService.Archive.Parsers;
using ArtHoarderArchiveService.PipeCommunications;

namespace ArtHoarderArchiveService.Archive;

public class ArchiveContextFactory
{
    private const int TimeOut = 4;
    private readonly object _syncRoot = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly IWebDownloader _webDownloader;
    private readonly SortedDictionary<string, OccupiedContainer<ArchiveContext>> _archiveContexts = new();
    public GalleryAnalyzer GalleryAnalyzer { get; }

    public ArchiveContextFactory(ILoggerFactory loggerFactory, IWebDownloader webDownloader,
        GalleryAnalyzer galleryAnalyzer)
    {
        _loggerFactory = loggerFactory;
        _webDownloader = webDownloader;
        GalleryAnalyzer = galleryAnalyzer;
    }

    private FileStream OpenFile(IMessager progressWriter, string path)
    {
        var  mainFilePath = Path.Combine(path, Constants.ArchiveMainFilePath);
        for (var t = 0; t < TimeOut; t++)
        {
            try
            {
                var fileStream = File.Open(mainFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return fileStream;
            }
            catch
            {
                Thread.Sleep(1000);
            }
        }

        progressWriter.WriteLine("Failed to open file");
        throw new Exception("Failed to open file");
    }

    public ArchiveContext CreateArchiveContext(IMessager progressWriter, string workDirectory, object owner)
    {
        lock (_syncRoot)
        {
            if (_archiveContexts.TryGetValue(workDirectory, out var container))
                return container.TakeItem(owner);

            var stream = OpenFile(progressWriter, workDirectory);
            var fileHandler = new FileHandler(_webDownloader, workDirectory);
            var logger = _loggerFactory.CreateLogger<ParsingHandler>();
            var parsingHandler = new ParsingHandler(logger, fileHandler, workDirectory);
            var universalParser = new UniversalParser(parsingHandler, _webDownloader);
            var archiveContext = new ArchiveContext(workDirectory, stream, fileHandler, universalParser);
            container = new OccupiedContainer<ArchiveContext>(archiveContext, owner,
                _ => DisposeContainer(workDirectory));
            _archiveContexts.Add(workDirectory, container);
            return archiveContext;
        }
    }

    public void RealiseContext(string workDirectory, object owner)
    {
        lock (_syncRoot)
            _archiveContexts.GetValueOrDefault(workDirectory)?.Realise(owner);
    }

    private void DisposeContainer(string workDirectory)
    {
        lock (_syncRoot)
            _archiveContexts.Remove(workDirectory);
    }
}
