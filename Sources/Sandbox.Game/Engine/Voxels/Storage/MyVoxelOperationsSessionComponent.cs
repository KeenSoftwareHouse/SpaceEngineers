using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ParallelTasks;
using VRage.Collections;
using VRage.Game.Components;

namespace Sandbox.Engine.Voxels.Storage
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyVoxelOperationsSessionComponent : MySessionComponentBase
    {
        private const int WaitForLazy = 300;

        public static MyVoxelOperationsSessionComponent Static;

        public bool ShouldWrite = true;
        public bool ShouldFlush = true;

        public static bool EnableCache = true;

        private volatile int m_scheduledCount = 0;

        private int m_wait = 0;

        public override void BeforeStart()
        {
            Static = this;
        }

        public override bool IsRequiredByGame
        {
            // Disabled for now till functional.
            get { return true; }
        }

        private readonly MyConcurrentHashSet<StorageData> m_storagesWithCache = new MyConcurrentHashSet<StorageData>();

        private class StorageData : WorkData, IEquatable<StorageData>
        {
            public StorageData(MyStorageBase storage)
            {
                Storage = storage;
            }

            public readonly MyStorageBase Storage;

            public bool Scheduled;

            public bool Equals(StorageData other)
            {
                return Storage == other.Storage;
            }

            public override int GetHashCode()
            {
                return Storage.GetHashCode();
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (ShouldFlush && m_storagesWithCache.Count != m_scheduledCount)
            {
                m_wait++;
                if (m_wait >= WaitForLazy && ShouldFlush)
                {
                    m_wait = 0;
                    foreach (var storage in m_storagesWithCache)
                    {
                        if (!storage.Scheduled)
                        {
                            Interlocked.Increment(ref m_scheduledCount);
                            storage.Scheduled = true;
                            Parallel.Start(FlushCaches, null, storage);
                        }
                    }
                }
                else if (ShouldWrite)
                {
                    foreach (var storage in m_storagesWithCache)
                    {
                        if (!storage.Scheduled)
                        {
                            Interlocked.Increment(ref m_scheduledCount);
                            storage.Scheduled = true;
                            Parallel.Start(WritePending, null, storage);
                        }
                    }
                }
            }
        }

        public void Add(MyStorageBase storage)
        {
            StorageData sdata = new StorageData(storage);

            m_storagesWithCache.Add(sdata);
        }

        private Action<WorkData> m_writePendingCallback;
        public void WritePending(WorkData data)
        {
            var storageData = (StorageData)data;

            storageData.Storage.WritePending();

            storageData.Scheduled = false;
            Interlocked.Decrement(ref m_scheduledCount);
        }

        public void FlushCaches(WorkData data)
        {
            var storageData = (StorageData)data;

            storageData.Storage.CleanCachedChunks();

            storageData.Scheduled = false;
            Interlocked.Decrement(ref m_scheduledCount);
        }

        public IEnumerable<MyStorageBase> QueuedStorages
        {
            get { return m_storagesWithCache.Select(x => x.Storage); }
        }

        public void Remove(MyStorageBase storage)
        {
            // this works because magic
            m_storagesWithCache.Remove(new StorageData(storage));
        }
    }
}
