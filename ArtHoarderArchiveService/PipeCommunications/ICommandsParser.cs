using ArtHoarderArchiveService.PipeCommunications.Verbs;

namespace ArtHoarderArchiveService.PipeCommunications;

public interface ICommandsParser
{
    public (string path, BaseVerb verb) ParsCommand(string command);
}
