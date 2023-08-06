using ArgsParser.Attributes;
using ArtHoarderArchiveService.Archive;
using ArtHoarderArchiveService.Archive.Infrastructure.Enums;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

[Verb("init")]
public class InitVerb : BaseVerb
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [Value(0, 0, 1)] public string? Path { get; set; }

    public override bool Validate(out List<string>? errors)
    {
        errors = null;
        return true;
    }

    public override void Invoke(IMessageWriter messageWriter, string path, CancellationToken cancellationToken)
    {
        if (Path != null)
            path = Path;

        switch (ArchiveInitialization.CreateArchive(path))
        {
            case CreationCode.Ok:
                messageWriter.Write("Archive created");
                break;
            case CreationCode.AlreadyExists:
                messageWriter.Write("Archive already exists");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
