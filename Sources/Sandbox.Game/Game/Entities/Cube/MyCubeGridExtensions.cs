using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using VRage.Groups;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.ModAPI;
using Sandbox.Game.Replication;
using VRage.Game;

namespace Sandbox
{
    public static class MyCubeGridExtensions
    {
        internal static bool HasSameGroupAndIsGrid<TGroupData>(this MyGroups<MyCubeGrid, TGroupData> groups, IMyEntity gridA, IMyEntity gridB)
            where TGroupData : IGroupData<MyCubeGrid>, new()
        {
            var a = gridA as MyCubeGrid;
            var b = gridB as MyCubeGrid;
            return a != null && b != null && groups.HasSameGroup(a, b);
        }

        public static BoundingSphere CalculateBoundingSphere(this MyObjectBuilder_CubeGrid grid)
        {
            return BoundingSphere.CreateFromBoundingBox(grid.CalculateBoundingBox());
        }
        public static BoundingBox CalculateBoundingBox(this MyObjectBuilder_CubeGrid grid)
        {
            float gridSize = MyDefinitionManager.Static.GetCubeSize(grid.GridSizeEnum);

            BoundingBox localBb = new BoundingBox(Vector3.MaxValue, Vector3.MinValue);

            try
            {
                foreach (var block in grid.CubeBlocks)
                {
                    MyCubeBlockDefinition definition;
                    if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(block.GetId(), out definition))
                    {
                        MyBlockOrientation ori = block.BlockOrientation;
                        Vector3 blockSize = Vector3.TransformNormal(new Vector3(definition.Size) * gridSize, ori);
                        blockSize = Vector3.Abs(blockSize);

                        Vector3 minCorner = new Vector3(block.Min) * gridSize - new Vector3(gridSize / 2);
                        Vector3 maxCorner = minCorner + blockSize;

                        localBb.Include(minCorner);
                        localBb.Include(maxCorner);
                    }
                }
            }
            catch (KeyNotFoundException e)
            {
                MySandboxGame.Log.WriteLine(e);
                return new BoundingBox();
            }

            return localBb;
        }

        public static void HookMultiplayer(this MyCubeBlock cubeBlock)
        {
            if (cubeBlock != null)
            {
                MyEntities.RaiseEntityCreated(cubeBlock);
                cubeBlock.IsReadyForReplication = true;
            }
        }
    }
}
