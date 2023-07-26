using ArgsParser.Attributes;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

[Verb("update", HelpText = "update")]
public class UpdateVerb
{
    [Option('a', "all")] public bool All { get; set; }
    [Option('u', "user", 1)] public List<string>? Users { get; set; }
    [Option('g', "gallery", 1)] public List<string>? GalleryUris { get; set; }
}
