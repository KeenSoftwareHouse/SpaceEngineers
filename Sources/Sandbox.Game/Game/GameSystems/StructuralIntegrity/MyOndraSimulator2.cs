using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GameSystems.StructuralIntegrity
{
    class MyOndraSimulator2 : IMyIntegritySimulator
    {
        private MyCubeGrid m_grid;
        private Dictionary<Vector3I, CubeData> m_cubes = new Dictionary<Vector3I, CubeData>(Vector3I.Comparer);
        private Stack<Vector3I> m_tmpCubes = new Stack<Vector3I>();
        private float m_totalMax;
        private float m_breakThreshold = 10f;
        private bool m_cubeChanged;

        public MyOndraSimulator2(MyCubeGrid grid)
        {
            m_grid = grid;
        }

        public void Add(MySlimBlock block)
        {
            bool isStatic = MyCubeGrid.IsInVoxels(block);
            var element = new CubeData(isStatic);
            if (!isStatic)
                element.CurrentOffset = 0.05f;
            m_cubes[block.Position] = element;
            m_cubeChanged = true;
        }

        public void Remove(MySlimBlock block)
        {
            m_cubes.Remove(block.Position);
            m_cubeChanged = true;
        }

        public bool Simulate(float deltaTime)
        {
            Refresh();

            Solve_Iterative(m_cubes, 0.9f, out m_totalMax);

            foreach (var block in m_grid.GetBlocks())
            {
                var data = m_cubes[block.Position];
                if (data.MaxDiff < m_breakThreshold)
                {
                    data.FramesOverThreshold = 0;
                }
                else
                {
                    ++data.FramesOverThreshold;
                    if (data.FramesOverThreshold > 5)
                        m_grid.UpdateBlockNeighbours(block);
                }
            }

            return true;
        }

        public void DebugDraw()
        {
            m_totalMax = Math.Max(m_totalMax, 0.2f);

            var size = m_grid.GridSize;

            foreach (var c in m_cubes)
            {
                Color color = Color.Black;
                if (!c.Value.IsStatic)
                {
                    color = GetTension(c.Value.MaxDiff, m_totalMax);
                }

                var local = Matrix.CreateScale(size * 1.02f) * Matrix.CreateTranslation(c.Key * size /*+ new Vector3(0, -c.Value.CurrentOffset / 20.0f, 0)*/);
                Matrix box = local * m_grid.WorldMatrix;

                string ten = c.Value.MaxDiff.ToString("0.00");

                MyRenderProxy.DebugDrawOBB(box, color.ToVector3(), 0.5f, true, true);
                MyRenderProxy.DebugDrawText3D(box.Translation, ten, c.Value.Merged ? Color.Black : Color.White, 0.5f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            }
        }

        public bool IsConnectionFine(MySlimBlock blockA, MySlimBlock blockB)
        {
            Debug.Assert(blockA.CubeGrid == m_grid);
            Debug.Assert(blockB.CubeGrid == m_grid);
            CubeData elemA, elemB;
            if (!m_cubes.TryGetValue(blockA.Position, out elemA) ||
                !m_cubes.TryGetValue(blockB.Position, out elemB))
                return true;

            return Math.Abs(elemA.TmpOffset - elemB.TmpOffset) < m_breakThreshold;
        }

        private static void Solve_Iterative(Dictionary<Vector3I, CubeData> cubes, float ratio, out float maxError)
        {
            // Apply force, tmp offset is current offset + movement
            foreach (var c in cubes)
            {
                if (c.Value.IsStatic)
                    continue;

                c.Value.LastDelta = Math.Max(0.5f, c.Value.LastDelta);

                float move = c.Value.LastDelta;
                //move += ;

                c.Value.TmpOffset = c.Value.CurrentOffset + move; // *(float)Math.Sqrt(c.Value.DistanceToStatic);
            }

            maxError = 0;

            // Solve, calculate new current offset
            foreach (var c in cubes)
            {
                if (c.Value.IsStatic)
                    continue;

                var dist = c.Value.DistanceToStatic;

                float sum = 0;
                float count = 0;
                float maxDiff = 0;

                // Calculates sum of neighbour tmp offsets
                SumConstraints(c.Value, cubes, c.Key + Vector3I.UnitX, c.Value.TmpOffset, ref sum, ref count, ref maxDiff);
                SumConstraints(c.Value, cubes, c.Key + Vector3I.UnitY, c.Value.TmpOffset, ref sum, ref count, ref maxDiff);
                SumConstraints(c.Value, cubes, c.Key + Vector3I.UnitZ, c.Value.TmpOffset, ref sum, ref count, ref maxDiff);
                SumConstraints(c.Value, cubes, c.Key - Vector3I.UnitX, c.Value.TmpOffset, ref sum, ref count, ref maxDiff);
                SumConstraints(c.Value, cubes, c.Key - Vector3I.UnitY, c.Value.TmpOffset, ref sum, ref count, ref maxDiff);
                SumConstraints(c.Value, cubes, c.Key - Vector3I.UnitZ, c.Value.TmpOffset, ref sum, ref count, ref maxDiff);

                // Save new offset into current offset and add neighbour force
                float delta = count > 0 ? -sum / count * ratio : 0;

                float absMove = (c.Value.TmpOffset + delta) - c.Value.CurrentOffset;

                //c.Value.LastDelta = absMove > 0.04f ? c.Value.LastDelta * 1.01f : 0.05f;
                //c.Value.LastDelta = Math.Min(c.Value.LastDelta, 0.05f * 4);
                //c.Value.LastDelta = 0.05f + delta;
                //c.Value.LastDelta *= 0.2f;
                c.Value.CurrentOffset = c.Value.TmpOffset + delta;
                c.Value.MaxDiff = maxDiff;
                c.Value.Sum = sum;
                maxError = Math.Max(maxError, maxDiff);
            }

            //foreach (var c in cubes.ToArray())
            //{
            //    if (!c.Value.IsStatic)
            //    {
            //        MergeNeighbor(cubes, c.Value, c.Key, Vector3I.UnitX);
            //        MergeNeighbor(cubes, c.Value, c.Key, Vector3I.UnitY);
            //        MergeNeighbor(cubes, c.Value, c.Key, Vector3I.UnitZ);
            //        MergeNeighbor(cubes, c.Value, c.Key, -Vector3I.UnitX);
            //        MergeNeighbor(cubes, c.Value, c.Key, -Vector3I.UnitY);
            //        MergeNeighbor(cubes, c.Value, c.Key, -Vector3I.UnitZ);
            //    }
            //}
        }

        private static void SumConstraints(CubeData me, Dictionary<Vector3I, CubeData> cubes, Vector3I neighbourPos, float myOffset, ref float sum, ref float count, ref float max)
        {
            CubeData cube;
            if (cubes.TryGetValue(neighbourPos, out cube) && cube != me)
            {
                var diff = myOffset - cube.TmpOffset;

                //if (diff < 0.00001f && !cube.IsStatic && !me.IsStatic)
                //{
                //    me.Merged = true;
                //    cubes[neighbourPos] = me;
                //}

                max = Math.Max(diff, max);
                sum += diff;
                count++;
            }
        }

        private void Refresh()
        {
            if (!m_cubeChanged)
                return;

            var copy = m_cubes.ToArray();
            m_cubes.Clear();

            foreach (var c in copy)
            {
                var data = new CubeData() { CurrentOffset = c.Value.CurrentOffset, IsStatic = c.Value.IsStatic, DistanceToStatic = c.Value.IsStatic ? 0 : int.MaxValue };
                m_cubes.Add(c.Key, data);
                if (data.IsStatic)
                {
                    m_tmpCubes.Push(c.Key);
                }
            }

            while (m_tmpCubes.Count > 0)
            {
                var pos = m_tmpCubes.Pop();

                PropagateNeighbor(m_cubes, m_tmpCubes, pos, Vector3I.UnitX);
                PropagateNeighbor(m_cubes, m_tmpCubes, pos, Vector3I.UnitY);
                PropagateNeighbor(m_cubes, m_tmpCubes, pos, Vector3I.UnitZ);
                PropagateNeighbor(m_cubes, m_tmpCubes, pos, -Vector3I.UnitX);
                PropagateNeighbor(m_cubes, m_tmpCubes, pos, -Vector3I.UnitY);
                PropagateNeighbor(m_cubes, m_tmpCubes, pos, -Vector3I.UnitZ);
            }

            m_cubeChanged = false;
        }

        private static void PropagateNeighbor(Dictionary<Vector3I, CubeData> cubes, Stack<Vector3I> toCheck, Vector3I pos, Vector3I dir)
        {
            CubeData curr = cubes[pos];
            CubeData next;
            if (cubes.TryGetValue(pos + dir, out next) && next.DistanceToStatic > curr.DistanceToStatic + 1)
            {
                next.DistanceToStatic = curr.DistanceToStatic + 1;
                toCheck.Push(pos + dir);
            }
        }

        private static Color GetTension(float offset, float max)
        {
            if (offset < max / 2)
            {
                // Green -> Yellow
                return new Color(offset / (max / 2), 1.0f, 0);
            }
            else
            {
                // Yellow -> Red
                return new Color(1.0f, 1.0f - (offset - max / 2) / (max / 2), 0);
            }
        }

        class CubeData
        {
            public bool IsStatic;
            public float CurrentOffset;
            public float TmpOffset;
            public float MaxDiff;
            public float LastMaxDiff;
            public float LastDelta;
            public float Sum;

            public int DistanceToStatic;

            public bool Merged = false;

            public int FramesOverThreshold;

            public CubeData(bool isStatic)
            {
                IsStatic = isStatic;
            }

            public CubeData(float offset = 0)
            {
                IsStatic = false;
                CurrentOffset = offset;
                TmpOffset = offset;
            }
        }

        public float GetSupportedWeight(Vector3I pos)
        {
            return 0;
        }

        public float GetTension(Vector3I pos)
        {
            return 0;
        }

        public void Draw()
        {
        }

        public void Close()
        {
        }

        public void ForceRecalc()
        {
        }
    }
}
