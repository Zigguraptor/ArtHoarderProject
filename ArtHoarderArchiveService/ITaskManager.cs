namespace ArtHoarderArchiveService;

public interface ITaskManager
{
    public void StartParallelTask(Task task, CancellationTokenSource tokenSource);
    public void EnqueueTask(Task task, CancellationTokenSource tokenSource);
}
