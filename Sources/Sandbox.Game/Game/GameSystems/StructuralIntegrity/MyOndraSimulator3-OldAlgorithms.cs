using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Utils;

namespace Sandbox.Game.GameSystems.StructuralIntegrity
{
    partial class MyOndraSimulator3 : IMyIntegritySimulator
    {

        #region Algorithms


        private void FindAndCaculateFromAdvanced()
        {
            HashSet<Node> closedList = new HashSet<Node>();
            List<Node> staticNodes = new List<Node>();
            Queue<Node> tmpBranches = new Queue<Node>();
            Queue<Node> queue = new Queue<Node>();

            // For each dynamic block
            foreach (var dyn in DynamicBlocks)
            {
                // TODO: Clear nodes parent and is leaf
                closedList.Clear();
                staticNodes.Clear();
                queue.Clear();
                dyn.PathCount = 1;

                int keepLookingDistance = 2; // How far to reach from first fixed is found

                // Find closest support (one or more closest fixed blocks)
                int critDist = int.MaxValue;
                dyn.Distance = 0;
                dyn.Parents.Clear();
                closedList.Add(dyn);
                queue.Enqueue(dyn);
                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    node.Ratio = 1;
                    if (node.Distance > critDist)
                        break;
                    foreach (var neighbour in node.Neighbours)
                    {
                        if (closedList.Add(neighbour))
                        {
                            neighbour.Parents.Clear();
                            neighbour.Parents.Add(node);
                            neighbour.Distance = node.Distance + 1;

                            if (neighbour.IsStatic)
                            {
                                staticNodes.Add(neighbour);
                                if (critDist == int.MaxValue)
                                    critDist = neighbour.Distance + keepLookingDistance;
                            }
                            else
                                queue.Enqueue(neighbour);
                        }
                        else if (neighbour.Distance == node.Distance + 1) // Another path with same length
                        {
                            neighbour.Parents.Add(node);
                        }
                    }
                }

                // Calculate angle between one vector and all other
                // Split weight support based on sum angle ratio
                float totalAngles = 0;
                if (staticNodes.Count > 1)
                {
                    foreach (var s in staticNodes)
                    {
                        float sumAngle = 0;
                        foreach (var s2 in staticNodes)
                        {
                            sumAngle += MyUtils.GetAngleBetweenVectorsAndNormalise(dyn.Pos - s.Pos, dyn.Pos - s2.Pos);
                        }
                        s.Ratio = sumAngle;
                        totalAngles += sumAngle;
                    }

                    foreach (var s in staticNodes)
                    {
                        s.Ratio /= totalAngles;
                    }
                }
                else
                {
                    foreach (var s in staticNodes)
                    {
                        s.Ratio = 1;
                    }
                }

                // Do reverse search
                foreach (var s in staticNodes)
                {
                    closedList.Clear();
                    tmpBranches.Clear();
                    s.PathCount = 1;

                    tmpBranches.Enqueue(s);
                    while (tmpBranches.Count > 0)
                    {
                        var node = tmpBranches.Dequeue();
                        float ratio = node.Ratio / node.Parents.Count;

                        foreach (var parent in node.Parents)
                        {
                            var delta = parent.Pos - node.Pos; // Node to parent
                            int index, parentIndex;
                            if (delta.X + delta.Y + delta.Z > 0)
                            {
                                index = delta.Y + delta.Z * 2; // UnitX = 0, UnitY = 1, UnitZ = 2
                                parentIndex = index + 3;
                            }
                            else
                            {
                                index = -delta.X * 3 - delta.Y * 4 - delta.Z * 5; // // -UnitX = 3, -UnitY = 4, -UnitZ = 5
                                parentIndex = index - 3;
                            }

                            node.SupportingWeights[index] -= dyn.Mass * ratio;
                            parent.SupportingWeights[parentIndex] += dyn.Mass * ratio;

                            if (closedList.Add(parent))
                            {
                                parent.Ratio = ratio;
                                tmpBranches.Enqueue(parent);
                            }
                            else
                            {
                                parent.Ratio += ratio;
                            }
                        }
                    }
                }
            }
        }

        class NodeDistanceComparer : IComparer<Node>
        {
            public int Compare(Node x, Node y)
            {
                return y.Distance - x.Distance;
            }
        }

        private void FindAndCaculateFromStatic()
        {
            HashSet<Node> closedList = new HashSet<Node>();
            List<Node> tmpNodes = new List<Node>();

            var cmp = new NodeDistanceComparer();

            // For each dynamic block
            foreach (var stat in StaticBlocks)
            {
                // TODO: Clear nodes parent and is leaf
                closedList.Clear();
                tmpNodes.Clear();

                using (Stats.Timing.Measure("SI - Static.FindDistances", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
                {
                    // Find shortest distance to each dynamic block
                    ProfilerShort.Begin("FindDistances");
                    FindDistances(stat, closedList, tmpNodes);
                    ProfilerShort.End();
                }

                tmpNodes.Clear();

                using (Stats.Timing.Measure("SI - Dynamic.Sort", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
                {
                    foreach (var block in closedList)
                    {
                        if (!block.IsStatic)
                        {
                            // Reset transfer mass
                            block.Ratio = 1.0f;
                            block.TransferMass = block.Mass;
                            tmpNodes.Add(block);
                        }
                    }

                    // Sort dynamic blocks in range by distance, biggest distance first
                    tmpNodes.Sort(cmp);
                }

                using (Stats.Timing.Measure("SI - Dynamic.Calculate", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
                {
                    // For each dynamic block
                    foreach (var node in tmpNodes)
                    {
                        if (node.IsStatic)
                            continue;

                        node.PathCount++;
                        foreach (var parent in node.Parents)
                        {
                            var delta = parent.Pos - node.Pos; // Node to parent
                            int index, parentIndex;
                            if (delta.X + delta.Y + delta.Z > 0)
                            {
                                index = delta.Y + delta.Z * 2; // UnitX = 0, UnitY = 1, UnitZ = 2
                                parentIndex = index + 3;
                            }
                            else
                            {
                                index = -delta.X * 3 - delta.Y * 4 - delta.Z * 5; // // -UnitX = 3, -UnitY = 4, -UnitZ = 5
                                parentIndex = index - 3;
                            }

                            node.SupportingWeights[index] -= node.TransferMass / node.Parents.Count;
                            parent.SupportingWeights[parentIndex] += node.TransferMass / node.Parents.Count;

                            parent.TransferMass += node.TransferMass / node.Parents.Count;
                        }
                    }
                }
            }
        }

        void FindDistances(Node from, HashSet<Node> closedList, List<Node> staticNodes)
        {
            from.Parents.Clear();
            from.Distance = 0;
            Queue<Node> queue = new Queue<Node>();
            closedList.Add(from);
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                ProfilerShort.Begin("NodeVisitCounter");
                ProfilerShort.End();
                var node = queue.Dequeue();
                node.Ratio = 0;
                foreach (var neighbour in node.Neighbours)
                {
                    if (closedList.Add(neighbour))
                    {
                        neighbour.Parents.Clear();
                        neighbour.Parents.Add(node);
                        neighbour.Distance = node.Distance + 1;

                        if (neighbour.IsStatic)
                            staticNodes.Add(neighbour);
                        else
                            queue.Enqueue(neighbour);
                    }
                    else if (neighbour.Distance == node.Distance + 1) // Another path with same length
                    {
                        neighbour.Parents.Add(node);
                    }
                }
            }
        }


        private void FindAndCaculateFromDynamic()
        {
            HashSet<Node> closedList = new HashSet<Node>();
            List<Node> staticNodes = new List<Node>();
            Queue<Node> tmpBranches = new Queue<Node>();

            // For each dynamic block
            foreach (var dyn in DynamicBlocks)
            {
                // TODO: Clear nodes parent and is leaf
                closedList.Clear();
                staticNodes.Clear();
                dyn.PathCount = 1;

                using (Stats.Timing.Measure("SI - Dynamic.FindDistances", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
                {
                    // Find shortest distance to each static block
                    ProfilerShort.Begin("FindDistances");
                    FindDistances(dyn, closedList, staticNodes);
                    ProfilerShort.End();
                }
                int numPaths = staticNodes.Count;
                float contribution = dyn.Mass / numPaths;

                using (Stats.Timing.Measure("SI - Dynamic.Calculate", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
                {
                    foreach (var s in staticNodes)
                    {
                        closedList.Clear();
                        tmpBranches.Clear();
                        s.Ratio = 1.0f;
                        dyn.PathCount = 1;

                        tmpBranches.Enqueue(s);
                        while (tmpBranches.Count > 0)
                        {
                            var node = tmpBranches.Dequeue();
                            float ratio = node.Ratio / node.Parents.Count;

                            foreach (var parent in node.Parents)
                            {
                                var delta = parent.Pos - node.Pos; // Node to parent
                                int index, parentIndex;
                                if (delta.X + delta.Y + delta.Z > 0)
                                {
                                    index = delta.Y + delta.Z * 2; // UnitX = 0, UnitY = 1, UnitZ = 2
                                    parentIndex = index + 3;
                                }
                                else
                                {
                                    index = -delta.X * 3 - delta.Y * 4 - delta.Z * 5; // // -UnitX = 3, -UnitY = 4, -UnitZ = 5
                                    parentIndex = index - 3;
                                }

                                node.SupportingWeights[index] -= contribution * ratio;
                                parent.SupportingWeights[parentIndex] += contribution * ratio;

                                if (closedList.Add(parent))
                                {
                                    parent.Ratio = ratio;
                                    tmpBranches.Enqueue(parent);
                                }
                                else
                                {
                                    parent.Ratio += ratio;
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        public void Draw()
        {
        }

        public void Close()
        {
        }

        public void ForceRecalc()
        {
            m_needsRecalc = true; 
        }
    }
}
