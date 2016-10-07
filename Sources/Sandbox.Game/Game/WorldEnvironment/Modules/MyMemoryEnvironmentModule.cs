using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using Sandbox.Game.WorldEnvironment.Definitions;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Sandbox.Game.WorldEnvironment.Modules
{
    public class MyMemoryEnvironmentModule : IMyEnvironmentModule
    {
        private MyLogicalEnvironmentSectorBase m_sector;

        private readonly HashSet<int> m_disabledItems = new HashSet<int>();

        public bool NeedToSave
        {
            get { return m_disabledItems.Count > 0; }
        }

        public unsafe void ProcessItems(Dictionary<short, MyLodEnvironmentItemSet> items, List<MySurfaceParams> surfaceParams, int[] surfaceParamLodOffsets, int changedLodMin, int changedLodMax)
        {
            fixed (ItemInfo* sectorItems = m_sector.Items.GetInternalArray())
                foreach (var item in m_disabledItems)
                {
                    // this prevents the proxies from getting this item
                    sectorItems[item].DefinitionIndex = -1;
                }
        }

        public void Init(MyLogicalEnvironmentSectorBase sector, MyObjectBuilder_Base ob)
        {
            if (ob != null)
                m_disabledItems.UnionWith(((MyObjectBuilder_DummyEnvironmentModule)ob).DisabledItems);

            m_sector = sector;
        }

        public void Close()
        {

        }

        public MyObjectBuilder_EnvironmentModuleBase GetObjectBuilder()
        {
            if (m_disabledItems.Count > 0)
                return new MyObjectBuilder_DummyEnvironmentModule { DisabledItems = m_disabledItems };
            return null;
        }

        public unsafe void OnItemEnable(int itemId, bool enabled)
        {
            if (enabled)
                m_disabledItems.Remove(itemId);
            else
                m_disabledItems.Add(itemId);

            // Prevent it from loading for the active session
            fixed (ItemInfo* sectorItems = m_sector.Items.GetInternalArray())
                sectorItems[itemId].DefinitionIndex = -1;
        }

        public void HandleSyncEvent(int logicalItem, object data, bool fromClient)
        {
        }

        public void DebugDraw()
        { }
    }
}
