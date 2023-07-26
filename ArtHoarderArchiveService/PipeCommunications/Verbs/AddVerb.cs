using ArgsParser.Attributes;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

[Verb("add", HelpText = "Add local user name or gallery profile to archive")]
public class AddVerb
{
    [Group("target", true)]
    [Option('u', "user", 1, 1, HelpText = "Add user")]
    public string? UserName { get; set; }

    [Group("target", true)]
    [Option('g', "gallery", 2, 2, HelpText = "Add gallery")]
    public List<string>? Gallery { get; set; }

    [Value(0, 0, 2)] public List<string>? Values { get; set; }
}
