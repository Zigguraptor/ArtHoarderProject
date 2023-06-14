using System.Net;
using System.Threading.RateLimiting;
using HtmlAgilityPack;

namespace ArtHoarderCore.Networking;

public static class WebDownloader
{
    private static readonly HttpClient Client;

    static WebDownloader()
    {
        var options = new TokenBucketRateLimiterOptions()
        {
            TokenLimit = 1,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 1000,
            ReplenishmentPeriod = TimeSpan.FromSeconds(4),
            TokensPerPeriod = 1,
            AutoReplenishment = true
        };
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        var delegatingHandler = new HttpClientRateLimitedHandler(new TokenBucketRateLimiter(options), handler);
        Client = new HttpClient(delegatingHandler);
        Client.DefaultRequestHeaders.Add("user-agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36");
    }
    
    public static async Task<HtmlDocument?> GetHtmlAsync(Uri uri)
    {
        var response = await Client.GetAsync(uri).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException("StatusCode: " + response.StatusCode + ". uri: " + uri); //TODO
        }

        var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    public static Task<HttpResponseMessage> GetAsync(Uri uri)
    {
        return Client.GetAsync(uri);
    }
}