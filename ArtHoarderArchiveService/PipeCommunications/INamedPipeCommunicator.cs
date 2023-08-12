namespace ArtHoarderArchiveService.PipeCommunications;

public interface INamedPipeCommunicator
{
    public Task StartCommunicationAsync(CancellationToken cancellationToken);
}
