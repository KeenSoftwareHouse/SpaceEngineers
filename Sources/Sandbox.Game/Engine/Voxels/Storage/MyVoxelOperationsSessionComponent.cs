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

        private int m_waitForFlush = 0;
        private int m_waitForWrite = 0;

        public MyVoxelOperationsSessionComponent()
        {
            m_flushCachesCallback = FlushCaches;
            m_writePendingCallback = WritePending;
        }

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
            if (m_storagesWithCache.Count != m_scheduledCount)
            {
                m_waitForWrite++;
                if (m_waitForWrite > 10)
                    m_waitForWrite = 0;

                m_waitForFlush++;
                if (m_waitForFlush >= WaitForLazy && ShouldFlush)
                {
                    m_waitForFlush = 0;
                    foreach (var storage in m_storagesWithCache)
                    {
                        if (!storage.Scheduled && storage.Storage.HasCachedChunks)
                        {
                            Interlocked.Increment(ref m_scheduledCount);
                            storage.Scheduled = true;
                            Parallel.Start(m_flushCachesCallback, null, storage);
                        }
                    }
                }
                else if (m_waitForWrite == 0 && ShouldWrite)
                {
                    foreach (var storage in m_storagesWithCache)
                    {
                        if (!storage.Scheduled && storage.Storage.HasPendingWrites)
                        {
                            Interlocked.Increment(ref m_scheduledCount);
                            storage.Scheduled = true;
                            Parallel.Start(m_writePendingCallback, null, storage);
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

        private Action<WorkData> m_flushCachesCallback;
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
