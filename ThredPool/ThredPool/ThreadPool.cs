using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThredPool
{    
    public partial class ThreadPool : IDisposable
    {
        private bool disposed;
        private readonly Random random = new();
        private readonly List<IClass> programs = new();
        private readonly object lockObj = new();
        private readonly CancellationTokenSource cRequest = new();
        private readonly ConcurrentQueue<Action> queue = new();
        private readonly AutoResetEvent threadGuard = new AutoResetEvent(false);
        private readonly ConcurrentQueue<Action<bool>> cqueue = new();           

        public ThreadPool(int numberOfThreads, DoSmth dosmth = DoSmth.Stealing)
        {
            for (var i = 0; i < numberOfThreads; i++)
            {
                IClass program = (dosmth == DoSmth.Sharing) ? new SharingP(this) : new StealingP(this);
                programs.Add(program);
            }
            programs.ForEach(program => program.Start());
        }
        public IMyTask<TResult> Enqueue<TResult>(IMyTask<TResult> task)
        {
            if (cRequest.IsCancellationRequested)
            {
                throw new OperationCanceledException("Пул потоков удален");
            }

            var threadId = Environment.CurrentManagedThreadId;

            IClass program = programs.Find(program => program.ID == threadId);

            if (program != null)
            {
                program.Enqueue(task.Work);
            }
            else
            {
                queue.Enqueue(task.Work);
            }
            return task;
        }
        public IMyTask<TResult> Enqueue<TResult>(Func<TResult> func) => Enqueue(new MyTask<TResult>(func));
        public void Dispose()
        {
            lock (lockObj)
            {
                if (disposed)
                    return;
                cRequest.Cancel();
                foreach (var program in programs) program.Join();
                cRequest?.Dispose();
                disposed = true;
            }
        }
        public int aliveProgramCount => programs.Count(program => program.Active);
        private IClass RandomProgram()
        {
            return programs[random.Next(programs.Count)];
        }

       
        public IMyTask<TResult> AddTask<TResult>(Func<TResult> funcRes)
        {
            if (this.cRequest.IsCancellationRequested)
            {
                return null;
            }

            var newTask = new MyTask<TResult>(this, funcRes);

            this.cqueue.Enqueue(newTask.ExecuteTaskManually);
            this.threadGuard.Set();

            return newTask;
        }

        private class StealingP : IClass
        {
            private readonly MyQueue<Action> queue;
            private readonly Thread thread;
            public int ID => thread.ManagedThreadId;
            public bool Active => thread.IsAlive;
            public void Start() => thread.Start();
            public void Join() => thread.Join();
            public void Enqueue(Action a) => queue.Enqueue(a);
            
            public StealingP(ThreadPool tp)
            {
                queue = new MyQueue<Action>();
                thread = new Thread(() =>
                {
                    while (!tp.cRequest.IsCancellationRequested)
                    {
                        if (queue.TryDequeueTail(out var program))
                        {
                            program();
                        }

                        if (tp.queue.TryDequeue(out var pool))
                        {
                            pool();
                        }

                        if (((StealingP)tp.RandomProgram()).queue.TryDequeueHead(out var stolen))
                        {
                            stolen();
                        }
                    }
                });
            }           
        }

        private class SharingP : IClass
        {
            private readonly MyQueue<Action> queue;
            private readonly Thread thread;
            public int ID => thread.ManagedThreadId;
            public bool Active => thread.IsAlive;
            public void Start() => thread.Start();
            public void Join() => thread.Join();
            public void Enqueue(Action a) => queue.Enqueue(a);
                        
            public SharingP(ThreadPool tp)
            {
                queue = new MyQueue<Action>();
                thread = new Thread(() =>
                {
                    while (!tp.cRequest.IsCancellationRequested)
                    {
                        if (queue.TryDequeueTail(out var program))
                        {
                            program();
                        }

                        if (tp.queue.TryDequeue(out var pool))
                        {
                            pool();
                        }

                        if (tp.random.Next(queue.Count + 1) == queue.Count)
                        {
                            SharingP target = (SharingP)tp.RandomProgram();
                            var (first, second) = ID <= target.ID ? (this, target) : (target, this);
                            lock (first.queue)
                            {
                                lock (second.queue)
                                {
                                    if (tp.cRequest.IsCancellationRequested)
                                    {
                                        return;
                                    }

                                    var (firstDeque, secondDeque) = first.queue.Count <= second.queue.Count ? (first.queue, second.queue) : (second.queue, first.queue);
                                    if (secondDeque.Count - firstDeque.Count > 5)
                                    {
                                        while (secondDeque.Count > firstDeque.Count)
                                        {
                                            if (secondDeque.TryDequeueHead(out var head))
                                            {
                                                firstDeque.Enqueue(head);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }            
        }
    }
}
