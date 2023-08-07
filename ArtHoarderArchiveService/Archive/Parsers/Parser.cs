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

    public async Task LightUpdateGalleryAsync(
        IProgressWriter progressWriter, Uri galleryUri, string dirName, CancellationToken cancellationToken)
    {
        var doc = await WebDownloader.GetHtmlAsync(galleryUri, cancellationToken).ConfigureAwait(false);
        if (doc == null)
        {
            var msg = $"Profile not found on uri: {galleryUri}";
            LogError(msg);
            progressWriter.WriteLog(msg, LogLevel.Error);
            return;
        }

        if (!_parsHandler.RegisterGalleryProfile(GetProfile(galleryUri, doc), dirName))
        {
            progressWriter.WriteLog($"Saving error. Changes aborted. {galleryUri}", LogLevel.Error);
            return;
        }

        progressWriter.Write($"Gallery analysis(May take a long time)... {galleryUri}");
        var linksTuple = GetNewSubmissionLinks(progressWriter, doc, _parsHandler.GetLastSubmissionUri(galleryUri));
        progressWriter.Write($"Successfully analyzed {galleryUri}");

        var scheduledGalleryUpdateInfo = new ScheduledGalleryUpdateInfo
        {
            GalleryUri = galleryUri,
            LastFullUpdate = _parsHandler.LastFullUpdate(galleryUri),
            Host = galleryUri.Host,
            FirstLoadedSubmissionUri = linksTuple.submissions[linksTuple.submissions.Count],
            LastLoadedPage = linksTuple.lastPage,
        };
        _parsHandler.RegScheduledGalleryUpdateInfo(scheduledGalleryUpdateInfo);

        if (linksTuple.submissions.Count > 0)
        {
            using var subBar = progressWriter.CreateSubProgressBar(galleryUri.ToString(), linksTuple.submissions.Count);
            var lastSuccessfulSubmission =
                await UpdateSubmissionsAsync(subBar, linksTuple.submissions, galleryUri, dirName, cancellationToken)
                    .ConfigureAwait(false);

            if (lastSuccessfulSubmission != null)
                _parsHandler.UpdateLastSuccessfulSubmission(galleryUri, lastSuccessfulSubmission);
        }
    }

    public async Task ScheduledUpdateGalleryAsync(IProgressWriter progressWriter,
        ScheduledGalleryUpdateInfo scheduledGalleryUpdateInfo, CancellationToken cancellationToken,
        string directoryName)
    {
        if (scheduledGalleryUpdateInfo.LastLoadedPage != null)
        {
            progressWriter.Write($"Gallery analysis(May take a long time)... {scheduledGalleryUpdateInfo.GalleryUri}");
            var uris = GetOldSubmissionLinks(progressWriter, scheduledGalleryUpdateInfo);
            progressWriter.Write($"Successfully analyzed {scheduledGalleryUpdateInfo.GalleryUri}");

            using var subBar =
                progressWriter.CreateSubProgressBar(scheduledGalleryUpdateInfo.GalleryUri.ToString(), uris.Count);
            await UpdateSubmissionsAsync(subBar, uris, scheduledGalleryUpdateInfo.GalleryUri, directoryName,
                cancellationToken);
        }
        else
        {
            var doc = await WebDownloader.GetHtmlAsync(scheduledGalleryUpdateInfo.GalleryUri, cancellationToken)
                .ConfigureAwait(false);
            if (doc == null)
            {
                var msg = $"Profile not found on uri: {scheduledGalleryUpdateInfo.GalleryUri}";
                LogError(msg);
                progressWriter.WriteLog(msg, LogLevel.Error);
                return;
            }

            progressWriter.Write($"Gallery analysis(May take a long time)... {scheduledGalleryUpdateInfo.GalleryUri}");
            var uris = GetAllSubmissionLinks(progressWriter, doc);
            progressWriter.Write($"Successfully analyzed {scheduledGalleryUpdateInfo.GalleryUri}");

            using var subBar =
                progressWriter.CreateSubProgressBar(scheduledGalleryUpdateInfo.GalleryUri.ToString(), uris.Count);
            await UpdateSubmissionsAsync(subBar, uris, scheduledGalleryUpdateInfo.GalleryUri, directoryName,
                cancellationToken);
        }
    }

    private async Task<Uri?> UpdateSubmissionsAsync(IProgressWriter progressWriter, List<Uri> uris,
        Uri sourceGalleryUri, string dirName, CancellationToken cancellationToken)
    {
        Uri? lastSuccessfulSubmission = null; //TODO
        var channel = Channel.CreateBounded<(HtmlDocument htmlDocument, Uri uri)>(new BoundedChannelOptions(uris.Count)
        {
            SingleReader = false,
            SingleWriter = true
        });

        var producingTask = ProduceAsync(channel.Writer);
        var consumingTask = ConsumeAsync(channel.Reader);
        await Task.WhenAll(producingTask, consumingTask);

        return lastSuccessfulSubmission;

        async Task ConsumeAsync(ChannelReader<(HtmlDocument htmlDocument, Uri uri)> reader)
        {
            await foreach (var tuple in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                progressWriter.UpdateBar(tuple.uri.ToString());
                _parsHandler.RegisterSubmission(GetSubmission(tuple.htmlDocument, tuple.uri, sourceGalleryUri),
                    dirName);
            }
        }

        async Task ProduceAsync(ChannelWriter<(HtmlDocument htmlDocument, Uri uri)> writer)
        {
            try
            {
                for (var i = uris.Count - 1; i >= 0; i--)
                {
                    var uri = uris[i];
                    if (cancellationToken.IsCancellationRequested) break;

                    var submissionDocument = await WebDownloader.GetHtmlAsync(uri, cancellationToken);
                    progressWriter.Write($"{uri} Loaded");

                    if (submissionDocument != null)
                    {
                        await writer.WriteAsync((submissionDocument, uri), cancellationToken);
                    }
                    else
                    {
                        var msg = $"\"{uri}\" Failed to load submission html doc. Parsing of this page is canceled.";
                        progressWriter.WriteLog(msg, LogLevel.Error);
                        LogError(msg);
                        progressWriter.UpdateBar();
                    }
                }
            }
            catch (Exception e)
            {
                //TODO progressWriter
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

    protected abstract (List<Uri> submissions, string lastPage) GetNewSubmissionLinks(
        IProgressWriter progressWriter, HtmlDocument profileDocument, Uri? lastLoadedSubmissionUri);

    protected abstract List<Uri> GetAllSubmissionLinks(IProgressWriter progressWriter, HtmlDocument profileDocument);

    protected abstract List<Uri> GetOldSubmissionLinks(IProgressWriter progressWriter,
        ScheduledGalleryUpdateInfo scheduledGalleryUpdateInfo);

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
