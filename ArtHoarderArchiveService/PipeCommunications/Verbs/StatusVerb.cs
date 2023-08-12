using ArgsParser.Attributes;
using ArtHoarderArchiveService.Archive;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

[Verb("status")]
public class StatusVerb : BaseVerb
{
    public override bool Validate(out List<string>? errors)
    {
        errors = null;
        return true;
    }

    public override void Invoke(IMessager messager, ArchiveContextFactory archiveContextFactory, string path,
        CancellationToken cancellationToken)
    {
        using var context = archiveContextFactory.CreateArchiveContext(path);
        var fileStateSet = context.GetFileStateSet();
        messager.WriteLine(fileStateSet.ToString());
    }
}
