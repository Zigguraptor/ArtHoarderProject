using System.Threading.Channels;
using ArtHoarderArchiveService.Archive.DAL.Entities;
using ArtHoarderArchiveService.Archive.Networking;
using HtmlAgilityPack;

namespace ArtHoarderArchiveService.Archive.Parsers;

internal abstract class Parser
{
    protected string Host { get; init; } = null!;

    private readonly IParsHandler _parsHandler;

    protected Parser(IParsHandler parsHandler)
    {
        _parsHandler = parsHandler;
    }

    public async Task<bool> ParsProfileGallery(Uri galleryUri, string dirName, CancellationToken cancellationToken)
    {
        var doc = await WebDownloader.GetHtmlAsync(galleryUri, cancellationToken).ConfigureAwait(false);
        if (doc == null)
        {
            LogError("Profile not found");
            // reporter.Report($"Error! Profile not found: {Host} {profileUri}");
            return false;
        }

        // reporter.Report($"Parsing profile: {profileUri}");
        if (!_parsHandler.RegisterGalleryProfile(GetProfile(galleryUri, doc), dirName))
            return false;

        // reporter.Report($"Gallery analysis(May take a long time)... {Host} {TryGetUserName(profileUri)}");
        var uris = GetSubmissionLinks(doc);
        if (uris.Count > 0)
        {
            // var subProgress = reporter.CreateSubProgress($"UpdateSubmissions {Host} {TryGetUserName(profileUri)}", uris.Length);
            await UpdateSubmissionsAsync(uris, galleryUri, dirName, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    private async Task UpdateSubmissionsAsync(List<Uri> uris, Uri sourceGallery, string dirName,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<(HtmlDocument htmlDocument, Uri uri)>(new BoundedChannelOptions(uris.Count)
        {
            SingleReader = false,
            SingleWriter = true
        });

        var producingTask = ProduceAsync(channel.Writer);
        var consumingTask = ConsumeAsync(channel.Reader);
        await Task.WhenAll(producingTask, consumingTask);

        async Task ConsumeAsync(ChannelReader<(HtmlDocument htmlDocument, Uri uri)> reader)
        {
            await foreach (var tuple in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                _parsHandler.RegisterSubmission(GetSubmission(tuple.htmlDocument, tuple.uri, sourceGallery),
                    dirName);
                // Console.WriteLine($"Обработан: {tuple.uri}");   
            }
        }

        async Task ProduceAsync(ChannelWriter<(HtmlDocument htmlDocument, Uri uri)> writer)
        {
            try
            {
                foreach (var uri in uris)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var submissionDocument = await WebDownloader.GetHtmlAsync(uri, cancellationToken);
                    // subProgressInfo.Report($"{uri} Loaded");
                    // subProgressInfo.Progress();

                    if (submissionDocument != null)
                    {
                        await writer.WriteAsync((submissionDocument, uri), cancellationToken);
                    }
                    else
                    {
                        LogError($"\"{uri}\" Failed to load submission html doc. Parsing of this page is canceled.");
                    }
                }
            }
            catch (Exception e)
            {
                var ex = e.ToString();
                Console.WriteLine(ex);
                LogError(ex);
            }
            finally
            {
                writer.Complete();
            }
        }
    }

    protected abstract GalleryProfile GetProfile(Uri profileUri, HtmlDocument profileDocument);
    protected abstract ParsedSubmission GetSubmission(HtmlDocument htmlDocument, Uri uri, Uri sourceGallery);

    protected abstract List<Uri> GetSubmissionLinks(HtmlDocument profileDocument);
    public abstract Task<List<Uri>> TryGetSubscriptionsAsync(Uri uri, CancellationToken cancellationToken);

    public abstract string? TryGetUserName(Uri uri);

    public abstract bool CheckLink(Uri uri);

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
}
