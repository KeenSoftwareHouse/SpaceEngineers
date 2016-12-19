using VRage.Render11.Common;
using VRageRender;

namespace VRage.Render11.GeometryStage2.Common
{
    struct MyIDGenerator
    {
        int m_lastUsedID;

        public int Generate()
        {
            return ++m_lastUsedID;
        }

        public int GetHighestID()
        {
            return m_lastUsedID;
        }

        public int GenerateIfNeeded(ref int id)
        {
            MyRenderProxy.Assert(id != 0, "ID 0 should be used. Unused Id is -1");
            if (id == -1)
                id = ++m_lastUsedID;
            return id;
        }

        public void Reset()
        {
            m_lastUsedID = 0;
        }
    }
    class MyIDGeneratorManager: IManager, IManagerUnloadData
    {
        public MyIDGenerator GBufferLods = new MyIDGenerator();
        public MyIDGenerator DepthLods = new MyIDGenerator();

        void IManagerUnloadData.OnUnloadData()
        {
            GBufferLods.Reset();
            DepthLods.Reset();
        }
    }
}
