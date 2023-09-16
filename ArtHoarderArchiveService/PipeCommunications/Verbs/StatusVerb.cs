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
        var context = archiveContextFactory.CreateArchiveContext(messager, path, this);
        var fileStateSet = context.GetFileStateSet();
        archiveContextFactory.RealiseContext(path, this);
        messager.WriteMessage(fileStateSet.ToString());
    }

    private void PrintFullArchiveStatus()
    {
    }
}
