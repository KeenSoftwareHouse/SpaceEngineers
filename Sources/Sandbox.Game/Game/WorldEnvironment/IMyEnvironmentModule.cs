using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Sandbox.Game.WorldEnvironment
{
    public unsafe struct MyLodEnvironmentItemSet
    {
        public List<int> Items;
        public fixed int LodOffsets[MyEnvironmentSectorConstants.MaximumLod + 1];
    }

    public interface IMyEnvironmentModule
    {
        // Postprocess items that have been scanned.
        // This happens on parallel.
        void ProcessItems(Dictionary<short, MyLodEnvironmentItemSet> items, List<MySurfaceParams> surfaceParams, int[] surfaceParamLodOffsets, int changedLodMin, int changedLodMax);

        // Initialize this sector module from saved data
        void Init(MyLogicalEnvironmentSectorBase sector, MyObjectBuilder_Base ob);

        // Close this module.
        void Close();

        // Get data to save. Return null to save nothing.
        MyObjectBuilder_EnvironmentModuleBase GetObjectBuilder();

        // Called on main thread when a item is removed.
        void OnItemEnable(int item, bool enable);

        // Mp Synchronization event
        void HandleSyncEvent(int logicalItem, object data, bool fromClient);

        // Used to ease debugging of modules
        void DebugDraw();
    }

    public interface IMyEnvironmentModuleProxy
    {
        // Initialize thew proxy
        void Init(MyEnvironmentSector sector, List<int> items);

        // Close the proxy, must be thread safe.
        void Close();

        // Called in main thread when the sector lod state has changed.
        void CommitLodChange(int lodBefore, int lodAfter);

        // Called in main thread whent the sector physics has changed.
        void CommitPhysicsChange(bool enabled);

        // Called on main thread when a item is modified.
        void OnItemChange(int index, short newModel);

        // Called on main thread when multiple items have been modified.
        void OnItemChangeBatch(List<int> items, int offset, short newModel);

        // Handle synchronization from this sector in other clients.
        void HandleSyncEvent(int item, object data, bool fromClient);

        // Used to ease debugging of proxy modules
        void DebugDraw();
    }
}
