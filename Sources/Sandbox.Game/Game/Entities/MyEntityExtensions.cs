using System.Collections.Generic;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Components;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace Sandbox.Game.Entities
{
    public static class MyEntityExtensions
    {
        internal static void SetCallbacks()
        {
            // ------------------ PLEASE READ -------------------------
            // VRAGE TODO: Delegates in MyEntity help us to get rid of sandbox. There are too many dependencies and this was the easy way to cut MyEntity out of sandbox.
            //             These delegates should not last here forever, after complete deletion of sandbox, there should be no reason for them to stay.
            MyEntity.AddToGamePruningStructureExtCallBack = MyEntityExtensions.AddToGamePruningStructure;
            MyEntity.RemoveFromGamePruningStructureExtCallBack = MyEntityExtensions.RemoveFromGamePruningStructure;
            MyEntity.UpdateGamePruningStructureExtCallBack = MyEntityExtensions.UpdateGamePruningStructure;
            MyEntity.MyEntityFactoryCreateObjectBuilderExtCallback = MyEntityExtensions.EntityFactoryCreateObjectBuilder;
            MyEntity.CreateDefaultSyncEntityExtCallback = MyEntityExtensions.CreateDefaultSyncEntity;
            MyEntity.MyWeldingGroupsAddNodeExtCallback = MyEntityExtensions.AddNodeToWeldingGroups;
            MyEntity.MyWeldingGroupsRemoveNodeExtCallback = MyEntityExtensions.RemoveNodeFromWeldingGroups;
            MyEntity.MyWeldingGroupsGetGroupNodesExtCallback = MyEntityExtensions.GetWeldingGroupNodes;
            MyEntity.MyWeldingGroupsGroupExistsExtCallback = MyEntityExtensions.WeldingGroupExists;
            MyEntity.MyProceduralWorldGeneratorTrackEntityExtCallback = MyEntityExtensions.ProceduralWorldGeneratorTrackEntity;
            MyEntity.CreateStandardRenderComponentsExtCallback = MyEntityExtensions.CreateStandardRenderComponents;
            MyEntity.InitComponentsExtCallback = MyComponentContainerExtension.InitComponents;
            MyEntity.MyEntitiesCreateFromObjectBuilderExtCallback = MyEntities.CreateFromObjectBuilder;
        }

        public static MyPhysicsBody GetPhysicsBody(this MyEntity thisEntity)
        {
            return thisEntity.Physics as MyPhysicsBody;
        }

        public static void UpdateGamePruningStructure(this MyEntity thisEntity)
        {
            if (thisEntity.InScene && (thisEntity.Parent == null || (thisEntity.Flags & EntityFlags.IsGamePrunningStructureObject) != 0))
            {
                //Debug.Assert(thisEntity.Parent == null, "Only top most entity should be in prunning structure");
                MyGamePruningStructure.Move(thisEntity);
                //foreach (var child in thisEntity.Hierarchy.Children) child.Container.Entity.UpdateGamePruningStructure();
            }
        }

        public static void AddToGamePruningStructure(this MyEntity thisEntity)
        {
            if (thisEntity.Parent != null && (thisEntity.Flags & EntityFlags.IsGamePrunningStructureObject) == 0)
                return;
            //Debug.Assert(thisEntity.Parent == null,"Only top most entity should be in prunning structure");
            MyGamePruningStructure.Add(thisEntity);
            //disabled for performance 
            //to re enable this feature implement way to query hierarchy children
            //foreach (var child in thisEntity.Hierarchy.Children)
            //    child.Container.Entity.AddToGamePruningStructure();
        }

        public static void RemoveFromGamePruningStructure(this MyEntity thisEntity)
        {
            MyGamePruningStructure.Remove(thisEntity);

            //if (thisEntity.Hierarchy != null)
            //{
            //    foreach (var child in thisEntity.Hierarchy.Children)
            //        child.Container.Entity.RemoveFromGamePruningStructure();
            //}
        }

        public static MyObjectBuilder_EntityBase EntityFactoryCreateObjectBuilder(this MyEntity thisEntity)
        {
            return MyEntityFactory.CreateObjectBuilder(thisEntity);
        }

        public static MySyncComponentBase CreateDefaultSyncEntity(this MyEntity thisEntity)
        {
            return new MySyncEntity(thisEntity);
        }

        // ---- communication with welding group ----

        public static void AddNodeToWeldingGroups(this MyEntity thisEntity)
        {
            MyWeldingGroups.Static.AddNode(thisEntity);
        }

        public static void RemoveNodeFromWeldingGroups(this MyEntity thisEntity)
        {
            MyWeldingGroups.Static.RemoveNode(thisEntity);
        }

        public static void GetWeldingGroupNodes(this MyEntity thisEntity, List<MyEntity> result)
        {
            MyWeldingGroups.Static.GetGroupNodes(thisEntity, result);
        }

        public static bool WeldingGroupExists(this MyEntity thisEntity)
        {
            return MyWeldingGroups.Static.GetGroup(thisEntity) != null;
        }

        // ---- communication with procedural world generator ----

        public static void ProceduralWorldGeneratorTrackEntity(this MyEntity thisEntity)
        {
            if (MyFakes.ENABLE_ASTEROID_FIELDS)
            {
                if (Sandbox.Game.World.Generator.MyProceduralWorldGenerator.Static != null)
                {
                    Sandbox.Game.World.Generator.MyProceduralWorldGenerator.Static.TrackEntity(thisEntity);
                }
            }
        }

        // ----------- inventory --------------------

        public static bool TryGetInventory(this MyEntity thisEntity, out MyInventoryBase inventoryBase)
        {
            inventoryBase = null;
            return thisEntity.Components.TryGet<MyInventoryBase>(out inventoryBase);
        }

        public static bool TryGetInventory(this MyEntity thisEntity, out MyInventory inventory)
        {
            inventory = null;
            if (thisEntity.Components.Has<MyInventoryBase>())
            {
                inventory = GetInventory(thisEntity, 0);
            }
            return inventory != null;
        }

        /// <summary>
        /// Search for inventory component with maching index.
        /// </summary>
        public static MyInventory GetInventory(this MyEntity thisEntity, int index = 0)
        {
            MyInventoryBase foundInventoryBase = thisEntity.GetInventoryBase(index);
            MyInventory rtnInventory = foundInventoryBase as MyInventory;
            return rtnInventory;
        }

        // ------------------------------------------

        internal static void CreateStandardRenderComponents(this MyEntity thisEntity)
        {
            thisEntity.Render = new MyRenderComponent();
            thisEntity.AddDebugRenderComponent(new MyDebugRenderComponent(thisEntity));
        }
    }
}