using System;

namespace ThredPool
{
    internal static class Program
    {
        private static void Main()
        {
            const int taskCount = 10;
            const int allowPool = 4;

            var pool = new ThredPool.ThreadPool(allowPool);

            var tasks = new ThredPool.IMyTask<int>[taskCount];
            for (int i = 0; i < tasks.Length; ++i)
            {
                tasks[i] = pool.AddTask(ReturnF);
            }

            Console.WriteLine("First ({0})", taskCount);
            foreach (var task in tasks)
            {
                Console.WriteLine(task.Result);
            }

            var nestedTasks = new ThredPool.IMyTask<int>[taskCount];
            for (int i = 0; i < tasks.Length; ++i)
            {
                nestedTasks[i] = tasks[i].ContinueWith(ReturnS);
            }

            Console.WriteLine("Second ({0})", taskCount);
            foreach (var task in nestedTasks)
            {
                Console.WriteLine(task.Result);
            }            
        }

        public static int ReturnF()
        {
            return 20;
        }

        public static int ReturnS(int sValue)
        {
            return sValue + 50;
        }
    }
    
}
