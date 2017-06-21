using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRage.Library.Utils;
using VRage.Noise;
using VRage.Noise.Combiners;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Profiler;

namespace Sandbox.Game.World.Generator
{
    public abstract class MyProceduralWorldModule
    {
        protected MyProceduralWorldModule m_parent;
        protected List<MyProceduralWorldModule> m_children = new List<MyProceduralWorldModule>();
        protected int m_seed;
        protected double m_objectDensity;

        protected MyDynamicAABBTreeD m_cellsTree = new MyDynamicAABBTreeD(Vector3D.Zero);
        protected Dictionary<Vector3I, MyProceduralCell> m_cells = new Dictionary<Vector3I, MyProceduralCell>();
        protected CachingHashSet<MyProceduralCell> m_dirtyCells = new CachingHashSet<MyProceduralCell>();

        protected static List<MyObjectSeed> m_tempObjectSeedList = new List<MyObjectSeed>();
        protected static List<MyProceduralCell> m_tempProceduralCellsList = new List<MyProceduralCell>();

        protected List<IMyAsteroidFieldDensityFunction> m_densityFunctionsFilled = new List<IMyAsteroidFieldDensityFunction>();
        protected List<IMyAsteroidFieldDensityFunction> m_densityFunctionsRemoved = new List<IMyAsteroidFieldDensityFunction>();

        public readonly double CELL_SIZE;
        public readonly int SCALE;

        protected const int BIG_PRIME1 = 16785407;
        protected const int BIG_PRIME2 = 39916801;
        protected const int BIG_PRIME3 = 479001599;

        protected const int TWIN_PRIME_MIDDLE1 = 240;
        protected const int TWIN_PRIME_MIDDLE2 = 312;
        protected const int TWIN_PRIME_MIDDLE3 = 462;

        protected MyProceduralWorldModule(double cellSize, int radiusMultiplier, int seed, double density, MyProceduralWorldModule parent = null)
        {
            Debug.Assert(cellSize >= 4 * 1024);
            Debug.Assert(parent == null || cellSize < parent.CELL_SIZE);
            CELL_SIZE = cellSize;
            Debug.Assert(radiusMultiplier > 0);
            Debug.Assert(parent == null || radiusMultiplier < parent.SCALE);
            SCALE = radiusMultiplier;
            m_seed = seed;
            m_objectDensity = density;
            m_parent = parent;
            if (parent != null)
            {
                parent.m_children.Add(this);
            }
        }

        protected void ChildrenAddDensityFunctionFilled(IMyAsteroidFieldDensityFunction func)
        {
            foreach (var child in m_children)
            {
                child.AddDensityFunctionFilled(func);
                child.ChildrenAddDensityFunctionFilled(func);
            }
        }

        protected void ChildrenRemoveDensityFunctionFilled(IMyAsteroidFieldDensityFunction func)
        {
            foreach (var child in m_children)
            {
                child.ChildrenRemoveDensityFunctionFilled(func);
                child.RemoveDensityFunctionFilled(func);
            }
        }

        protected void ChildrenAddDensityFunctionRemoved(IMyAsteroidFieldDensityFunction func)
        {
            foreach (var child in m_children)
            {
                child.AddDensityFunctionRemoved(func);
                child.ChildrenAddDensityFunctionRemoved(func);
            }
        }

        protected void ChildrenRemoveDensityFunctionRemoved(IMyAsteroidFieldDensityFunction func)
        {
            foreach (var child in m_children)
            {
                child.ChildrenRemoveDensityFunctionRemoved(func);
                child.RemoveDensityFunctionRemoved(func);
            }
        }

        protected void AddDensityFunctionFilled(IMyAsteroidFieldDensityFunction func)
        {
            m_densityFunctionsFilled.Add(func);
        }

        protected void RemoveDensityFunctionFilled(IMyAsteroidFieldDensityFunction func)
        {
            m_densityFunctionsFilled.Remove(func);
        }

        public void AddDensityFunctionRemoved(IMyAsteroidFieldDensityFunction func)
        {
            m_densityFunctionsRemoved.Add(func);
        }

        protected void RemoveDensityFunctionRemoved(IMyAsteroidFieldDensityFunction func)
        {
            m_densityFunctionsRemoved.Remove(func);
        }

        public void GetObjectSeeds(BoundingSphereD sphere, List<MyObjectSeed> list, bool scale = true)
        {
            ProfilerShort.Begin("GetObjectSeedsInSphere");
            var scaledSphere = sphere;
            if (scale)
            {
                scaledSphere.Radius *= SCALE;
            }
            GenerateObjectSeeds(ref scaledSphere);

            OverlapAllBoundingSphere(ref scaledSphere, list);
            ProfilerShort.End();
        }

        protected abstract MyProceduralCell GenerateProceduralCell(ref Vector3I cellId);

        protected int GetCellSeed(ref Vector3I cell)
        {
            unchecked
            {
                return m_seed + cell.X * BIG_PRIME1 + cell.Y * BIG_PRIME2 + cell.Z * BIG_PRIME3;
            }
        }

        protected int GetObjectIdSeed(MyObjectSeed objectSeed)
        {
            int hash = objectSeed.CellId.GetHashCode();
            hash = (hash * 397) ^ m_seed;
            hash = (hash * 397) ^ objectSeed.Params.Index;
            hash = (hash * 397) ^ objectSeed.Params.Seed;
            return hash;
        }

        public abstract void GenerateObjects(List<MyObjectSeed> list, HashSet<MyObjectSeedParams> existingObjectsSeeds);

        protected void GenerateObjectSeeds(ref BoundingSphereD sphere)
        {
            ProfilerShort.Begin("GenerateObjectSeedsInBox");

            BoundingBoxD box = BoundingBoxD.CreateFromSphere(sphere);

            Vector3I cellId = Vector3I.Floor(box.Min / CELL_SIZE);
            for (var iter = GetCellsIterator(sphere); iter.IsValid(); iter.GetNext(out cellId))
            {
                if (m_cells.ContainsKey(cellId))
                {
                    continue;
                }

                var cellBox = new BoundingBoxD(cellId * CELL_SIZE, (cellId + 1) * CELL_SIZE);
                if (sphere.Contains(cellBox) == ContainmentType.Disjoint)
                {
                    continue;
                }

                var cell = GenerateProceduralCell(ref cellId);
                if (cell != null)
                {
                    m_cells.Add(cellId, cell);
                    var cellBBox = cell.BoundingVolume;
                    cell.proxyId = m_cellsTree.AddProxy(ref cellBBox, cell, 0);
                }
            }
            ProfilerShort.End();
        }

        private List<IMyModule> tmpDensityFunctions = new List<IMyModule>();

        protected IMyModule GetCellDensityFunctionFilled(BoundingBoxD bbox)
        {
            foreach (IMyAsteroidFieldDensityFunction func in m_densityFunctionsFilled)
                if (func.ExistsInCell(ref bbox))
                    tmpDensityFunctions.Add(func);

            if (tmpDensityFunctions.Count == 0)
            {
                return null;
            }

            int functionsCount = tmpDensityFunctions.Count;
            while (functionsCount > 1)
            {
                for (int i = 0; i < functionsCount / 2; ++i)
                    tmpDensityFunctions[i] = new MyMax(tmpDensityFunctions[i * 2], tmpDensityFunctions[i * 2 + 1]);

                if (functionsCount % 2 == 1)
                    tmpDensityFunctions[functionsCount - 1] = tmpDensityFunctions[functionsCount / 2];

                functionsCount = functionsCount / 2 + functionsCount % 2;
            }

            IMyModule ret = tmpDensityFunctions[0];

            tmpDensityFunctions.Clear();

            return ret;
        }

        protected IMyModule GetCellDensityFunctionRemoved(BoundingBoxD bbox)
        {
            foreach (IMyAsteroidFieldDensityFunction func in m_densityFunctionsRemoved)
                if (func.ExistsInCell(ref bbox))
                    tmpDensityFunctions.Add(func);

            if (tmpDensityFunctions.Count == 0)
            {
                return null;
            }

            int functionsCount = tmpDensityFunctions.Count;
            while (functionsCount > 1)
            {
                for (int i = 0; i < functionsCount / 2; ++i)
                    tmpDensityFunctions[i] = new MyMin(tmpDensityFunctions[i * 2], tmpDensityFunctions[i * 2 + 1]);

                if (functionsCount % 2 == 1)
                    tmpDensityFunctions[functionsCount - 1] = tmpDensityFunctions[functionsCount / 2];

                functionsCount = functionsCount / 2 + functionsCount % 2;
            }

            IMyModule ret = tmpDensityFunctions[0];

            tmpDensityFunctions.Clear();

            return ret;
        }

        public void MarkCellsDirty(BoundingSphereD toMark, BoundingSphereD? toExclude = null, bool scale = true)
        {
            BoundingSphereD toMarkScaled = new BoundingSphereD(toMark.Center, toMark.Radius * (scale ? SCALE : 1));
            BoundingSphereD toExcludeScaled = new BoundingSphereD();
            if (toExclude.HasValue)
            {
                toExcludeScaled = toExclude.Value;
                if (scale)
                {
                    toExcludeScaled.Radius *= SCALE;
                }
            }
            ProfilerShort.Begin("Mark dirty cells");
            Vector3I cellId = Vector3I.Floor((toMarkScaled.Center - toMarkScaled.Radius) / CELL_SIZE);
            for (var iter = GetCellsIterator(toMarkScaled); iter.IsValid(); iter.GetNext(out cellId))
            {
                MyProceduralCell cell;
                if (m_cells.TryGetValue(cellId, out cell))
                {
                    if (!toExclude.HasValue || toExcludeScaled.Contains(cell.BoundingVolume) == ContainmentType.Disjoint)
                    {
                        m_dirtyCells.Add(cell);
                    }
                }
            }
            ProfilerShort.End();
        }

        public void ProcessDirtyCells(Dictionary<MyEntity, MyEntityTracker> trackedEntities)
        {
            m_dirtyCells.ApplyAdditions();

            if (m_dirtyCells.Count == 0)
            {
                return;
            }
            ProfilerShort.Begin("Find false possitive dirty cells");
            foreach (var cell in m_dirtyCells)
            {
                foreach (var tracker in trackedEntities.Values)
                {
                    var scaledBoundingVolume = tracker.BoundingVolume;
                    scaledBoundingVolume.Radius *= SCALE;
                    if (scaledBoundingVolume.Contains(cell.BoundingVolume) != ContainmentType.Disjoint)
                    {
                        m_dirtyCells.Remove(cell);
                        break;
                    }
                }
            }
            m_dirtyCells.ApplyRemovals();

            ProfilerShort.BeginNextBlock("Remove stuff");
            foreach (var cell in m_dirtyCells)
            {
                cell.GetAll(m_tempObjectSeedList);

                foreach (var objectSeed in m_tempObjectSeedList)
                {
                    if (objectSeed.Params.Generated)
                    {
                        CloseObjectSeed(objectSeed);
                    }
                }
                m_tempObjectSeedList.Clear();
            }

            ProfilerShort.BeginNextBlock("Remove dirty cells");
            foreach (var cell in m_dirtyCells)
            {
                m_cells.Remove(cell.CellId);
                m_cellsTree.RemoveProxy(cell.proxyId);
            }
            m_dirtyCells.Clear();
            ProfilerShort.End();
        }

        protected abstract void CloseObjectSeed(MyObjectSeed objectSeed);

        protected Vector3I_RangeIterator GetCellsIterator(BoundingSphereD sphere)
        {
            return GetCellsIterator(BoundingBoxD.CreateFromSphere(sphere));
        }

        protected Vector3I_RangeIterator GetCellsIterator(BoundingBoxD bbox)
        {
            Vector3I min = Vector3I.Floor(bbox.Min / CELL_SIZE);
            Vector3I max = Vector3I.Floor(bbox.Max / CELL_SIZE);

            return new Vector3I_RangeIterator(ref min, ref max);
        }

        protected void OverlapAllBoundingSphere(ref BoundingSphereD sphere, List<MyObjectSeed> list)
        {
            m_cellsTree.OverlapAllBoundingSphere(ref sphere, m_tempProceduralCellsList);
            foreach (var cell in m_tempProceduralCellsList)
            {
                cell.OverlapAllBoundingSphere(ref sphere, list);
            }
            m_tempProceduralCellsList.Clear();
        }

        protected void OverlapAllBoundingBox(ref BoundingBoxD box, List<MyObjectSeed> list)
        {
            m_cellsTree.OverlapAllBoundingBox(ref box, m_tempProceduralCellsList);
            foreach (var cell in m_tempProceduralCellsList)
            {
                cell.OverlapAllBoundingBox(ref box, list);
            }
            m_tempProceduralCellsList.Clear();
        }

        internal void GetAllCells(List<MyProceduralCell> list)
        {
            m_cellsTree.GetAll(list, false);
        }
    }
}
