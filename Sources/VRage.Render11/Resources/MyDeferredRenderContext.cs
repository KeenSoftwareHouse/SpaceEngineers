using System.Collections.Generic;
using VRage.Collections;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRageRender;

namespace VRage.Render11.Resources
{
    internal class MyDeferredRenderContextManager: IManager, IManagerDevice
    {
        static int m_poolSize;

        MyConcurrentPool<MyRenderContext> m_pool;
        bool m_isDeviceInit = false;
        static List<MyRenderContext> m_tmpList = new List<MyRenderContext>();

        static MyDeferredRenderContextManager()
        {
            int processorCount = VRage.Library.MyEnvironment.ProcessorCount;
            m_poolSize = processorCount > 8 ? processorCount : 8;
        }

        public MyRenderContext AcquireRC()
        {
            MyRenderProxy.Assert(m_isDeviceInit);
            var rc = m_pool.Get();
            rc.ClearState();
            return rc;
        }

        public void FreeRC(MyRenderContext rc)
        {
            MyRenderProxy.Assert(m_isDeviceInit);
            m_pool.Return(rc);
        }

        public int GetRCsCount()
        {
            return m_poolSize;
        }

        public void OnDeviceInit()
        {
            m_pool = new MyConcurrentPool<MyRenderContext>(m_poolSize, true);
            
            // Initialize all RCs
            m_tmpList.Clear();
            int poolSize = m_pool.Count;
            for (int i = 0; i < poolSize; i++)
            {
                MyRenderContext rc = m_pool.Get();
                m_tmpList.Add(rc);
                rc.Initialize();
            }
            foreach (var rc in m_tmpList)
                m_pool.Return(rc);
            m_tmpList.Clear();

            m_isDeviceInit = true;
        }

        public void OnDeviceEnd()
        {
            m_isDeviceInit = false;

            // Initialize all RCs
            m_tmpList.Clear();
            for (int i = 0; i < m_pool.Count; i++)
            {
                MyRenderContext rc = m_pool.Get();
                m_tmpList.Add(rc);
                rc.Dispose();
            }
            foreach (var RC in m_tmpList)
                m_pool.Return(RC);
            m_tmpList.Clear();
        }

        public void OnDeviceReset()
        {
            OnDeviceEnd();
            OnDeviceInit();
        }
    }
}
