namespace ArtHoarderArchiveService.Archive.Parsers.Settings;

internal class ParserTypeWSettings : ParserSettings
{
    public int UserNameOrderInProfileLink { get; init; }
    public string XpathProfileIcon { get; init; }
    public string XpathProfileName { get; init; }
    public string XpathProfileCreationDataTime { get; init; }
    public string XpathProfileStatus { get; init; }
    public string XpathProfileDescription { get; init; }
    public string UriIconAttributeName { get; init; }
    public string XpathSubmissions { get; init; }
    public string XpathSubmissionPublicationTime { get; init; }
    public string XpathSubmissionFileSrc { get; init; }
    public string XpathSubmissionNextFile { get; init; }
    public string XpathSubmissionFileSrcAttribute { get; init; }
    public string XpathSubmissionTitle { get; init; }
    public string XpathSubmissionDescription { get; init; }
    public string XpathSubmissionTags { get; init; }
    public string XpathGalleryUri { get; init; }
    public string XpathNextPageButton { get; init; }
    public string XpathSubscriptions { get; init; }
    public string XpathSubscriptionsNextPage { get; init; }
    public string XpathSubscriptionsNextPageAttribute { get; init; }
    public string XpathSubscriptionsLinks { get; init; }
    public string SubmissionNextFileAttribute { get; init; }
}
