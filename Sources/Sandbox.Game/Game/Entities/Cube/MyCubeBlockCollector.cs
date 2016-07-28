using Havok;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using VRage.Utils;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Graphics;
using VRage;
using Sandbox.Common;
using VRage;
using VRage.Game;

namespace Sandbox.Game.Entities.Cube
{
    class MyCubeBlockCollector : IDisposable
    {
        public struct ShapeInfo
        {
            public int Count;
            public Vector3I Min;
            public Vector3I Max;
        }
        
        // Improves performance a little
        public const bool SHRINK_CONVEX_SHAPE = false;
        public const float BOX_SHRINK = 0.0f;
        const bool ADD_INNER_BONES_TO_CONVEX = true;

        public List<ShapeInfo> ShapeInfos = new List<ShapeInfo>();
        public List<HkShape> Shapes = new List<HkShape>();
        
        HashSet<MySlimBlock> m_tmpRefreshSet = new HashSet<MySlimBlock>();
        List<Vector3> m_tmpHelperVerts = new List<Vector3>();
        List<Vector3I> m_tmpCubes = new List<Vector3I>();

        HashSet<Vector3I> m_tmpCheck;

        public void Dispose()
        {
            Clear();
        }

        public void Clear()
        {
            ShapeInfos.Clear();
            foreach (var s in Shapes)
            {
                s.RemoveReference();
            }
            Shapes.Clear();
        }

        /// <summary>
        /// Tests whether there are no overlaps.
        /// </summary>
        bool IsValid()
        {
            if (m_tmpCheck == null)
                m_tmpCheck = new HashSet<Vector3I>();

            try
            {
                Vector3I current;
                foreach (var info in ShapeInfos)
                {
                    for (current.X = info.Min.X; current.X <= info.Max.X; ++current.X)
                    {
                        for (current.Y = info.Min.Y; current.Y <= info.Max.Y; ++current.Y)
                        {
                            for (current.Z = info.Min.Z; current.Z <= info.Max.Z; ++current.Z)
                            {
                                if (!m_tmpCheck.Add(current))
                                    return false;
                            }
                        }
                    }
                }
                return true;
            }
            finally
            {
                m_tmpCheck.Clear();
            }
        }

        public void Collect(MyCubeGrid grid, MyVoxelSegmentation segmenter, MyVoxelSegmentationType segmentationType, IDictionary<Vector3I, HkMassElement> massResults)
        {
            foreach (var block in grid.GetBlocks())
            {
                if (block.FatBlock is MyCompoundCubeBlock)
                {
                    CollectCompoundBlock((MyCompoundCubeBlock)block.FatBlock, massResults);
                    Debug.Assert(IsValid(), "Overlapping shapes detected, block shapes cannot overlap!");
                }
                else
                {
                    CollectBlock(block, block.BlockDefinition.PhysicsOption, massResults);
                    Debug.Assert(IsValid(), "Overlapping shapes detected, block shapes cannot overlap!");
                }
            }

            AddSegmentedParts(grid.GridSize, segmenter, segmentationType);
            m_tmpCubes.Clear();

            Debug.Assert(Shapes.Count > 0, "Shape count cannot be zero");
            Debug.Assert(massResults == null || massResults.Count > 0, "No mass elements, something is wrong!");
        }

        /// <summary>
        /// Intended for quite small refreshes (few blocks).
        /// Collect is faster for large refresh.
        /// Removes also dirty mass elements.
        /// </summary>
        public void CollectArea(MyCubeGrid grid, HashSet<Vector3I> dirtyBlocks, MyVoxelSegmentation segmenter, MyVoxelSegmentationType segmentationType, IDictionary<Vector3I, HkMassElement> massResults)
        {
            ProfilerShort.Begin("Remove dirty");
            foreach (var pos in dirtyBlocks)
            {
                if(massResults != null)
                    massResults.Remove(pos);

                var block = grid.GetCubeBlock(pos);
                if (block != null)
                {
                    m_tmpRefreshSet.Add(block);
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Add new");
            foreach (var block in m_tmpRefreshSet)
            {
                if (block.FatBlock is MyCompoundCubeBlock)
                {
                    ProfilerShort.Begin("Collect compound");
                    CollectCompoundBlock((MyCompoundCubeBlock)block.FatBlock, massResults);
                    //Debug.Assert(IsValid(), "Overlapping shapes detected, block shapes cannot overlap!");
                    ProfilerShort.End();
                }
                else
                {
                    ProfilerShort.Begin("Collect block");
                    CollectBlock(block, block.BlockDefinition.PhysicsOption, massResults);
                    //Debug.Assert(IsValid(), "Overlapping shapes detected, block shapes cannot overlap!");
                    ProfilerShort.End();
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("IsValidTest");
            Debug.Assert(IsValid(), "Overlapping shapes detected, block shapes cannot overlap! Uncomment upper asserts to find what block caused this");
            ProfilerShort.End();

            ProfilerShort.Begin("Add segments");
            AddSegmentedParts(grid.GridSize, segmenter, segmentationType);
            ProfilerShort.End();

            m_tmpCubes.Clear();
            m_tmpRefreshSet.Clear(); // Clear is required, we certainly don't want to hold last reference to blocks
        }

        public void CollectMassElements(MyCubeGrid grid, IDictionary<Vector3I, HkMassElement> massResults)
        {
            if (massResults == null)
                return;

            foreach (var block in grid.GetBlocks())
            {
                if (block.FatBlock is MyCompoundCubeBlock)
                {             
                    foreach (var cmpBlock in ((MyCompoundCubeBlock)(block.FatBlock)).GetBlocks())
                    {
                        if (cmpBlock.BlockDefinition.BlockTopology == MyBlockTopology.TriangleMesh)
                        {
                            AddMass(cmpBlock, massResults);
                        }
                    }
                }
                else
                {
                    AddMass(block, massResults);
                }
            }
        }

        private void CollectCompoundBlock(MyCompoundCubeBlock compoundBlock, IDictionary<Vector3I, HkMassElement> massResults)
        {
            int startPos = ShapeInfos.Count;

            // Collect compound blocks
            foreach (var cmpBlock in compoundBlock.GetBlocks())
            {
                Debug.Assert(cmpBlock.BlockDefinition.BlockTopology == MyBlockTopology.TriangleMesh);
                if (cmpBlock.BlockDefinition.BlockTopology == MyBlockTopology.TriangleMesh)
                {
                    CollectBlock(cmpBlock, cmpBlock.BlockDefinition.PhysicsOption, massResults, false);
                }
            }

            // Merge it into one shape info
            if (ShapeInfos.Count > (startPos + 1))
            {
                var info = ShapeInfos[startPos];
                while (ShapeInfos.Count > (startPos + 1))
                {
                    int lastPos = ShapeInfos.Count - 1;                    
                    info.Count += ShapeInfos[lastPos].Count;
                    ShapeInfos.RemoveAt(lastPos);
                }
                ShapeInfos[startPos] = info;
            }
        }

        void AddSegmentedParts(float gridSize, MyVoxelSegmentation segmenter, MyVoxelSegmentationType segmentationType)
        {
            if (segmenter != null)
            {
                int mergeIterations = segmentationType == MyVoxelSegmentationType.Optimized ? 1 : 0;
                //SegmenterBenchmark(collector, segmenter, mergeIterations);

                ProfilerShort.Begin("Prepare segmentation");
                segmenter.ClearInput();
                foreach (var cube in m_tmpCubes)
                {
                    segmenter.AddInput(cube);
                }
                ProfilerShort.End();

                ProfilerShort.Begin("Make segments");
                var segments = segmenter.FindSegments(segmentationType, mergeIterations);
                ProfilerShort.End();

                ProfilerShort.Begin("Add segments");
                foreach (var s in segments)
                {
                    Vector3 min = s.Min * gridSize - new Vector3(gridSize / 2.0f);
                    Vector3 max = s.Max * gridSize + new Vector3(gridSize / 2.0f);
                    AddBox(s.Min, s.Max, ref min, ref max);
                }
                ProfilerShort.End();
            }
            else
            {
                ProfilerShort.Begin("Add full cubes");
                foreach (var c in m_tmpCubes)
                {
                    Vector3 min = c * gridSize - new Vector3(gridSize / 2.0f);
                    Vector3 max = c * gridSize + new Vector3(gridSize / 2.0f);
                    AddBox(c, c, ref min, ref max);
                }
                ProfilerShort.End();
            }
        }

        private void AddBox(Vector3I minPos, Vector3I maxPos, ref Vector3 min, ref Vector3 max)
        {
            Vector3 center = (min + max) / 2.0f;
            Vector3 halfExtent = max - center;
            halfExtent -= BOX_SHRINK;

            HkBoxShape boxShape = new HkBoxShape(halfExtent, MyPerGameSettings.PhysicsConvexRadius);
            var shape = new HkConvexTranslateShape(boxShape, center, HkReferencePolicy.TakeOwnership);

            Shapes.Add(shape);
            ShapeInfos.Add(new ShapeInfo() { Count = 1, Min = minPos, Max = maxPos });
        }

        void CollectBlock(MySlimBlock block, MyPhysicsOption physicsOption, IDictionary<Vector3I, HkMassElement> massResults, bool allowSegmentation = true)
        {
            if (!block.HasPhysics || block.CubeGrid == null)
                return;

            if (massResults != null)
                AddMass(block, massResults);

            if (block.BlockDefinition.BlockTopology == MyBlockTopology.Cube)
            {
                Debug.Assert(block.Min == block.Max, "Calculation assume that cube blocks have size 1x1x1");
                var cubeTopology = block.BlockDefinition.CubeDefinition.CubeTopology;
                if (MyFakes.ENABLE_SIMPLE_GRID_PHYSICS)
                {
                    physicsOption = MyPhysicsOption.Box;
                }
                else if ((cubeTopology == MyCubeTopology.Box) && block.CubeGrid.Skeleton.IsDeformed(block.Min, 0.05f, block.CubeGrid, false))
                {
                    physicsOption = MyPhysicsOption.Convex;
                }

                switch (physicsOption)
                {
                    case MyPhysicsOption.Box:
                        AddBoxes(block);
                        break;

                    case MyPhysicsOption.Convex:
                        AddConvexShape(block, true);
                        break;
                }
            }
            else
            {
                if (physicsOption != MyPhysicsOption.None)
                {
                    HkShape[] havokShapes = null;
                    if (block.FatBlock != null)
                    {
                        havokShapes = block.FatBlock.ModelCollision.HavokCollisionShapes;
                    }

                    if ((havokShapes != null && havokShapes.Length > 0) && !MyFakes.ENABLE_SIMPLE_GRID_PHYSICS)
                    {
                        // first set of shapes goes into block.Position
                        Vector3 blockPos;
                        if (block.FatBlock.ModelCollision.ExportedWrong)
                        {
                            blockPos = block.Position * block.CubeGrid.GridSize;
                        }
                        else
                        {
                            blockPos = block.FatBlock.PositionComp.LocalMatrix.Translation;
                        }
                        HkShape[] shapes = block.FatBlock.ModelCollision.HavokCollisionShapes;
                        Quaternion blockOrientation;
                        block.Orientation.GetQuaternion(out blockOrientation);

                        Vector3 scale = Vector3.One * block.FatBlock.ModelCollision.ScaleFactor;

                        if (shapes.Length == 1 && shapes[0].ShapeType == HkShapeType.List)
                        {
                            HkListShape list = (HkListShape)shapes[0];
                            for (int i = 0; i < list.TotalChildrenCount; i++)
                            {
                                HkShape child = list.GetChildByIndex(i);
                                System.Diagnostics.Debug.Assert(child.IsConvex, "Children in the list must be convex!");
                                Shapes.Add(new HkConvexTransformShape((HkConvexShape)child, ref blockPos, ref blockOrientation, ref scale, HkReferencePolicy.None));
                            }
                        }
                        else
                            if (shapes.Length == 1 && shapes[0].ShapeType == HkShapeType.Mopp)
                            {
                                HkMoppBvTreeShape list = (HkMoppBvTreeShape)shapes[0];
                                for (int i = 0; i < list.ShapeCollection.ShapeCount; i++)
                                {
                                    HkShape child = list.ShapeCollection.GetShape((uint)i, null);
                                    System.Diagnostics.Debug.Assert(child.IsConvex, "Children in the list must be convex!");
                                    Shapes.Add(new HkConvexTransformShape((HkConvexShape)child, ref blockPos, ref blockOrientation, ref scale, HkReferencePolicy.None));
                                }
                            }
                            else

                            for (int i = 0; i < shapes.Length; i++)
                            {
                                Shapes.Add(new HkConvexTransformShape((HkConvexShape)shapes[i], ref blockPos, ref blockOrientation, ref scale, HkReferencePolicy.None));
                            }
                        ShapeInfos.Add(new ShapeInfo() { Count = shapes.Length, Min = block.Min, Max = block.Max });
                    }
                    else
                    {
                        // This will add boxes
                        for (int x = block.Min.X; x <= block.Max.X; x++)
                        {
                            for (int y = block.Min.Y; y <= block.Max.Y; y++)
                            {
                                for (int z = block.Min.Z; z <= block.Max.Z; z++)
                                {
                                    var pos = new Vector3I(x, y, z);
                                    // NOTE: Disabled because it's not visually represented
                                    //if (block.CubeGrid.Skeleton.IsDeformed(pos, 0.05f) && !MyFakes.ENABLE_SIMPLE_GRID_PHYSICS)
                                    //{
                                    //    AddConvexShape(pos, block.CubeGrid.Skeleton, false);
                                    //}
                                    //else

                                    if (allowSegmentation)
                                    {
                                        m_tmpCubes.Add(pos);
                                    }
                                    else
                                    {
                                        Vector3 min = pos * block.CubeGrid.GridSize - new Vector3(block.CubeGrid.GridSize / 2.0f);
                                        Vector3 max = pos * block.CubeGrid.GridSize + new Vector3(block.CubeGrid.GridSize / 2.0f);
                                        AddBox(pos, pos, ref min, ref max);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void AddMass(MySlimBlock block, IDictionary<Vector3I, HkMassElement> massResults)
        {
            float mass = block.BlockDefinition.Mass;
            if (MyFakes.ENABLE_COMPOUND_BLOCKS && block.FatBlock is MyCompoundCubeBlock)
            {
                mass = 0f;
                MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                foreach (var innerBlock in compoundBlock.GetBlocks())
                {
                    mass += innerBlock.GetMass();
                    Debug.Assert(innerBlock.BlockDefinition.Size == Vector3I.One, "Invalid block found in compound block - only blocks with size 1 can be set in compound block!");
                }
            }

            var size = (block.Max - block.Min + Vector3I.One) * block.CubeGrid.GridSize;
            var center = (block.Min + block.Max) * 0.5f * block.CubeGrid.GridSize;
            HkMassProperties massProperties = new HkMassProperties();
            massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(size / 2, mass);
            massResults[block.Position] = new HkMassElement() { Properties = massProperties, Tranform = Matrix.CreateTranslation(center) };
        }

        Vector3 GetPointPos(Vector3I point, MySlimBlock block, bool applySkeleton)
        {
            return GetPointPos((Vector3)point, block, applySkeleton);
        }

        Vector3 GetPointPos(Vector3 point, MySlimBlock block, bool applySkeleton)
        {
            float gridSize = block.CubeGrid.GridSize;
            Vector3 pos = block.Min * gridSize;

            Matrix blockOrientation;
            block.Orientation.GetMatrix(out blockOrientation);
            Vector3I pointTransformed = Vector3I.Round(Vector3.Transform(point, blockOrientation));

            if (applySkeleton)
            {
                pos += block.CubeGrid.Skeleton.GetBone(block.Min, pointTransformed + Vector3I.One);
                //pos += Vector3.Clamp(block.CubeGrid.Skeleton.Bones[boneIndex], new Vector3(-gridSize), new Vector3(gridSize));
            }
            return Vector3.Transform(point * gridSize / 2, blockOrientation) + pos;
        }

        void AddConvexShape(MySlimBlock block, bool applySkeleton)
        {
            Debug.Assert(block.Min == block.Max, "Calculation assume that cube blocks have size 1x1x1");
            Debug.Assert(block.BlockDefinition.BlockTopology == MyBlockTopology.Cube, "Convex shape is available only for cube block");

            m_tmpHelperVerts.Clear();

            switch (block.BlockDefinition.CubeDefinition.CubeTopology)
            {
                case MyCubeTopology.Slope:
                case MyCubeTopology.RotatedSlope:
                    // Main 6 corners
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, -1), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 9 bones
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 0), block, applySkeleton));

                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));

                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 0), block, applySkeleton));
                    }
                    break;
                case MyCubeTopology.RoundSlope:
                    // Main 6 corners
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, -1), block, applySkeleton));

                    //Slope points
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3(-1f, 0.414f, 0.414f), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3(1f, 0.414f, 0.414f), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 9 bones
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 0), block, applySkeleton));

                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));

                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 0), block, applySkeleton));
                    }
                    break;
                case MyCubeTopology.Corner:
                case MyCubeTopology.RotatedCorner:
                    // Main 4 corners
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Inner bones (bottom)
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, -1), block, applySkeleton));

                        // Inner bones (middle)
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                    }
                    break;
                case MyCubeTopology.RoundCorner:
                    // Main 4 corners
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));

                    //Slope points
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3(-0.414f, 0.414f, -1f), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3(-0.414f, -1f, 0.414f), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3(1f, 0.414f, 0.414f), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Inner bones (bottom)
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, -1), block, applySkeleton));

                        // Inner bones (middle)
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                    }
                    break;

                case MyCubeTopology.InvCorner:
                    // Main 7 corners
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 16 bones
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, 0), block, applySkeleton));
                    }
                    break;
                case MyCubeTopology.RoundInvCorner:
                    // Main 7 corners
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));

                    //Slope points
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3(0.414f, -0.414f, -1f), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3(0.414f, -1f, -0.414f), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3(1f, -0.414f, -0.414f), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 16 bones
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, 0), block, applySkeleton));
                    }
                    break;

                case MyCubeTopology.Box:
                case MyCubeTopology.RoundedSlope:
                    // Main 8 corners
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 19 bones
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, 0), block, applySkeleton));
                    }
                    break;

                case MyCubeTopology.Slope2Base:
                    // Main 8 corners
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 19 bones
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(-1, 0.5f, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(0, 0.5f, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(0, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(1, 0.5f, 0), block, applySkeleton));
                    }
                    break;

                case MyCubeTopology.Slope2Tip:
                    // Main 6 corners
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, -1), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 9 bones
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(-1, -0.5f, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 0), block, applySkeleton));

                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(0, -0.5f, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));

                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(1, -0.5f, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 0), block, applySkeleton));
                    }
                    break;
                case MyCubeTopology.Corner2Base:
                    // Main 6 corners
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 0), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, -1), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 9 bones
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 0), block, applySkeleton));

                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(0.5f, -0.5f, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));

                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(1, -0.5f, -0.5f), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 0), block, applySkeleton));
                    }
                    break;
                case MyCubeTopology.Corner2Tip:
                    // Main 4 corners
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Inner bones (bottom)
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(0.5f, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, -1), block, applySkeleton));

                        // Inner bones (middle)
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(1, -0.5f, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3(0.5f, -0.5f, -1), block, applySkeleton));
                    }
                    break;
                case MyCubeTopology.InvCorner2Base:
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 16 bones
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, 0), block, applySkeleton));
                    }
                    break;

                    break;
                case MyCubeTopology.InvCorner2Tip:
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, -1), block, applySkeleton));
                    //m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, -1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 1), block, applySkeleton));
                    m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, -1), block, applySkeleton));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 16 bones
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(-1, 1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, -1, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, -1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(0, 1, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 0), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 0, 1), block, applySkeleton));
                        m_tmpHelperVerts.Add(GetPointPos(new Vector3I(1, 1, 0), block, applySkeleton));
                    }
                    break;

                    break;
                default:
                    Debug.Fail("Unknown topology");
                    return;
            }

            Shapes.Add(new HkConvexVerticesShape(m_tmpHelperVerts.GetInternalArray(), m_tmpHelperVerts.Count, SHRINK_CONVEX_SHAPE, MyPerGameSettings.PhysicsConvexRadius));
            ShapeInfos.Add(new ShapeInfo() { Count = 1, Min = block.Min, Max = block.Max });
        }

        /// <param name="block"></param>
        /// <param name="applySkeleton"></param>
        /// <param name="allowedDeformation">Absolute value of allowed deformation (in meters) to consider cube full</param>
        void AddBoxes(MySlimBlock block)
        {
            for (int x = block.Min.X; x <= block.Max.X; x++)
            {
                for (int y = block.Min.Y; y <= block.Max.Y; y++)
                {
                    for (int z = block.Min.Z; z <= block.Max.Z; z++)
                    {
                        var pos = new Vector3I(x, y, z);
                        m_tmpCubes.Add(pos);
                    }
                }
            }
        }
    }
}
