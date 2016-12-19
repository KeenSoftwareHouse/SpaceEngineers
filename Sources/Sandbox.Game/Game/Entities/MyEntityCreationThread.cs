using Havok;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using VRage;
using VRage.Collections;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Profiler;
using VRage.Utils;

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
            public List<IMyEntity> EntityIds;
            public List<MyEntity> SubGrids;
            public List<MyObjectBuilder_EntityBase> SubgridBuilders;
        }

        MyConcurrentQueue<Item> m_jobQueue = new MyConcurrentQueue<Item>(16);
        MyConcurrentQueue<Item> m_resultQueue = new MyConcurrentQueue<Item>(16);
        AutoResetEvent m_event = new AutoResetEvent(false);
        Thread m_thread;
        bool m_exitting;

        public MyEntityCreationThread()
        {
            RuntimeHelpers.RunClassConstructor(typeof(MyEntityIdentifier).TypeHandle);
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
            MyEntityIdentifier.InitPerThreadStorage(2048);
            Item item;
            while (!m_exitting)
            {
                if (ConsumeWork(out item))
                {
                    if (item.ObjectBuilder != null)
                    {
                        if (item.Result == null)
                        {
                            item.Result = MyEntities.CreateFromObjectBuilderNoinit(item.ObjectBuilder);
                        }
                        item.Result.SentFromServer = true;
                        item.InScene = (item.ObjectBuilder.PersistentFlags & MyPersistentEntityFlags2.InScene) == MyPersistentEntityFlags2.InScene;
                        item.ObjectBuilder.PersistentFlags &= ~MyPersistentEntityFlags2.InScene;
                        item.Result.DebugAsyncLoading = true;
                        MyEntities.InitEntity(item.ObjectBuilder, ref item.Result);
                        if (item.Result != null)
                        {
                            if (item.SubgridBuilders != null)
                            {
                                item.SubGrids = new List<MyEntity>();
                                foreach (var subGridbulider in item.SubgridBuilders)
                                {
                                    MyEntity subGrid = MyEntities.CreateFromObjectBuilderNoinit(subGridbulider);
                                    subGridbulider.PersistentFlags &= ~MyPersistentEntityFlags2.InScene;
                                    item.Result.DebugAsyncLoading = true;

                                    MyEntities.InitEntity(subGridbulider, ref subGrid);
                                    item.SubGrids.Add(subGrid);
                                }
                                item.SubgridBuilders.Clear();
                                item.SubgridBuilders = null;
                            }
                         
                            item.EntityIds = new List<IMyEntity>();
                            MyEntityIdentifier.GetPerThreadEntities(item.EntityIds);
                            MyEntityIdentifier.ClearPerThreadEntities();
                            m_resultQueue.Enqueue(item);

                        }
                    }
                    else
                    {
                        if(item.Result != null)
                            item.Result.DebugAsyncLoading = true;

                        // This is ok, just invoking action asynchronously
                        m_resultQueue.Enqueue(item);
                    }
                }
                ProfilerShort.Commit();
            }
            MyEntityIdentifier.DestroyPerThreadStorage();
            HkBaseSystem.QuitThread();
            ProfilerShort.DestroyThread();
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

        public void SubmitWork(MyObjectBuilder_EntityBase objectBuilder, bool addToScene, Action<MyEntity> doneHandler, MyEntity entity,List<MyObjectBuilder_EntityBase> subGridBuliders)
        {
            SubmitWork(new Item() { ObjectBuilder = objectBuilder, AddToScene = addToScene, DoneHandler = doneHandler, Result = entity, SubgridBuilders = subGridBuliders});
        }

        public bool ConsumeResult()
        {
            Item result;
            if (m_resultQueue.TryDequeue(out result))
            {
                if (result.Result != null)
                {
                    result.Result.DebugAsyncLoading = false;
                }
                bool conflictFound = false;
                if (result.EntityIds != null)
                {
                    while (MyEntities.HasEntitiesToDelete())
                    {
                        MyEntities.DeleteRememberedEntities();
                    }
                    foreach (var id in result.EntityIds)
                    {
                        IMyEntity entity;
                        if (MyEntityIdentifier.TryGetEntity(id.EntityId,out entity))
                        {
                            MyLog.Default.WriteLine("Entity found !  id : " + id.EntityId.ToString() + " existing : " + entity.ToString() + " adding: " + id.ToString());
                            conflictFound = true;
                           // Debug.Fail("double entity add !!");
                        }                  
                    }
                    if (conflictFound == false)
                    {
                        foreach (var id in result.EntityIds)
                        {
                            MyEntityIdentifier.AddEntityWithId(id);
                        }
                    }
                    result.EntityIds.Clear();
                }

                if (conflictFound == false)
                {
                    if (result.AddToScene)
                    {
                        MyEntities.Add(result.Result, result.InScene);
                        if (result.SubGrids != null)
                        {
                            foreach (var subGrid in result.SubGrids)
                            {
                                MyEntities.Add(subGrid, result.InScene);
                            }
                            result.SubGrids.Clear();
                            result.SubGrids = null;
                        }
                    }
                    if (result.DoneHandler != null)
                    {
                        result.DoneHandler(result.Result);
                    }
                }
                else
                {
                    if (result.DoneHandler != null)
                    {
                        result.DoneHandler(null);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
