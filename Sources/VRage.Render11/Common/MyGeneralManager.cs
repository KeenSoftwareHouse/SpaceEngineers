using System.Collections.Generic;
using System.Linq;
using VRage.Render11.Resources;
using VRageRender;

namespace VRage.Render11.Common
{
    internal enum MyGeneralManagerState
    {
        NOT_INIT,
        INIT,
    }

    internal class MyGeneralManager
    {
        readonly List<IManager> m_allManagers = new List<IManager>();

        MyGeneralManagerState m_deviceState = MyGeneralManagerState.NOT_INIT;

        public void RegisterManager(IManager manager)
        {
            MyRenderProxy.Assert(!m_allManagers.Contains(manager));

            m_allManagers.Add(manager);

            if (manager is IManagerDevice)
            {
                IManagerDevice managerDevice = (IManagerDevice)manager;
                if (m_deviceState == MyGeneralManagerState.INIT)
                    managerDevice.OnDeviceInit();
            }
        }

        public void UnregisterManager(IManager manager)
        {
            MyRenderProxy.Assert(m_allManagers.Contains(manager));

            if (manager is IManagerDevice)
            {
                IManagerDevice managerDevice = (IManagerDevice)manager;
                if (m_deviceState == MyGeneralManagerState.INIT)
                    managerDevice.OnDeviceEnd();
            }

            m_allManagers.Remove(manager);
        }

        public void OnDeviceInit()
        {
            MyRenderProxy.Assert(m_deviceState == MyGeneralManagerState.NOT_INIT);

            m_deviceState = MyGeneralManagerState.INIT;
            foreach (IManager manager in m_allManagers)
            {
                if (manager is IManagerDevice)
                    ((IManagerDevice)manager).OnDeviceInit();
            }
        }

        public void OnDeviceReset()
        {
            if (m_deviceState == MyGeneralManagerState.NOT_INIT)
                return;

            foreach (IManager manager in m_allManagers)
            {
                if (manager is IManagerDevice)
                    ((IManagerDevice)manager).OnDeviceReset();
            }
        }

        public void OnDeviceEnd()
        {
            if (m_deviceState == MyGeneralManagerState.NOT_INIT)
                return;

            foreach (IManager manager in Enumerable.Reverse(m_allManagers))
            {
                if (manager is IManagerDevice)
                    ((IManagerDevice)manager).OnDeviceEnd();
            }
            m_deviceState = MyGeneralManagerState.NOT_INIT;
        }

        public void OnUnloadData()
        {
            foreach (IManager manager in Enumerable.Reverse(m_allManagers))
            {
                if (manager is IManagerUnloadData)
                    ((IManagerUnloadData)manager).OnUnloadData();
            }
        }

        public void OnFrameEnd()
        {
            foreach (IManager manager in m_allManagers)
            {
                if (manager is IManagerFrameEnd)
                    ((IManagerFrameEnd)manager).OnFrameEnd();
            }
        }

        public void OnUpdate()
        {
            foreach (IManager manager in m_allManagers)
            {
                if (manager is IManagerUpdate)
                    ((IManagerUpdate)manager).OnUpdate();
            }
        }
    }
}
