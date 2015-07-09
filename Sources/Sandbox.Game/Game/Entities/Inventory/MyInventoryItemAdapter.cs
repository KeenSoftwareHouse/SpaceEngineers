using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Inventory
{
    public class MyInventoryItemAdapter : IMyInventoryItemAdapter
    {
        public static MyInventoryItemAdapter Static = new MyInventoryItemAdapter();

        private MyPhysicalItemDefinition m_physItem = null;
        private MyCubeBlockDefinition m_blockDef = null;         

        public void Adapt(IMyInventoryItem inventoryItem)
        {
            m_physItem = null;
            m_blockDef = null;

            var poob = inventoryItem.Content as MyObjectBuilder_PhysicalObject;
            if (poob != null) Adapt(poob.GetObjectId());
            else Adapt(inventoryItem.GetDefinitionId());
        }

        public void Adapt(MyDefinitionId itemDefinition)
        {
            if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(itemDefinition, out m_physItem)) return;
            MyDefinitionManager.Static.TryGetCubeBlockDefinition(itemDefinition, out m_blockDef);
        }

        public bool TryAdapt(MyDefinitionId itemDefinition)
        {
            m_physItem = null;
            m_blockDef = null;

            if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(itemDefinition, out m_physItem)) return true;
            if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(itemDefinition, out m_blockDef)) return true;

            return false;
        }

        public float Mass
        {
            get
            {
                if (m_physItem != null) return m_physItem.Mass;
                if (m_blockDef != null)
                {
                    if (MyDestructionData.Static != null && Sync.IsServer)
                    {
                        return MyDestructionHelper.MassFromHavok(MyDestructionData.Static.GetBlockMass(m_blockDef.Model, m_blockDef)) ;
                    }
                    else
                    {
                        return m_blockDef.Mass;
                    }
                }

                Debug.Assert(false, "Invalid inventory item!");
                return 0.0f;
            }
        }

        public float Volume
        {
            get
            {
                if (m_physItem != null) return m_physItem.Volume;

                if (m_blockDef != null)
                {
                    float size = MyDefinitionManager.Static.GetCubeSize(m_blockDef.CubeSize);
                    return m_blockDef.Size.Size * size * size * size;
                }

                Debug.Assert(false, "Invalid inventory item!");
                return 0.0f;
            }
        }

        public string DisplayNameText
        {
            get
            {
                if (m_physItem != null) return m_physItem.DisplayNameText;
                if (m_blockDef != null) return m_blockDef.DisplayNameText;

                Debug.Assert(false, "Invalid inventory item!");
                return "";
            }
        }

        public string Icon
        {
            get
            {
                if (m_physItem != null) return m_physItem.Icon;
                if (m_blockDef != null) return m_blockDef.Icon;

                Debug.Assert(false, "Invalid inventory item!");
                return "";
            }
        }

        public VRage.Utils.MyStringId? IconSymbol
        {
            get
            {
                if (m_physItem != null) return m_physItem.IconSymbol;
                if (m_blockDef != null) return null;

                Debug.Assert(false, "Invalid inventory item!");
                return null;
            }
        }
    }
}
