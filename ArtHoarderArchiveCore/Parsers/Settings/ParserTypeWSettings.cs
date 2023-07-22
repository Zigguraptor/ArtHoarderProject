namespace ArtHoarderCore.Parsers.Settings;

internal class ParserTypeWSettings : IParserSettings
{
    public string ParserType { get; init; } = "W";
    public string Host { get; init; }

    private readonly Dictionary<string, string> _settings;

    public ParserTypeWSettings(string host, Dictionary<string, string> settings)
    {
        Host = host;
        _settings = settings;
    }

    public int UserNameOrderInProfileLink => int.Parse(_settings["UserNameOrderInProfileLink"]);
    public string XpathProfileIcon => _settings["XpathProfileIcon"];
    public string XpathProfileName => _settings["XpathProfileName"];
    public string XpathProfileCreationDataTime => _settings["XpathProfileCreationDataTime"];
    public string XpathProfileStatus => _settings["XpathProfileStatus"];
    public string XpathProfileDescription => _settings["XpathProfileDescription"];
    public string UriIconAttributeName => _settings["UriIconAttributeName"];
    public string XpathSubmissions => _settings["XpathSubmissions"];
    public string XpathSubmissionPublicationTime => _settings["XpathSubmissionPublicationTime"];
    public string XpathSubmissionFileSrc => _settings["XpathSubmissionFileSrc"];
    public string XpathSubmissionNextFile => _settings["XpathSubmissionNextFile"];
    public string XpathSubmissionFileSrcAttribute => _settings["XpathSubmissionFileSrcAttribute"];
    public string XpathSubmissionTitle => _settings["XpathSubmissionTitle"];
    public string XpathSubmissionDescription => _settings["XpathSubmissionDescription"];
    public string XpathSubmissionTags => _settings["XpathSubmissionTags"];
    public string XpathGalleryUri => _settings["XpathGalleryUri"];
    public string XpathNextPageButton => _settings["XpathNextPageButton"];
    public string XpathSubscriptions => _settings["XpathSubscriptions"];
    public string XpathSubscriptionsNextPage => _settings["XpathSubscriptionsNextPage"];
    public string XpathSubscriptionsNextPageAttribute => _settings["XpathSubscriptionsNextPageAttribute"];
    public string XpathSubscriptionsLinks => _settings["XpathSubscriptionsLinks"];
    public string SubmissionNextFileAttribute => _settings["SubmissionNextFileAttribute"];
}