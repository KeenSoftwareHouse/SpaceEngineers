using System.Collections.Generic;
using SharpDX.Direct3D11;
using VRage.Collections;
using System.Diagnostics;
using VRage.Render11.Common;


namespace VRageRender
{
    static class MyQueryFactory
    {
        internal const int MaxFramesLag = 8;
        internal const int MaxTimestampQueries = 4096;
        internal static MyConcurrentPool<MyQuery> m_disjointQueries;
        internal static MyConcurrentPool<MyQuery> m_timestampQueries;
        
        internal static List<MyOcclusionQuery> m_pool;

        static MyQueryFactory()
        {
            m_disjointQueries = new MyConcurrentPool<MyQuery>(MaxFramesLag, true);
            m_timestampQueries = new MyConcurrentPool<MyQuery>(MaxTimestampQueries, true);
        }

        internal static MyOcclusionQuery CreateOcclusionQuery()
        {
            if (m_pool.Count > 0)
            {
                var item = m_pool[m_pool.Count - 1];
                m_pool.RemoveAt(m_pool.Count - 1);
                return item;
            }

            return new MyOcclusionQuery();
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

                var desc = new QueryDescription();
                desc.Type = type;
                m_query = new Query(MyRender11.Device, desc);
            }
        }

        public static implicit operator Query(MyQuery q)
        {
            return q.m_query;
        }
    }

    class MyOcclusionQuery: MyImmediateRC
    {
        Query m_query;

        internal MyOcclusionQuery()
        {
            var desc = new QueryDescription();
            desc.Type = QueryType.Occlusion;
            m_query = new Query(MyRender11.Device, desc);
        }

        internal void Destroy()
        {
            MyQueryFactory.m_pool.Add(this);
        }

        internal void Begin()
        {
            RC.Begin(m_query);
        }

        internal void End()
        {
            RC.End(m_query);
        }

        internal bool GetResult(out int num, bool stalling = false)
        {
            if (!stalling)
            {
                return RC.GetData(m_query, AsynchronousFlags.DoNotFlush, out num);
            }
            else
            {
                while (!RC.GetData(m_query, AsynchronousFlags.None, out num))
                {
                    System.Threading.Thread.Sleep(1);
                }
                return true;
            }
        }
    }
}
