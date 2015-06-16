using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Graphics;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using VRage;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Game.World;
using VRage.Utils;


namespace Sandbox.Game.Entities.Cube
{
    public class MyGridShape : IDisposable
    {
        public const float BreakImpulse = 30000;

        private MyVoxelSegmentation m_segmenter = null;// = new MyVoxelSegmentation();

        private MyCubeBlockCollector m_blockCollector = new MyCubeBlockCollector();
        private HkMassProperties m_massProperties = new HkMassProperties();

        private List<HkMassElement> m_tmpElements = new List<HkMassElement>();
        private List<HkShape> m_tmpShapes = new List<HkShape>();

        private HashSet<MySlimBlock> m_tmpRemovedBlocks = new HashSet<MySlimBlock>();
        private HashSet<Vector3I> m_tmpRemovedCubes = new HashSet<Vector3I>();
        private HashSet<Vector3I> m_tmpAdditionalCubes = new HashSet<Vector3I>();

        private MyCubeGrid m_grid;
        private HkGridShape m_root;

        private Dictionary<Vector3I, HkMassElement> m_massElements;
        public HkdBreakableShape BreakableShape { get; set; }
        private Dictionary<Vector3I, HkdShapeInstanceInfo> m_blocksShapes = new Dictionary<Vector3I, HkdShapeInstanceInfo>(); //dic for connectivity>

        // TODO: Use this and build final mass properties from mass properties calculated for cells, cell size 8 will be probably fine
        //private MySparseGrid<HkMassElement, HkMassProperties> m_massElements;

        private static List<Vector3S> m_removalMins = new List<Vector3S>();
        private static List<Vector3S> m_removalMaxes = new List<Vector3S>();
        private static List<bool> m_removalResults = new List<bool>();

        public HkMassProperties? MassProperties
        {
            get { return m_grid.IsStatic ? null : (HkMassProperties?)m_massProperties; }
        }

        public MyGridShape(MyCubeGrid grid)
        {
            m_grid = grid;
            if (MyPerGameSettings.Destruction)
                return;
            if (MyPerGameSettings.UseGridSegmenter)
                m_segmenter = new MyVoxelSegmentation();
            if (!grid.IsStatic)
            {
                m_massElements = new Dictionary<Vector3I, HkMassElement>();
            }

            try
            {
                m_blockCollector.Collect(grid, m_segmenter, MyVoxelSegmentationType.Simple, m_massElements);
                m_root = new HkGridShape(m_grid.GridSize, HkReferencePolicy.None);

                AddShapesFromCollector();

                if (!m_grid.IsStatic)
                {
                    UpdateMassProperties();
                }
            }
            finally
            {
                m_blockCollector.Clear();
            }
        }

        private void AddShapesFromCollector()
        {
            int num = 0;
            for (int i = 0; i < m_blockCollector.ShapeInfos.Count; i++)
            {
                var info = m_blockCollector.ShapeInfos[i];

                // TODO: optimize and move it to native code
                m_tmpShapes.Clear();
                for (int j = 0; j < info.Count; j++)
                {
                    m_tmpShapes.Add(m_blockCollector.Shapes[num + j]);
                }
                num += info.Count;
                m_root.AddShapes(m_tmpShapes, new Vector3S(info.Min), new Vector3S(info.Max));
            }
            m_tmpShapes.Clear();
        }

        private void UpdateMassProperties()
        {
            m_tmpElements.Clear();
            foreach (var kv in m_massElements)
            {
                m_tmpElements.Add(kv.Value);
            }
            Debug.Assert(m_massElements.Count > 0, "Mass can't be zero, in that case, grid should not be created");

            // HACK: this prevents crash, but it's generally on wrong place, but we don't know how to handle it higher on call stack
            if (m_tmpElements.Count > 0)
            {
                try
                {
                    ProfilerShort.Begin("TensorComputer");
                    m_massProperties = HkInertiaTensorComputer.CombineMassProperties(m_tmpElements);
                    ProfilerShort.End();
                }
                catch
                {
                    MySandboxGame.Log.WriteLine("Error building mopp, element count: " + m_tmpElements.Count);
                    MySandboxGame.Log.WriteLine("Elements: " + m_tmpElements.Select(s => s.Properties.Mass.ToString() + "kg").Aggregate((a, b) => a + ", " + b));
                    throw;
                }
            }
            m_tmpElements.Clear();
        }

        public void Dispose()
        {
            foreach(var connnList in m_connections.Values)
            {
                foreach(var c in connnList)
                {
                    c.RemoveReference();
                }
            }
            m_connections.Clear();
            if (BreakableShape.IsValid())
            {
                BreakableShape.RemoveReference();
                BreakableShape.ClearHandle();
            }
            foreach (var shape in m_blocksShapes.Values)
            {
                if (shape.Shape.IsValid())
                    shape.Shape.RemoveReference();
                shape.RemoveReference();
            }
            m_blocksShapes.Clear();

            if (!MyPerGameSettings.Destruction)
                m_root.Base.RemoveReference();
        }

        public void RefreshBlocks(HkRigidBody rigidBody, HkRigidBody rigidBody2, MyGridPhysics.MyDirtyBlocksInfo dirtyCubesInfo, HkdBreakableBody destructionBody = null)
        {
            ProfilerShort.Begin("Refresh shape");

            if (m_grid.Physics.HavokWorld != null)
                UnmarkBreakable(m_grid.Physics.HavokWorld, rigidBody);

            UpdateDirtyBlocks(dirtyCubesInfo.DirtyBlocks);
            UpdateMass(rigidBody);
            UpdateShape(rigidBody, rigidBody2, destructionBody);

            if (m_grid.Physics.HavokWorld != null)
                MarkBreakable(m_grid.Physics.HavokWorld, rigidBody);

            ProfilerShort.End();
        }

        [Conditional("DEBUG")]
        private void CheckShapePositions(List<MyCubeBlockCollector.ShapeInfo> infos)
        {
            foreach (var info in infos)
            {
                Vector3I current;
                for (current.X = info.Min.X; current.X <= info.Max.X; ++current.X)
                {
                    for (current.Y = info.Min.Y; current.Y <= info.Max.Y; ++current.Y)
                    {
                        for (current.Z = info.Min.Z; current.Z <= info.Max.Z; ++current.Z)
                        {
                            Debug.Assert(!m_root.Contains((short)current.X, (short)current.Y, (short)current.Z), "Shape already exists in grid shape on position: " + current.ToString());
                        }
                    }
                }
            }
        }


        private static void ExpandBlock(Vector3I cubePos, MyCubeGrid grid, HashSet<MySlimBlock> existingBlocks, HashSet<Vector3I> checkList, HashSet<Vector3I> expandResult)
        {
            var block = grid.GetCubeBlock(cubePos);
            if (block != null && existingBlocks.Add(block))
            {
                Vector3I current;
                for (current.X = block.Min.X; current.X <= block.Max.X; ++current.X)
                    for (current.Y = block.Min.Y; current.Y <= block.Max.Y; ++current.Y)
                        for (current.Z = block.Min.Z; current.Z <= block.Max.Z; ++current.Z)
                        {
                            if (!checkList.Contains(current))
                            {
                                expandResult.Add(current);
                            }
                        }
            }
        }

        private static void ExpandBlock(Vector3I cubePos, MyCubeGrid grid, HashSet<MySlimBlock> existingBlocks, HashSet<Vector3I> expandResult)
        {
            var block = grid.GetCubeBlock(cubePos);
            if (block != null && existingBlocks.Add(block))
            {
                Vector3I current;
                for (current.X = block.Min.X; current.X <= block.Max.X; ++current.X)
                    for (current.Y = block.Min.Y; current.Y <= block.Max.Y; ++current.Y)
                        for (current.Z = block.Min.Z; current.Z <= block.Max.Z; ++current.Z)
                        {
                            expandResult.Add(current);
                        }
            }
        }

        private HashSet<Vector3I> m_updateConnections = new HashSet<Vector3I>();
        public void UpdateDirtyBlocks(HashSet<Vector3I> dirtyCubes, bool recreateShape = true)
        {
            ProfilerShort.Begin("Update physics");
            if (dirtyCubes.Count > 0)
            {
                if (MyPerGameSettings.Destruction && BreakableShape.IsValid())
                {
                    ProfilerShort.Begin("UpdateShapeList");
                    int newShapes = 0;
                    HashSet<MySlimBlock> newBlocks = new HashSet<MySlimBlock>();
                    ProfilerShort.Begin("Dirty");
                    foreach (var dirty in dirtyCubes)
                    {
                        UpdateConnections(dirty);
                        BlocksConnectedToWorld.Remove(dirty);
                        if (m_blocksShapes.ContainsKey(dirty))
                        {
                            var toRemove = m_blocksShapes[dirty];
                            toRemove.Shape.RemoveReference();
                            toRemove.RemoveReference();
                            m_blocksShapes.Remove(dirty);
                        }

                        var b = m_grid.GetCubeBlock(dirty);
                        if (b == null || newBlocks.Contains(b))
                            continue;
                        newBlocks.Add(b);
                        newShapes++;
                    }
                    ProfilerShort.BeginNextBlock("NewBlocks");
                    foreach (var b in newBlocks)
                    {
                        Matrix m;
                        var breakableShape = CreateBlockShape(b, out m);
                        if (breakableShape.HasValue)
                        {
                            Debug.Assert(!m_blocksShapes.ContainsKey(b.Position), "Shape for this block already exists!");
                            m_blocksShapes[b.Position] = new HkdShapeInstanceInfo(breakableShape.Value, m);
                        }
                    }

                    foreach (var shapeInstance in m_blocksShapes.Values)
                        m_shapeInfosList.Add(shapeInstance);
                    ProfilerShort.BeginNextBlock("ConnectionsToWorld");
                    if (newBlocks.Count > 0)
                        FindConnectionsToWorld(newBlocks);
                    ProfilerShort.End();
                    ProfilerShort.End();

                    if (recreateShape)
                    {
                        ProfilerShort.Begin("CreateNewCompound");
                        BreakableShape.RemoveReference();
                        BreakableShape = new HkdCompoundBreakableShape(null, m_shapeInfosList);
                        BreakableShape.SetChildrenParent(BreakableShape);
                        BreakableShape.BuildMassProperties(ref m_massProperties);
                        //(BreakableShape as HkdCompoundBreakableShape).RecalcMassPropsFromChildren();
                        BreakableShape.SetStrenghtRecursively(Sandbox.MyDestructionConstants.STRENGTH, 0.7f);
                        ProfilerShort.End();
                    }
                    ProfilerShort.Begin("CreateConnections");
                    //CreateConnectionsManually(BreakableShape);
                    UpdateConnectionsManually(BreakableShape, m_updateConnections);
                    m_updateConnections.Clear();
                    AddConnections();
                    Debug.Assert(m_connectionsToAddCache.Count == 0);
                    ProfilerShort.End();
                    m_shapeInfosList.Clear();
                }
                else
                {
                    try
                    {
                        ProfilerShort.Begin("Expand to blocks");
                        foreach (var cubePos in dirtyCubes)
                        {
                            if (m_tmpRemovedCubes.Add(cubePos))
                            {
                                ExpandBlock(cubePos, m_grid, m_tmpRemovedBlocks, m_tmpRemovedCubes);
                            }
                        }
                        ProfilerShort.End();

                        ProfilerShort.Begin("Remove first");
                        Vector3I current;
                        m_removalMins.Clear();
                        m_removalMaxes.Clear();
                        m_removalResults.Clear();
                        m_root.RemoveShapes(m_tmpRemovedCubes, m_removalMins, m_removalMaxes, m_removalResults);
                        for (int i = 0; i < m_removalMins.Count; ++i)
                        {
                            if (m_removalResults[i])
                            {
                                for (current.X = m_removalMins[i].X; current.X <= m_removalMaxes[i].X; ++current.X)
                                    for (current.Y = m_removalMins[i].Y; current.Y <= m_removalMaxes[i].Y; ++current.Y)
                                        for (current.Z = m_removalMins[i].Z; current.Z <= m_removalMaxes[i].Z; ++current.Z)
                                        {
                                            if (m_tmpRemovedCubes.Add(current)) // If it's new position, not already in dirty
                                            {
                                                // This has to be expanded to whole blocks and processed again
                                                ExpandBlock(current, m_grid, m_tmpRemovedBlocks, m_tmpRemovedCubes, m_tmpAdditionalCubes);
                                            }
                                        }
                            }
                        }
                        ProfilerShort.End();

                        ProfilerShort.Begin("Remove additional");
                        while (m_tmpAdditionalCubes.Count > 0)
                        {
                            m_removalMins.Clear();
                            m_removalMaxes.Clear();
                            m_removalResults.Clear();
                            m_root.RemoveShapes(m_tmpAdditionalCubes, m_removalMins, m_removalMaxes, m_removalResults);
                            m_tmpAdditionalCubes.Clear();
                            for (int i = 0; i < m_removalMins.Count; ++i)
                            {
                                if (m_removalResults[i])
                                {
                                    for (current.X = m_removalMins[i].X; current.X <= m_removalMaxes[i].X; ++current.X)
                                        for (current.Y = m_removalMins[i].Y; current.Y <= m_removalMaxes[i].Y; ++current.Y)
                                            for (current.Z = m_removalMins[i].Z; current.Z <= m_removalMaxes[i].Z; ++current.Z)
                                            {
                                                if (m_tmpRemovedCubes.Add(current)) // If it's new position, not already in dirty
                                                {
                                                    // This has to be expanded to whole blocks and processed again
                                                    ExpandBlock(current, m_grid, m_tmpRemovedBlocks, m_tmpRemovedCubes, m_tmpAdditionalCubes);
                                                }
                                            }
                                }
                            }
                        }
                        ProfilerShort.End();

                        ProfilerShort.Begin("Recollect");
                        m_blockCollector.CollectArea(m_grid, m_tmpRemovedCubes, m_segmenter, MyVoxelSegmentationType.Simple, m_massElements);
                        ProfilerShort.End();

                        ProfilerShort.Begin("Debug-CheckPositions");
                        CheckShapePositions(m_blockCollector.ShapeInfos);
                        ProfilerShort.End();

                        ProfilerShort.Begin("Add");
                        AddShapesFromCollector();
                        ProfilerShort.End();
                    }
                    finally
                    {
                        m_blockCollector.Clear();
                        m_tmpRemovedBlocks.Clear();
                        m_tmpRemovedCubes.Clear();
                        m_tmpAdditionalCubes.Clear();
                    }
                }
            }
            ProfilerShort.End();
        }

        private void UpdateConnections(Vector3I dirty)
        {
            var lst = new List<Vector3I>(7);
            lst.Add(dirty);
            lst.Add(dirty + Vector3I.Up);
            lst.Add(dirty + Vector3I.Down);
            lst.Add(dirty + Vector3I.Left);
            lst.Add(dirty + Vector3I.Right);
            lst.Add(dirty + Vector3I.Forward);
            lst.Add(dirty + Vector3I.Backward);
            foreach(var v in lst)
            {
                if (m_connections.ContainsKey(v))
                {
                    foreach (var c in m_connections[v])
                        c.RemoveReference();
                    m_connections[v].Clear();
                }
                var cube = m_grid.GetCubeBlock(v);
                if (cube != null)
                {
                    if (m_connections.ContainsKey(cube.Position))
                    {
                        foreach (var c in m_connections[cube.Position])
                            c.RemoveReference();
                        m_connections[cube.Position].Clear();
                    }
                    m_updateConnections.Add(cube.Position);
                }
                m_updateConnections.Add(v);
            }
        }

        private void UpdateShape(HkRigidBody rigidBody, HkRigidBody rigidBody2, HkdBreakableBody destructionBody)
        {
            ProfilerShort.Begin("SetShape");
            if (destructionBody != null)
            {
                ProfilerShort.Begin("SetBreakableShape");
                destructionBody.BreakableShape = BreakableShape;
                ProfilerShort.BeginNextBlock("ConnectToWorld");
                CreateConnectionToWorld(destructionBody);
                ProfilerShort.End();
                //breakableShape.Dispose();
            }
            else
            {
                rigidBody.SetShape(m_root);
                if (rigidBody2 != null)
                {
                    rigidBody2.SetShape(m_root);
                }
            }
            ProfilerShort.End();
        }

        List<Havok.HkRigidBody> m_penetrations = new List<Havok.HkRigidBody>();
        private void FindConnectionsToWorld(HashSet<MySlimBlock> blocks)
        {
            if (m_grid.Physics != null && m_grid.Physics.LinearVelocity.LengthSquared() > 0) //jn: TODO nicer
                return;
            int counter = 0;
            ProfilerShort.Begin("FindConnectionsToWorld");
            var q = Quaternion.Identity;
            foreach (var b in blocks)
            {
                var geometryBox = b.FatBlock.GetGeometryLocalBox();
                Vector3 halfExtents = geometryBox.Size / 2;

                Vector3D pos;
                b.ComputeScaledCenter(out pos);
                pos += geometryBox.Center;
                pos = Vector3D.Transform(pos, m_grid.WorldMatrix);

                Matrix blockMatrix;
                b.Orientation.GetMatrix(out blockMatrix);
                q = Quaternion.CreateFromRotationMatrix(blockMatrix * m_grid.WorldMatrix.GetOrientation());

                Sandbox.Engine.Physics.MyPhysics.GetPenetrationsBox(ref halfExtents, ref pos, ref q, m_penetrations, Sandbox.Engine.Physics.MyPhysics.CollideWithStaticLayer);
                counter++;
                bool isStatic = false;
                foreach (var p in m_penetrations)
                {
                    if (p == null)
                        continue;
                    var e = p.UserObject as Sandbox.Engine.Physics.MyPhysicsBody;
                    if (e != null && e.Entity != null && e.Entity is MyVoxelMap)
                    {
                        isStatic = true;
                        break;
                    }
                }

                m_penetrations.Clear();
                if (isStatic && !BlocksConnectedToWorld.Contains(b.Position))
                {
                    isStatic = false;
                    m_blocksShapes[b.Position].GetChildren(m_shapeInfosList2);
                    for (int i = 0; i < m_shapeInfosList2.Count; i++)
                    {
                        var child = m_shapeInfosList2[i];
                        if (child.Shape.GetChildrenCount() > 0)
                        {
                            child.Shape.GetChildren(m_shapeInfosList2);
                            continue;
                        }
                        Vector4 min;
                        Vector4 max;
                        child.Shape.GetShape().GetLocalAABB(0.01f, out min, out max);//.Transform(CubeGrid.PositionComp.WorldMatrix);
                        BoundingBox bb = new BoundingBox(new Vector3(min), new Vector3(max));
                        bb = bb.Translate(b.Position * m_grid.GridSize);
                        var bbd = bb.Transform(m_grid.WorldMatrix);
                        halfExtents = bbd.HalfExtents;
                        pos = bbd.Center;
                        Sandbox.Engine.Physics.MyPhysics.GetPenetrationsBox(ref halfExtents, ref pos, ref q, m_penetrations, Sandbox.Engine.Physics.MyPhysics.CollideWithStaticLayer);
                        counter++;
                        foreach (var p in m_penetrations)
                        {
                            if (p == null)
                                continue;
                            var e = p.UserObject as Sandbox.Engine.Physics.MyPhysicsBody;
                            if (e != null && e.Entity != null && e.Entity is MyVoxelMap)
                            {
                                isStatic = true;
                                child.Shape.SetFlagRecursively(HkdBreakableShape.Flags.IS_FIXED);
                                break;
                            }
                        }
                        m_penetrations.Clear();
                    }
                    m_shapeInfosList2.Clear();
                    if(isStatic)
                        BlocksConnectedToWorld.Add(b.Position);
                }
            }
            ProfilerShort.End(counter);
        }

        public void RecalculateConnectionsToWorld(HashSet<MySlimBlock> blocks)
        {
            BlocksConnectedToWorld.Clear();

            FindConnectionsToWorld(blocks);
        }

        public HashSet<Vector3I> BlocksConnectedToWorld = new HashSet<Vector3I>();
        public void CreateConnectionToWorld(HkdBreakableBody destructionBody)
        {
            if (BlocksConnectedToWorld.Count == 0)
                return;
            HkdFixedConnectivity conn = HkdFixedConnectivity.Create();
            var voxelMap = MySession.Static.VoxelMaps.Instances.Single();
            foreach (var pos in BlocksConnectedToWorld)
            {
                HkdFixedConnectivity.Connection c = new HkdFixedConnectivity.Connection(Vector3.Zero, Vector3.Up, 1, m_blocksShapes[pos].Shape, voxelMap.Physics.RigidBody, 0);
                conn.AddConnection(ref c);
                c.RemoveReference();
            }
            destructionBody.SetFixedConnectivity(conn);
            conn.RemoveReference();
        }

        private List<HkdShapeInstanceInfo> m_shapeInfosList = new List<HkdShapeInstanceInfo>(); //list for compound shape
        private List<HkdShapeInstanceInfo> m_shapeInfosList2 = new List<HkdShapeInstanceInfo>(); //list for compound shape
        private List<HkdConnection> m_connectionsToAddCache = new List<HkdConnection>(); //connections are created before they can be added so we cache them
        public HkdBreakableShape? CreateBreakableShape()
        {
            ProfilerShort.Begin("CreateBreakableShape");

            ProfilerShort.Begin("CollectShapes");
            m_blocksShapes.Clear();
            foreach (var b in m_grid.GetBlocks())
            {
                Matrix m;
                var bs = CreateBlockShape(b, out m);
                if (bs.HasValue)
                {
                    var shapeInstance = new HkdShapeInstanceInfo(bs.Value, m);
                    m_shapeInfosList.Add(shapeInstance);
                    m_blocksShapes[b.Position] = shapeInstance;
                }
            }
            ProfilerShort.End();

            if (m_blocksShapes.Count == 0)
            {
                Debug.Fail("No breakable shapes in grid!");
                ProfilerShort.End();
                return null;
            }

            ProfilerShort.Begin("Create");
            if (BreakableShape.IsValid())
                BreakableShape.RemoveReference();
            BreakableShape = new HkdCompoundBreakableShape(null, m_shapeInfosList);
            BreakableShape.SetChildrenParent(BreakableShape);
            try
            {
                BreakableShape.SetStrenghtRecursively(Sandbox.MyDestructionConstants.STRENGTH, 0.7f);
            }
            catch(Exception e)
            {
                MyLog.Default.WriteLine(e);
                MyLog.Default.WriteLine("BS Valid: " + BreakableShape.IsValid());
                MyLog.Default.WriteLine("BS Child count: " + BreakableShape.GetChildrenCount());
                MyLog.Default.WriteLine("Grid shapes: " + m_shapeInfosList.Count);
                foreach(var child in m_shapeInfosList)
                {
                    if (!child.Shape.IsValid())
                        MyLog.Default.WriteLine("Invalid child!");
                    else
                        MyLog.Default.WriteLine("Child strength: " + child.Shape.GetStrenght());
                }
                throw new InvalidOperationException();
            }
            ProfilerShort.End();
            ProfilerShort.Begin("Connect");
            CreateConnectionsManually(BreakableShape);
            ProfilerShort.End();
            FindConnectionsToWorld(m_grid.GetBlocks());

            m_shapeInfosList.Clear();
            ProfilerShort.End();
            return BreakableShape;
        }

        private static bool HasBreakableShape(string model, MyCubeBlockDefinition block)
        {
            var modelData = MyModels.GetModelOnlyData(model);
            return modelData != null && modelData.HavokBreakableShapes != null && modelData.HavokBreakableShapes.Length > 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="block"></param>
        /// <returns>Cloned shape</returns>
        private static HkdBreakableShape GetBreakableShape(string model, MyCubeBlockDefinition block)
        {
            if (MyFakes.LAZY_LOAD_DESTRUCTION)
            {
                var data = MyModels.GetModelOnlyData(model);
                if (data.HavokBreakableShapes == null)
                {
                    MyDestructionData.Static.LoadModelDestruction(model, block, false, data.BoundingBoxSize);
                }
            }
            return MyDestructionData.Static.BlockShapePool.GetBreakableShape(model, block);
        }

        List<HkShape> m_khpShapeList = new List<HkShape>();
        private HkdBreakableShape? CreateBlockShape(Sandbox.Game.Entities.Cube.MySlimBlock b, out Matrix blockTransform)
        {
            ProfilerShort.Begin("CreateBlockShape");
            blockTransform = Matrix.Identity;
            if (b.FatBlock == null)
            {
                Debug.Fail("Armor blocks are not allowed in medieval");
                ProfilerShort.End();
                return null;
            }
            
            HkdBreakableShape breakableShape;
            Matrix compoundChildTransform = Matrix.Identity;

            if (b.FatBlock is MyCompoundCubeBlock)
            {
                ProfilerShort.Begin("Cmpnd");
                blockTransform.Translation = b.FatBlock.PositionComp.LocalMatrix.Translation;
                var cb = b.FatBlock as MyCompoundCubeBlock;
                if (cb.GetBlocksCount() == 1)
                {
                    ProfilerShort.Begin("SingleBlock");
                    var block = cb.GetBlocks()[0];
                    var defId = block.FatBlock.BlockDefinition;
                    Matrix m;
                    var model = block.CalculateCurrentModel(out m);
                    if (MyFakes.LAZY_LOAD_DESTRUCTION || HasBreakableShape(model, defId))
                    {
                        ProfilerShort.Begin("Clone");
                        breakableShape = GetBreakableShape(model, defId);
                        ProfilerShort.End();
                    }

                    block.Orientation.GetMatrix(out compoundChildTransform);
                    blockTransform = compoundChildTransform * blockTransform;
                    ProfilerShort.End();
                }
                else
                {
                    var pos = b.Position * m_grid.GridSize;

                    float mass = 0;
                    ProfilerShort.Begin("GetBlocks");
                    foreach(var block in cb.GetBlocks())
                    {
                        block.Orientation.GetMatrix(out compoundChildTransform);
                        compoundChildTransform.Translation = Vector3.Zero;
                        var blockDef = block.BlockDefinition;
                        Matrix m;
                        var model = block.CalculateCurrentModel(out m);
                        if (MyFakes.LAZY_LOAD_DESTRUCTION || HasBreakableShape(model, blockDef))
                        {
                            ProfilerShort.Begin("Clone");

                            breakableShape = GetBreakableShape(model, blockDef);
                            breakableShape.UserObject |= (uint)HkdBreakableShape.Flags.FRACTURE_PIECE;
                            System.Diagnostics.Debug.Assert(breakableShape.IsValid(), "Invalid breakableShape");

                            ProfilerShort.End();
                            mass += blockDef.Mass;
                            m_shapeInfosList2.Add(new HkdShapeInstanceInfo(breakableShape, compoundChildTransform));
                        }
                    }

                    if (m_shapeInfosList2.Count == 0)
                    {
                        ProfilerShort.End();
                        return null;
                    }

                    ProfilerShort.BeginNextBlock("CreateCompoundBlockShape");
                    //HkShape hkpShape = new HkListShape(m_khpShapeList.ToArray(), m_khpShapeList.Count, HkReferencePolicy.None);
                    //m_khpShapeList.Clear();
                    HkdBreakableShape compound = new HkdCompoundBreakableShape(null, m_shapeInfosList2);
                    ((HkdCompoundBreakableShape)compound).RecalcMassPropsFromChildren();
                    var mp = new HkMassProperties();
                    compound.BuildMassProperties(ref mp);
                    breakableShape = new HkdBreakableShape(compound.GetShape(), ref mp);
                    compound.RemoveReference();
                    foreach (var si in m_shapeInfosList2)
                    {
                        var siRef = si;
                        breakableShape.AddShape(ref siRef);
                    }

                    ProfilerShort.BeginNextBlock("Connect");
                    //slow slow slow
                    //breakableShape.AutoConnect(MyDestructionData.Static.TemporaryWorld);
                    //slow wrong
                    //breakableShape.ConnectSemiAccurate(MyDestructionData.Static.TemporaryWorld);
                    //fast frong
                    for (int i = 0; i < m_shapeInfosList2.Count; i++)
                    {
                        for (int j = 0; j < m_shapeInfosList2.Count; j++)
                        {
                            if (i != j)
                            {
                                ConnectShapesWithChildren(breakableShape, m_shapeInfosList2[i].Shape, m_shapeInfosList2[j].Shape);
                            }
                        }
                    }
                    ProfilerShort.BeginNextBlock("Cleanup");
                    foreach (var si in m_shapeInfosList2)
                    {
                        si.Shape.RemoveReference();
                        si.RemoveReference();
                    }
                    m_shapeInfosList2.Clear();
                    ProfilerShort.End();
                }
                ProfilerShort.End();
            }
            else
            {
                ProfilerShort.Begin("SingleBlock");
                b.Orientation.GetMatrix(out blockTransform);
                blockTransform.Translation = b.FatBlock.PositionComp.LocalMatrix.Translation;
                Matrix m;
                var model = b.CalculateCurrentModel(out m);
                if (b.FatBlock is MyFracturedBlock)
                {
                    ProfilerShort.Begin("CloneFracture");
                    breakableShape = (b.FatBlock as MyFracturedBlock).Shape;
                    breakableShape.AddReference();
                    ProfilerShort.End();
                }
                else if (MyFakes.LAZY_LOAD_DESTRUCTION || HasBreakableShape(model, b.BlockDefinition))
                {
                    ProfilerShort.Begin("Clone");
                    breakableShape = GetBreakableShape(model, b.BlockDefinition);
                    ProfilerShort.End();
                }
                ProfilerShort.End();
            }
            ProfilerShort.Begin("Property");
            HkPropertyBase posProp = new HkVec3IProperty(b.Position);
            Debug.Assert(breakableShape.IsValid());
            breakableShape.SetPropertyRecursively(HkdBreakableShape.PROPERTY_GRID_POSITION, posProp);
            posProp.RemoveReference();
            ProfilerShort.End();
            //breakableShape.DisableRefCountRecursively();
            ProfilerShort.End();
            return breakableShape;
        }

        HashSet<MySlimBlock> m_processedBlock = new HashSet<MySlimBlock>();
        private static List<HkdShapeInstanceInfo> m_shapeInfosList3 = new List<HkdShapeInstanceInfo>();
        private void UpdateConnectionsManually(HkdBreakableShape shape, HashSet<Vector3I> dirtyCubes)
        {
            ProfilerShort.Begin("Updateconnections");
            uint i = 0;
            foreach (var dirty in dirtyCubes)
            {
                ProfilerShort.Begin("GetNContains");
                var b = m_grid.GetCubeBlock(dirty);
                if (b == null || m_processedBlock.Contains(b))
                {
                    ProfilerShort.End();
                    continue;
                }
                ProfilerShort.End();
                ProfilerShort.Begin("NewConnections");
                if (!m_connections.ContainsKey(b.Position))
                    m_connections[b.Position] = new List<HkdConnection>();
                var blockConnections = m_connections[b.Position];
                foreach (var n in b.Neighbours)
                {
                    ConnectBlocks(shape, b, n, blockConnections);
                    i++;
                }
                ProfilerShort.End();
                m_processedBlock.Add(b);
            }
            m_processedBlock.Clear();
            ProfilerShort.End();
            ProfilerShort.CustomValue("Dirty", dirtyCubes.Count, null);
            ProfilerShort.CustomValue("Updated", i, null);
        }

        private Dictionary<Vector3I, List<HkdConnection>> m_connections = new Dictionary<Vector3I, List<HkdConnection>>();
        public void CreateConnectionsManually(HkdBreakableShape shape)
        {
            ProfilerShort.Begin("CollectConnections");
            m_connections.Clear();
            foreach (var blockA in m_grid.CubeBlocks)
            {
                if (!m_blocksShapes.ContainsKey(blockA.Position))
                    continue;
                if (!m_connections.ContainsKey(blockA.Position))
                    m_connections[blockA.Position] = new List<HkdConnection>();
                var blockConnections = m_connections[blockA.Position];
                foreach (var blockB in blockA.Neighbours)
                {
                    if (!m_blocksShapes.ContainsKey(blockB.Position))
                        continue;
                    ConnectBlocks(shape, blockA, blockB, blockConnections);
                }
            }
            AddConnections();
            ProfilerShort.End();
        }

        private void AddConnections()
        {
            ProfilerShort.Begin("AddConnections");
            int i = 0;
            foreach (var lst in m_connections.Values)
            {
                i += lst.Count;
            }
            BreakableShape.ClearConnections();
            BreakableShape.ReplaceConnections(m_connections, i);
            //foreach (var lst in m_connections.Values)
            //{
            //    foreach (var connection in lst)
            //    {
            //        var c = connection;
            //        //BreakableShape.AddConnection(ref c);
            //        Debug.Assert(CheckConnection(c), "Old connection. Should be removed.");
            //        i++;
            //        BreakableShape.AddConnection(ref c);
            //    }
            //}
            ProfilerShort.End();
            
            ProfilerShort.CustomValue("ConnectionCount", i, null);
        }

        unsafe private bool CheckConnection(HkdConnection c)
        {
            HkdBreakableShape par = c.ShapeA;
            while (par.HasParent)
            {
                par = par.GetParent();
            }
            if (par != BreakableShape)
                return false;
            par = c.ShapeB;
            while (par.HasParent)
            {
                par = par.GetParent();
            }
            if (par != BreakableShape)
                return false;
            return true;
        }

        private void ConnectBlocks(HkdBreakableShape parent, MySlimBlock blockA, MySlimBlock blockB, List<HkdConnection> blockConnections)
        {
            if (!m_blocksShapes.ContainsKey(blockA.Position))
            {
                Debug.Fail("Block missing shape!" + blockA.BlockDefinition.Id.SubtypeId);
                return;
            }

            if (!m_blocksShapes.ContainsKey(blockB.Position))
            {
                Debug.Fail("Block missing shape! " + blockB.BlockDefinition.Id.SubtypeId);
                return;
            }

            ProfilerShort.Begin("");

            var shapeA = m_blocksShapes[blockA.Position];
            var shapeB = m_blocksShapes[blockB.Position];
            shapeB.GetChildren(m_shapeInfosList2);
            bool anyConnection = shapeB.Shape.GetChildrenCount() == 0;

            foreach (var child in m_shapeInfosList2)
            {
                var c = child;
                c.DynamicParent = HkdShapeInstanceInfo.INVALID_INDEX;
            }

            var posB = blockB.Position * m_grid.GridSize;
            var posA = blockA.Position * m_grid.GridSize;
            Vector3 dir = blockB.Position - blockA.Position;
            dir = Vector3.Normalize(dir);
            Matrix blockOr = shapeB.GetTransform().GetOrientation();
            for (int i = 0; i < m_shapeInfosList2.Count; i++)
            {
                var child = m_shapeInfosList2[i];
                
                ProfilerShort.Begin("Transform");
                Matrix m = child.GetTransform();
                var dynParent = child.DynamicParent;
                while (dynParent != HkdShapeInstanceInfo.INVALID_INDEX)
                {
                    m *= m_shapeInfosList2[dynParent].GetTransform();
                    dynParent = m_shapeInfosList2[dynParent].DynamicParent;
                }
                m *= blockOr;
                ProfilerShort.BeginNextBlock("AAbb");
                Vector4 min, max;
                child.Shape.GetShape().GetLocalAABB(0.1f, out min, out max);
                ProfilerShort.BeginNextBlock("Check1");
                var childPos =  posB + Vector3.Transform(new Vector3(min), m);
                if (((posA - childPos) * dir).AbsMax() > 1.35f/*m_grid.GridSize*/)
                {
                    ProfilerShort.BeginNextBlock("Check2");
                    childPos = posB + Vector3.Transform(new Vector3(max), m);
                    if (((posA - childPos) * dir).AbsMax() > 1.35f/*m_grid.GridSize*/)
                    {
                        ProfilerShort.End();
                        continue;
                    }
                }
                ProfilerShort.BeginNextBlock("CreateConnections");
                //If you remove following line, all roofs on Castle from scenarion will be rotated
                anyConnection = true;
                //end of strange line

                var toChild = CreateConnection(shapeA.Shape, child.Shape, posA,posB + Vector3.Transform(child.CoM, m));//blockB.Position * m_grid.GridSize);
                blockConnections.Add(toChild);
                ProfilerShort.BeginNextBlock("DynParent");
                child.GetChildren(m_shapeInfosList2);
                for (int j = m_shapeInfosList2.Count - child.Shape.GetChildrenCount(); j < m_shapeInfosList2.Count; j++)
                {
                    var si = m_shapeInfosList2[j];
                    si.DynamicParent = (ushort)i;
                }
                ProfilerShort.End();
            }
            if (anyConnection)
            {
                var toBlock = CreateConnection(shapeA.Shape, shapeB.Shape, blockA.Position * m_grid.GridSize, blockB.Position * m_grid.GridSize);
                blockConnections.Add(toBlock);
            }
            m_shapeInfosList2.Clear();
            ProfilerShort.End();
        }

        public static void ConnectShapesWithChildren(HkdBreakableShape parent, HkdBreakableShape shapeA, HkdBreakableShape shapeB)
        {
            var c = CreateConnection(shapeA, shapeB, shapeA.CoM, shapeB.CoM);
            //parent.AddConnection(ref c);
            c.AddToCommonParent();
            c.RemoveReference();
            shapeB.GetChildren(m_shapeInfosList3);
            foreach (var child in m_shapeInfosList3)
            {
                var c2 = CreateConnection(shapeA, child.Shape, shapeA.CoM, shapeB.CoM);
                //parent.AddConnection(ref c2);
                c2.AddToCommonParent();
                c2.RemoveReference();
            }
            m_shapeInfosList3.Clear();
        }

        private static HkdConnection CreateConnection(HkdBreakableShape aShape, HkdBreakableShape bShape, Vector3 pivotA, Vector3 pivotB)
        {
            ProfilerShort.Begin("CreateConnection-Shape-Shape");
            var normal = bShape.CoM - aShape.CoM;
            //if (normal.Length() == 0)
            //    normal = Vector3.Forward;
            HkdConnection c = new HkdConnection(aShape, bShape, pivotA, pivotB, normal, 6.25f);
            ProfilerShort.End();
            return c;
        }

        private void UpdateMass(HkRigidBody rigidBody)
        {
            if (!m_grid.IsStatic && !rigidBody.IsFixed && rigidBody.GetMotionType() != HkMotionType.Keyframed)
            {
                ProfilerShort.Begin("Update mass");
                if (!MyPerGameSettings.Destruction)
                    UpdateMassProperties();
                ProfilerShort.End();

                ProfilerShort.Begin("Set mass");
                rigidBody.Mass = m_massProperties.Mass;
                rigidBody.SetMassProperties(ref m_massProperties);
                ProfilerShort.End();
            }
        }

        public void MarkBreakable(HkWorld world, HkRigidBody rigidBody)
        {
            if (MyPerGameSettings.Destruction)
                return;
            ProfilerShort.Begin("Unmark breakable");
            // TODO: Go through all shapes
            var it = m_root.GetIterator();
            while (it.IsValid)
            {
                // TODO: proper impulses
                world.BreakOffPartsUtil.MarkPieceBreakable(rigidBody, it.CurrentShapeKey, BreakImpulse);
                it.Next();
            }
            ProfilerShort.End();
        }

        public void UnmarkBreakable(HkWorld world, HkRigidBody rigidBody)
        {
            if (MyPerGameSettings.Destruction)
                return;
            ProfilerShort.Begin("Unmark breakable");
            var it = m_root.GetIterator();
            while (it.IsValid)
            {
                world.BreakOffPartsUtil.UnmarkPieceBreakable(rigidBody, it.CurrentShapeKey);
                it.Next();
            }

            ProfilerShort.End();
        }

        public void RefreshMass()
        {
            // MW: so far just plain recalculation.
            m_blockCollector.CollectMassElements(m_grid, m_massElements);
            UpdateMass(m_grid.Physics.RigidBody);
        }

        public void UpdateMassFromInventories(HashSet<MySlimBlock> blocks, MyPhysicsBody rb)
        {
            if (rb.RigidBody.IsFixedOrKeyframed)
                return;
            ProfilerShort.Begin("GridShape.UpdateMassFromInv");
            Debug.Assert(BreakableShape.IsValid(), "This routine works with breakable shape mass properties.");
            foreach(var block in blocks)
            {
                var owner = block.FatBlock as IMyInventoryOwner;
                if (owner == null) continue;
                float mass = 0;
                for(int i = 0; i < owner.InventoryCount; i++)
                {
                    mass += (float)owner.GetInventory(i).CurrentMass;
                }
                var size = (block.Max - block.Min + Vector3I.One) * block.CubeGrid.GridSize;
                var center = (block.Min + block.Max) * 0.5f * block.CubeGrid.GridSize;
                HkMassProperties massProperties = new HkMassProperties();
                massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(size / 2, mass);
                m_tmpElements.Add(new HkMassElement() { Properties = massProperties, Tranform = Matrix.CreateTranslation(center) });
            }
            HkMassProperties originalMp = new HkMassProperties();
            BreakableShape.BuildMassProperties(ref originalMp);
            m_tmpElements.Add(new HkMassElement() { Properties = originalMp, Tranform = Matrix.Identity });
            var mp = HkInertiaTensorComputer.CombineMassProperties(m_tmpElements);
            m_tmpElements.Clear();
            rb.RigidBody.SetMassProperties(ref mp);
            if(!rb.RigidBody.IsActive)
                rb.RigidBody.Activate();
            ProfilerShort.End();
        }

        public static implicit operator HkShape(MyGridShape shape)
        {
            return shape.m_root;
        }

        public float GetBlockMass(Vector3I position)
        {
            Debug.Assert(m_grid.CubeExists(position), "No shape for this position!");
            if (m_blocksShapes.ContainsKey(position))
                return MyDestructionHelper.MassFromHavok(m_blocksShapes[position].Shape.GetMass());
            else
            {
                if (m_grid.CubeExists(position))
                    return m_grid.GetCubeBlock(position).GetMass();
                else
                    return 1;
            }
        }

        internal void DebugDraw()
        {
            if (MyDebugDrawSettings.BREAKABLE_SHAPE_CHILD_COUNT)
            {
                foreach (var s in m_blocksShapes)
                {
                    var position = m_grid.GridIntegerToWorld((s.Value.GetTransform().Translation + s.Value.CoM) / m_grid.GridSize);
                    if ((position - MySector.MainCamera.Position).Length() > 20)
                        continue;
                    VRageRender.MyRenderProxy.DebugDrawText3D(position, MyValueFormatter.GetFormatedInt(s.Value.Shape.GetChildrenCount()), Color.White, 0.65f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                }
            }

            if (Sandbox.Game.Gui.MyHonzaInputComponent.ShowRealBlockMass == Gui.MyHonzaInputComponent.ShownMassEnum.None)
                return;
            if ((m_grid.PositionComp.GetPosition() - MySector.MainCamera.Position).Length() > 20 + m_grid.PositionComp.WorldVolume.Radius)
                return;
            foreach (var pair in m_blocksShapes)
            {
                var position = m_grid.GridIntegerToWorld((pair.Value.GetTransform().Translation + pair.Value.CoM) / m_grid.GridSize);
                if ((position - MySector.MainCamera.Position).Length() > 20)
                    continue;
                var block = m_grid.GetCubeBlock(pair.Key);
                if (block == null) continue;
                float mass = block.GetMass();
                if (block.FatBlock is MyFracturedBlock)
                    mass = m_blocksShapes[block.Position].Shape.GetMass();
                switch(Sandbox.Game.Gui.MyHonzaInputComponent.ShowRealBlockMass)
                {
                    case Gui.MyHonzaInputComponent.ShownMassEnum.Real:
                        mass = MyDestructionHelper.MassFromHavok(mass);
                        break;
                    case Gui.MyHonzaInputComponent.ShownMassEnum.SI:
                        mass = MyDestructionHelper.MassFromHavok(mass);
                        mass = Sandbox.Game.GameSystems.StructuralIntegrity.MyAdvancedStaticSimulator.MassToSI(mass);
                        break;
                    default:
                        break;
                }
                VRageRender.MyRenderProxy.DebugDrawText3D(position, MyValueFormatter.GetFormatedFloat(mass, (mass < 10 ? 2 : 0)), Color.White, 0.6f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            }
        }
    }
}
