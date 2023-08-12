using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;

namespace ArtHoarderArchiveService.Archive.Networking;

internal sealed class HttpClientRateLimitedHandler : DelegatingHandler
{
    private readonly AppSettings _appSettings;
    private readonly SortedDictionary<string, TokenBucketRateLimiter> _limiters;

    public HttpClientRateLimitedHandler(AppSettings appSettings) :
        base(new HttpClientHandler { AllowAutoRedirect = false })
    {
        _appSettings = appSettings;
        _limiters = new SortedDictionary<string, TokenBucketRateLimiter>();

        if (_appSettings.ConnectionLimiters == null) return;
        foreach (var (host, limit) in _appSettings.ConnectionLimiters)
            _limiters.Add(host, CreateLimiter(limit));
    }

    private static TokenBucketRateLimiter CreateLimiter(int milliseconds)
    {
        var options = new TokenBucketRateLimiterOptions
        {
            TokenLimit = 1,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 1000,
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(milliseconds),
            TokensPerPeriod = 1,
            AutoReplenishment = true
        };
        return new TokenBucketRateLimiter(options);
    }

    private RateLimiter GetLimiterForHost(string host)
    {
        if (_limiters.TryGetValue(host, out var limiter)) return limiter;
        limiter = CreateLimiter(_appSettings.DefaultConnectionLimiter);
        _limiters.Add(host, limiter);

        return limiter;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri == null) throw new Exception("Request does not contain a uri");

        var limiter = GetLimiterForHost(request.RequestUri.Host);
        using var lease = await limiter.AcquireAsync(permitCount: 1, cancellationToken);

        if (lease.IsAcquired)
            return await base.SendAsync(request, cancellationToken);

        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        if (lease.TryGetMetadata(
                MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.Add(
                "Retry-After",
                ((int)retryAfter.TotalSeconds).ToString(
                    NumberFormatInfo.InvariantInfo));
        }

        return response;
    }
}
