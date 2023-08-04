namespace ArtHoarderArchiveService
{
    public class TaskManager : ITaskManager
    {
        private readonly ILogger<TaskManager> _logger;
        private readonly List<ArtHoarderTask> _runningTasks = new(8);
        private readonly object _runningTasksSyncRoot = new();
        private readonly Queue<ArtHoarderTask> _tasksQueue = new(8);
        private readonly object _tasksQueueSyncRoot = new();

        public TaskManager(ILogger<TaskManager> logger)
        {
            _logger = logger;
        }

        public void StartParallelTask(ArtHoarderTask artHoarderTask)
        {
            artHoarderTask.Start().ContinueWith(_ => FinalizeTask(artHoarderTask)).ConfigureAwait(false);
            lock (_runningTasksSyncRoot)
            {
                _runningTasks.Add(artHoarderTask);
            }
        }

        public void EnqueueTask(ArtHoarderTask artHoarderTask)
        {
            lock (_tasksQueueSyncRoot)
                _tasksQueue.Enqueue(artHoarderTask);
            UpdateQueueStatus();
        }

        private void UpdateQueueStatus()
        {
            lock (_runningTasksSyncRoot)
            {
                lock (_tasksQueueSyncRoot)
                {
                    if (_runningTasks.Count != 0 || _tasksQueue.Count <= 0) return;

                    var t = _tasksQueue.Dequeue();
                    t.Start().ContinueWith(_ => FinalizeTask(t)).ConfigureAwait(false);
                    _runningTasks.Add(t);
                }
            }
        }

        private void FinalizeTask(ArtHoarderTask artHoarderTask)
        {
            lock (_runningTasksSyncRoot)
            {
                if (!_runningTasks.Remove(artHoarderTask))
                {
                    _logger.LogWarning("List of running tasks does not contain task: {Task}.",
                        artHoarderTask.ToString());
                }
            }
        }
    }
}
