using System.Collections.Generic;
using VRage.Collections;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Rendering;
using VRage.Render11.RenderContext;
using VRageRender;

namespace VRage.Render11.Resources
{
    internal class MyDeferredRenderContextManager: IManager, IManagerDevice
    {
        MyConcurrentPool<MyRenderContext> m_pool;
        bool m_isDeviceInit = false;
        static List<MyRenderContext> m_tmpList = new List<MyRenderContext>();
        public static int MaxDeferredRCsCount 
        {
            get { return MyPassIdResolver.AllPassesCount; }
        }

        static MyDeferredRenderContextManager()
        {
            int processorCount = VRage.Library.MyEnvironment.ProcessorCount;
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

        public void OnDeviceInit()
        {
            m_pool = new MyConcurrentPool<MyRenderContext>(MaxDeferredRCsCount, true);
            
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
            if (m_isDeviceInit)
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
        }

        public void OnDeviceReset()
        {
            OnDeviceEnd();
            OnDeviceInit();
        }
    }
}
