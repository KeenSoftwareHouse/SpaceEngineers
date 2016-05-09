#region Using Statements

using System.Collections.Generic;
using VRageMath;

#endregion

namespace VRageRender
{

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// element flags
    /// </summary>
    [System.Flags]
    public enum MyElementFlag
    {
        EF_AABB_DIRTY = 1 << 1,
        EF_SENSOR_ELEMENT = 1 << 2,
        EF_RB_ELEMENT = 1 << 3,
        EF_MODEL_PREFER_LOD0 = 1 << 4,
    }

    /// <summary>
    /// Element used as a base class for the prunning structure. 
    /// </summary>
    abstract class MyElement
    {
        public BoundingBoxD WorldAABB
        {
            get
            {
                if ((Flags & MyElementFlag.EF_AABB_DIRTY) > 0)
                {
                    UpdateWorldAABB();
                }

                return m_aabb;
            }
        }


        public MyElementFlag Flags;

        /// <summary>
        /// Update of aabb if necessary, implementation in shape elements
        /// </summary>
        public virtual void UpdateWorldAABB()
        {
            Flags &= ~MyElementFlag.EF_AABB_DIRTY;
        }

        public uint ID { get { return m_id; } set { m_id = value; } }

        protected BoundingBoxD m_aabb;
        private int m_proxyData;
        private int m_shadowProxyData;
        private uint m_id;
        public static int PROXY_UNASSIGNED = int.MaxValue;

        public MyElement(uint id)
        {
            m_id = id;
            
            Flags = MyElementFlag.EF_AABB_DIRTY;
            m_aabb = new BoundingBoxD();        
            m_proxyData = PROXY_UNASSIGNED;
            m_shadowProxyData = PROXY_UNASSIGNED;
        }

        public int ProxyData { get { return m_proxyData; } set { m_proxyData = value; } }
        public int ShadowProxyData { get { return m_shadowProxyData; } set { m_shadowProxyData = value; } }
    }
}
