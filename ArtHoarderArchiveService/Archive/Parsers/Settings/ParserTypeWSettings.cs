namespace ArtHoarderArchiveService.Archive.Parsers.Settings;

internal class ParserTypeWSettings : ParserSettings
{
    public int UserNameOrderInProfileLink { get; init; }
    public string XpathProfileIcon { get; init; } = null!;
    public string XpathProfileName { get; init; } = null!;
    public string XpathProfileCreationDataTime { get; init; } = null!;
    public string XpathProfileStatus { get; init; } = null!;
    public string XpathProfileDescription { get; init; } = null!;
    public string UriIconAttributeName { get; init; } = null!;
    public string XpathSubmissions { get; init; } = null!;
    public string XpathSubmissionPublicationTime { get; init; } = null!;
    public string XpathSubmissionFileSrc { get; init; } = null!;
    public string XpathSubmissionNextFile { get; init; } = null!;
    public string XpathSubmissionFileSrcAttribute { get; init; } = null!;
    public string XpathSubmissionTitle { get; init; } = null!;
    public string XpathSubmissionDescription { get; init; } = null!;
    public string XpathSubmissionTags { get; init; } = null!;
    public string XpathGalleryUri { get; init; } = null!;
    public string XpathNextPageButton { get; init; } = null!;
    public string XpathSubscriptions { get; init; } = null!;
    public string XpathSubscriptionsNextPage { get; init; } = null!;
    public string XpathSubscriptionsNextPageAttribute { get; init; } = null!;
    public string XpathSubscriptionsLinks { get; init; } = null!;
    public string SubmissionNextFileAttribute { get; init; } = null!;
}
