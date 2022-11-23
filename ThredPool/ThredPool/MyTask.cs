using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThredPool
{
    public class MyTask<TResult> : IMyTask<TResult>
    {
        private ThreadPool myPool;        
        private volatile bool complete;
        private TResult result;
        private readonly Func<TResult> funcRes;          
        private readonly List<Exception> exceptions = new();        
        private readonly object lockObj = new();
        private Exception newException = null;
        private ManualResetEvent manualResetE = new ManualResetEvent(false);
        public MyTask(ThreadPool myPool, Func<TResult> funcRes)
        {
            this.myPool = myPool;
            this.funcRes = funcRes;
        }
        public bool IsCompleted { get; private set; } = false;
        public MyTask(Func<TResult> func) => funcRes = func;
        public bool Complete => complete;
               

        public TResult Result
        {
            get
            {
                Work();
                if (exceptions.Count > 0)
                {
                    throw new AggregateException(exceptions);
                }
                return result;
            }
        }

        public void Work()
        {
            if (complete) return;
            lock (lockObj)
            {
                if (complete) return;
                try
                {
                    result = funcRes.Invoke();
                }
                catch (AggregateException aggexcept)
                {
                    exceptions.AddRange(aggexcept.InnerExceptions);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                finally
                {
                    complete = true;
                }
            }
        }
        public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> func) => new MyTask<TNewResult>(() => func(Result));

        public void ExecuteTaskManually(bool preventExecution = false)
        {
            if (!preventExecution)
            {
                try
                {
                    this.result = this.funcRes();
                }
                catch (Exception e)
                {
                    this.newException = e;
                }
            }
            else
            {
                this.newException =
                    new AggregateException("Task execution cancelled!");
            }

            this.IsCompleted = true;
            this.manualResetE.Set();
        }
    }
}
