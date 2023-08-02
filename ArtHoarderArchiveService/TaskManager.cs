namespace ArtHoarderArchiveService
{
    public class TaskManager : ITaskManager
    {
        private readonly ILogger<TaskManager> _logger;
        private readonly List<ArtHoarderTask> _runningTasks = new(8);
        private readonly Queue<ArtHoarderTask> _tasksQueue = new(8);

        public TaskManager(ILogger<TaskManager> logger)
        {
            _logger = logger;
        }

        public void StartParallelTask(ArtHoarderTask artHoarderTask, CancellationToken cancellationToken)
        {
            artHoarderTask.Start(FinalizeTask, cancellationToken);
            _runningTasks.Add(artHoarderTask);
        }

        public void EnqueueTask(ArtHoarderTask artHoarderTask)
        {
            _tasksQueue.Enqueue(artHoarderTask);
            UpdateQueueStatus();
        }

        private void UpdateQueueStatus()
        {
            if (_runningTasks.Count == 0 && _tasksQueue.Count > 0)
            {
                var t = _tasksQueue.Dequeue();
                t.Start(FinalizeTask);
                _runningTasks.Add(t);
            }
        }

        private void FinalizeTask(ArtHoarderTask artHoarderTask)
        {
            if (!_runningTasks.Remove(artHoarderTask))
            {
                _logger.LogWarning("List of running tasks does not contain task: {Task}.", artHoarderTask.ToString());
            }
        }
    }
}
