namespace ArtHoarderArchiveService;

public interface ITaskManager
{
    public Task StartParallelTask(Task task, CancellationTokenSource tokenSource);
    public Task EnqueueTask(Task task, CancellationTokenSource tokenSource);
}
