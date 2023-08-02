namespace ArtHoarderArchiveService;

public interface ITaskManager
{
    public void EnqueueTask(ArtHoarderTask artHoarderTask);
}
