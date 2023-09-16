using System.Net;
using HtmlAgilityPack;

namespace ArtHoarderArchiveService.Archive.Networking;

public class WebDownloader : IWebDownloader
{
    private readonly ILogger<WebDownloader> _logger;
    private readonly HttpClient _client;

    public WebDownloader(ILogger<WebDownloader> logger, HttpMessageHandler httpMessageHandler)
    {
        _logger = logger;
        _client = new HttpClient(httpMessageHandler);
        _client.DefaultRequestHeaders.Add("user-agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36");
    }

    public HtmlDocument GetHtml(Uri uri, CancellationToken cancellationToken)
    {
        var response = _client.GetAsync(uri, cancellationToken).Result;
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException("StatusCode: " + response.StatusCode + ". uri: " + uri); //TODO
        }

        var html = response.Content.ReadAsStringAsync(cancellationToken).Result;
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    public HttpResponseMessage Get(Uri uri, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get {uri}", uri.ToString());
        return _client.GetAsync(uri, cancellationToken).Result;
    }
}
