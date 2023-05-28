using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using ThredPool;


namespace TestProject
{
    public class Tests
    {
        [Test]
        public void SimpleStealing()
        {
            using var pool = new ThredPool.ThreadPool(4);
            var task = pool.Enqueue(() => 10);
            Assert.AreEqual(10, task.Result);
        }

        [Test]
        public void SimpleSharing()
        {
            using var pool = new ThredPool.ThreadPool(4, DoSmth.Sharing);
            var task = pool.Enqueue(() => 10);
            Assert.AreEqual(10, task.Result);
        }

        [Test]
        public void WaitAllStealing()
        {
            using var pool = new ThredPool.ThreadPool(4);
            var root = pool.Enqueue(() => 0);
            const int n = 10;
            var lastTask = Enumerable.Range(0, n)
                .Aggregate(root, (t, _) => t.ContinueWith(result =>
                {
                    Thread.Sleep(1000);
                    return result + 1;
                }));
            pool.Dispose();
            Assert.AreEqual(n, lastTask.Result);
        }

        [Test]
        public void WaitAllSharing()
        {
            using var pool = new ThredPool.ThreadPool(4, DoSmth.Sharing);
            var root = pool.Enqueue(() => 0);
            const int n = 10;
            var lastTask = Enumerable.Range(0, n)
                .Aggregate(root, (t, _) => t.ContinueWith(result =>
                {
                    Thread.Sleep(1000);
                    return result + 1;
                }));
            pool.Dispose();
            Assert.AreEqual(n, lastTask.Result);
        }

        [Test]
        public void CompleteStealing()
        {
            using var pool = new ThredPool.ThreadPool(4);
            var task = pool.Enqueue(() =>
            {
                Thread.Sleep(2000);
                return 10;
            });
            Assert.False(task.Complete);
            Assert.AreEqual(10, task.Result);
            Assert.True(task.Complete);
        }

        [Test]
        public void CompleteSharing()
        {
            using var pool = new ThredPool.ThreadPool(4, DoSmth.Sharing);
            var task = pool.Enqueue(() =>
            {
                Thread.Sleep(3000);
                return 10;
            });
            Assert.False(task.Complete);
            Assert.AreEqual(10, task.Result);
            Assert.True(task.Complete);
        }

        [Test]
        public void NumbSteailng()
        {
            using var pool = new ThredPool.ThreadPool(4);
            Assert.AreEqual(4, pool.aliveProgramCount);
            pool.Dispose();
            Assert.Zero(pool.aliveProgramCount);
        }

        [Test]
        public void NumbSharing()
        {
            using var pool = new ThredPool.ThreadPool(4, DoSmth.Sharing);
            Assert.AreEqual(4, pool.aliveProgramCount);
            pool.Dispose();
            Assert.Zero(pool.aliveProgramCount);
        }

        [Test]
        public void InnerTaskStealing()
        {
            using var pool = new ThredPool.ThreadPool(4);
            var task = pool.Enqueue(() =>
            {
                var Task = pool.Enqueue(() => 5);
                return 10 + Task.Result;
            });
            Assert.AreEqual(15, task.Result);
        }

        [Test]
        public void InnerTaskSharing()
        {
            using var pool = new ThredPool.ThreadPool(4, DoSmth.Sharing);
            var task = pool.Enqueue(() =>
            {
                var Task = pool.Enqueue(() => 5);
                return 10 + Task.Result;
            });
            Assert.AreEqual(15, task.Result);
        }
    }
}
