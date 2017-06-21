using System.Collections.Generic;
using SharpDX.Direct3D11;
using VRage.Collections;
using System.Diagnostics;
using System.Management.Instrumentation;
using VRage.Render11.Common;


namespace VRageRender
{
    internal static class MyQueryFactory
    {
        internal const int MAX_FRAMES_LAG = 8;
        private const int MAX_TIMESTAMP_QUERIES = 4096;
        private static readonly MyConcurrentPool<MyQuery> m_disjointQueries;
        private static readonly MyConcurrentPool<MyQuery> m_timestampQueries;

        private static readonly List<MyOcclusionQuery> m_pool = new List<MyOcclusionQuery>();

        static MyQueryFactory()
        {
            m_disjointQueries = new MyConcurrentPool<MyQuery>(MAX_FRAMES_LAG, true);
            m_timestampQueries = new MyConcurrentPool<MyQuery>(MAX_TIMESTAMP_QUERIES, true);
        }

        internal static MyOcclusionQuery CreateOcclusionQuery(string debugName)
        {
            if (m_pool.Count > 0)
            {
                var item = m_pool[m_pool.Count - 1];
                m_pool.RemoveAt(m_pool.Count - 1);
                item.DebugName = debugName;
                return item;
            }

            return new MyOcclusionQuery(debugName);
        }

        internal static void RelaseOcclusionQuery(MyOcclusionQuery q)
        {
            m_pool.Add(q);
        }

        internal static MyQuery CreateTimestampQuery()
        {
            var q = m_timestampQueries.Get();
            q.LazyInit(QueryType.Timestamp);
            return q;
        }

        internal static void RelaseTimestampQuery(MyQuery q)
        {
            m_timestampQueries.Return(q);
        }

        internal static MyQuery CreateDisjointQuery()
        {
            var q = m_disjointQueries.Get();
            q.LazyInit(QueryType.TimestampDisjoint);
            return q;
        }

        internal static void RelaseDisjointQuery(MyQuery q)
        {
            m_disjointQueries.Return(q);
        }

    }

    class MyQuery
    {
        internal Query m_query;
        QueryType ? m_type;

        internal void LazyInit(QueryType type)
        {
            if(m_query == null)
            {
                Debug.Assert(!m_type.HasValue);

                m_type = type;

                var desc = new QueryDescription {Type = type};
                m_query = new Query(MyRender11.Device, desc);
            }
        }

        public static implicit operator Query(MyQuery q)
        {
            return q.m_query;
        }
    }

    internal class MyOcclusionQuery: MyImmediateRC
    {
        private readonly Query m_query;
        internal bool Running { get; private set; }
        private bool Ignore;

        internal string DebugName { set { m_query.DebugName = value; } }

        internal MyOcclusionQuery(string debugName)
        {
            var desc = new QueryDescription {Type = QueryType.Occlusion};
            m_query = new Query(MyRender11.Device, desc) {DebugName = debugName};
            Running = false;
        }

        internal void Destroy()
        {
            Ignore = Running;
            MyQueryFactory.RelaseOcclusionQuery(this);
        }

        internal void Begin()
        {
            if (!Ignore)
            {
                RC.Begin(m_query);
                Running = true;
            }
        }

        internal void End()
        {
            if (!Ignore)
                RC.End(m_query);
        }

        internal long GetResult(bool stalling = false)
        {
            long num;
            if (!stalling)
            {
                if (!RC.GetData(m_query, AsynchronousFlags.DoNotFlush, out num))
                    num = -1;
            }
            else
            {
                while (!RC.GetData(m_query, AsynchronousFlags.None, out num))
                {
                    System.Threading.Thread.Sleep(1);
                }
            }
            Running = Ignore = false;
            return num;
        }
    }
}
