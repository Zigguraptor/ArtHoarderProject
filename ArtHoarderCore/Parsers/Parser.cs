using System.Collections.Concurrent;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Infrastructure;
using ArtHoarderCore.Networking;
using HtmlAgilityPack;

namespace ArtHoarderCore.Parsers;

internal abstract class Parser
{
    protected string Host { get; init; } = null!;

    private readonly IParsHandler _parsHandler;

    protected Parser(IParsHandler parsHandler)
    {
        _parsHandler = parsHandler;
    }

    public async Task ParsProfileGalleryAsync(Uri profileUri, string ownerName, ProgressReporter reporter)
    {
        var doc = await WebDownloader.GetHtmlAsync(profileUri).ConfigureAwait(false);
        if (doc == null)
        {
            LogError("Profile not found");
            reporter.Report($"Error! Profile not found: {Host} {profileUri}");
            return;
        }

        reporter.Report($"Parsing profile: {profileUri}");
        if (await _parsHandler.RegisterGalleryProfileAsync(GetProfile(profileUri, doc), ownerName)
                .ConfigureAwait(false) == false)
            return;

        reporter.Report($"Gallery analysis(May take a long time)... {Host} {TryGetUserName(profileUri)}");
        var uris = GetSubmissionLinks(doc).ToArray();
        if (uris.Length > 0)
        {
            var subProgress =
                reporter.CreateSubProgress($"UpdateSubmissions {Host} {TryGetUserName(profileUri)}", uris.Length);
            await UpdateSubmissions(uris, profileUri, ownerName, subProgress).ConfigureAwait(false);
        }
    }

    private Task UpdateSubmissions(Uri[] uris, Uri sourceGallery, string ownerName,
        SubProgressInfo subProgressInfo)
    {
        var buffer = new BlockingCollection<(HtmlDocument htmlDocument, Uri uri)>(boundedCapacity: 5);

        var producerTask = Parallel.ForEachAsync(uris, ProduceAsync).ContinueWith(_ => buffer.CompleteAdding());
        var consumerTasks = new List<Task>();
        for (var i = 0; i < Environment.ProcessorCount; i++)
            consumerTasks.Add(ConsumeAsync());


        return Task.WhenAll(producerTask, Task.WhenAll(consumerTasks));

        async ValueTask ProduceAsync(Uri uri, CancellationToken cancellationToken)
        {
            var submissionDocument = await WebDownloader.GetHtmlAsync(uri);
            subProgressInfo.Report($"{uri} Loaded");
            subProgressInfo.Progress();

            if (submissionDocument != null)
            {
                buffer.Add((submissionDocument, uri), cancellationToken);
            }
            else
            {
                LogError($"\"{uri}\" Failed to load submission html doc. Parsing of this page is canceled.");
            }
        }

        async Task ConsumeAsync()
        {
            foreach (var tuple in buffer.GetConsumingEnumerable())
            {
                await _parsHandler.RegisterSubmissionAsync(
                    GetSubmission(tuple.htmlDocument, tuple.uri, sourceGallery),
                    ownerName).ConfigureAwait(false);
            }
        }
    }

    protected abstract GalleryProfile GetProfile(Uri profileUri, HtmlDocument profileDocument);
    protected abstract ParsedSubmission GetSubmission(HtmlDocument htmlDocument, Uri uri, Uri sourceGallery);

    protected abstract List<Uri> GetSubmissionLinks(HtmlDocument profileDocument);
    public abstract Task<IEnumerable<Uri>> TryGetSubscriptions(Uri uri, ProgressReporter progressReporter);

    protected void LogWarning(string message)
    {
        _parsHandler.Logger.WarningLog(
            $"Class: \"{GetType()}\". Settings for \"{Host}\" {message}");
    }

    protected void LogError(string message)
    {
        _parsHandler.Logger.ErrorLog(
            $"Class: \"{GetType()}\". Settings for \"{Host}\" {message}");
    }

    public abstract string? TryGetUserName(Uri uri);

    public abstract bool CheckLink(Uri uri);
}