using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public abstract class MyPrecalcJob
    {
        public readonly Action OnCompleteDelegate;

        /// <summary>
        /// Invalid tasks finishes normally and restarts afterwards. Even if results
        /// are not valid, they may still be useful.
        /// </summary>
        public bool IsValid;

        public virtual bool IsCanceled { get { return false; } }

        protected MyPrecalcJob(bool enableCompletionCallback)
        {
            MyPrecalcComponent.AssertUpdateThread();
            if (enableCompletionCallback)
                OnCompleteDelegate = OnComplete;
        }

        public abstract void DoWork();

        public abstract void Cancel();

        protected virtual void OnComplete()
        {
            MyPrecalcComponent.AssertUpdateThread();
        }

        public WorkOptions Options
        {
            get { return Parallel.DefaultOptions; }
        }

        public virtual int Priority { get { return 0; } }

        public virtual void DebugDraw(Color c) { }
    }

    public class MyWorkTracker<TWorkId, TWork> : IEnumerable<KeyValuePair<TWorkId, TWork>>
        where TWork : MyPrecalcJob
    {
        private readonly Dictionary<TWorkId, TWork> m_worksById;

        public MyWorkTracker(IEqualityComparer<TWorkId> comparer = null)
        {
            MyPrecalcComponent.AssertUpdateThread();

            m_worksById = new Dictionary<TWorkId, TWork>(comparer ?? EqualityComparer<TWorkId>.Default);
        }

        public void Add(TWorkId id, TWork work)
        {
            MyPrecalcComponent.AssertUpdateThread();

            work.IsValid = true;
            m_worksById.Add(id, work);
        }

        public void Invalidate(TWorkId id)
        {
            MyPrecalcComponent.AssertUpdateThread();

            TWork work;
            if (m_worksById.TryGetValue(id, out work))
            {
                work.IsValid = false;
            }
        }

        public void InvalidateAll()
        {
            MyPrecalcComponent.AssertUpdateThread();

            foreach (var work in m_worksById.Values)
            {
                work.IsValid = false;
            }
        }

        public void CancelAll()
        {
            MyPrecalcComponent.AssertUpdateThread();

            foreach (var work in m_worksById.Values)
            {
                work.Cancel();
            }
            m_worksById.Clear();
        }

        public void Cancel(TWorkId id)
        {
            MyPrecalcComponent.AssertUpdateThread();

            TWork work;
            if (m_worksById.TryGetValue(id, out work))
            {
                work.Cancel();
                m_worksById.Remove(id);
            }
        }

        public bool Exists(TWorkId id)
        {
            MyPrecalcComponent.AssertUpdateThread();

            return m_worksById.ContainsKey(id);
        }

        public bool TryGet(TWorkId id, out TWork work)
        {
            MyPrecalcComponent.AssertUpdateThread();

            return m_worksById.TryGetValue(id, out work);
        }

        public void Complete(TWorkId id)
        {
            MyPrecalcComponent.AssertUpdateThread();

            m_worksById.Remove(id);
        }

        public Dictionary<TWorkId, TWork>.Enumerator GetEnumerator()
        {
            return m_worksById.GetEnumerator();
        }

        IEnumerator<KeyValuePair<TWorkId, TWork>> IEnumerable<KeyValuePair<TWorkId, TWork>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
