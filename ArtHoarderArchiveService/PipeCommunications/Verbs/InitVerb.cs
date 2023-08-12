using ArgsParser.Attributes;
using ArtHoarderArchiveService.Archive;
using ArtHoarderArchiveService.Archive.Infrastructure.Enums;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

[Verb("init")]
public class InitVerb : BaseVerb
{
    public InitVerb()
    {
        IsParallel = true;
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [Value(0, 0, 1)] public string? Path { get; set; }

    public override bool Validate(out List<string>? errors)
    {
        errors = null;
        return true;
    }

    public override void Invoke(IMessager messager, ArchiveContextFactory archiveContextFactory, string path,
        CancellationToken cancellationToken)
    {
        if (Path != null)
            path = Path;

        switch (ArchiveInitialization.CreateArchive(path))
        {
            case CreationCode.Ok:
                messager.WriteLine("Archive created");
                break;
            case CreationCode.AlreadyExists:
                messager.WriteLine("Archive already exists");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
