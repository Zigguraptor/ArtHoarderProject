using ArgsParser.Attributes;
using ArtHoarderArchiveService.Archive;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

public abstract class BaseVerb
{
    [Option('p', "parallel-run", HelpText = "Run this task as parallel")]
    public bool IsParallel { get; set; }

    public abstract bool Validate(out List<string>? errors);

    public abstract void Invoke(IMessager messager, ArchiveContextFactory archiveContextFactory, string path,
        CancellationToken cancellationToken);
}
