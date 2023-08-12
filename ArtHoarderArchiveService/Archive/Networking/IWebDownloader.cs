using HtmlAgilityPack;

namespace ArtHoarderArchiveService.Archive.Networking;

public interface IWebDownloader
{
    public HtmlDocument GetHtml(Uri uri, CancellationToken cancellationToken);
    HttpResponseMessage Get(Uri uri, CancellationToken cancellationToken);
}
