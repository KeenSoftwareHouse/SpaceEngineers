using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ParallelTasks
{
    class ForLoopWork
        : IWork
    {
        private static Pool<ForLoopWork> pool = new Pool<ForLoopWork>();

        private Action<int> action;
        private int length;
        private int stride;
        private volatile int index;

        public WorkOptions Options { get; private set; }

        public ForLoopWork()
        {
            Options = new WorkOptions() { MaximumThreads = int.MaxValue };
        }

        public void Prepare(Action<int> action, int startInclusive, int endExclusive, int stride)
        {
            this.action = action;
            this.index = startInclusive;
            this.length = endExclusive;
            this.stride = stride;
        }

        public void DoWork(WorkData workData = null)
        {
            int start;
            while ((start = IncrementIndex()) < length)
            {
                int end = Math.Min(start + stride, length);
                for (int i = start; i < end; i++)
                {
                    action(i);
                }
            }
        }

        private int IncrementIndex()
        {
#if XBOX
            int x;
            do
            {
                x = index;
            } while (Interlocked.CompareExchange(ref index, x + stride, x) != x);
            return x;
#else
#pragma warning disable 420
            // Interlocked is safe to use with volatile
            return Interlocked.Add(ref index, stride) - stride;
#pragma warning restore 420
#endif
        }

        public static ForLoopWork Get()
        {
            return pool.Get(System.Threading.Thread.CurrentThread);
        }

        public void Return()
        {
            action = null;
            pool.Return(System.Threading.Thread.CurrentThread, this);
        }
    }

    class ForEachLoopWork<T>
        : IWork
    {
        static Pool<ForEachLoopWork<T>> pool = Pool<ForEachLoopWork<T>>.Instance;

        private Action<T> action;
        private IEnumerator<T> enumerator;
        private volatile bool notDone;
        private object syncLock;

        public WorkOptions Options { get; private set; }

        public ForEachLoopWork()
        {
            Options = new WorkOptions() { MaximumThreads = int.MaxValue };
            syncLock = new object();
        }

        public void Prepare(Action<T> action, IEnumerator<T> enumerator)
        {
            this.action = action;
            this.enumerator = enumerator;
            this.notDone = true;
        }

        public void DoWork(WorkData workData = null)
        {
            T item = default(T);
            while (notDone)
            {
                bool haveValue = false;
                lock (syncLock)
                {
                    if (notDone = enumerator.MoveNext())
                    {
                        item = enumerator.Current;
                        haveValue = true;
                    }
                }

                if (haveValue)
                    action(item);
            }
        }

        public static ForEachLoopWork<T> Get()
        {
            return pool.Get(System.Threading.Thread.CurrentThread);
        }

        public void Return()
        {
            enumerator = null;
            action = null;
            pool.Return(System.Threading.Thread.CurrentThread, this);
        }
    }
}
