﻿using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using VRage;
using VRage.Collections;
using VRage.Noise;
using VRage.Noise.Combiners;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.World.Generator
{
    struct MyCompositeShapeGeneratedData
    {
        public IMyModule MacroModule;
        public IMyModule DetailModule;
        public MyCsgShapeBase[] FilledShapes;
        public MyCsgShapeBase[] RemovedShapes;
        public MyVoxelMaterialDefinition DefaultMaterial;
        public MyCompositeShapeOreDeposit[] Deposits;
    }

    [MyStorageDataProvider(10002)]
    class MyCompositeShapeProvider : IMyStorageDataProvider
    {
        const uint CURRENT_VERSION = 1;

        [ThreadStatic]
        private static List<MyCsgShapeBase> m_overlappedRemovedShapes;
        private static List<MyCsgShapeBase> OverlappedRemovedShapes
        {
            get
            {
                if (m_overlappedRemovedShapes == null)
                    m_overlappedRemovedShapes = new List<MyCsgShapeBase>();
                return m_overlappedRemovedShapes;
            }
        }

        [ThreadStatic]
        private static List<MyCsgShapeBase> m_overlappedFilledShapes;
        private static List<MyCsgShapeBase> OverlappedFilledShapes
        {
            get
            {
                if (m_overlappedFilledShapes == null)
                    m_overlappedFilledShapes = new List<MyCsgShapeBase>();
                return m_overlappedFilledShapes;
            }
        }

        [ThreadStatic]
        private static List<MyCompositeShapeOreDeposit> m_overlappedDeposits;
        private static List<MyCompositeShapeOreDeposit> OverlappedDeposits
        {
            get
            {
                if (m_overlappedDeposits == null)
                    m_overlappedDeposits = new List<MyCompositeShapeOreDeposit>();
                return m_overlappedDeposits;
            }
        }

        struct State
        {
            public uint Version;
            public int Generator;
            public int Seed;
            public float Size;
        }

        private State m_state;

        private MyCompositeShapeGeneratedData m_data;

        // for deserialization
        public MyCompositeShapeProvider()
        {
        }

        public static MyCompositeShapeProvider CreateAsteroidShape(int seed, float size, int generatorEntry)
        {
            var result = new MyCompositeShapeProvider();
            result.m_state.Version = CURRENT_VERSION;
            if (generatorEntry <0)
                generatorEntry = 0;
            result.m_state.Generator = generatorEntry;
            result.m_state.Seed = seed;
            result.m_state.Size = size;

            MyCompositeShapes.AsteroidGenerators[result.m_state.Generator](seed, size, out result.m_data);

            return result;
        }

        private static void SetupReading(
            int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod,
            out float lodVoxelSizeHalf, out BoundingBox queryBox, out BoundingSphere querySphere)
        {
            ProfilerShort.Begin("SetupReading");
            lodVoxelSizeHalf = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * (1 << lodIndex);
            Vector3 localMin, localMax;
            {
                Vector3D localPositionD;
                var min = minInLod << lodIndex;
                var max = maxInLod << lodIndex;
                MyVoxelCoordSystems.VoxelCoordToLocalPosition(ref min, out localPositionD);
                localMin = localPositionD;
                MyVoxelCoordSystems.VoxelCoordToLocalPosition(ref max, out localPositionD);
                localMax = localPositionD;

                localMin -= lodVoxelSizeHalf;
                localMax += lodVoxelSizeHalf;
            }
            queryBox = new BoundingBox(localMin, localMax);
            BoundingSphere.CreateFromBoundingBox(ref queryBox, out querySphere);
            ProfilerShort.End();
        }

        internal void ReadContentRange(MyStorageDataCache target, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod)
        {
            float lodVoxelSizeHalf;
            BoundingBox queryBox;
            BoundingSphere querySphere;
            SetupReading(lodIndex, ref minInLod, ref maxInLod, out lodVoxelSizeHalf, out queryBox, out querySphere);
            float lodVoxelSize = 2f * lodVoxelSizeHalf;

            ProfilerShort.Begin("Testing removed shapes");
            var overlappedRemovedShapes = OverlappedRemovedShapes;
            overlappedRemovedShapes.Clear();
            ContainmentType testRemove = ContainmentType.Disjoint;
            for (int i = 0; i < m_data.RemovedShapes.Length; ++i)
            {
                var test = m_data.RemovedShapes[i].Contains(ref queryBox, ref querySphere, lodVoxelSize);
                if (test == ContainmentType.Contains)
                {
                    testRemove = ContainmentType.Contains;
                    break; // completely empty so we can leave
                }
                else if (test == ContainmentType.Intersects)
                {
                    testRemove = ContainmentType.Intersects;
                    overlappedRemovedShapes.Add(m_data.RemovedShapes[i]);
                }
            }
            ProfilerShort.End();
            if (testRemove == ContainmentType.Contains)
            {
                ProfilerShort.Begin("target.BlockFillContent");
                target.BlockFillContent(writeOffset, writeOffset + (maxInLod - minInLod), MyVoxelConstants.VOXEL_CONTENT_EMPTY);
                ProfilerShort.End();
                return;
            }

            ProfilerShort.Begin("Testing filled shapes");
            var overlappedFilledShapes = OverlappedFilledShapes;
            overlappedFilledShapes.Clear();
            ContainmentType testFill = ContainmentType.Disjoint;
            for (int i = 0; i < m_data.FilledShapes.Length; ++i)
            {
                var test = m_data.FilledShapes[i].Contains(ref queryBox, ref querySphere, lodVoxelSize);
                if (test == ContainmentType.Contains)
                {
                    overlappedFilledShapes.Clear();
                    testFill = ContainmentType.Contains;
                    break;
                }
                else if (test == ContainmentType.Intersects)
                {
                    overlappedFilledShapes.Add(m_data.FilledShapes[i]);
                    testFill = ContainmentType.Intersects;
                }
            }
            ProfilerShort.End();

            if (testFill == ContainmentType.Disjoint)
            {
                ProfilerShort.Begin("target.BlockFillContent");
                target.BlockFillContent(writeOffset, writeOffset + (maxInLod - minInLod), MyVoxelConstants.VOXEL_CONTENT_EMPTY);
                ProfilerShort.End();
                return;
            }
            else if (testRemove == ContainmentType.Disjoint && testFill == ContainmentType.Contains)
            {
                ProfilerShort.Begin("target.BlockFillContent");
                target.BlockFillContent(writeOffset, writeOffset + (maxInLod - minInLod), MyVoxelConstants.VOXEL_CONTENT_FULL);
                ProfilerShort.End();
                return;
            }

            ProfilerShort.Begin("Distance field computation");
            Vector3I v = minInLod;
            Vector3 localPos = v * lodVoxelSize;
            Vector3 localPosStart = v * lodVoxelSize;
            var writeOffsetLoc = writeOffset - minInLod;
            for (v.Z = minInLod.Z; v.Z <= maxInLod.Z; ++v.Z)
            {
                for (v.Y = minInLod.Y; v.Y <= maxInLod.Y; ++v.Y)
                {
                    v.X = minInLod.X;
                    var write2 = v + writeOffsetLoc;
                    var write = target.ComputeLinear(ref write2);
                    for (; v.X <= maxInLod.X; ++v.X)
                    {
                        //Vector3 localPos = v * lodVoxelSize;

                        //ProfilerShort.Begin("Dist filled");
                        float distFill;
                        if (testFill == ContainmentType.Contains)
                        {
                            distFill = -1f;
                        }
                        else
                        {
                            //ProfilerShort.Begin("shape distances");
                            distFill = 1f;
                            foreach (var shape in overlappedFilledShapes)
                            {
                                distFill = Math.Min(distFill, shape.SignedDistance(ref localPos, lodVoxelSize, m_data.MacroModule, m_data.DetailModule));
                                if (distFill <= -1)
                                    break;
                                
                            }
                            //ProfilerShort.End();
                        }

                        //ProfilerShort.BeginNextBlock("Dist removed");
                        float distRemoved = 1f;
                        if (testRemove != ContainmentType.Disjoint)
                        {
                            foreach (var shape in overlappedRemovedShapes)
                            {
                                distRemoved = Math.Min(distRemoved, shape.SignedDistance(ref localPos, lodVoxelSize, m_data.MacroModule, m_data.DetailModule));
                                if (distRemoved <= -1)
                                    break;
                            }
                        }                          
                        //ProfilerShort.BeginNextBlock("content");
                        float signedDist = MathHelper.Max(distFill, -distRemoved);

                        var fillRatio = MathHelper.Clamp(-signedDist, -1f, 1f) * 0.5f + 0.5f;
                        byte content = (byte)(fillRatio * MyVoxelConstants.VOXEL_CONTENT_FULL);
                        target.Content(write, content);
                        Debug.Assert(Math.Abs((((float)content) / MyVoxelConstants.VOXEL_CONTENT_FULL) - fillRatio) <= 0.5f);
                        //ProfilerShort.End();
                        write += target.StepLinear;
                        localPos.X += lodVoxelSize;
                    }
                    localPos.Y += lodVoxelSize;
                    localPos.X = localPosStart.X;
                }
                localPos.Z += lodVoxelSize;
                localPos.Y = localPosStart.Y;
            }
            ProfilerShort.End();
        }

        internal void ReadMaterialRange(MyStorageDataCache target, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod)
        {
            float lodVoxelSizeHalf;
            BoundingBox queryBox;
            BoundingSphere querySphere;
            SetupReading(lodIndex, ref minInLod, ref maxInLod, out lodVoxelSizeHalf, out queryBox, out querySphere);
            float lodVoxelSize = 2f * lodVoxelSizeHalf;

            var overlappedDeposits = OverlappedDeposits;
            {
                ProfilerShort.Begin("Testing deposit shapes");
                overlappedDeposits.Clear();
                ContainmentType testDeposits = ContainmentType.Disjoint;
                for (int i = 0; i < m_data.Deposits.Length; ++i)
                {
                    var test = m_data.Deposits[i].Shape.Contains(ref queryBox, ref querySphere, lodVoxelSize);
                    if (test != ContainmentType.Disjoint)
                    {
                        overlappedDeposits.Add(m_data.Deposits[i]);
                        testDeposits = ContainmentType.Intersects;
                    }
                }
                ProfilerShort.End();

                if (testDeposits == ContainmentType.Disjoint)
                {
                    ProfilerShort.Begin("target.BlockFillMaterial");
                    target.BlockFillMaterial(writeOffset, writeOffset + (maxInLod - minInLod), m_data.DefaultMaterial.Index);
                    ProfilerShort.End();
                    return;
                }
            }

            // Check if any material provides occlusion.
            m_hasOcclusionHint = false;
            foreach (var deposit in overlappedDeposits)
            {
                if (deposit.ProvidesOcclusionHint)
                {
                    m_hasOcclusionHint = true;
                }
            };

            ProfilerShort.Begin("Material computation");
            if (overlappedDeposits.Count == 1)
            {
                var deposit = overlappedDeposits.First();

                deposit.ReadMaterialRange(target, ref writeOffset, lodIndex, ref minInLod, ref maxInLod, lodVoxelSizeHalf);
            }
            else
            {
                bool thisHinted;

                Vector3I v;
                for (v.Z = minInLod.Z; v.Z <= maxInLod.Z; ++v.Z)
                {
                    for (v.Y = minInLod.Y; v.Y <= maxInLod.Y; ++v.Y)
                    {
                        for (v.X = minInLod.X; v.X <= maxInLod.X; ++v.X)
                        {
                            Vector3 localPos = v * lodVoxelSize;

                            float closestDistance = 1f;
                            byte closestMaterialIdx = m_data.DefaultMaterial.Index;
                            thisHinted = false;
                            if (!MyFakes.DISABLE_COMPOSITE_MATERIAL)
                            {
                                foreach (var deposit in overlappedDeposits)
                                {
                                    float distance = deposit.Shape.SignedDistance(ref localPos, MyVoxelConstants.VOXEL_SIZE_IN_METRES, m_data.MacroModule, m_data.DetailModule);
                                    if (distance < 0f && distance <= closestDistance)
                                    {
                                        closestDistance = distance;
                                        // DA: Pass default material to the layered deposit so only that does these if-s.
                                        var materialDef = deposit.GetMaterialForPosition(ref localPos, lodVoxelSize);
                                        closestMaterialIdx = materialDef == null ? m_data.DefaultMaterial.Index : materialDef.Index;
                                        thisHinted = deposit.ProvidesOcclusionHint;
                                    }
                                }
                            }

                            var write = v - minInLod + writeOffset;
                            target.Material(ref write, closestMaterialIdx);
                            if (m_hasOcclusionHint && !thisHinted)
                                target.Content(ref write, (byte)(target.Content(ref write) >> 4));
                        }
                    }
                }
            }
            ProfilerShort.End();
        }

        int IMyStorageDataProvider.SerializedSize
        {
            get { unsafe { return sizeof(State); } }
        }

        void IMyStorageDataProvider.WriteTo(Stream stream)
        {
            stream.WriteNoAlloc(m_state.Version);
            stream.WriteNoAlloc(m_state.Generator);
            stream.WriteNoAlloc(m_state.Seed);
            stream.WriteNoAlloc(m_state.Size);
        }

        void IMyStorageDataProvider.ReadFrom(ref MyOctreeStorage.ChunkHeader header, Stream stream, ref bool isOldFormat)
        {
            m_state.Version   = stream.ReadUInt32();
            if (m_state.Version != CURRENT_VERSION)
            {
                // Making sure this gets saved in new format and serialized cache holding old format is discarded.
                isOldFormat = true;
                m_state.Version = CURRENT_VERSION;
            }

            m_state.Generator = stream.ReadInt32();
            m_state.Seed      = stream.ReadInt32();
            m_state.Size      = stream.ReadFloat();

            MyCompositeShapes.AsteroidGenerators[m_state.Generator](m_state.Seed, m_state.Size, out m_data);
        }

        void IMyStorageDataProvider.ReadRange(MyStorageDataCache target, MyStorageDataTypeEnum dataType, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod)
        {
            if (dataType == MyStorageDataTypeEnum.Content)
                ReadContentRange(target, ref writeOffset, lodIndex, ref minInLod, ref maxInLod);
            else
                ReadMaterialRange(target, ref writeOffset, lodIndex, ref minInLod, ref maxInLod);
        }

        void IMyStorageDataProvider.DebugDraw(ref MatrixD worldMatrix)
        {
            var translation = worldMatrix.Translation;
            var filledColor = Color.Red;
            var removedColor = Color.Green;
            var materialColor = Color.CornflowerBlue;
            foreach (var shape in m_data.FilledShapes)
            {
                shape.DebugDraw(ref translation, filledColor);
            }

            foreach (var shape in m_data.RemovedShapes)
            {
                shape.DebugDraw(ref translation, removedColor);
            }

            foreach (var deposit in m_data.Deposits)
            {
                deposit.DebugDraw(ref translation,ref materialColor);
            }
        }

        void IMyStorageDataProvider.ReindexMaterials(Dictionary<byte, byte> oldToNewIndexMap)
        {
            // To be able to handle changes in materials, I would need to store more than just arguments used to generate this shape,
            // as even with the same seed I will get different results.
            // Ignoring for now.
        }

        public float GetDistanceToPoint(ref Vector3D localPosition)
        {
            float nearest = float.MaxValue;
            Vector3 localFloat = (Vector3)localPosition;
            for (int i = 0; i < m_data.FilledShapes.Length; ++i)
            {
                float current = m_data.FilledShapes[i].SignedDistanceUnchecked(ref localFloat, 1, m_data.MacroModule, m_data.DetailModule);
                if (current < nearest)
                {
                    nearest = current;
                }
            }

            return nearest;
        }

        public MyVoxelMaterialDefinition GetMaterialAtPosition(ref Vector3D worldPosition)
        {
			Vector3 localFloat = (Vector3)worldPosition;
            var materialDef = m_data.Deposits[0].GetMaterialForPosition(ref localFloat, 1);
            return materialDef == null ?  m_data.DefaultMaterial : materialDef;
        }

        public bool HasMaterialSpawningFlora()
        {
            foreach (var deposit in m_data.Deposits)
            {
                if (deposit.SpawnsFlora())
                {
                    return true;
                }
            }
            return false;
        }

        private bool m_hasOcclusionHint;


        public bool ProvidesOcclusionHint
        {
            get { return m_hasOcclusionHint; }
        }
    }
}
