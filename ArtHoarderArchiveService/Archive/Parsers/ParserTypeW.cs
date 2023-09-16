using System.Globalization;
using ArtHoarderArchiveService.Archive.DAL.Entities;
using ArtHoarderArchiveService.Archive.Networking;
using ArtHoarderArchiveService.Archive.Parsers.Settings;
using ArtHoarderArchiveService.PipeCommunications;
using HtmlAgilityPack;

namespace ArtHoarderArchiveService.Archive.Parsers;

internal class ParserTypeW : Parser
{
    protected readonly ParserTypeWSettings ParserTypeWSettings;

    public ParserTypeW(IParsHandler parsHandler, IWebDownloader webDownloader, ParserTypeWSettings parserTypeWSettings)
        : base(parsHandler, webDownloader)
    {
        ParserTypeWSettings = parserTypeWSettings;
        Host = ParserTypeWSettings.Host;
    }

    protected DateTime TimeToDataDateTime(string time)
    {
        time = time[5..];
        Console.WriteLine($"\"{time}\" ParseTypeFa. Time convertion debug");

        if (DateTime.TryParseExact(time, "dd MMM yyyy hh:mm:ss", //Sun, 11 Dec 2022 00:45:38 GMT
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return dateTime;
        }

        if (DateTime.TryParseExact(time, "dd MMM yyyy hh:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
        {
            return dateTime;
        }

        LogWarning($"Failed to convert date time. \"{time}\"");
        return DateTime.MinValue;
    }

    protected override GalleryProfile GetProfile(Uri profileUri, HtmlDocument profileDocument)
    {
        return new GalleryProfile
        {
            Uri = profileUri,
            UserName = GetProfileName(profileUri, profileDocument),
            CreationTime = GetProfileCreationTime(profileUri, profileDocument),
            Status = GetProfileStatus(profileDocument),
            Description = GetProfileDescription(profileDocument),
            IconFileUri = GetProfileIconUri(profileDocument),
        };
    }

    protected override async Task<ParsedSubmission> ParsSubmissionAsync(HtmlDocument submissionDocument, Uri uri,
        Uri sourceGallery, CancellationToken cancellationToken)
    {
        var fileUris = GetSubmissionFileUris(submissionDocument, uri, cancellationToken);
        var extractedBytes = new ExtractedBytes[fileUris.Count];

        for (var i = 0; i < fileUris.Count; i++)
        {
            var fileBytes = await WebDownloader.Get(fileUris[i], cancellationToken).Content
                .ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            extractedBytes[i] = new ExtractedBytes(fileUris[i], fileBytes);
        }

        return new ParsedSubmission(uri, sourceGallery, extractedBytes)
        {
            Uri = uri,
            SourceGalleryUri = sourceGallery,
            Title = GetSubmissionTitle(submissionDocument, uri),
            Description = GetSubmissionDescription(submissionDocument, uri),
            Tags = GetSubmissionTags(submissionDocument, uri),
            PublicationTime = GetPublicationTime(submissionDocument, uri)
        };
    }

    protected virtual string? GetProfileName(Uri profileUri, HtmlDocument profileDocument)
    {
        var node = profileDocument.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathProfileName);
        if (node != null) return node.InnerHtml.Trim();

        LogWarning($"\"{profileUri}\" Profile name not found");
        return null;
    }

    protected virtual DateTime GetProfileCreationTime(Uri profileUri, HtmlDocument profileDocument)
    {
        var node = profileDocument.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathProfileCreationDataTime);

        if (node != null)
        {
            var time = node.InnerHtml;
            var index = time.LastIndexOf('>') + 1;
            time = time.Substring(index, time.Length - 1 - index);

            if (DateTime.TryParseExact(time, "MMM dd, yyyy hh:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dateTime))
            {
                return dateTime;
            }

            if (DateTime.TryParseExact(time, "MMM d, yyyy hh:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateTime))
            {
                return dateTime;
            }
        }

        LogWarning($"\"{profileUri}\" Profile creation time not found");
        return DateTime.MinValue;
    }

    protected virtual string? GetProfileStatus(HtmlDocument profileDocument)
    {
        var node = profileDocument.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathProfileStatus);
        if (node != null)
        {
            return node.InnerHtml; //TODO add regex
        }

        LogWarning("Profile status not found");
        return null;
    }

    protected virtual string? GetProfileDescription(HtmlDocument profileDocument)
    {
        var node = profileDocument.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathProfileDescription);
        if (node != null) return node.InnerHtml;

        LogWarning("Profile description not found");
        return null;
    }

    protected virtual Uri? GetProfileIconUri(HtmlDocument profileDocument)
    {
        var node = profileDocument.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathProfileIcon);

        var uriText = node?.Attributes[ParserTypeWSettings.UriIconAttributeName]?.Value;
        if (uriText is { Length: > 1 } && Uri.TryCreate(uriText, UriKind.Absolute, out var uri))
            return uri;

        return null;
    }

    protected virtual DateTime GetPublicationTime(HtmlDocument document, Uri uri)
    {
        var node = document.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathSubmissionPublicationTime);

        if (DateTime.TryParseExact(node?.Attributes.First().Value, "MMM dd, yyyy hh:mm tt",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return dateTime;
        }

        if (DateTime.TryParseExact(node?.Attributes.First().Value, "MMM d, yyyy hh:mm tt",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
        {
            return dateTime;
        }

        Console.WriteLine(node?.Attributes.First().Value);
        LogWarning($"\"{uri}\" publication time not found");
        return DateTime.MinValue;
    }

    protected virtual List<Uri> GetSubmissionFileUris(HtmlDocument htmlDocument, Uri uri,
        CancellationToken cancellationToken)
    {
        var result = new List<Uri>();
        if (ParserTypeWSettings.XpathSubmissionNextFile.Length == 0)
        {
            var submissionFileUri = GetSubmissionFileUri(htmlDocument, uri);
            if (submissionFileUri != null) result.Add(submissionFileUri);
            return result;
        }

        do
        {
            var submissionFileUri = GetSubmissionFileUri(htmlDocument, uri);
            if (submissionFileUri != null) result.Add(submissionFileUri);
        } while (MoveNextSubmissionFile(ref htmlDocument, cancellationToken));

        return result;
    }

    private Uri? GetSubmissionFileUri(HtmlDocument htmlDocument, Uri uri)
    {
        HtmlNode node;
        try
        {
            node = htmlDocument.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathSubmissionFileSrc);
        }
        catch
        {
            LogError(
                $"Node selecting error. XpathSubmissionFileSrc:\"{ParserTypeWSettings.XpathSubmissionFileSrc}\"");
            return null;
        }

        if (node == null)
        {
            LogError($"\"{uri}\" Submission file src not found");
            return null;
        }

        var nodeValue = node.Attributes.FirstOrDefault(attribute =>
            attribute.Name == ParserTypeWSettings.XpathSubmissionFileSrcAttribute)?.Value;

        if (Uri.TryCreate(nodeValue, UriKind.Absolute, out var fileUri)) return fileUri;
        LogError($"URI creating error \"{fileUri}\"");
        return null;
    }

    private bool MoveNextSubmissionFile(ref HtmlDocument htmlDocument, CancellationToken cancellationToken)
    {
        HtmlNode nextButtonNode;
        try
        {
            nextButtonNode =
                htmlDocument.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathSubmissionNextFile);
        }
        catch
        {
            LogWarning("Failed selecting submission next file"); //TODO log html doc
            return false;
        }

        var nextButton = nextButtonNode?.Attributes[ParserTypeWSettings.SubmissionNextFileAttribute].Value;
        if (nextButton == null) return false;

        if (!Uri.TryCreate("https:" + nextButton, UriKind.Absolute, out var uri))
        {
            LogError($"URI creating error \"https:{nextButton}\"");
            return false;
        }

        var nextHtmlDocument = WebDownloader.GetHtml(uri, cancellationToken);
        if (nextHtmlDocument == null) return false;
        htmlDocument = nextHtmlDocument;

        return true;
    }

    protected virtual string? GetSubmissionTitle(HtmlDocument document, Uri uri)
    {
        var node = document.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathSubmissionTitle);
        if (node != null) return node.InnerText;

        LogWarning($"\"{uri}\" Submission title not found");
        return null;
    }

    protected virtual string? GetSubmissionDescription(HtmlDocument document, Uri uri)
    {
        var node = document.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathSubmissionDescription);
        if (node != null) return node.InnerHtml;

        LogWarning($"\"{uri}\" Submission description not found");
        return null;
    }

    protected virtual string? GetSubmissionTags(HtmlDocument document, Uri uri)
    {
        var nodes = document.DocumentNode.SelectNodes(ParserTypeWSettings.XpathSubmissionTags);
        if (nodes != null)
            return nodes.Aggregate("", (current, node) => current + (node.InnerText + ", "));


        LogWarning($"\"{uri}\" Submission tags not found");
        return null;
    }

    protected override (List<Uri> submissions, string lastPage) GetNewSubmissionLinks(IProgressWriter progressWriter,
        HtmlDocument profileDocument, Uri? lastLoadedSubmissionUri, CancellationToken cancellationToken)
    {
        var page = GetGalleryDocument(profileDocument, cancellationToken);
        if (page == null)
        {
            const string msg = "Gallery page not found. Possibly an error in \"XpathGalleryUri\"";
            progressWriter.WriteMessage(msg);
            //TODO log
            return (new List<Uri>(0), "");
        }

        var uris = new List<Uri>();
        HtmlDocument lastPage;
        do
        {
            lastPage = page!;
            var nods = page!.DocumentNode.SelectNodes(ParserTypeWSettings.XpathSubmissions);
            if (nods == null)
            {
                LogWarning("Submissions not found on one of the pages");
                continue;
            }

            foreach (var node in nods)
            {
                try
                {
                    //TODO extract attribute name
                    uris.Add(new Uri("https://" + Host + node.Attributes.First().Value));
                }
                catch
                {
                    progressWriter.WriteLog("Parsing error. Possibly an error in \"XpathSubmissions\"",
                        LogLevel.Error);
                }
            }
        } while (TryGetNextGalleryPage(page, out page, cancellationToken));

        return (uris, lastPage.Text);
    }

    protected override List<Uri> GetOldSubmissionLinks(IProgressWriter progressWriter,
        ScheduledGalleryUpdateInfo scheduledGalleryUpdateInfo, CancellationToken cancellationToken)
    {
        if (scheduledGalleryUpdateInfo.LastLoadedPage == null ||
            scheduledGalleryUpdateInfo.FirstLoadedSubmissionUri == null)
        {
            var profile = WebDownloader.GetHtml(scheduledGalleryUpdateInfo.GalleryUri, cancellationToken);
            return GetAllSubmissionLinks(progressWriter, profile, cancellationToken);
        }

        var page = new HtmlDocument();
        page.LoadHtml(scheduledGalleryUpdateInfo.LastLoadedPage);
        var nods = page.DocumentNode.SelectNodes(ParserTypeWSettings.XpathSubmissions);
        if (nods == null)
        {
            const string msg = "Submissions not found on one of the pages";
            progressWriter.WriteLog(msg, LogLevel.Error);
            LogWarning(msg);
            var profile = WebDownloader.GetHtml(scheduledGalleryUpdateInfo.GalleryUri, cancellationToken);
            return GetAllSubmissionLinks(progressWriter, profile, cancellationToken);
        }

        var uris = new List<Uri>();
        var firstLoadedSubmissionUri = scheduledGalleryUpdateInfo.FirstLoadedSubmissionUri.ToString();
        var firstOrDefault = nods.FirstOrDefault(n => n.Attributes.First().Value == firstLoadedSubmissionUri);
        if (firstOrDefault == null)
        {
            progressWriter.WriteLog("Cached submission link node not found. Reloading Gallery.", LogLevel.Warning);
            var profile = WebDownloader.GetHtml(scheduledGalleryUpdateInfo.GalleryUri, cancellationToken);
            return GetAllSubmissionLinks(progressWriter, profile, cancellationToken);
        }

        var i = nods.IndexOf(firstOrDefault);
        for (; i < nods.Count; i++)
        {
            try
            {
                //TODO extract attribute name
                uris.Add(new Uri("https://" + Host + nods[i].Attributes.First().Value));
            }
            catch
            {
                progressWriter.WriteLog("Parsing error. Possibly an error in the diagram.(XpathSubmissions)",
                    LogLevel.Error);
            }
        }

        while (TryGetNextGalleryPage(page, out page, cancellationToken))
        {
            nods = page!.DocumentNode.SelectNodes(ParserTypeWSettings.XpathSubmissions);
            if (nods == null)
            {
                const string msg = "Submissions not found on one of the pages";
                progressWriter.WriteLog(msg, LogLevel.Warning);
                LogWarning(msg);
                continue;
            }

            try
            {
                //TODO extract attribute name
                uris.Add(new Uri("https://" + Host + nods[i].Attributes.First().Value));
            }
            catch
            {
                progressWriter.WriteLog("Parsing error. Possibly an error in the diagram.(XpathSubmissions)",
                    LogLevel.Error);
            }
        }

        return uris;
    }


    protected override List<Uri> GetAllSubmissionLinks(IProgressWriter progressWriter, HtmlDocument profileDocument,
        CancellationToken cancellationToken)
    {
        var uris = new List<Uri>();
        var galleryDocument = GetGalleryDocument(profileDocument, cancellationToken);
        if (galleryDocument == null) return uris;

        do
        {
            var nods = galleryDocument!.DocumentNode.SelectNodes(ParserTypeWSettings.XpathSubmissions);
            if (nods == null)
            {
                LogWarning("Submissions not found on one of the pages");
                continue;
            }

            foreach (var node in nods)
            {
                try
                {
                    //TODO extract attribute name
                    uris.Add(new Uri("https://" + Host + node.Attributes.First().Value));
                }
                catch
                {
                    progressWriter.WriteLog("Parsing error. Possibly an error in \"XpathSubmissions\"",
                        LogLevel.Error);
                }
            }
        } while (TryGetNextGalleryPage(galleryDocument, out galleryDocument, cancellationToken));

        return uris;
    }

    public override List<Uri> TryGetSubscriptions(Uri uri, CancellationToken cancellationToken)
    {
        // reporter.SetProgressStage("Load subscriptions pages");
        // reporter.Report($"Download \"{uri}\"");
        var document = WebDownloader.GetHtml(uri, cancellationToken);

        var currentSubscriptionsPageUri = document?.DocumentNode
            .SelectSingleNode(ParserTypeWSettings.XpathSubscriptions)
            .Attributes.First()
            .Value;
        var pages = new Stack<HtmlDocument>();

        if (currentSubscriptionsPageUri == null) return GetSubscriptionsLinks(pages);

        currentSubscriptionsPageUri = "https://" + Host + currentSubscriptionsPageUri;
        // reporter.Report($"Download \"{currentSubscriptionsPageUri}\"");
        var subscriptionsPage = WebDownloader.GetHtml(new Uri(currentSubscriptionsPageUri), cancellationToken);

        if (subscriptionsPage == null) return GetSubscriptionsLinks(pages);

        pages.Push(subscriptionsPage);
        while (true)
        {
            string? next;
            try
            {
                next = pages.Peek()
                    .DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathSubscriptionsNextPage)
                    .Attributes[ParserTypeWSettings.XpathSubscriptionsNextPageAttribute]
                    .Value;
            }
            catch
            {
                // reporter.Error(
                //     "Failed to get \"next\" button. Update parser settings.\nXpathSubscriptionsNextPage\nXpathSubscriptionsNextPageAttribute");
                return GetSubscriptionsLinks(pages);
            }


            if (next == null) break;
            next = "https://" + Host + next;

            if (currentSubscriptionsPageUri == next) break;
            currentSubscriptionsPageUri = next;


            if (!Uri.TryCreate(currentSubscriptionsPageUri, UriKind.Absolute, out var nextUri))
            {
                // reporter.Error($"Parsing error. \"{currentSubscriptionsPageUri}\" not url");
                return GetSubscriptionsLinks(pages);
            }

            // reporter.Report($"Download \"{currentSubscriptionsPageUri}\"");
            var page = WebDownloader.GetHtml(nextUri, cancellationToken);
            if (page == null) break;

            pages.Push(page);
        }

        return GetSubscriptionsLinks(pages);
    }

    private List<Uri> GetSubscriptionsLinks(IEnumerable<HtmlDocument> documents)
    {
        var htmlDocuments = documents as HtmlDocument[] ?? documents.ToArray();

        // reporter.SetProgressStage("Pars pages");
        // reporter.SetProgressBar(0, htmlDocuments.Length);

        var uris = new List<Uri>();
        foreach (var document in htmlDocuments)
        {
            uris.AddRange(document.DocumentNode.SelectNodes(ParserTypeWSettings.XpathSubscriptionsLinks)
                .Select(d => new Uri("https://" + Host + d.Attributes.First().Value)));

            // reporter.Progress();
        }

        return uris;
    }

    public override string? TryGetUserName(Uri uri)
    {
        var path = uri.LocalPath.Split('/');
        if (path.Length <= 0) return null;
        return path[ParserTypeWSettings.UserNameOrderInProfileLink];
    }

    protected virtual HtmlDocument? GetGalleryDocument(HtmlDocument profileDocument,
        CancellationToken cancellationToken)
    {
        var uri = GetGalleryUri(profileDocument);
        if (uri == null) return null;

        var doc = WebDownloader.GetHtml(uri, cancellationToken);
        return doc;
    }

    protected virtual Uri? GetGalleryUri(HtmlDocument profileDocument)
    {
        var node = profileDocument.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathGalleryUri);
        if (node != null)
        {
            try
            {
                return new Uri("https://" + Host + node.Attributes.First().Value);
            }
            catch
            {
                // ignored
            }
        }

        LogWarning("Profile gallery not found");
        return null;
    }

    protected virtual bool TryGetNextGalleryPage(HtmlDocument page, out HtmlDocument? nextPage,
        CancellationToken cancellationToken)
    {
        var next = page.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathNextPageButton);
        if (next != null && next.InnerText.Contains("Next"))
        {
            nextPage = WebDownloader.GetHtml(new Uri("https://" + Host + next.Attributes.First().Value),
                cancellationToken);
            return true;
        }

        nextPage = null;
        return false;
    }
}
