using ArgsParser.Attributes;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

public abstract class BaseVerb
{
    [Option('p', "parallel-run", HelpText = "Run this task as parallel")]
    public bool IsParallel { get; set; }

    public abstract bool Validate(out List<string>? errors);
    public abstract void Invoke(IMessageWriter messageWriter, string path, CancellationToken cancellationToken);
}
