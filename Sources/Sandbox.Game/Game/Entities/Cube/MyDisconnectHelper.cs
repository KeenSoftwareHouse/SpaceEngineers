using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.Graphics;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.Common;
using Sandbox.Engine.Utils;
using VRage;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using VRage.Profiler;
using Sandbox.Game.World;

namespace Sandbox.Game.Entities.Cube
{
    public class MyDisconnectHelper
    {
        public struct Group
        {
            public int FirstBlockIndex;
            public int BlockCount;
            public bool IsValid; // False when grid has no physics (e.g. interior light) or when grid is in voxels
            public long EntityId;
        }

        private HashSet<MySlimBlock> m_disconnectHelper = new HashSet<MySlimBlock>();
        private Queue<MySlimBlock> m_neighborSearchBaseStack = new Queue<MySlimBlock>();
        private List<MySlimBlock> m_sortedBlocks = new List<MySlimBlock>();
        private List<Group> m_groups = new List<Group>();
        private Group m_largestGroupWithPhysics;

        private List<MySlimBlock> m_tmpBlocks = new List<MySlimBlock>();

        public bool Disconnect(MyCubeGrid grid, MySlimBlock testBlock = null, bool testDisconnect = false)
        {
            ProfilerShort.Begin("Collect+IsInVoxels");
            m_largestGroupWithPhysics = default(Group);
            m_groups.Clear();
            m_sortedBlocks.Clear();
            m_disconnectHelper.Clear();
            foreach (var block in grid.GetBlocks())
            {
                if (block == testBlock)
                    continue;

                m_disconnectHelper.Add(block);
            }
            ProfilerShort.End();

            ProfilerShort.Begin("GroupBy");
            while (m_disconnectHelper.Count > 0)
            {
                Group group = default(Group);
                group.FirstBlockIndex = m_sortedBlocks.Count;
                AddNeighbours(m_disconnectHelper.FirstElement(), out group.IsValid, testBlock);
                group.BlockCount = m_sortedBlocks.Count - group.FirstBlockIndex;

                if (group.IsValid && group.BlockCount > m_largestGroupWithPhysics.BlockCount)
                {
                    if (m_largestGroupWithPhysics.BlockCount > 0) // Is valid
                    {
                        // order matters, insert in correct place
                        int i = 0;
                        for (i = 0; i < m_groups.Count; i++)
                        {
                            if (m_groups[i].FirstBlockIndex > m_largestGroupWithPhysics.FirstBlockIndex)
                            {
                                m_groups.Insert(i, m_largestGroupWithPhysics);
                                break;
                            }
                        }

                        if (i == m_groups.Count)
                        {
                            m_groups.Add(m_largestGroupWithPhysics);
                        }
                    }

                    m_largestGroupWithPhysics = group;
                }
                else
                {
                    m_groups.Add(group);
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("RemoveLargestGroup");
            m_sortedBlocks.RemoveRange(m_largestGroupWithPhysics.FirstBlockIndex, m_largestGroupWithPhysics.BlockCount);
            for (int i = 0; i < m_groups.Count; i++)
            {
                var g = m_groups[i];
                if (g.FirstBlockIndex > m_largestGroupWithPhysics.FirstBlockIndex)
                {
                    g.FirstBlockIndex -= m_largestGroupWithPhysics.BlockCount;
                    m_groups[i] = g;
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("CreateSplits");
            if (m_groups.Count > 0)
            {
                if (testDisconnect)
                {
                    m_groups.Clear();
                    m_sortedBlocks.Clear();
                    m_disconnectHelper.Clear();
                    ProfilerShort.End();
                    return true;
                }
                MyCubeGrid.CreateSplits(grid, m_sortedBlocks, m_groups);
            }
            else
            {
                if (!MySession.Static.Settings.StationVoxelSupport)
                {
                    if (grid.IsStatic)
                        grid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;
                }
            }
            ProfilerShort.End();

            m_groups.Clear();
            m_sortedBlocks.Clear();
            m_disconnectHelper.Clear();
            return false;
        }

        private void AddNeighbours(MySlimBlock firstBlock, out bool anyWithPhysics, MySlimBlock testBlock)
        {
            anyWithPhysics = false;

            if (m_disconnectHelper.Remove(firstBlock))
            {
                anyWithPhysics |= firstBlock.HasPhysics;
                m_sortedBlocks.Add(firstBlock);
                m_neighborSearchBaseStack.Enqueue(firstBlock);
            }

            while (m_neighborSearchBaseStack.Count > 0)
            {
                var currentBlock = m_neighborSearchBaseStack.Dequeue();
                foreach (var n in currentBlock.Neighbours)
                {
                    if (n == testBlock)
                        continue;

                    if (m_disconnectHelper.Remove(n))
                    {
                        anyWithPhysics |= n.HasPhysics;
                        m_sortedBlocks.Add(n);
                        m_neighborSearchBaseStack.Enqueue(n);
                    }
                }
            }
        }

        public static bool IsDestroyedInVoxels(MySlimBlock block)
        {
            if (block == null || block.CubeGrid.IsStatic)
            {
                return false;
            }
            else
            {
                var grid = block.CubeGrid;
                // 0.5 does not work
                var blockPos = Vector3.Transform((block.Max + block.Min) * 0.5f * grid.GridSize, grid.WorldMatrix);
                // Theoretically 1/60th of a second should be OK because there was no collision previous frame, but 1.5 was shown to work experimentally
                var oldPos = blockPos - grid.Physics.LinearVelocity * 1.5f;
                Vector3 tmp;
                //System.Diagnostics.Debug.Assert(MyEntities.IsInsideVoxel(blockPos, oldPos, out tmp) == MyEntities.IsInsideVoxel(blockPos, blockPos - Vector3.One * 10000, out tmp), "Wrong calc in InsideVoxels");
                return MyEntities.IsInsideVoxel(blockPos, oldPos, out tmp);
            }
        }

        //this tests if removing a block will cause a grid to split
        //this can take several ms per query, so should be called in a thread or very sparingly
        public bool TryDisconnect(MySlimBlock testBlock)
        {
            return Disconnect(testBlock.CubeGrid, testBlock, true);
        }
    }
}
