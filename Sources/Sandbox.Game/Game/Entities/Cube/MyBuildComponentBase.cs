using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRageMath;

namespace Sandbox.Game.World
{
    public abstract class MyBuildComponentBase : MySessionComponentBase
    {
        protected MyComponentList m_materialList = new MyComponentList();

        public DictionaryReader<MyDefinitionId, int> TotalMaterials { get { return m_materialList.TotalMaterials; } }

        public abstract IMyComponentInventory GetBuilderInventory(long entityId);
        public abstract IMyComponentInventory GetBuilderInventory(MyEntity builder);

        public abstract bool HasBuildingMaterials(MyEntity builder);

        // Convention: All these functions will erase the RequiredMaterials first thing when they're called
        public abstract void GetGridSpawnMaterials(MyCubeBlockDefinition definition, MatrixD worldMatrix, bool isStatic);
        public abstract void GetBlockPlacementMaterials(MyCubeBlockDefinition definition, Vector3I position, MyBlockOrientation orientation, MyCubeGrid grid);
        public abstract void GetBlocksPlacementMaterials(HashSet<MyCubeGrid.MyBlockLocation> hashSet, MyCubeGrid grid);
        public abstract void GetGridSpawnMaterials(MyObjectBuilder_CubeGrid grid);

        // This function does some modifications to the cube block's object builder before it's built, usually integrity changes, etc...
        public abstract void BeforeCreateBlock(MyCubeBlockDefinition definition, MyEntity builder, MyObjectBuilder_CubeBlock ob);

        // This function uses RequiredMaterials, so call to Get...Materials has to precede it!
        public abstract void AfterGridCreated(MyCubeGrid grid, MyEntity builder);
        public abstract void AfterGridsSpawn(Dictionary<MyDefinitionId, int> buildItems, MyEntity builder);
        public abstract void AfterBlockBuild(MySlimBlock block, MyEntity builder);
        public abstract void AfterBlocksBuild(HashSet<MyCubeGrid.MyBlockLocation> builtBlocks, MyEntity builder);
    }
}
