namespace ArtHoarderArchiveService.PipeCommunications;

public interface ICommandsParser
{
    public ArtHoarderTask ParsCommand(string command);
}
