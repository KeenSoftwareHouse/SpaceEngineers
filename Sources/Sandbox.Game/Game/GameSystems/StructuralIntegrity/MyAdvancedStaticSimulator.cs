#define ENHANCED_DEBUG
#region Using

using ParallelTasks;
using Sandbox.Common;
using Sandbox.Definitions;
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
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Utils;

#endregion

namespace Sandbox.Game.GameSystems.StructuralIntegrity
{
    partial class MyAdvancedStaticSimulator : IMyIntegritySimulator
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
            //public List<Node> Parents = new List<Node>();
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
            public MyPhysicalMaterialDefinition PhysicalMaterial;

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

        class GridSimulationData
        {
            public int BlockCount;
            public float TotalMax;
            public HashSet<Node> StaticBlocks = new HashSet<Node>();
            public HashSet<Node> DynamicBlocks = new HashSet<Node>();
            public Dictionary<Vector3I, Node> All = new Dictionary<Vector3I, Node>();

            public Dictionary<Vector3I, float> DynamicWeights = new Dictionary<Vector3I, float>();
            public HashSet<MyCubeGrid> ConstrainedGrid = new HashSet<MyCubeGrid>();
            public Queue<PathInfo> Queue = new Queue<PathInfo>();
        }


        #endregion

        #region Global data

        public static bool Multithreaded = true;

        private static float m_closestDistanceThreshold = 3;

        bool m_needsRecalc = false;

        public static bool DrawText = true;
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
        int DYNAMIC_UPDATE_DELAY = 3;
        Dictionary<MyEntity, CollidingEntityInfo> m_collidingEntities = new Dictionary<MyEntity, CollidingEntityInfo>();
        int m_frameCounter = 0;
        int m_lastFrameCollision = 0;


        GridSimulationData m_finishedData = new GridSimulationData();
        GridSimulationData m_simulatedData = new GridSimulationData();

        bool m_simulationDataPrepared = false;
        bool m_simulationDataReady = false;
        bool m_simulationInProgress = false;

        #endregion

        #region Static methods

        public static float MassToSI(float mass)
        {
            return mass / 30000; //weight of full stone cube
        }

        public static float MassFromSI(float mass)
        {
            return mass * 30000; //weight of full stone cube
        }

        #endregion

        #region Constructor

        public MyAdvancedStaticSimulator(MyCubeGrid grid)
        {
            m_grid = grid;
            m_selectedGrid = m_grid;
            if (m_grid.BlocksCount > 0)
                SelectedCube = m_grid.GetBlocks().First().Position;
        }

        #endregion

        #region Simulation

        public bool Simulate(float deltaTime)
        {
            if (m_grid.Physics == null)
                return false;

            m_frameCounter++;

            if (m_simulationDataReady)
            {
                SwapSimulatedDatas();
                m_simulationDataReady = false;
                m_simulationDataPrepared = true;
                return true;
            }

            // Change detected by changing block count
            if (m_grid.GetBlocks().Count == m_finishedData.BlockCount && m_simulationDataPrepared && !m_needsRecalc)
               return false;

            m_needsRecalc = true;

            if (m_simulationInProgress)
                return false;

            m_simulationInProgress = true;

            //Loading blocks must be synchronized, we dont support multithreaded blocks management (add, remove)
            LoadBlocks(m_simulatedData);


            if (!Multithreaded)
            {
                m_needsRecalc = false;

                using (Stats.Timing.Measure("SI TOTAL - FindAndCaculateAdvancedStatic", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
                {
                    FindAndCaculateFromAdvancedStatic(m_simulatedData);
                }

                SwapSimulatedDatas();

                m_simulationInProgress = false;
                m_simulationDataPrepared = true;
            }
            else
            {
                m_needsRecalc = false;

                Parallel.Start(() => 
                { 
                    FindAndCaculateFromAdvancedStatic(m_simulatedData); 
                },
                    () =>
                {
                    m_simulationInProgress = false;
                    m_simulationDataReady = true;
                });
            }

            return true;
        }

        void SwapSimulatedDatas()
        {
            var temp = m_finishedData;
            m_finishedData = m_simulatedData;
            m_simulatedData = temp;
        }

        #endregion

        #region Load blocks

        private void LoadBlocks(GridSimulationData simData)
        {
            simData.BlockCount = m_grid.GetBlocks().Count;

            simData.All.Clear();
            simData.DynamicBlocks.Clear();
            simData.StaticBlocks.Clear();

            using (Stats.Timing.Measure("SI - Collect", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
            {
                // Store blocks
                foreach (var block in m_grid.GetBlocks())
                {
                    if (simData.All.ContainsKey(block.Position))
                    {
                        Debug.Fail("Same blocks in grid!");
                        continue;
                    }

                    bool isStatic = m_grid.Physics.Shape.BlocksConnectedToWorld.Contains(block.Position);

                    if (isStatic)
                    {
                        var n = new Node(block.Position, true);
                        n.PhysicalMaterial = block.BlockDefinition.PhysicalMaterial;
                        simData.StaticBlocks.Add(n);
                        simData.All.Add(block.Position, n);
                    }
                    else 
                    {
                        

                        float mass = m_grid.Physics.Shape.GetBlockMass(block.Position);
                        var cubeMass = MassToSI(mass);

                        var physicalMaterial = block.BlockDefinition.PhysicalMaterial;
                        if (block.FatBlock is MyCompoundCubeBlock)
                        {
                            var compBlock = block.FatBlock as MyCompoundCubeBlock;

                            physicalMaterial = compBlock.GetBlocks().First().BlockDefinition.PhysicalMaterial;

                            //Simulate blocks where or pieces are generated
                            bool allAreGenerated = true;
                            foreach (var b in compBlock.GetBlocks())
                            {
                                if (!b.BlockDefinition.IsGeneratedBlock)
                                {
                                    allAreGenerated = false;
                                    break;
                                }
                            }

                            bool isGenerated = true;
                            foreach (var b in compBlock.GetBlocks())
                            {
                                if (!b.BlockDefinition.IsGeneratedBlock)
                                {
                                    isGenerated = false;
                                    break;
                                }
                                else
                                    if (b.BlockDefinition.IsGeneratedBlock && b.BlockDefinition.PhysicalMaterial.Id.SubtypeName == "Stone")
                                    {
                                        isGenerated = false;
                                        break;
                                    }
                                    else
                                        if (b.BlockDefinition.IsGeneratedBlock && b.BlockDefinition.PhysicalMaterial.Id.SubtypeName == "RoofTile" && allAreGenerated)
                                        {
                                            isGenerated = false;
                                            cubeMass *= 6f;
                                            break;
                                        }
                                        else
                                            if (b.BlockDefinition.IsGeneratedBlock && b.BlockDefinition.PhysicalMaterial.Id.SubtypeName == "RoofWood" && allAreGenerated)
                                            {
                                                isGenerated = false;
                                                cubeMass *= 3f;
                                                break;
                                            }
                                            else
                                                if (b.BlockDefinition.IsGeneratedBlock && b.BlockDefinition.PhysicalMaterial.Id.SubtypeName == "RoofHay" && allAreGenerated)
                                                {
                                                    isGenerated = false;
                                                    cubeMass *= 1.2f;
                                                    break;
                                                }
                                                else
                                                {
                                                }
                            }

                            //we dont want to simulate these pieces..
                            if (isGenerated)
                                continue;
                        }

                        Vector3I pos = block.Min;
                        float volumeRecip = 1.0f / block.BlockDefinition.Size.Size;
                        for (var it = new Vector3I_RangeIterator(ref block.Min, ref block.Max); it.IsValid(); it.GetNext(out pos))
                        {
                            var node = new Node(pos, false);

                            node.Mass = cubeMass * volumeRecip;

                            node.PhysicalMaterial = physicalMaterial;

                            simData.DynamicBlocks.Add(node);
                            simData.All.Add(pos, node);
                        }
                    }
                }

                foreach (var block in simData.DynamicWeights)
                {
                    if (simData.All.ContainsKey(block.Key))
                    {
                        simData.All[block.Key].Mass += block.Value;
                    }
                    else
                    {
                        var node = new Node(block.Key, false);
                        node.Mass = simData.DynamicWeights[block.Key];
                        node.IsDynamicWeight = true;
                        simData.DynamicBlocks.Add(node);
                        simData.All.Add(block.Key, node);
                    }
                }
            }

            m_grid.Physics.ContactPointCallback -= Physics_ContactPointCallback;
            m_grid.Physics.ContactPointCallback += Physics_ContactPointCallback;

            AddNeighbours(simData);
        }


        private void AddNeighbours(GridSimulationData simData)
        {
            using (Stats.Timing.Measure("SI - AddNeighbours", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
            {
                foreach (var node in simData.All)
                {
                    foreach (var offset in Offsets)
                    {
                        Node neighbour;
                        if (simData.All.TryGetValue(node.Value.Pos + offset, out neighbour))
                        {
                            node.Value.Neighbours.Add(neighbour);
                        }
                    }
                }
            }
        }

        #endregion

        #region Dynamic SI handlers

        void Physics_ContactPointCallback(ref Engine.Physics.MyPhysics.MyContactPointEvent e)
        {
          //  RestoreDynamicMasses();
            //if (m_frameCounter > DYNAMIC_UPDATE_DELAY)
            //    m_frameCounter = 0;
            //else
            //    return;

            //if (m_lastFrameCollision != m_frameCounter)
            //    DynamicWeights.Clear();

            //MyGridContactInfo info = new MyGridContactInfo(ref e.ContactPointEvent, m_grid);

            //if (info.CollidingEntity == null)
            //    return;

            //if (info.CollidingEntity.Physics == null)
            //    return;

            //if (info.CollidingEntity.Physics.IsStatic)
            //    return;

            //float speed = info.CollidingEntity.Physics.LinearVelocity.Length();
            //if (speed < 0.1f)
            //    return;

            //Vector3I blockPos = m_grid.WorldToGridInteger(info.ContactPosition + Vector3.Up * 0.25f);

            //float dot = Vector3.Dot(Vector3.Normalize(info.CollidingEntity.Physics.LinearVelocity), Vector3.Down);

            //float collidingMass = 0;
            //m_constrainedGrid.Clear();
            //MyCubeGrid collidingGrid = info.CollidingEntity as MyCubeGrid;
            //if (collidingGrid != null)
            //{
                
            //    m_constrainedGrid.Add(collidingGrid);

            //    AddConstrainedGrids(collidingGrid);

            //    foreach (var grid in m_constrainedGrid)
            //    {
            //        collidingMass += grid.Physics.Mass;
            //    }
            //}
            //else
            //{
            //    collidingMass = info.CollidingEntity.Physics.Mass;
            //}

            //collidingMass = info.CollidingEntity is Sandbox.Game.Entities.Character.MyCharacter ? MassToSI(collidingMass) : MassToSI(MyDestructionHelper.MassFromHavok(collidingMass));

            //float siMass = collidingMass * MyPetaInputComponent.SI_DYNAMICS_MULTIPLIER;

            ////if (dot < 0) //impact from downside
            ////    return;

            
            //float impact =  siMass * speed * dot + siMass;
            //if (impact < 0)
            //    return;

            //if (m_grid.GridSizeEnum == Common.ObjectBuilders.MyCubeSize.Large)
            //{
            //}

            //DynamicWeights[blockPos] = impact;

            //ForceRecalc = true;

            //m_lastFrameCollision = m_frameCounter;

            //if (!m_collidingEntities.ContainsKey(info.CollidingEntity))
            //{
            //    m_collidingEntities.Add(info.CollidingEntity, new CollidingEntityInfo()
            //    {
            //        Position = blockPos,
            //        FrameTime = m_frameCounter
            //    });
            //    info.CollidingEntity.PositionComp.OnPositionChanged += PositionComp_OnPositionChanged;
            //}
            //else
            //    m_collidingEntities[info.CollidingEntity].FrameTime = m_frameCounter;
        }

        void PositionComp_OnPositionChanged(MyPositionComponentBase obj)
        {
            //if (m_collidingEntities.ContainsKey((MyEntity)obj.Entity))
            //{
            //    if (m_frameCounter - m_collidingEntities[(MyEntity)obj.Entity].FrameTime > 20)
            //    { //Object not contacted with grid for 20 frames
            //        obj.OnPositionChanged -= PositionComp_OnPositionChanged;
            //        DynamicWeights.Remove(m_collidingEntities[(MyEntity)obj.Entity].Position);
            //        m_collidingEntities.Remove((MyEntity)obj.Entity);
            //        ForceRecalc = true;
            //    }
            //}
        }


        void AddConstrainedGrids(MyCubeGrid collidingGrid)
        {
            //if (collidingGrid.Physics == null)
            //    return;

            //foreach (var constraint in collidingGrid.Physics.Constraints)
            //{
            //    if (constraint.RigidBodyA == null) continue;
            //    if (constraint.RigidBodyB == null) continue;

            //    if (constraint.RigidBodyA.GetBody() == null) continue;
            //    if (constraint.RigidBodyB.GetBody() == null) continue;

            //    var gridA = constraint.RigidBodyA.GetBody().Entity as MyCubeGrid;
            //    var gridB = constraint.RigidBodyB.GetBody().Entity as MyCubeGrid;

            //    if (gridA != null)
            //    {
            //        if (!m_constrainedGrid.Contains(gridA))
            //        {
            //            m_constrainedGrid.Add(gridA);
            //            AddConstrainedGrids(gridA);
            //        }
            //    }

            //    if (gridB != null)
            //    {
            //        if (!m_constrainedGrid.Contains(gridB))
            //        {
            //            m_constrainedGrid.Add(gridB);
            //            AddConstrainedGrids(gridB);
            //        }
            //    }
            //}
        }

        void RestoreDynamicMasses()
        {
            // Store blocks
            //foreach (var block in DynamicBlocks)
            //{
            //    var cubeMass = MassToSI(m_grid.Physics.Shape.GetBlockMass(block.Pos));
            //    block.Mass = cubeMass;
            //}
        }


        #endregion

        #region FindAndCaculateFromAdvancedStatic

        private void FindAndCaculateFromAdvancedStatic(GridSimulationData simData)
        {
            // Instead of using uniform distribution of support between all fixed nodes, calculate non-uniform distribution based on vectors for few closest nodes
            // Use this distribution when setting support weight and transferring mass
            // SupportingWeights: x * support
            // ? TransferMass: x * numStaticBlocksForCurrentDynamicBlock * TransferMass

            int keepLookingDistance = (int)ClosestDistanceThreshold; // How far keep looking when closest static block is found
            simData.Queue.Clear();

            // Set initial distance to max for all dynamic
            foreach (var dyn in simData.DynamicBlocks)
            {
                dyn.Distance = int.MaxValue;
            }

            // Wide search from all static blocks
            foreach (var stat in simData.StaticBlocks)
            {
                stat.Distance = 0;
                var path = new PathInfo() { EndNode = stat, StartNode = stat, Distance = 0, DirectionRatio = 1 };
#if ENHANCED_DEBUG
                path.PathNodes.Add(stat);
#endif
                simData.Queue.Enqueue(path);
            }

            while (simData.Queue.Count > 0)
            {
                var path = simData.Queue.Dequeue();
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
                            float t = neighbour.PhysicalMaterial.HorisontalTransmissionMultiplier * path.EndNode.PhysicalMaterial.HorisontalTransmissionMultiplier;

                            float massRatio = MathHelper.Clamp(path.EndNode.Mass / (neighbour.Mass + path.EndNode.Mass), 0, 1);

                            t *= neighbour.Mass * massRatio; //Horisontal transmission is very low on weak objects (floor, roof). Should be correctly get from mount point area
                            float[] horisontalTransmission = new float[] { t, 1, t };

                            int component = ((Vector3D)(neighbour.Pos - path.EndNode.Pos)).AbsMaxComponent();
                            neighbourPath.DirectionRatio = path.DirectionRatio * DirectionRatios[component] * horisontalTransmission[component];
                            neighbour.Paths.Add(path.StartNode, neighbourPath);
                            simData.Queue.Enqueue(neighbourPath);
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
            foreach (var dyn in simData.DynamicBlocks)
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
                        {
                            s.Value.Ratio /= totalAngles;
                            if (s.Value.Ratio < 0.000001f)
                                s.Value.Ratio = 0;
                        }
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
            foreach (var staticBlock in simData.StaticBlocks)
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

                    if (node.Pos == new Vector3I(-1, 1, 0))
                    {
                    }


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
                        if (index == 0 || index == 2 || index == 3 || index == 5)
                        {
            
                            node.SupportingWeights[index] -= node.PhysicalMaterial.HorisontalFragility * outgoingPerParent;
                        }
                        else
                            node.SupportingWeights[index] -= outgoingPerParent;

                        if (parentIndex == 0 || parentIndex == 2 || parentIndex == 3 || parentIndex == 5)
                        {
                            parent.EndNode.SupportingWeights[parentIndex] += parent.EndNode.PhysicalMaterial.HorisontalFragility * outgoingPerParent;
                        }
                        else
                            parent.EndNode.SupportingWeights[parentIndex] +=  outgoingPerParent;
#if ENHANCED_DEBUG
                        parent.EndNode.SupportingNodeswWithWeights[parentIndex].Add(new Tuple<Node, float>(node, outgoingPerParent));
#endif


                        parent.EndNode.TransferMass += outgoingPerParent;
                    }
                    node.TransferMass -= outgoing;
                }
            }


            using (Stats.Timing.Measure("SI - Sum", VRage.Stats.MyStatTypeEnum.Sum | VRage.Stats.MyStatTypeEnum.DontDisappearFlag))
            {
                foreach (var node in simData.All)
                {
                    //node.Value.TotalSupportingWeight = 0;
                }

                foreach (var node in simData.All)
                {
                    if (node.Key == new Vector3I(-1, 1, 0))
                    {
                    }

                    float sum = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        float sideWeight = node.Value.SupportingWeights[i];

                        sum += Math.Abs(sideWeight);
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
                List<Node> supportingNeighbours = new List<Node>();
                foreach (var node in simData.All)
                {
                    supportingNeighbours.Clear();

                    float totalSharedSupport = node.Value.TotalSupportingWeight;
                    float totalMass = node.Value.Mass;

                    foreach (var neighbour in node.Value.Neighbours)
                    {
                        bool isHorisontalNeighbour = neighbour.Pos.Y == node.Key.Y;
                        bool isVerticallySupported = neighbour.Paths.Any(x => (x.Key.Pos.X == node.Key.X && x.Key.Pos.Z == node.Key.Z));

                        if (isHorisontalNeighbour && isVerticallySupported)
                        {
                            totalSharedSupport += neighbour.TotalSupportingWeight;
                            totalMass += neighbour.Mass;
                            supportingNeighbours.Add(neighbour);
                        }
                    }

                    if (supportingNeighbours.Count > 0)
                    {
                        float supportPerNode = totalSharedSupport / totalMass;
                        foreach (var neighbour in supportingNeighbours)
                        {
                            neighbour.TotalSupportingWeight = supportPerNode * neighbour.Mass;
                        }
                        node.Value.TotalSupportingWeight = supportPerNode * node.Value.Mass;
                    }
                }

                foreach (var node in simData.All)
                {
                    simData.TotalMax = Math.Max(simData.TotalMax, node.Value.TotalSupportingWeight);
                }

                // var timeMs = (Stopwatch.GetTimestamp() - ts) / (float)Stopwatch.Frequency * 1000.0f;
                //Console.WriteLine(String.Format("Generated structural integrity, time: {0}ms, object name: {1}", timeMs, m_grid.ToString()));
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

        public void Draw()
        {
            if (!m_simulationDataPrepared)
                return;

            foreach (var c in m_finishedData.All)
            {
                Color color = Color.Gray;
                float tension = 0;

                if (c.Value.IsDynamicWeight)
                    continue;

                if (!c.Value.IsStatic)
                {
                    float MaxSupportedWeight = 10;
                    tension = c.Value.TotalSupportingWeight / (c.Value.Mass * c.Value.PhysicalMaterial.SupportMultiplier);
                    
                    color = GetTension(tension, MaxSupportedWeight);
                }

                string text = null;
                //string text = tension.ToString("0.00");
                //string text = c.Key.ToString();
                DrawCube(m_grid.GridSize, c.Key, ref color, text);
            }
        }

        public void DebugDraw()
        {
            if (!m_simulationDataPrepared)
                return;

            SlidingOffset += 0.005f;

            if (MyPetaInputComponent.DEBUG_DRAW_PATHS)
            {
                var selcolor = Color.Aqua;
                DrawCube(m_grid.GridSize, SelectedCube, ref selcolor, null);

                if (m_finishedData.All.ContainsKey(SelectedCube))
                {
                    var node = m_finishedData.All[SelectedCube];

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
                    foreach (var c in m_finishedData.All)
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
                            tension = c.Value.TotalSupportingWeight / (c.Value.Mass * c.Value.PhysicalMaterial.SupportMultiplier);
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

                    foreach (var c in m_finishedData.All)
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
            m_selectedGrid = block.CubeGrid;
            m_selectedCube = block.Position;
        }

        public void Remove(MySlimBlock block)
        {
            m_selectedGrid = block.CubeGrid;
        }

        
        public bool IsConnectionFine(MySlimBlock blockA, MySlimBlock blockB)
        {
            return true;

        }

        public float GetSupportedWeight(Vector3I pos)
        {
            if (!m_simulationDataPrepared)
                return 0;

            Node node;
            if (m_finishedData.All.TryGetValue(pos, out node))
            {
                return node.TotalSupportingWeight;
            }

            return 0;
        }

        public float GetTension(Vector3I pos)
        {
            if (!m_simulationDataPrepared)
                return 0;

            Node node;
            if (m_finishedData.All.TryGetValue(pos, out node))
            {
                return node.TotalSupportingWeight / (node.Mass * node.PhysicalMaterial.SupportMultiplier);
            }

            return 0;
        }

        public void Close()
        {
            if (m_grid.Physics != null)
                m_grid.Physics.ContactPointCallback -= Physics_ContactPointCallback;
        }

        public void ForceRecalc()
        {
            m_needsRecalc = true;
        }
    }
}
