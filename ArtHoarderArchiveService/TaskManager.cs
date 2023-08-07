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

        public Task StartParallelTask(Task task, CancellationTokenSource tokenSource)
        {
            lock (_syncRoot)
                return RunTask(task, tokenSource);
        }

        public Task EnqueueTask(Task task, CancellationTokenSource tokenSource)
        {
            lock (_syncRoot)
            {
                if (_tasksQueue.Count > 0)
                {
                    _tasksQueue.Enqueue((task, tokenSource));
                    return task;
                }

                if (_runningTasks.Count > 0)
                {
                    _tasksQueue.Enqueue((task, tokenSource));
                    return task;
                }

                return RunTask(task, tokenSource);
            }
        }

        private Task RunTask(Task task, CancellationTokenSource tokenSource)
        {
            _runningTasks.Add(task, tokenSource);
            task.ContinueWith(FinalizeTask);
            task.Start();
            return task;
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
