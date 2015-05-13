#region Using Statements

using System.Collections.Generic;
using VRageMath;
using VRage.Utils;
using System;

#endregion

namespace VRageRender
{
    public enum MyOcclusionQueryID
    {
        MAIN_RENDER = 0,
        /*
        CASCADE_1 = 1,
        CASCADE_2 = 2,
        CASCADE_3 = 3,
        CASCADE_4 = 4,*/
    }

    public enum MyOcclusionQueryRenderType
    {
        HWDepth,
        CustomDepth
    }

    internal class MyOcclusionQueryIssue
    {
        public MyOcclusionQueryIssue(MyCullableRenderObject cullObject)
        {
            CullObject = cullObject;
        }

        public MyCullableRenderObject CullObject { get; private set; }
        public MyOcclusionQuery OcclusionQuery { get; set; }
        public bool OcclusionQueryVisible { get; set; }
        public bool OcclusionQueryIssued { get; set; }
        public MyOcclusionQueryRenderType RenderType { get; set; }

    }

    internal class MyCullableRenderObject : MyRenderObject
    {
        MyOcclusionQueryIssue[] m_queries = new MyOcclusionQueryIssue[Enum.GetValues(typeof(MyOcclusionQueryID)).Length];
        
        public int EntitiesContained { get; set; }

        public MyDynamicAABBTreeD CulledObjects { get; private set; }

        protected MyCullableRenderObject(uint id, string debugName)
            : base(id, debugName) 
        {
            CulledObjects = new MyDynamicAABBTreeD(MyRender.PrunningExtension);
            EntitiesContained = 0;

            for (int i = 0; i < Enum.GetValues(typeof(MyOcclusionQueryID)).Length; i++)
            {
                m_queries[i] = new MyOcclusionQueryIssue(this);
                m_queries[i].RenderType = MyOcclusionQueryRenderType.HWDepth;
            }

            m_queries[(int)MyOcclusionQueryID.MAIN_RENDER].RenderType = MyOcclusionQueryRenderType.CustomDepth;
        }

        protected MyCullableRenderObject(uint id)
            : this(id, "CullObject")
        {
        }

        public MyCullableRenderObject(uint id, BoundingBoxD aabb)
            : this(id)
        {
            m_aabb = aabb;
        }

        public override void UpdateWorldAABB()
        {
            base.UpdateWorldAABB();
        }

        public MyOcclusionQueryIssue GetQuery(MyOcclusionQueryID id)
        {
            return m_queries[(int)id];
        }

        public override void LoadContent()
        {
            base.LoadContent();

            for (int i = 0; i < Enum.GetValues(typeof(MyOcclusionQueryID)).Length; i++)
            {
                MyOcclusionQuery occlusionQuery = MyOcclusionQueries.Get();

                m_queries[i].OcclusionQueryIssued = false;
                m_queries[i].OcclusionQueryVisible = true;

                System.Diagnostics.Debug.Assert(m_queries[i].OcclusionQuery == null);
                m_queries[i].OcclusionQuery = occlusionQuery;
            }
        }

        public override void UnloadContent()
        {
            for (int i = 0; i < Enum.GetValues(typeof(MyOcclusionQueryID)).Length; i++)
            {
                if (m_queries[i].OcclusionQuery != null)
                {
                    m_queries[i].OcclusionQueryIssued = false;
                    MyOcclusionQueries.Return(m_queries[i].OcclusionQuery);
                    m_queries[i].OcclusionQuery = null;
                }
            }

            base.UnloadContent();
        }
    }
}
