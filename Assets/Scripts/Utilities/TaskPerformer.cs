using System;
using System.Threading.Tasks;

namespace DockerIrl.Utilities
{
    /// <summary>
    /// Performs a task once a time.
    /// Limits task execution to one at a time.
    /// Forbids execution of same task multiple times at the same time.
    /// Please tell me u understood.
    /// </summary>
    public class TaskPerformer
    {
        private readonly Func<Task> taskFactory;
        private Task runningTask;

        public TaskPerformer(Func<Task> taskFactory)
        {
            this.taskFactory = taskFactory;
        }

        public Task Call()
        {
            if (runningTask != null)
            {
                return runningTask;
            }

            runningTask = taskFactory.Invoke();

            runningTask.ContinueWith((t) =>
            {
                runningTask = null;
            });

            return runningTask;
        }
    }
}