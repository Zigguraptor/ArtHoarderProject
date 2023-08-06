namespace ArtHoarderArchiveService
{
    public class TaskManager : ITaskManager
    {
        private readonly ILogger<TaskManager> _logger;


        private readonly object _syncRoot = new();
        private readonly SortedDictionary<Task, CancellationTokenSource> _runningTasks = new();
        private readonly Queue<(Task task, CancellationTokenSource tokenSource)> _tasksQueue = new(8);

        public TaskManager(ILogger<TaskManager> logger)
        {
            _logger = logger;
        }

        public void StartParallelTask(Task task, CancellationTokenSource tokenSource)
        {
            lock (_syncRoot)
                RunTask(task, tokenSource);
        }

        public void EnqueueTask(Task task, CancellationTokenSource tokenSource)
        {
            lock (_syncRoot)
            {
                if (_tasksQueue.Count > 0)
                {
                    _tasksQueue.Enqueue((task, tokenSource));
                    return;
                }

                if (_runningTasks.Count > 0)
                {
                    _tasksQueue.Enqueue((task, tokenSource));
                    return;
                }

                RunTask(task, tokenSource);
            }
        }

        private void RunTask(Task task, CancellationTokenSource tokenSource)
        {
            _runningTasks.Add(task, tokenSource);
            task.ContinueWith(FinalizeTask).ConfigureAwait(false);
            task.Start();
        }

        private void FinalizeTask(Task task)
        {
            lock (_syncRoot)
            {
                _runningTasks.Remove(task);
                if (_tasksQueue.Count <= 0) return;

                var valueTuple = _tasksQueue.Dequeue();
                RunTask(valueTuple.task, valueTuple.tokenSource);
            }
        }
    }
}
