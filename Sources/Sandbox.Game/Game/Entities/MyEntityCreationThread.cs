using Havok;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VRage;
using VRage.Collections;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Entities
{
    public class MyEntityCreationThread : IDisposable
    {
        struct Item
        {
            public MyObjectBuilder_EntityBase ObjectBuilder;
            public bool AddToScene;
            public bool InScene;
            public MyEntity Result;
            public Action<MyEntity> DoneHandler;
        }

        MyConcurrentQueue<Item> m_jobQueue = new MyConcurrentQueue<Item>(16);
        MyConcurrentQueue<Item> m_resultQueue = new MyConcurrentQueue<Item>(16);
        AutoResetEvent m_event = new AutoResetEvent(false);
        Thread m_thread;
        bool m_exitting;

        public MyEntityCreationThread()
        {
            m_thread = new Thread(new ThreadStart(ThreadProc));
            m_thread.Start();
        }

        public void Dispose()
        {
            m_exitting = true;
            m_event.Set();
            m_thread.Join();
        }

        void ThreadProc()
        {
            Thread.CurrentThread.Name = "Entity creation thread";
            HkBaseSystem.InitThread("Entity creation thread");
            ProfilerShort.Autocommit = false;
            Item item;
            while (!m_exitting)
            {
                if (ConsumeWork(out item))
                {
                    if (item.Result == null)
                    {
                        item.Result = MyEntities.CreateFromObjectBuilderNoinit(item.ObjectBuilder);
                    }
                    item.InScene = (item.ObjectBuilder.PersistentFlags & MyPersistentEntityFlags2.InScene) == MyPersistentEntityFlags2.InScene;
                    item.ObjectBuilder.PersistentFlags &= ~MyPersistentEntityFlags2.InScene;
                    item.Result.DebugAsyncLoading = true;
                    MyEntities.InitEntity(item.ObjectBuilder, ref item.Result);
                    if (item.Result != null)
                    {
                        m_resultQueue.Enqueue(item);
                    }
                }
                ProfilerShort.Commit();
            }
            HkBaseSystem.QuitThread();
        }

        void SubmitWork(Item item)
        {
            m_jobQueue.Enqueue(item);
            m_event.Set();
        }

        bool ConsumeWork(out Item item)
        {
            if (m_jobQueue.Count == 0)
            {
                m_event.WaitOne();
            }
            return m_jobQueue.TryDequeue(out item);
        }

        public void SubmitWork(MyObjectBuilder_EntityBase objectBuilder, bool addToScene, Action<MyEntity> doneHandler, MyEntity entity = null)
        {
            SubmitWork(new Item() { ObjectBuilder = objectBuilder, AddToScene = addToScene, DoneHandler = doneHandler, Result = entity });
        }

        public bool ConsumeResult()
        {
            Item result;
            if (m_resultQueue.TryDequeue(out result))
            {
                result.Result.DebugAsyncLoading = false;
                if (result.AddToScene)
                {
                    MyEntities.Add(result.Result, result.InScene);
                }
                if (result.DoneHandler != null)
                {
                    result.DoneHandler(result.Result);
                }
                return true;
            }
            return false;
        }
    }
}
