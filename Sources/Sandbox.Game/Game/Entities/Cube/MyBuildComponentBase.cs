using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.World
{
    public abstract class MyBuildComponentBase : MySessionComponentBase
    {
        Dictionary<MyDefinitionId, int> m_requiredMaterials = new Dictionary<MyDefinitionId, int>();
        public Dictionary<MyDefinitionId, int> RequiredMaterials { get { return m_requiredMaterials; } }

        public abstract IMyComponentInventory GetBuilderInventory(long entityId);
        public abstract IMyComponentInventory GetBuilderInventory(MyEntity builder);

        public abstract bool HasBuildingMaterials(MyEntity builder);

        // Convention: All these functions will erase the RequiredMaterials first thing when they're called
        public abstract void GetGridSpawnMaterials(MyCubeBlockDefinition definition, MatrixD worldMatrix, bool isStatic);
        public abstract void GetBlockPlacementMaterials(MyCubeBlockDefinition definition, Vector3I position, MyBlockOrientation orientation, MyCubeGrid grid);
        public abstract void GetBlocksPlacementMaterials(HashSet<MyCubeGrid.MyBlockLocation> hashSet, MyCubeGrid grid);
        public abstract void GetGridSpawnMaterials(MyObjectBuilder_CubeGrid grid);

        /* We probably won't use these functions
        public abstract bool CanSpawnGrid(MyCubeBlockDefinition definition, MatrixD worldMatrix, bool isStatic, MyEntity builder);
        public abstract bool CanPlaceBlock(MyCubeBlockDefinition definition, Vector3I position, MyBlockOrientation orientation, MyCubeGrid grid, MyEntity builder);
        public abstract bool CanPlaceBlocks(HashSet<MyCubeGrid.MyBlockLocation> hashSet, MyCubeGrid grid, MyEntity builder);
        public abstract bool CanPasteGrid(Dictionary<MyDefinitionId, int> buildItems, MyEntity builder);
         */

        // This will be moved somewhere else
        public abstract void SpawnGrid(MyCubeBlockDefinition definition, MatrixD worldMatrix, MyEntity builder, bool isStatic);

        // This function does some modifications to the cube block's object builder before it's built, usually integrity changes, etc...
        public abstract void BeforeCreateBlock(MyCubeBlockDefinition definition, MyEntity builder, MyObjectBuilder_CubeBlock ob);

        // This function uses RequiredMaterials, so call to Get...Materials has to precede it!
        public abstract void AfterGridCreated(MyCubeGrid grid, MyEntity builder);
        public abstract void AfterGridsSpawn(Dictionary<MyDefinitionId, int> buildItems, MyEntity builder);
        public abstract void AfterBlockBuild(MySlimBlock block, MyEntity builder);
        public abstract void AfterBlocksBuild(HashSet<MyCubeGrid.MyBlockLocation> builtBlocks, MyEntity builder);
    }
}
