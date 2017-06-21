//#define ENHANCED_DEBUG

using Sandbox.Common;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Utils;

namespace Sandbox.Game.GameSystems.StructuralIntegrity
{
    partial class MyOndraSimulator3 : IMyIntegritySimulator
    {
        #region Nested classes

        class PathInfo
        {
            public Node EndNode;
            public Node StartNode; // Starting fixed node which is making wide search
            public int Distance;
            public float Ratio;
            public float DirectionRatio;
            public List<PathInfo> Parents = new List<PathInfo>();
#if ENHANCED_DEBUG
            public List<Node> PathNodes = new List<Node>(); //Starting with Start, ending with Node
#endif
        }

        class Node
        {
            // Per path
            public List<Node> Parents = new List<Node>();
            public int Distance;
            public float Ratio;
            public float TransferMass;
            public bool IsDynamicWeight;

            // Keyed by static node, each static node has different path etc
            public Dictionary<Node, PathInfo> Paths = new Dictionary<Node, PathInfo>();
            
            // Only owned by static nodes, nodes with biggest distance are on the top
            public Stack<PathInfo> OwnedPaths = new Stack<PathInfo>();

            // X, Y, Z, -X, -Y, -Z (faces)
            public float[] SupportingWeights = new float[6];
#if ENHANCED_DEBUG
            public List<Tuple<Node, float>>[] SupportingNodeswWithWeights = new List<Tuple<Node, float>>[6];
            public List<Tuple<Node, float>>[] OutgoingNodeswWithWeights = new List<Tuple<Node, float>>[6];
#endif
            public float TotalSupportingWeight;
            public int PathCount;

            // Constant values
            public Vector3I Pos;
            public float Mass = 1;
            public bool IsStatic;
            public List<Node> Neighbours = new List<Node>();

            public Node(Vector3I pos, bool isStatic)
            {
                Pos = pos;
                IsStatic = isStatic;

#if ENHANCED_DEBUG
                for (int i = 0; i < 6; i++)
                {
                    SupportingNodeswWithWeights[i] = new List<Tuple<Node,float>>();
                    OutgoingNodeswWithWeights[i] = new List<Tuple<Node, float>>();
                }
#endif
            }
        }

        class CollidingEntityInfo
        {
            public Vector3I Position;
            public int FrameTime;
        }

        #endregion

        #region Global data

        public static string[] Algs = {"Stat", "Dyn", "Adv", "AdvS"};
        public static int AlgIndex = 3;

        private static float m_blurAmount = 0.057f;
        private static float m_closestDistanceThreshold = 3;
        private static float m_blurIterations = 7;
        private static bool m_blurEnabled = false;
        private static bool m_blurStaticShareSupport = true;

        bool m_needsRecalc;

        public static bool DrawText = true;
        public static bool BlurEnabled { get { return m_blurEnabled; } set { m_blurEnabled = value; } }
        public static bool BlurStaticShareSupport { get { return m_blurStaticShareSupport; } set { m_blurStaticShareSupport = value; } }
        public static float BlurAmount { get { return m_blurAmount; } set { m_blurAmount = value; } }
        public static float BlurIterations { get { return m_blurIterations; } set { m_blurIterations = value; } }
        public static float ClosestDistanceThreshold { get { return m_closestDistanceThreshold; } set { m_closestDistanceThreshold = value; } }

        static Vector3I[] Offsets = new Vector3I[]
        {
            new Vector3I(0, 0, 1),
            new Vector3I(0, 1, 0),
            new Vector3I(1, 0, 0),
            new Vector3I(0, 0, -1),
            new Vector3I(0, -1, 0),
            new Vector3I(-1, 0, 0),
        };


        static float SideRatio = 0.25f;

        static float[] DirectionRatios = new float[]
        {
            SideRatio,
            1,
            SideRatio,
        };

        #endregion

        #region Local data

        MyCubeGrid m_grid;
        int BlockCount;
        float TotalMax;
        HashSet<Node> StaticBlocks = new HashSet<Node>();
        HashSet<Node> DynamicBlocks = new HashSet<Node>();
        Dictionary<Vector3I, Node> All = new Dictionary<Vector3I, Node>();

        int m_frameCounter = 0;
        int m_lastFrameCollision = 0;
        int DYNAMIC_UPDATE_DELAY = 3;

        Dictionary<MyEntity, CollidingEntityInfo> m_collidingEntities = new Dictionary<MyEntity, CollidingEntityInfo>();

        #endregion

        public MyOndraSimulator3(MyCubeGrid grid)
        {
            m_grid = grid;
            m_selectedGrid = m_grid;
            SelectedCube = m_grid.GetBlocks().First().Position;
        }

        private bool Refresh()
        {
            m_frameCounter++;

            // Change detected by changing block count
            if (m_grid.GetBlocks().Count == BlockCount && !m_needsRecalc)
                return false;

            m_needsRecalc = false;

            m_selectedGrid = m_grid;

            var ts = Stopwatch.GetTimestamp();
            BlockCount = m_grid.GetBlocks().Count;

            LoadBlocks();
            AddNeighbours();

            if (AlgIndex == 0)
            {
                using (Stats.Timing.Measure("SI TOTAL - FindAndCaculateFromStatic", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
                {
                    FindAndCaculateFromStatic();
                }
            }
            else if (AlgIndex == 1)
            {
                using (Stats.Timing.Measure("SI TOTAL - FindAndCaculateFromDynamic", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
                {
                    FindAndCaculateFromDynamic();
                }
            }
            else if (AlgIndex == 2)
            {
                using (Stats.Timing.Measure("SI TOTAL - FindAndCaculateAdvanced", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
                {
                    FindAndCaculateFromAdvanced();
                }
            }
            else
            {
                using (Stats.Timing.Measure("SI TOTAL - FindAndCaculateAdvancedStatic", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
                {
                    FindAndCaculateFromAdvancedStatic();
                }
            }

            using (Stats.Timing.Measure("SI - Sum", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
            {
                foreach (var node in All)
                {
                    //node.Value.TotalSupportingWeight = 0;
                }

                foreach (var node in All)
                {
                    float sum = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        sum += Math.Abs(node.Value.SupportingWeights[i]);
                        //sum = Math.Max(sum, Math.Abs(node.Value.SupportingWeights[i]));
                    }

                    if (!(sum == 0 && node.Value.PathCount == 0))
                    {
                       // sum = sum * 2 - 1;
                        node.Value.TotalSupportingWeight += node.Value.IsStatic ? 0 : (sum * 0.5f / node.Value.PathCount);
                    }
                }

                // Idea behind blur:
                // When block is supporting too much weight, it deforms still supports something, but allows neighbour block to support more weight, thus sharing support

                for (int i = 0; BlurEnabled && i < BlurIterations; i++)
                {
                    foreach (var node in DynamicBlocks)
                    {
                        node.Ratio = 0;
                    }

                    foreach (var node in DynamicBlocks)
                    {
                        float sum = node.TotalSupportingWeight;
                        int count = 1;
                        foreach (var n in node.Neighbours)
                        {
                            if (!BlurStaticShareSupport && n.IsStatic)
                                continue;

                            sum += n.TotalSupportingWeight;
                            count++;
                        }
                        float avg = sum / count;

                        node.Ratio += (avg - node.TotalSupportingWeight) * BlurAmount;

                        foreach (var n in node.Neighbours)
                        {
                            if (n.IsStatic)
                                continue;

                            n.Ratio += (avg - n.TotalSupportingWeight) * BlurAmount;
                        }
                    }

                    foreach (var node in DynamicBlocks)
                    {
                        node.TotalSupportingWeight += node.Ratio;
                    }
                }

                foreach (var node in All)
                {
                    TotalMax = Math.Max(TotalMax, node.Value.TotalSupportingWeight);
                }
                
                var timeMs = (Stopwatch.GetTimestamp() - ts) / (float)Stopwatch.Frequency * 1000.0f;
                //Console.WriteLine(String.Format("Generated structural integrity, time: {0}ms, object name: {1}", timeMs, m_grid.ToString()));
            }

            return true;
        }

        #region Updating

        private void LoadBlocks()
        {
            if (m_grid.Physics == null)
                return;
           
            All.Clear();
            DynamicBlocks.Clear();
            StaticBlocks.Clear();

            using (Stats.Timing.Measure("SI - Collect", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
            {
                // Store blocks
                foreach (var block in m_grid.GetBlocks())
                {
                    bool isStatic = m_grid.Physics.Shape.BlocksConnectedToWorld.Contains(block.Position);

                    if (isStatic)
                    {
                        var n = new Node(block.Position, true);
                        StaticBlocks.Add(n);
                        All.Add(block.Position, n);
                    }
                    else 
                    {
                        var node = new Node(block.Position, false);
                    
                        var cubeMass = MassToSI(m_grid.Physics.Shape.GetBlockMass(block.Position));
                        node.Mass = cubeMass;

                        DynamicBlocks.Add(node);
                        All.Add(block.Position, node);
                    }
                }

                foreach (var block in DynamicWeights)
                {
                    if (All.ContainsKey(block.Key))
                    {
                        All[block.Key].Mass += block.Value;
                    }
                    else
                    {
                        var node = new Node(block.Key, false);
                        node.Mass = DynamicWeights[block.Key];
                        node.IsDynamicWeight = true;
                        DynamicBlocks.Add(node);
                        All.Add(block.Key, node);
                    }
                }
            }

            m_grid.Physics.ContactPointCallback -= Physics_ContactPointCallback;
            m_grid.Physics.ContactPointCallback += Physics_ContactPointCallback;
        }

        Dictionary<Vector3I, float> DynamicWeights = new Dictionary<Vector3I, float>();

        HashSet<MyCubeGrid> m_constrainedGrid = new HashSet<MyCubeGrid>();

        void Physics_ContactPointCallback(ref Engine.Physics.MyPhysics.MyContactPointEvent e)
        {
          //  RestoreDynamicMasses();
            //if (m_frameCounter > DYNAMIC_UPDATE_DELAY)
            //    m_frameCounter = 0;
            //else
            //    return;

            if (m_lastFrameCollision != m_frameCounter)
                DynamicWeights.Clear();

            MyGridContactInfo info = new MyGridContactInfo(ref e.ContactPointEvent, m_grid);
            
            if (info.CollidingEntity.Physics.IsStatic)
                return;

            float speed = info.CollidingEntity.Physics.LinearVelocity.Length();
            if (speed < 0.1f)
                return;

            Vector3I blockPos = m_grid.WorldToGridInteger(info.ContactPosition + Vector3.Up * 0.25f);

            float dot = Vector3.Dot(Vector3.Normalize(info.CollidingEntity.Physics.LinearVelocity), Vector3.Down);

            float collidingMass = 0;
            m_constrainedGrid.Clear();
            MyCubeGrid collidingGrid = info.CollidingEntity as MyCubeGrid;
            if (collidingGrid != null)
            {
                
                m_constrainedGrid.Add(collidingGrid);

                AddConstrainedGrids(collidingGrid);

                foreach (var grid in m_constrainedGrid)
                {
                    collidingMass += grid.Physics.Mass;
                }
            }
            else
            {
                collidingMass = info.CollidingEntity.Physics.Mass;
            }

            collidingMass = info.CollidingEntity is Sandbox.Game.Entities.Character.MyCharacter ? MassToSI(collidingMass) : MassToSI(MyDestructionHelper.MassFromHavok(collidingMass));

            float siMass = collidingMass * MyPetaInputComponent.SI_DYNAMICS_MULTIPLIER;

            //if (dot < 0) //impact from downside
            //    return;

            
            float impact =  siMass * speed * dot + siMass;
            if (impact < 0)
                return;

            if (m_grid.GridSizeEnum == MyCubeSize.Large)
            {
            }

            DynamicWeights[blockPos] = impact;

            m_needsRecalc = true;

            m_lastFrameCollision = m_frameCounter;

            if (!m_collidingEntities.ContainsKey(info.CollidingEntity))
            {
                m_collidingEntities.Add(info.CollidingEntity, new CollidingEntityInfo()
                {
                    Position = blockPos,
                    FrameTime = m_frameCounter
                });
                info.CollidingEntity.PositionComp.OnPositionChanged += PositionComp_OnPositionChanged;
            }
            else
                m_collidingEntities[info.CollidingEntity].FrameTime = m_frameCounter;
        }

        void PositionComp_OnPositionChanged(MyPositionComponentBase obj)
        {
            if (m_collidingEntities.ContainsKey((MyEntity)obj.Container.Entity))
            {
                if (m_frameCounter - m_collidingEntities[(MyEntity)obj.Container.Entity].FrameTime > 20)
                { //Object not contacted with grid for 20 frames
                    obj.OnPositionChanged -= PositionComp_OnPositionChanged;
                    DynamicWeights.Remove(m_collidingEntities[(MyEntity)obj.Container.Entity].Position);
                    m_collidingEntities.Remove((MyEntity)obj.Container.Entity);
                    m_needsRecalc = true;
                }
            }
        }


        void AddConstrainedGrids(MyCubeGrid collidingGrid)
        {
            foreach (var constraint in collidingGrid.Physics.Constraints)
            {
                var gridA = constraint.RigidBodyA.GetBody().Entity as MyCubeGrid;
                var gridB = constraint.RigidBodyB.GetBody().Entity as MyCubeGrid;

                if (!m_constrainedGrid.Contains(gridA))
                {
                    m_constrainedGrid.Add(gridA);
                    AddConstrainedGrids(gridA);
                }

                if (!m_constrainedGrid.Contains(gridB))
                {
                    m_constrainedGrid.Add(gridB);
                    AddConstrainedGrids(gridB);
                }
            }
        }

        void RestoreDynamicMasses()
        {
            // Store blocks
            foreach (var block in DynamicBlocks)
            {
                var cubeMass = MassToSI(m_grid.Physics.Shape.GetBlockMass(block.Pos));
                block.Mass = cubeMass;
            }
        }

        public static float MassToSI(float mass)
        {
            return mass / 30000; //weight of full stone cube
        }

        public static float MassFromSI(float mass)
        {
            return mass * 30000; //weight of full stone cube
        }

        private void AddNeighbours()
        {
            using (Stats.Timing.Measure("SI - AddNeighbours", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
            {
                foreach (var node in All)
                {
                    foreach (var offset in Offsets)
                    {
                        Node neighbour;
                        if (All.TryGetValue(node.Value.Pos + offset, out neighbour))
                        {
                            node.Value.Neighbours.Add(neighbour);
                        }
                    }
                }
            }
        }

        #endregion



        #region Algorithms



        private void FindAndCaculateFromAdvancedStatic()
        {
            // Instead of using uniform distribution of support between all fixed nodes, calculate non-uniform distribution based on vectors for few closest nodes
            // Use this distribution when setting support weight and transferring mass
            // SupportingWeights: x * support
            // ? TransferMass: x * numStaticBlocksForCurrentDynamicBlock * TransferMass

            int keepLookingDistance = (int)ClosestDistanceThreshold; // How far keep looking when closest static block is found
            Queue<PathInfo> queue = new Queue<PathInfo>();

            // Set initial distance to max for all dynamic
            foreach (var dyn in DynamicBlocks)
            {
                dyn.Distance = int.MaxValue;
            }

            // Wide search from all static blocks
            foreach (var stat in StaticBlocks)
            {
                stat.Distance = 0;
                var path = new PathInfo() { EndNode = stat, StartNode = stat, Distance = 0, DirectionRatio = 1 };
#if ENHANCED_DEBUG
                path.PathNodes.Add(stat);
#endif
                queue.Enqueue(path);
            }

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                foreach (var neighbour in path.EndNode.Neighbours)
                {
                    if (neighbour.IsStatic)
                        continue;

                    PathInfo neighbourPath;
                    if (!neighbour.Paths.TryGetValue(path.StartNode, out neighbourPath))
                    {
                        if ((path.Distance - keepLookingDistance) <= neighbour.Distance)
                        {
                            neighbour.Distance = Math.Min(neighbour.Distance, path.Distance);

                            neighbourPath = new PathInfo();
                            neighbourPath.Distance = path.Distance + 1;
                            neighbourPath.StartNode = path.StartNode;
                            neighbourPath.EndNode = neighbour;
#if ENHANCED_DEBUG
                            neighbourPath.PathNodes = path.PathNodes.ToList();
                            neighbourPath.PathNodes.Add(neighbour);
#endif
                            neighbourPath.Parents.Add(path);
                            neighbourPath.DirectionRatio = path.DirectionRatio * DirectionRatios[((Vector3D)(neighbour.Pos - path.EndNode.Pos)).AbsMaxComponent()];
                            neighbour.Paths.Add(path.StartNode, neighbourPath);
                            queue.Enqueue(neighbourPath);
                            path.StartNode.OwnedPaths.Push(neighbourPath);
                        }
                    }
                    else if (neighbourPath.Distance == path.Distance + 1) // Another path with same length
                    {
                        neighbourPath.Parents.Add(path);
                    }
                }
            }



            // Iterate all dynamic blocks and calculate support ratio for each static
            foreach (var dyn in DynamicBlocks)
            {
                dyn.PathCount = 1;

                if (dyn.Pos == new Vector3I(-6, 6, 0))
                {
                }
                // Uniform distribution
                //foreach (var s in dyn.Paths)
                //{
                //    s.Value.Ratio = 1.0f / dyn.Paths.Count;
                //}
                //continue;

                // Non-uniform distribution
                // Calculate angle between one vector and all other
                // Split weight support based on sum angle ratio
                float totalAngles = 0;
                if (dyn.Paths.Count > 1)
                {
                    foreach (var s in dyn.Paths)
                    {
                        Vector3 localVector1 = (dyn.Pos - s.Value.StartNode.Pos) * m_grid.GridSize;
                        Vector3 worldVector1 = Vector3.TransformNormal(localVector1, m_grid.WorldMatrix);

                       // float sumAngle = 0;
                        float sumAngleReduced = 0;
                        foreach (var s2 in dyn.Paths)
                        {
                            if (s.Key == s2.Key)
                            {
                                continue;
                            }

                            Vector3 localVector2 = (dyn.Pos - s2.Value.StartNode.Pos) * m_grid.GridSize;
                            Vector3 worldVector2 = Vector3.TransformNormal(localVector2, m_grid.WorldMatrix);
                            float angle = MyUtils.GetAngleBetweenVectorsAndNormalise(worldVector1, worldVector2);

                            float dot1 = Math.Abs(Vector3.Normalize(worldVector1).Dot(Vector3.Up));
                            float dot2 = Math.Abs(Vector3.Normalize(worldVector2).Dot(Vector3.Up));

                            float lowerBound = 0.1f;
                            dot1 = MathHelper.Lerp(lowerBound, 1, dot1);
                            dot2 = MathHelper.Lerp(lowerBound, 1, dot2);

                            float reducedAngle = angle;

                            if (!MyPetaInputComponent.OLD_SI)
                            {
                                //Reduce dependent on gravity
                                reducedAngle = angle * dot1 * s.Value.DirectionRatio;
                            }

                            //sumAngle += angle;
                            sumAngleReduced += reducedAngle;
                        }
                        s.Value.Ratio = sumAngleReduced;
                        totalAngles += sumAngleReduced;
                    }

                    foreach (var s in dyn.Paths)
                    {
                        if (totalAngles > 0)
                            s.Value.Ratio /= totalAngles;
                        else
                            s.Value.Ratio = 1;
                    }
                }
                else
                {
                    foreach (var s in dyn.Paths)
                    {
                        s.Value.Ratio = 1.0f;
                    }
                }
            }

            // Iterate all static blocks and calculate support mass and mass transfer
            foreach (var staticBlock in StaticBlocks)
            {
                // Initial mass and ratio
                foreach (var path in staticBlock.OwnedPaths)
                {
                    path.EndNode.TransferMass = 0;
                }

                // For each block in path (ordered by distance, furthest first)
                while (staticBlock.OwnedPaths.Count > 0)
                {
                    var pathInfo = staticBlock.OwnedPaths.Pop();
                    var node = pathInfo.EndNode;

                    Debug.Assert(pathInfo.StartNode == staticBlock, "Wrong path");
                    Debug.Assert(!node.IsStatic, "Static node unexpected");

                    float outgoing = node.TransferMass + node.Mass * pathInfo.Ratio;
                    //float outgoindTotal = 0;
                    //foreach (var p in pathInfo.Parents)
                    //    outgoindTotal += p.DirectionRatio;

                    float outgoingPerParent = outgoing / pathInfo.Parents.Count;

                    foreach (var parent in pathInfo.Parents)
                    {
                        var delta = parent.EndNode.Pos - node.Pos; // Node to parent
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

                        //outgoingPerParent = outgoing * parent.DirectionRatio / outgoindTotal;

#if ENHANCED_DEBUG
                        node.OutgoingNodeswWithWeights[index].Add(new Tuple<Node, float>(parent.EndNode, outgoingPerParent));
#endif
                        node.SupportingWeights[index] -= outgoingPerParent;


                        parent.EndNode.SupportingWeights[parentIndex] += outgoingPerParent;
#if ENHANCED_DEBUG
                        parent.EndNode.SupportingNodeswWithWeights[parentIndex].Add(new Tuple<Node, float>(node, outgoingPerParent));
#endif


                        parent.EndNode.TransferMass += outgoingPerParent;
                    }
                    node.TransferMass -= outgoing;
                }
            }
        }

      

        #endregion

        #region Debug draw

        static Vector3I m_selectedCube;
        static MyCubeGrid m_selectedGrid;
        public static Vector3I SelectedCube
        {
            get { return m_selectedCube; }
            set
            {
                if (m_selectedGrid != null)
                {
                    if (m_selectedGrid.GetCubeBlock(value) != null)
                        m_selectedCube = value;
                }
            }
        }


        static float SlidingOffset = 0;

        public void DebugDraw()
        {
            SlidingOffset += 0.005f;

            if (MyPetaInputComponent.DEBUG_DRAW_PATHS)
            {
                var selcolor = Color.Aqua;
                DrawCube(m_grid.GridSize, SelectedCube, ref selcolor, null);

                if (All.ContainsKey(SelectedCube))
                {
                    var node = All[SelectedCube];

                    float spacing = 0.2f;
                    float offset = -spacing * node.Paths.Count / 2.0f;
                    float increment = spacing;

                    //    if (drawOutgoing)
                    {
                        int p = 0;
                        float ratioSum = 0;
                        foreach (var path in node.Paths)
                        {
                            DebugDrawPath(path.Value, offset, p);
                            offset += increment;
                            p++;
                            ratioSum += path.Value.Ratio;

                            if (path.Value.Parents.Count > 1)
                            {
                                foreach (var ppath in path.Value.Parents)
                                {
                                    DebugDrawPath(ppath, offset, p);
                                }
                            }
                        }

                        //foreach (var outg in node.OutgoingNodeswWithWeights)
                        //{
                        //    if (outg.Count > 0)
                        //    {
                        //        string s = "";
                        //        foreach (var outNode in outg)
                        //        {
                        //            s += outNode.Item2.ToString() + " + ";
                        //        }

                        //        Vector3 delta = outg[0].Item1.Pos - node.Pos;

                        //        Vector3D pos = Vector3D.Transform((node.Pos + delta * 0.4f) * m_grid.GridSize, m_grid.WorldMatrix);
                        //        MyRenderProxy.DebugDrawText3D(pos, s, Color.White, 0.5f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

                        //    }
                        //}

                        Vector3D selpos = Vector3D.Transform(node.Pos * m_grid.GridSize, m_grid.WorldMatrix);
                        MyRenderProxy.DebugDrawText3D(selpos + Vector3D.Up / 2, SelectedCube.ToString(), Color.White, 0.5f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

                        //foreach (var nodesSup in node.SupportingNodeswWithWeights)
                        //{
                        //    if (nodesSup.Count > 0)
                        //    {
                        //        string s = "";
                        //        foreach (var outNode in nodesSup)
                        //        {
                        //            s += outNode.Item2.ToString() + " + ";
                        //        }

                        //        Vector3 delta = nodesSup[0].Item1.Pos - node.Pos;

                        //        Vector3D pos = Vector3D.Transform((node.Pos + delta * 0.4f) * m_grid.GridSize, m_grid.WorldMatrix);
                        //        MyRenderProxy.DebugDrawText3D(pos, s, Color.White, 0.5f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

                        //    }
                        //}
                    }
                }

            }

            //MyRenderProxy.DebugDrawAABB(m_grid.PositionComp.WorldAABB, Color.White, 1, 1, false);


            if (MyPetaInputComponent.DEBUG_DRAW_TENSIONS)
            {


                bool drawTensions = true;

                var size = m_grid.GridSize;

                if (drawTensions)
                {
                    foreach (var c in All)
                    {
                        Color color = Color.Gray;
                        float tension = 0;

                        if (c.Value.IsDynamicWeight)
                            continue;

                        if (!c.Value.IsStatic)
                        {
                            //1kg cube can hold 10kg
                            //0.5kg - 5kg
                            //etc.
                            //Max. supported weight <0..1> is then TotalSupportingWeight / (mass * MaxSupportedWeight)

                            float MaxSupportedWeight = 10;
                            //tension = c.Value.TotalSupportingWeight / (c.Value.Mass * MaxSupportedWeight);
                            tension = c.Value.TotalSupportingWeight / c.Value.Mass;
                            //tension = c.Value.TotalSupportingWeight;

                            color = GetTension(tension, MaxSupportedWeight);
                        }

                        string text = tension.ToString("0.00");
                        //string text = c.Key.ToString();
                        DrawCube(size, c.Key, ref color, text);
                    }
                }
                else
                {   //Draw weights

                    foreach (var c in All)
                    {
                        Color color = GetTension(c.Value.Mass, 4);

                        string text = c.Value.Mass.ToString("0.00");
                        DrawCube(size, c.Key, ref color, text);
                    }
                }
            }
        }

        private void DebugDrawPath(PathInfo pathInfo, float offset, int index)
        {
#if ENHANCED_DEBUG
            for (int i = 0; i < pathInfo.PathNodes.Count - 1; i++)
            {
                var startNode = pathInfo.PathNodes[i];
                var endNode = pathInfo.PathNodes[i + 1];

                var startPosition = Vector3D.Transform(startNode.Pos * m_grid.GridSize, m_grid.WorldMatrix);
                var endPosition = Vector3D.Transform(endNode.Pos * m_grid.GridSize, m_grid.WorldMatrix);

                var offsetVector = new Vector3(offset);
                //MyRenderProxy.DebugDrawLine3D(startPosition + offsetVector, endPosition + offsetVector, Color.Red, Color.Red, false);
                DrawSlidingLine(endPosition + offsetVector, startPosition + offsetVector, Color.White, Color.Red);

                if (startNode.IsStatic)
                    MyRenderProxy.DebugDrawSphere(startPosition + offsetVector, 0.5f, Color.Gray, 1, false);

                if (i == pathInfo.PathNodes.Count - 2)
                {
                    MyRenderProxy.DebugDrawText3D(endPosition + offsetVector, index.ToString() + " (" + pathInfo.Ratio + ")", Color.White, 0.5f, false, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                }
            }
#endif
        }

        static void DrawSlidingLine(Vector3D startPosition, Vector3D endPosition, Color color1, Color color2)
        {
            float pieceLength = 0.1f;
            float lineLength = Vector3.Distance(startPosition, endPosition);
            Vector3D lineDir = Vector3D.Normalize(endPosition - startPosition);
            Vector3D delta =  lineDir * pieceLength;
            Color currentColor = color1;

            Vector3D min = Vector3D.Min(startPosition, endPosition);
            Vector3D max = Vector3D.Max(startPosition, endPosition);

            float startOffset = SlidingOffset - (pieceLength * 2.0f) * ((int)(SlidingOffset / (pieceLength * 2.0f))) - 2 * pieceLength;
            Vector3D currentPos = startPosition + startOffset * lineDir;

            float actualLength = 0;
            while (actualLength < lineLength)
            {
                MyRenderProxy.DebugDrawLine3D(Vector3D.Clamp(currentPos, min, max), Vector3D.Clamp(currentPos + delta, min, max), currentColor, currentColor, false);

                if (currentColor == color1) 
                    currentColor = color2; 
                else 
                    currentColor = color1;

                actualLength += pieceLength;
                currentPos += delta;
            }

            //MyRenderProxy.DebugDrawLine3D(startPosition, endPosition, color1, color2, false);
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

        private void DrawCube(float size, Vector3I pos, ref Color color, string text)
        {
            var local = Matrix.CreateScale(size * 1.02f) * Matrix.CreateTranslation(pos * size);
            Matrix box = local * m_grid.WorldMatrix;

            MyRenderProxy.DebugDrawOBB(box, color.ToVector3(), 0.5f, true, true);

            if (DrawText && text != null && text != "0.00" && (Vector3D.Distance(box.Translation, Sandbox.Game.World.MySector.MainCamera.Position) < 30)) 
                 MyRenderProxy.DebugDrawText3D(box.Translation, text, Color.White, 0.5f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
        }

        
        #endregion



        public void Add(MySlimBlock block)
        {
        }

        public void Remove(MySlimBlock block)
        {
        }

        public bool Simulate(float deltaTime)
        {
            return Refresh();
        }

        
        public bool IsConnectionFine(MySlimBlock blockA, MySlimBlock blockB)
        {
            return true;

        }

        public float GetSupportedWeight(Vector3I pos)
        {
            Node node;
            if (All.TryGetValue(pos, out node))
            {
                return node.TotalSupportingWeight;
            }

            return 0;
        }

        public float GetTension(Vector3I pos)
        {
            Node node;
            if (All.TryGetValue(pos, out node))
            {
                return node.TotalSupportingWeight / node.Mass;
            }

            return 0;
        }
    }
}
