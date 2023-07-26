using ArgsParser.Attributes;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

[Verb("init")]
public class InitVerb
{
    [Value(0, 0, 1)] public string? Path { get; set; }
}
