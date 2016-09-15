using Sandbox.Game.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Models;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities.Cube
{
    public class MyCubeGridRenderData
    {
        /// <summary>
        /// Cell cube count per axis (cell is 30x30x30)
        /// </summary>
        public static int SplitCellCubeCount = 30;

        private const int MAX_DECALS_PER_CUBE = 30;
        private Dictionary<Vector3I, List<MyDecalPartIdentity>> m_cubeDecals = new Dictionary<Vector3I, List<MyDecalPartIdentity>>();

        Vector3 m_basePos;
        Dictionary<Vector3I, MyCubeGridRenderCell> m_cells = new Dictionary<Vector3I, MyCubeGridRenderCell>();

        HashSet<MyCubeGridRenderCell> m_dirtyCells = new HashSet<MyCubeGridRenderCell>();

        MyRenderComponentCubeGrid m_gridRender;

        public Dictionary<Vector3I, MyCubeGridRenderCell> Cells { get { return m_cells; } }

        public MyCubeGridRenderData(MyRenderComponentCubeGrid grid)
        {
            m_gridRender = grid;
        }
        public void AddCubePart(MyCubePart part)
        {
            var pos = part.InstanceData.Translation;
            var cell = GetCell(pos);

            // We have to add anyway, even when it's already there, bones may have changed
            cell.AddCubePart(part);
            m_dirtyCells.Add(cell);
        }

        public void RemoveCubePart(MyCubePart part)
        {
            var pos = part.InstanceData.Translation;
            var cell = GetCell(pos);
            if (cell.RemoveCubePart(part))
                m_dirtyCells.Add(cell);
        }

        private long CalculateEdgeHash(Vector3 point0, Vector3 point1)
        {
            // There should be per-cell unique ID
            // Best would be to calculate it in int coords
            long h0 = point0.GetHash();
            long h1 = point1.GetHash();

            return h0 * h1;
        }

        public void AddEdgeInfo(ref Vector3 point0, ref Vector3 point1, ref Vector3 normal0, ref Vector3 normal1, Color color, MySlimBlock owner)
        {
            var hash = CalculateEdgeHash(point0, point1);
            var pos = (point0 + point1) * 0.5f;
            Vector3I direction = Vector3I.Round((point0 - point1) / m_gridRender.GridSize);

            MyEdgeInfo info = new MyEdgeInfo(ref pos, ref direction, ref normal0, ref normal1, ref color, MyStringHash.GetOrCompute(owner.BlockDefinition.EdgeType));
            var cell = GetCell(pos);
            if (cell.AddEdgeInfo(hash, info, owner))
                m_dirtyCells.Add(cell);
        }

        public void RemoveEdgeInfo(Vector3 point0, Vector3 point1, MySlimBlock owner)
        {
            var hash = CalculateEdgeHash(point0, point1);
            var pos = (point0 + point1) * 0.5f;
            var cell = GetCell(pos);
            if (cell.RemoveEdgeInfo(hash, owner))
                m_dirtyCells.Add(cell);
        }

        public void RebuildDirtyCells(RenderFlags renderFlags)
        {
            if (m_dirtyCells.Count == 0)
                return;
            foreach (var cell in m_dirtyCells)
            {
                ProfilerShort.Begin("Cell rebuild");
                cell.RebuildInstanceParts(renderFlags);
                ProfilerShort.End();
            }
            m_dirtyCells.Clear();
        }

        /// <summary>
        /// Set base position, usually min of bounding box
        /// </summary>
        public void SetBasePositionHint(Vector3 basePos)
        {
            Debug.Assert(m_cells.Count == 0, "SetBasePositionHint cannot be called when there's render parts already");
            if (m_cells.Count == 0)
            {
                m_basePos = basePos;
            }
        }

        internal MyCubeGridRenderCell GetCell(Vector3 pos)
        {
            // NOTE: Cell position != cube position
            Vector3I cellPos = Vector3I.Round((pos - m_basePos) / (SplitCellCubeCount * m_gridRender.GridSize));
            MyCubeGridRenderCell result;
            if (!m_cells.TryGetValue(cellPos, out result))
            {
                result = new MyCubeGridRenderCell(m_gridRender);
                result.DebugName = cellPos.ToString();
                m_cells[cellPos] = result;
            }
            return result;
        }

        public void AddDecal(Vector3I position, MyCubeGridHitInfo gridHitInfo, uint decalId)
        {
            MyCube cube;
            bool found = m_gridRender.CubeGrid.TryGetCube(position, out cube);
            if (!found)
                return;

            if (gridHitInfo.CubePartIndex != -1)
            {
                var part = cube.Parts[gridHitInfo.CubePartIndex];
                var cell = GetCell(part.InstanceData.Translation);
                cell.AddCubePartDecal(part, decalId);
            }

            List<MyDecalPartIdentity> decals;
            found = m_cubeDecals.TryGetValue(position, out decals);
            if (!found)
            {
                decals = new List<MyDecalPartIdentity>();
                m_cubeDecals[position] = decals;
            }

            if (decals.Count > MAX_DECALS_PER_CUBE)
            {
                RemoveDecal(position, decals, 0);
                decals.RemoveAt(0);
            }

            decals.Add(new MyDecalPartIdentity() { DecalId = decalId, CubePartIndex = gridHitInfo.CubePartIndex });
        }

        public void RemoveDecals(Vector3I position)
        {
            List<MyDecalPartIdentity> decals;
            if (m_cubeDecals.TryGetValue(position, out decals))
            {
                MyCube cube = null;
                for (int it = 0; it < decals.Count; it++)
                    RemoveDecal(position, decals, it, ref cube);

                decals.Clear();
            }
        }

        private void RemoveDecal(Vector3I position, List<MyDecalPartIdentity> decals, int index)
        {
            MyCube cube = null;
            RemoveDecal(position, decals, index, ref cube);
        }

        private void RemoveDecal(Vector3I position, List<MyDecalPartIdentity> decals, int index, ref MyCube cube)
        {
            MyDecalPartIdentity decal = decals[index];
            MyDecals.RemoveDecal(decal.DecalId);

            if (cube == null)
            {
                bool found = m_gridRender.CubeGrid.TryGetCube(position, out cube);
                if (!found)
                    return;
            }

            if (decal.CubePartIndex != -1)
            {
                var part = cube.Parts[decal.CubePartIndex];
                var cell = GetCell(position);
                cell.RemoveCubePartDecal(part, decal.DecalId);
            }
        }

        internal void DebugDraw()
        {
            foreach (var cell in m_cells)
            {
                cell.Value.DebugDraw();
            }
        }

        struct MyDecalPartIdentity
        {
            public uint DecalId;
            public int CubePartIndex;
        }
    }
}
