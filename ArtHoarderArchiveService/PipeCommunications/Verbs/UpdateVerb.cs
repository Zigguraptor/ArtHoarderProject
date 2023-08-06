using ArgsParser.Attributes;
using ArtHoarderArchiveService.Archive;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

[Verb("update", HelpText = "update")]
public class UpdateVerb : BaseVerb
{
    [Option('a', "all")] public bool All { get; set; }
    [Option('o', "include-old")] public bool OldIncluded { get; set; }
    [Option('u', "user", 1)] public List<string>? Users { get; set; }
    [Option('g', "gallery", 1)] public List<string>? GalleryUris { get; set; }

    public override bool Validate(out List<string>? errors)
    {
        errors = null;
        if (All) return true;
        return Users != null || GalleryUris != null;
    }

    public override void Invoke(IMessageWriter messageWriter, string path, CancellationToken cancellationToken)
    {
        using var context = new ArchiveContext(path);
        context.UpdateAllGalleriesAsync(messageWriter, OldIncluded, cancellationToken).ConfigureAwait(false);
    }
}
