using Sandbox.Game.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
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

        // hack/optimization that prevents fast shooting turrets from cause rebuild in almost every frame
        int m_rebuildDirtyCounter;
        int m_nextRebuildDirtyCount;
        public void RebuildDirtyCells(RenderFlags renderFlags)
        {
            ++m_rebuildDirtyCounter;
            if (m_rebuildDirtyCounter < m_nextRebuildDirtyCount || m_dirtyCells.Count == 0)
                return;
            m_nextRebuildDirtyCount = m_rebuildDirtyCounter + 10; // change if stuttering is still noticable
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

        private const int MAX_DECALS_PER_CUBE = 10;
        private Dictionary<Vector3I, List<uint>> m_cubeDecals = new Dictionary<Vector3I, List<uint>>();
        public void AddDecal(Vector3I cube, Vector3 position, Vector3 normal, string material)
        {
            if (string.IsNullOrEmpty(material))
                return;
            if (!m_cubeDecals.ContainsKey(cube))
                m_cubeDecals[cube] = new List<uint>();
            if(m_cubeDecals[cube].Count > MAX_DECALS_PER_CUBE)
            {
                MyRenderProxy.RemoveDecal(m_cubeDecals[cube][0]);
                m_cubeDecals[cube].RemoveAt(0);
            }
            Quaternion q = Quaternion.CreateFromAxisAngle(normal, MyRandom.Instance.NextFloat() * MathHelper.TwoPi);
            var perp = Vector3.CalculatePerpendicularVector(normal);
            perp = new Vector3((new Quaternion(perp, 0) * q).ToVector4()); //rotate around normal
            var pos = MatrixD.CreateWorld(position, normal, perp);
            var size = (1f + MyRandom.Instance.NextFloat(-0.35f,0.35f)) *1.5f; //TODO: variable size?
            float depth = 0.2f;
            pos = Matrix.CreateScale(new Vector3(size,size,depth)) * pos;
            //pos.Translation = pos.Translation + pos.Backward * depth;
            var decalId = MyRenderProxy.CreateDecal(m_gridRender.GetRenderObjectID(), (Matrix)pos, material);
            m_cubeDecals[cube].Add(decalId);

        }

        public void RemoveDecals(Vector3I cube)
        {
            List<uint> decals;
            if(m_cubeDecals.TryGetValue(cube, out decals))
            {
                foreach (var decal in decals)
                    MyRenderProxy.RemoveDecal(decal);
                decals.Clear();
            }
        }

        internal void DebugDraw()
        {
            foreach (var cell in m_cells)
            {
                cell.Value.DebugDraw();
            }
        }
    }
}
