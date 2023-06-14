using System.Globalization;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Infrastructure;
using ArtHoarderCore.Networking;
using ArtHoarderCore.Parsers.Settings;
using HtmlAgilityPack;

namespace ArtHoarderCore.Parsers;

internal class ParserTypeW : Parser
{
    private readonly string[] _mainProfilePath =
    {
        "user",
        "???"
    };

    protected readonly ParserTypeWSettings ParserTypeWSettings;

    private const string ErrorMsg =
        "It is not right. It shouldn't be like that. NextGalleryPage() should return false if there is no doc.";

    public ParserTypeW(IParsHandler parsHandler, ParserTypeWSettings parserTypeWSettings) : base(parsHandler)
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

    protected override ParsedSubmission GetSubmission(HtmlDocument submissionDocument, Uri uri, Uri sourceGallery)
    {
        return new ParsedSubmission(uri, sourceGallery, GetSubmissionFileUris(submissionDocument, uri))
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

            Console.WriteLine($"\"{time}\" 120 ParseTypeFa");

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
            var text = node.InnerHtml;
            return text[..text.LastIndexOf('|')];
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

        var uri = node?.Attributes[ParserTypeWSettings.UriIconAttributeName]?.Value;
        if (uri != null && uri.Length > 1)
            return new Uri("https:" + uri);

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

    protected virtual List<Uri> GetSubmissionFileUris(HtmlDocument document, Uri uri)
    {
        var result = new List<Uri>();
        while (true)
        {
            var node = document.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathSubmissionFileSrc);
            if (node != null)
            {
                try
                {
                    result.Add(new Uri(
                        "https:" + node.Attributes[ParserTypeWSettings.XpathSubmissionFileSrcAttribute].Value));
                }
                catch (Exception e)
                {
                    LogWarning(e.ToString());
                }
            }
            else
            {
                LogWarning($"\"{uri}\" Submission file src not found");
                return result;
            }

            var nextButton = document.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathSubmissionNextFile)?
                .Attributes[ParserTypeWSettings.SubmissionNextFileAttribute].Value;
            if (nextButton != null)
            {
                try
                {
                    var htmlDocument = WebDownloader.GetHtmlAsync(new Uri("https:" + nextButton)).Result;
                    if (htmlDocument == null) break;
                    document = htmlDocument;
                }
                catch
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }


        return result;
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

    protected override List<Uri> GetSubmissionLinks(HtmlDocument profileDocument)
    {
        var pages = GetGalleryPages(profileDocument);

        var uris = new List<Uri>();
        foreach (var page in pages)
        {
            var nods = page.DocumentNode.SelectNodes(ParserTypeWSettings.XpathSubmissions);
            if (nods == null)
            {
                LogWarning("Submissions not found on one of the pages");
                continue;
            }

            uris.AddRange(nods.Select(node => new Uri("https://" + Host + node.Attributes.First().Value)));
        }

        return uris;
    }

    public override async Task<IEnumerable<Uri>> TryGetSubscriptions(Uri uri, ProgressReporter reporter)
    {
        reporter.SetProgressStage("Load subscriptions pages");
        reporter.Report($"Download \"{uri}\"");
        var document = await WebDownloader.GetHtmlAsync(uri);

        var currentSubscriptionsPageUri = document?.DocumentNode
            .SelectSingleNode(ParserTypeWSettings.XpathSubscriptions)
            .Attributes.First()
            .Value;
        var pages = new Stack<HtmlDocument>();

        if (currentSubscriptionsPageUri == null) return GetSubscriptionsLinks(pages, reporter);

        currentSubscriptionsPageUri = "https://" + Host + currentSubscriptionsPageUri;
        reporter.Report($"Download \"{currentSubscriptionsPageUri}\"");
        var subscriptionsPage =
            await WebDownloader.GetHtmlAsync(new Uri(currentSubscriptionsPageUri)).ConfigureAwait(false);

        if (subscriptionsPage == null) return GetSubscriptionsLinks(pages, reporter);

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
                reporter.Error(
                    "Failed to get \"next\" button. Update parser settings.\nXpathSubscriptionsNextPage\nXpathSubscriptionsNextPageAttribute");
                return GetSubscriptionsLinks(pages, reporter);
            }


            if (next == null) break;
            next = "https://" + Host + next;

            if (currentSubscriptionsPageUri == next) break;
            currentSubscriptionsPageUri = next;


            if (!Uri.TryCreate(currentSubscriptionsPageUri, UriKind.Absolute, out var nextUri))
            {
                reporter.Error($"Parsing error. \"{currentSubscriptionsPageUri}\" not url");
                return GetSubscriptionsLinks(pages, reporter);
            }

            reporter.Report($"Download \"{currentSubscriptionsPageUri}\"");
            var page = await WebDownloader.GetHtmlAsync(nextUri).ConfigureAwait(false);
            if (page == null) break;

            pages.Push(page);
        }

        return GetSubscriptionsLinks(pages, reporter);
    }

    private IEnumerable<Uri> GetSubscriptionsLinks(IEnumerable<HtmlDocument> documents, ProgressReporter reporter)
    {
        var htmlDocuments = documents as HtmlDocument[] ?? documents.ToArray();

        reporter.SetProgressStage("Pars pages");
        reporter.SetProgressBar(0, htmlDocuments.Length);

        var uris = new List<Uri>();
        foreach (var document in htmlDocuments)
        {
            uris.AddRange(document.DocumentNode.SelectNodes(ParserTypeWSettings.XpathSubscriptionsLinks)
                .Select(d => new Uri("https://" + Host + d.Attributes.First().Value)));

            reporter.Progress();
        }

        return uris;
    }

    public override string? TryGetUserName(Uri uri)
    {
        var path = uri.LocalPath.Split('/');
        if (path.Length <= 0) return null;
        return path[ParserTypeWSettings.UserNameOrderInProfileLink];
    }

    public override bool CheckLink(Uri uri)
    {
        var path = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (path.Length < _mainProfilePath.Length) return true;

        for (var i = 0; i < path.Length; i++)
        {
            if (_mainProfilePath[i] != "???" && _mainProfilePath[i] != path[i])
                return false;
        }

        return true;
    }

    protected virtual List<HtmlDocument> GetGalleryPages(HtmlDocument profileDocument)
    {
        var pages = new List<HtmlDocument>();

        var uri = GetGalleryUri(profileDocument);
        if (uri == null) return pages;

        var doc = WebDownloader.GetHtmlAsync(uri).Result;
        if (doc == null) return pages;

        pages.Add(doc);

        while (TryGetNextGalleryPage(doc, out doc))
            pages.Add(doc ?? throw new InvalidOperationException(ErrorMsg));

        return pages;
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

    protected virtual bool TryGetNextGalleryPage(HtmlDocument page, out HtmlDocument? nextPage)
    {
        var next = page.DocumentNode.SelectSingleNode(ParserTypeWSettings.XpathNextPageButton);
        if (next != null && next.InnerText.Contains("Next"))
        {
            nextPage = WebDownloader.GetHtmlAsync(new Uri("https://" + Host + next.Attributes.First().Value)).Result;
            return true;
        }

        nextPage = null;
        return false;
    }
}