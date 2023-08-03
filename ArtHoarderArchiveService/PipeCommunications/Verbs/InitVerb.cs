using ArgsParser.Attributes;
using ArtHoarderArchiveService.Archive;
using ArtHoarderArchiveService.Archive.Infrastructure.Enums;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

[Verb("init")]
public class InitVerb : BaseVerb
{
    [Value(0, 0, 1)] public string? Path { get; set; }

    public override bool Validate(out List<string>? errors)
    {
        errors = null;
        return true;
    }

    public override void Invoke(IMessageWriter statusWriter, string path, CancellationToken cancellationToken)
    {
        if (Path != null)
            path = Path;

        switch (ArchiveInitialization.CreateArchive(path))
        {
            case CreationCode.Ok:
                statusWriter.Write("Archive created");
                break;
            case CreationCode.AlreadyExists:
                statusWriter.Write("Archive already exists");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
