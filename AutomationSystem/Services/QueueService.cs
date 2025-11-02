using System.Collections.Generic;
using System.Threading;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Task execution queue for managing arena-related tasks.
    /// </summary>
    public class QueueService
    {
        private readonly Queue<ArenaTask> taskQueue = new Queue<ArenaTask>();
        private readonly Thread workerThread;
        private bool isRunning = true;

        public QueueService()
        {
            workerThread = new Thread(ProcessQueue);
            workerThread.Start();
        }

        /// <summary>
        /// Adds a task to the queue.
        /// </summary>
        public void EnqueueTask(ArenaTask task)
        {
            lock (taskQueue)
            {
                taskQueue.Enqueue(task);
                Monitor.Pulse(taskQueue);
            }
        }

        private void ProcessQueue()
        {
            while (isRunning)
            {
                ArenaTask task = null;
                lock (taskQueue)
                {
                    if (taskQueue.Count > 0)
                    {
                        task = taskQueue.Dequeue();
                    }
                    else
                    {
                        Monitor.Wait(taskQueue);
                    }
                }
                if (task != null)
                {
                    task.Execute();
                }
            }
        }

        public void Stop()
        {
            isRunning = false;
            lock (taskQueue)
            {
                Monitor.Pulse(taskQueue);
            }
            workerThread.Join();
        }
    }

    public abstract class ArenaTask
    {
        public abstract void Execute();
    }
}
