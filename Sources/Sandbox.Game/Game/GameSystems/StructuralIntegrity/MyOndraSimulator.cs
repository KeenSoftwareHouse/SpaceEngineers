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
    class MyOndraSimulator : IMyIntegritySimulator
    {
        private static HashSet<Vector3I> m_disconnectHelper = new HashSet<Vector3I>();

        private MyCubeGrid m_grid;
        private Dictionary<Vector3I, Element> Lookup = new Dictionary<Vector3I, Element>(Vector3I.Comparer);
        private HashSet<Element> Elements = new HashSet<Element>();
        private HashSet<Vector3I> TmpDisconnectList = new HashSet<Vector3I>(Vector3I.Comparer);
        private bool m_blocksChanged;

        private float m_breakThreshold = 10f;
        private float m_totalMax;

        public MyOndraSimulator(MyCubeGrid grid)
        {
            m_grid = grid;
        }

        public void Add(MySlimBlock block)
        {
            bool isStatic = MyCubeGrid.IsInVoxels(block);
            var element = new Element(isStatic);
            if (!isStatic)
                element.CurrentOffset = 0.05f;
            Lookup[block.Position] = element;
            m_blocksChanged = true;
        }

        public void Remove(MySlimBlock block)
        {
            Lookup.Remove(block.Position);
            m_blocksChanged = true;
        }

        public bool Simulate(float deltaTime)
        {
            CheckBlockChanges();

            Solve_Iterative(0.9f, out m_totalMax);

            foreach (var block in m_grid.GetBlocks())
            {
                if (Lookup[block.Position].AbsSum >= m_breakThreshold)
                {
                    m_grid.UpdateBlockNeighbours(block);
                }
            }

            return true;
        }

        public void DebugDraw()
        {
            m_totalMax = Math.Max(m_totalMax, 0.2f);

            var size = m_grid.GridSize;

            float sum = 0;
            foreach (var c in Lookup)
            {
                if (!c.Value.IsStatic)
                {
                    var color = GetTension(c.Value.MaxDiff, m_totalMax);
                    sum += c.Value.AbsSum;
                    string text = c.Value.AbsSum.ToString("0.00");

                    DrawCube(size, c, ref color, text);
                }
            }

            var stat = Lookup.Where(s => s.Value.IsStatic);
            if (stat.Any())
            {
                Color color = Color.Black;
                DrawCube(size, stat.First(), ref color, sum.ToString());
            }
        }

        public bool IsConnectionFine(MySlimBlock blockA, MySlimBlock blockB)
        {
            Debug.Assert(blockA.CubeGrid == m_grid);
            Debug.Assert(blockB.CubeGrid == m_grid);
            Element elemA, elemB;
            if (!Lookup.TryGetValue(blockA.Position, out elemA) ||
                !Lookup.TryGetValue(blockB.Position, out elemB))
                return true;

            return Math.Max(elemA.AbsSum, elemB.AbsSum) < m_breakThreshold;
        }

        private void CheckBlockChanges()
        {
            if (!m_blocksChanged)
                return;

            m_blocksChanged = false;

            var copy = Lookup.ToDictionary(s => s.Key, v => v.Value);
            Lookup.Clear();

            Stack<KeyValuePair<Vector3I, Element>> tmp = new Stack<KeyValuePair<Vector3I, Element>>();

            // Merge everything possible
            while (copy.Count > 0)
            {
                var first = copy.First();
                copy.Remove(first.Key);

                if (!first.Value.IsStatic)
                    Elements.Add(first.Value);

                tmp.Push(first);
                while (tmp.Count > 0)
                {
                    var item = tmp.Pop();
                    first.Value.Cubes.Add(item.Key);
                    Lookup.Add(item.Key, first.Value);

                    if (first.Value.IsStatic)
                        continue;

                    AddNeighbor(tmp, copy, item.Key + Vector3I.UnitX);
                    AddNeighbor(tmp, copy, item.Key + Vector3I.UnitY);
                    AddNeighbor(tmp, copy, item.Key + Vector3I.UnitZ);
                    AddNeighbor(tmp, copy, item.Key - Vector3I.UnitX);
                    AddNeighbor(tmp, copy, item.Key - Vector3I.UnitY);
                    AddNeighbor(tmp, copy, item.Key - Vector3I.UnitZ);
                }
            }
        }

        private static void AddNeighbor(Stack<KeyValuePair<Vector3I, Element>> addTo, Dictionary<Vector3I, Element> lookup, Vector3I pos)
        {
            Element e;
            if (lookup.TryGetValue(pos, out e) && !e.IsStatic)
            {
                addTo.Push(new KeyValuePair<Vector3I, Element>(pos, e));
                lookup.Remove(pos);
            }
        }

        private void Solve_Iterative(float ratio, out float maxError)
        {
            // Apply force, tmp offset is current offset + movement
            foreach (var c in Elements)
            {
                Debug.Assert(!c.IsStatic);

                float move;
                move = 0.05f;

                c.TmpOffset = c.CurrentOffset + move;
            }

            maxError = 0;

            float threshold = 0.055f;

            // Solve, calculate new current offset
            foreach (var c in Elements)
            {
                Debug.Assert(!c.IsStatic);

                float sum = 0;
                float absSum = 0;
                float count = 0;
                float maxDiff = 0;

                // Calculates sum of neighbour tmp offsets
                foreach (var item in c.Cubes)
                {
                    SumConstraints(c, item, item + Vector3I.UnitX, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
                    SumConstraints(c, item, item + Vector3I.UnitY, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
                    SumConstraints(c, item, item + Vector3I.UnitZ, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
                    SumConstraints(c, item, item - Vector3I.UnitX, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
                    SumConstraints(c, item, item - Vector3I.UnitY, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
                    SumConstraints(c, item, item - Vector3I.UnitZ, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
                }

                // Include myself, more cubes, harder to move
                sum += 0 * c.Cubes.Count;
                count += c.Cubes.Count;

                // Save new offset into current offset and add neighbour force
                float delta = count > 0 ? -sum / count * ratio : 0;

                float absMove = (c.TmpOffset + delta) - c.CurrentOffset;

                //c.Value.LastDelta = absMove > 0.04f ? c.Value.LastDelta * 1.01f : 0.05f;
                //c.Value.LastDelta = Math.Min(c.Value.LastDelta, 0.05f * 4);
                //c.Value.LastDelta = 0.05f + delta;
                //c.Value.LastDelta *= 0.2f;
                c.CurrentOffset = c.TmpOffset + delta;
                c.MaxDiff = maxDiff;
                c.Sum = sum;
                c.AbsSum = absSum;
                maxError = Math.Max(maxError, maxDiff);
            }

            Disconnect();

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

        private void SumConstraints(Element me, Vector3I myPos, Vector3I neighbourPos, float myOffset, ref float sum, ref float absSum, ref float count, ref float max, float disconnectThreshold)
        {
            Element cube;
            if (Lookup.TryGetValue(neighbourPos, out cube) && cube != me)
            {
                var diff = myOffset - cube.TmpOffset;

                //if (diff < 0.00001f && !cube.IsStatic && !me.IsStatic)
                //{
                //    me.Merged = true;
                //    cubes[neighbourPos] = me;
                //}

                max = Math.Max(diff, max);
                sum += diff * cube.Cubes.Count;
                absSum += Math.Abs(diff);
                count += cube.Cubes.Count;

                if (diff > disconnectThreshold)
                    TmpDisconnectList.Add(myPos);
            }
        }

        private void Disconnect()
        {
            while (TmpDisconnectList.Count > 0)
            {
                var first = TmpDisconnectList.First();
                TmpDisconnectList.Remove(first);

                var e = Lookup[first];
                if (e.Cubes.Count == 1)
                    continue;

                e.Cubes.Remove(first);

                var newEl = new Element(false) { CurrentOffset = e.CurrentOffset };
                newEl.Cubes.Add(first);
                Elements.Add(newEl);
                Lookup[first] = newEl;

                TestDisconnect(e);
            }
        }

        private void TestDisconnect(Element e)
        {
            while (e.Cubes.Count > 1)
            {
                try
                {
                    var first = e.Cubes.First();
                    AddNeighbors(m_disconnectHelper, first, e);

                    // All cubes added
                    if (e.Cubes.Count == m_disconnectHelper.Count)
                    {
                        return;
                    }
                    else
                    {
                        var newEl = new Element(false) { CurrentOffset = e.CurrentOffset };
                        foreach (var c in m_disconnectHelper)
                        {
                            e.Cubes.Remove(c);
                            newEl.Cubes.Add(c);
                            Lookup[c] = newEl;
                        }
                        Elements.Add(newEl);
                    }

                }
                finally
                {
                    m_disconnectHelper.Clear();
                }
            }
        }

        private static void AddNeighbors(HashSet<Vector3I> helper, Vector3I pos, Element e)
        {
            if (e.Cubes.Contains(pos) && helper.Add(pos))
            {
                AddNeighbors(helper, pos + Vector3I.UnitX, e);
                AddNeighbors(helper, pos + Vector3I.UnitY, e);
                AddNeighbors(helper, pos + Vector3I.UnitZ, e);
                AddNeighbors(helper, pos - Vector3I.UnitX, e);
                AddNeighbors(helper, pos - Vector3I.UnitY, e);
                AddNeighbors(helper, pos - Vector3I.UnitZ, e);
            }
        }

        private void DrawCube(float size, KeyValuePair<Vector3I, Element> c, ref Color color, string text)
        {
            var local = Matrix.CreateScale(size * 1.02f) * Matrix.CreateTranslation(c.Key * size /*+ new Vector3(0, -c.Value.CurrentOffset / 20.0f, 0)*/);
            Matrix box = local * m_grid.WorldMatrix;

            MyRenderProxy.DebugDrawOBB(box, color.ToVector3(), 0.5f, true, true);
            MyRenderProxy.DebugDrawText3D(box.Translation, text, c.Value.Cubes.Count > 1 ? Color.Black : Color.White, 0.5f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
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

        class Element
        {
            public bool IsStatic;
            public float CurrentOffset;
            public float TmpOffset;
            public float MaxDiff;
            public float LastDelta;
            public float Sum;
            public float AbsSum;

            public HashSet<Vector3I> Cubes = new HashSet<Vector3I>();

            public Element(bool isStatic)
            {
                IsStatic = isStatic;
            }

            public Element(float offset = 0)
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
