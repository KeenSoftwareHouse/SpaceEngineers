using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.WorldEnvironment.Definitions;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.Game;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace Sandbox.Game.WorldEnvironment
{
    public abstract class MyLogicalEnvironmentSectorBase : IMyEventProxy
    {
        public long Id;

        public Vector3D WorldPos;
        public Vector3D[] Bounds;
        public IMyEnvironmentOwner Owner { get; protected set; }

        public abstract void EnableItem(int itemId, bool enabled);

        public abstract void UpdateItemModel(int itemId, short modelId);
        public abstract void UpdateItemModelBatch(List<int> items, short newModelId);

        public abstract List<ItemInfo> Items { get; }

        public abstract T GetModule<T>() where T : IMyEnvironmentModule;

        public abstract IMyEnvironmentModule GetModuleForDefinition(MyRuntimeEnvironmentItemInfo def);

        public abstract void GetItemDefinition(ushort index, out MyRuntimeEnvironmentItemInfo def);

        public abstract string DebugData { get; }

        #region Multiplayer

        public abstract void RaiseItemEvent<T>(int logicalItem, ref MyDefinitionId modDef, T eventData, bool fromClient);

        public abstract bool ServerOwned { get; internal set; }

        public int MinLod { get; protected set; }

        public abstract void Init(MyObjectBuilder_EnvironmentSector sectorBuilder);

        public abstract MyObjectBuilder_EnvironmentSector GetObjectBuilder();

        #endregion

        public event Action OnClose;

        public virtual void Close()
        {
            var closers = OnClose;
            if (closers != null)
            {
                closers();
            }
        }

        public abstract void DebugDraw(int lod);
    }

    public class MyEnvironmentModelUpdateBatch : IDisposable
    {
        private struct ModelList
        {
            public List<int> Items;
            public short Model;
        }

        Dictionary<MyDefinitionId, ModelList> m_modelPerItemDefinition = new Dictionary<MyDefinitionId, ModelList>();

        private IMyEnvironmentOwner m_owner;
        private MyLogicalEnvironmentSectorBase m_sector;

        public MyEnvironmentModelUpdateBatch(MyLogicalEnvironmentSectorBase sector)
        {
            m_sector = sector;
            m_owner = m_sector.Owner;
        }

        public void Add(MyDefinitionId modelDef, int item)
        {
            ModelList itemsForModel;
            if (!m_modelPerItemDefinition.TryGetValue(modelDef, out itemsForModel))
            {
                itemsForModel.Items = new List<int>();

                if (modelDef.TypeId.IsNull)
                    itemsForModel.Model = -1;
                else
                {
                    MyPhysicalModelDefinition model = MyDefinitionManager.Static.GetDefinition<MyPhysicalModelDefinition>(modelDef);
                    itemsForModel.Model = model != null ? m_owner.GetModelId(model) : (short)-1;
                }

                m_modelPerItemDefinition[modelDef] = itemsForModel;
            }

            itemsForModel.Items.Add(item);
        }

        public void Dispose()
        {
            Dispatch();
        }

        public void Dispatch()
        {
            foreach (var dataSet in m_modelPerItemDefinition.Values)
                m_sector.UpdateItemModelBatch(dataSet.Items, dataSet.Model);
        }
    }
}