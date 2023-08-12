using ArtHoarderArchiveService.Archive.Networking;
using ArtHoarderArchiveService.Archive.Parsers;

namespace ArtHoarderArchiveService.Archive;

//В этом классе должны быть только те методы, которые не влияют на архив. Что логично.
public class GalleryAnalyzer
{
    private readonly UniversalParser _universalParser;

    public GalleryAnalyzer(IWebDownloader webDownloader)
    {
        //Нельзя вызывать методы этого парсера влияющие на архив.
        //Пустой обработчик не будет ничего записывать, он выкинет исключение!
        _universalParser = new UniversalParser(new EmptyParsingHandler(), webDownloader);
    }

    public List<Uri>? TryGetSubscriptions(Uri uri, CancellationToken cancellationToken)
    {
        return _universalParser.GetSubscriptions(uri, cancellationToken);
    }

    public string? TryGetUserName(Uri uri)
    {
        return _universalParser.TryGetUserName(uri);
    }
}
