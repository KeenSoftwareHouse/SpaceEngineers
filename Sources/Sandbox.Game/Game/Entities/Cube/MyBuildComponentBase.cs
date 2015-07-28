using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Inventory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Components;
using VRageMath;

namespace Sandbox.Game.World
{
    public abstract class MyBuildComponentBase : MySessionComponentBase
    {
        protected MyComponentList m_materialList = new MyComponentList();
        protected MyComponentCombiner m_componentCombiner = new MyComponentCombiner();

        public DictionaryReader<MyDefinitionId, int> TotalMaterials { get { return m_materialList.TotalMaterials; } }

        public abstract MyInventoryBase GetBuilderInventory(long entityId);
        public abstract MyInventoryBase GetBuilderInventory(MyEntity builder);

        public abstract bool HasBuildingMaterials(MyEntity builder);

        // CH: TODO: This is here just temporarily. We should move it to a better place later, maybe character definition?
        public virtual void AfterCharacterCreate(MyCharacter character)
        {
			if (MyFakes.ENABLE_MEDIEVAL_INVENTORY)
			{
				character.InventoryAggregate = new Sandbox.Game.Entities.Inventory.MyInventoryAggregate("CharacterInventories");
				character.InventoryAggregate.AddComponent(new Sandbox.Game.Entities.Inventory.MyInventoryAggregate("Internal"));
			}
		}

        // Convention: All these functions will erase the RequiredMaterials first thing when they're called
        public abstract void GetGridSpawnMaterials(MyCubeBlockDefinition definition, MatrixD worldMatrix, bool isStatic);
        public abstract void GetBlockPlacementMaterials(MyCubeBlockDefinition definition, Vector3I position, MyBlockOrientation orientation, MyCubeGrid grid);
        public abstract void GetBlocksPlacementMaterials(HashSet<MyCubeGrid.MyBlockLocation> hashSet, MyCubeGrid grid);
        public abstract void GetGridSpawnMaterials(MyObjectBuilder_CubeGrid grid);

        // This function does some modifications to the cube block's object builder before it's built, usually integrity changes, etc...
        public virtual void BeforeCreateBlock(MyCubeBlockDefinition definition, MyEntity builder, MyObjectBuilder_CubeBlock ob)
        {
            if (definition.EntityComponents == null) return;

            if (ob.ComponentContainer == null)
            {
                ob.ComponentContainer = new MyObjectBuilder_ComponentContainer();
            }

            foreach (var componentOb in definition.EntityComponents)
            {
                var data = new MyObjectBuilder_ComponentContainer.ComponentData();
                data.TypeId = componentOb.Key.ToString();
                data.Component = componentOb.Value;
                ob.ComponentContainer.Components.Add(data);
            }
        }

        // This function uses RequiredMaterials, so call to Get...Materials has to precede it!
        public abstract void AfterGridCreated(MyCubeGrid grid, MyEntity builder);
        public abstract void AfterGridsSpawn(Dictionary<MyDefinitionId, int> buildItems, MyEntity builder);
        public abstract void AfterBlockBuild(MySlimBlock block, MyEntity builder);
        public abstract void AfterBlocksBuild(HashSet<MyCubeGrid.MyBlockLocation> builtBlocks, MyEntity builder);

        internal MyFixedPoint GetItemAmountCombined(MyInventoryBase availableInventory, MyDefinitionId myDefinitionId)
        {
            return m_componentCombiner.GetItemAmountCombined(availableInventory, myDefinitionId);
        }

        internal void RemoveItemsCombined(MyInventoryBase inventory, int itemAmount, MyDefinitionId itemDefinitionId)
        {
            m_materialList.Clear();
            m_materialList.AddMaterial(itemDefinitionId, itemAmount);
            m_componentCombiner.RemoveItemsCombined(inventory, m_materialList.TotalMaterials);
            m_materialList.Clear();
            return;
        }
    }
}
