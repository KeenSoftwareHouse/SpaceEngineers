using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Utils;

namespace Sandbox.Game.GUI.DebugInputComponents
{
    public class MyOndraDebugIntegrity
    {
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

        class StructureData
        {
            public MyCubeGrid m_grid;
            public Dictionary<Vector3I, Element> Lookup = new Dictionary<Vector3I, Element>();
            public HashSet<Element> Elements = new HashSet<Element>();

            public HashSet<Vector3I> TmpDisconnectList = new HashSet<Vector3I>();
        }

        Dictionary<MyCubeGrid, StructureData> m_grids = new Dictionary<MyCubeGrid, StructureData>();
        List<Vector3I> m_removeList = new List<Vector3I>();

        public MyOndraDebugIntegrity()
        {
        }

        public void Handle()
        {
            if (MySession.Static == null)
                return;

            Refresh();

            foreach (var s in m_grids)
            {
                DrawMe(s.Value);
            }
        }

        static Color GetTension(float offset, float max)
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

        static void SumConstraints(Element me, Vector3I myPos, StructureData str, Vector3I neighbourPos, float myOffset, ref float sum, ref float absSum, ref float count, ref float max, float disconnectThreshold)
        {
            Element cube;
            if (str.Lookup.TryGetValue(neighbourPos, out cube) && cube != me)
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
                    str.TmpDisconnectList.Add(myPos);
            }
        }

        static void DrawMe(StructureData str)
        {
            var cubes = str.Lookup;

            float totalMax;

            Solve_Iterative(str, 0.9f, out totalMax);

            totalMax = Math.Max(totalMax, 0.2f);
            //totalMax = 1.0f;

            var size = str.m_grid.GridSize;

            float sum = 0;
            foreach (var c in cubes)
            {
                if (!c.Value.IsStatic)
                {
                    var color = GetTension(c.Value.MaxDiff, totalMax);
                    sum += c.Value.AbsSum;
                    string text = c.Value.AbsSum.ToString("0.00");

                    DrawCube(str, size, c, ref color, text);
                }
            }

            var stat = cubes.Where(s => s.Value.IsStatic);
            if (stat.Any())
            {
                Color color = Color.Black;
                //DrawCube(str, size, stat.First(), ref color, sum.ToString());
            }

        }

        private static void DrawCube(StructureData str, float size, KeyValuePair<Vector3I, Element> c, ref Color color, string text)
        {
            var local = Matrix.CreateScale(size * 1.02f) * Matrix.CreateTranslation(c.Key * size + new Vector3(0, -c.Value.CurrentOffset / 20.0f, 0));
            Matrix box = local * str.m_grid.WorldMatrix;
            //ten = c.Value.DistanceToStatic.ToString();

            MyRenderProxy.DebugDrawOBB(box, color.ToVector3(), 0.5f, true, true);
            MyRenderProxy.DebugDrawText3D(box.Translation, text, c.Value.Cubes.Count > 1 ? Color.Black : Color.White, 0.5f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
        }

        private static void Solve_Iterative(StructureData str, float ratio, out float maxError)
        {
            // Apply force, tmp offset is current offset + movement
            foreach (var c in str.Elements)
            {
                Debug.Assert(!c.IsStatic);

                float move;
                move = 0.05f;

                c.TmpOffset = c.CurrentOffset + move;
            }

            maxError = 0;

            float threshold = 0.055f;

            // Solve, calculate new current offset
            foreach (var c in str.Elements)
            {
                Debug.Assert(!c.IsStatic);

                float sum = 0;
                float absSum = 0;
                float count = 0;
                float maxDiff = 0;

                // Calculates sum of neighbour tmp offsets
                foreach (var item in c.Cubes)
                {
                    SumConstraints(c, item, str, item + Vector3I.UnitX, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
                    SumConstraints(c, item, str, item + Vector3I.UnitY, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
                    SumConstraints(c, item, str, item + Vector3I.UnitZ, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
                    SumConstraints(c, item, str, item - Vector3I.UnitX, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
                    SumConstraints(c, item, str, item - Vector3I.UnitY, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
                    SumConstraints(c, item, str, item - Vector3I.UnitZ, c.TmpOffset, ref sum, ref absSum, ref count, ref maxDiff, threshold);
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

            Disconnect(str);

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

        static HashSet<Vector3I> m_disconnectHelper = new HashSet<Vector3I>();

        static void Disconnect(StructureData str)
        {
            while (str.TmpDisconnectList.Count > 0)
            {
                var first = str.TmpDisconnectList.First();
                str.TmpDisconnectList.Remove(first);

                var e = str.Lookup[first];
                if (e.Cubes.Count == 1)
                    continue;

                e.Cubes.Remove(first);

                var newEl = new Element(false) { CurrentOffset = e.CurrentOffset };
                newEl.Cubes.Add(first);
                str.Elements.Add(newEl);
                str.Lookup[first] = newEl;

                TestDisconnect(str, e);
            }
        }

        private static void TestDisconnect(StructureData str, Element e)
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
                            str.Lookup[c] = newEl;
                        }
                        str.Elements.Add(newEl);
                    }

                }
                finally
                {
                    m_disconnectHelper.Clear();
                }
            }
        }

        static void AddNeighbors(HashSet<Vector3I> helper, Vector3I pos, Element e)
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

        //private static void MergeNeighbor(Dictionary<Vector3I, CubeData> cubes, CubeData cube, Vector3I pos, Vector3I dir)
        //{
        //    CubeData neighbour;
        //    if (cubes.TryGetValue(pos + dir, out neighbour) && !neighbour.IsStatic && Math.Abs(neighbour.MaxDiff - cube.MaxDiff) < 0.001f)
        //    {
        //        cube.Merged = true;
        //        cubes[pos + dir] = cube;
        //    }
        //}

        long startTimestamp;

        private void Refresh()
        {
            var time = (Stopwatch.GetTimestamp() - startTimestamp) / (double)Stopwatch.Frequency;
            Stats.Timing.Write("IntegrityRunTime: {0}s", (float)time, VRage.Stats.MyStatTypeEnum.CurrentValue | VRage.Stats.MyStatTypeEnum.FormatFlag, 100, 1);

            var grids = MyEntities.GetEntities().OfType<MyCubeGrid>();

            if (m_grids.Count == 0)
            {
                startTimestamp = Stopwatch.GetTimestamp();
            }

            foreach (var gr in m_grids.ToArray())
            {
                if (gr.Value.m_grid.Closed)
                {
                    m_grids.Remove(gr.Value.m_grid);
                }
            }

            foreach (var g in grids)
            {
                StructureData structure;
                if (!m_grids.TryGetValue(g, out structure))
                {
                    structure = new StructureData();
                    structure.m_grid = g;
                    m_grids[g] = structure;
                }

                bool cubeChanged = false;

                foreach (var c in structure.Lookup)
                {
                    if (g.GetCubeBlock(c.Key) == null)
                    {
                        cubeChanged = true;
                        m_removeList.Add(c.Key);
                    }
                }

                foreach (var x in m_removeList)
                {
                    structure.Lookup.Remove(x);
                }
                m_removeList.Clear();

                foreach (var b in g.GetBlocks())
                {
                    bool isStatic;

                    if (b.BlockDefinition.DisplayNameEnum == MyStringId.GetOrCompute("DisplayName_Block_HeavyArmorBlock"))
                        isStatic = true;
                    else if (b.BlockDefinition.DisplayNameEnum == MyStringId.GetOrCompute("DisplayName_Block_LightArmorBlock"))
                        isStatic = false;
                    else
                        continue;

                    Element cube;
                    if (!structure.Lookup.TryGetValue(b.Position, out cube))
                    {
                        cubeChanged = true;
                        cube = new Element(isStatic);
                        if (!isStatic)
                            cube.CurrentOffset = 0.05f;
                        structure.Lookup[b.Position] = cube;
                    }
                }

                if (cubeChanged)
                {
                    var copy = structure.Lookup.ToDictionary(s => s.Key, v => v.Value);
                    structure.Lookup.Clear();

                    Stack<KeyValuePair<Vector3I, Element>> tmp = new Stack<KeyValuePair<Vector3I, Element>>();

                    // Merge everything possible
                    while (copy.Count > 0)
                    {
                        var first = copy.First();
                        copy.Remove(first.Key);

                        if (!first.Value.IsStatic)
                            structure.Elements.Add(first.Value);

                        tmp.Push(first);
                        while (tmp.Count > 0)
                        {
                            var item = tmp.Pop();
                            first.Value.Cubes.Add(item.Key);
                            structure.Lookup.Add(item.Key, first.Value);

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
            }
        }

        static void AddNeighbor(Stack<KeyValuePair<Vector3I, Element>> addTo, Dictionary<Vector3I, Element> lookup, Vector3I pos)
        {
            Element e;
            if (lookup.TryGetValue(pos, out e) && !e.IsStatic)
            {
                addTo.Push(new KeyValuePair<Vector3I, Element>(pos, e));
                lookup.Remove(pos);
            }
        }
    }
}
