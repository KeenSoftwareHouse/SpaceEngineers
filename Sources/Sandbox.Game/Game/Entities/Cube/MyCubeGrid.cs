#region Using

using Havok;
using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Components;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;

#endregion

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Grid - small ship, large ship, station
    /// Cubes (armor, walls...) are merge and rendered by this entity
    /// Blocks (turret, thrusts...) are rendered as child entities
    /// </summary>
    [MyEntityType(typeof(MyObjectBuilder_CubeGrid))]
    public partial class MyCubeGrid : MyEntity, IMyGridConnectivityTest
    {
        private static readonly float MAX_TRASH_DISTANCE_M = 50000.0f;

        // Objects smaller than one pixel in diameter are thrash (assume uniform pixel arc sizes across the screen)
        private static readonly float MIN_TRASH_ARC_RADIUS = (float)Math.PI / 1280.0f;


        static MyCubeGrid()
        {
            for (int i = 0; i < 26; ++i)
            {
                m_neighborOffsetIndices.Add((NeighborOffsetIndex)i);
                m_neighborDistances.Add(0.0f);
                m_neighborOffsets.Add(new Vector3I(0, 0, 0));
            }

            m_neighborOffsets[(int)NeighborOffsetIndex.XUP] = new Vector3I(1, 0, 0);
            m_neighborOffsets[(int)NeighborOffsetIndex.XDOWN] = new Vector3I(-1, 0, 0);
            m_neighborOffsets[(int)NeighborOffsetIndex.YUP] = new Vector3I(0, 1, 0);
            m_neighborOffsets[(int)NeighborOffsetIndex.YDOWN] = new Vector3I(0, -1, 0);
            m_neighborOffsets[(int)NeighborOffsetIndex.ZUP] = new Vector3I(0, 0, 1);
            m_neighborOffsets[(int)NeighborOffsetIndex.ZDOWN] = new Vector3I(0, 0, -1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XUP_YUP] = new Vector3I(1, 1, 0);
            m_neighborOffsets[(int)NeighborOffsetIndex.XUP_YDOWN] = new Vector3I(1, -1, 0);
            m_neighborOffsets[(int)NeighborOffsetIndex.XDOWN_YUP] = new Vector3I(-1, 1, 0);
            m_neighborOffsets[(int)NeighborOffsetIndex.XDOWN_YDOWN] = new Vector3I(-1, -1, 0);
            m_neighborOffsets[(int)NeighborOffsetIndex.YUP_ZUP] = new Vector3I(0, 1, 1);
            m_neighborOffsets[(int)NeighborOffsetIndex.YUP_ZDOWN] = new Vector3I(0, 1, -1);
            m_neighborOffsets[(int)NeighborOffsetIndex.YDOWN_ZUP] = new Vector3I(0, -1, 1);
            m_neighborOffsets[(int)NeighborOffsetIndex.YDOWN_ZDOWN] = new Vector3I(0, -1, -1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XUP_ZUP] = new Vector3I(1, 0, 1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XUP_ZDOWN] = new Vector3I(1, 0, -1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XDOWN_ZUP] = new Vector3I(-1, 0, 1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XDOWN_ZDOWN] = new Vector3I(-1, 0, -1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XUP_YUP_ZUP] = new Vector3I(1, 1, 1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XUP_YUP_ZDOWN] = new Vector3I(1, 1, -1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XUP_YDOWN_ZUP] = new Vector3I(1, -1, 1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XUP_YDOWN_ZDOWN] = new Vector3I(1, -1, -1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XDOWN_YUP_ZUP] = new Vector3I(-1, 1, 1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XDOWN_YUP_ZDOWN] = new Vector3I(-1, 1, -1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XDOWN_YDOWN_ZUP] = new Vector3I(-1, -1, 1);
            m_neighborOffsets[(int)NeighborOffsetIndex.XDOWN_YDOWN_ZDOWN] = new Vector3I(-1, -1, -1);

            GridCounter = 0;
        }

        /// <summary>
        /// Used when calculating damage from deformation application
        /// </summary>
        private float m_totalBoneDisplacement;

        internal MyVoxelSegmentation BonesToSend = new MyVoxelSegmentation();
        private int m_bonesSendCounter = 0;

        private MyDirtyRegion m_dirtyRegion = new MyDirtyRegion();
        private MyCubeSize m_gridSizeEnum;
        private Vector3I m_min = Vector3I.MaxValue;
        private Vector3I m_max = Vector3I.MinValue;
        private readonly Dictionary<Vector3I, MyCube> m_cubes = new Dictionary<Vector3I, MyCube>(1024);

        private HashSet<MySlimBlock> m_cubeBlocks = new HashSet<MySlimBlock>();
        private MyLocalityGrouping m_explosions = new MyLocalityGrouping(MyLocalityGrouping.GroupingMode.Overlaps);

        public HashSetReader<Vector3I> DirtyBlocks { get { return new HashSetReader<Vector3I>(m_dirtyRegion.Cubes); } }

        public MyCubeGridRenderData RenderData { get { return Render.RenderData; } }

        private HashSet<MyCubeBlock> m_processedBlocks = new HashSet<MyCubeBlock>();
        private HashSet<MyCubeBlock> m_blocksForDraw = new HashSet<MyCubeBlock>();
        private List<MyCubeGrid> m_tmpGrids = new List<MyCubeGrid>();
        public HashSet<MyCubeBlock> BlocksForDraw { get { return m_blocksForDraw; } }
        private bool m_disconnectsDirty;
        private HashSet<MySlimBlock> m_blocksForDamageApplication = new HashSet<MySlimBlock>();
        internal MyStructuralIntegrity StructuralIntegrity
        {
            get;
            private set;
        }

        private HashSet<Vector3UByte> m_tmpBuildFailList = new HashSet<Vector3UByte>();
        private List<Vector3UByte> m_tmpBuildOffsets = new List<Vector3UByte>();
        private List<MySlimBlock> m_tmpBuildSuccessBlocks = new List<MySlimBlock>();

        private static List<MyCockpit> m_tmpOccupiedCockpits = new List<MyCockpit>();

        public List<IMyBlockAdditionalModelGenerator> AdditionalModelGenerators { get { return Render.AdditionalModelGenerators; } }

        internal MyGridSkeleton Skeleton;
        public readonly BlockTypeCounter BlockCounter = new BlockTypeCounter();

        public MyCubeGridSystems GridSystems { get; private set; }

        public Dictionary<MyObjectBuilderType, int> BlocksCounters = new Dictionary<MyObjectBuilderType, int>();

        internal new MySyncGrid SyncObject
        {
            get { return (MySyncGrid)base.SyncObject; }
        }

        static private Vector3[] m_gizmoCorners = new Vector3[8];
        static private MyBillboardViewProjection m_viewProjection = new MyBillboardViewProjection();

        private const float m_gizmoMaxDistanceFromCamera = 100.0f;
        private const float m_gizmoDrawLineScale = 0.002f;

        public bool IsStatic { get { return StaticGrids.Contains(this); } }
        public float GridSize { get; private set; }

        public Vector3I Min { get { return m_min; } }
        public Vector3I Max { get { return m_max; } }

        public Vector3I? XSymmetryPlane = null;
        public Vector3I? YSymmetryPlane = null;
        public Vector3I? ZSymmetryPlane = null;
        public bool XSymmetryOdd = false;
        public bool YSymmetryOdd = false;
        public bool ZSymmetryOdd = false;

        private bool m_destructibleBlocks;

        // Used for UI & Sync
        public bool DestructibleBlocks
        {
            get
            {
                return m_destructibleBlocks;
            }
            set
            {
                m_destructibleBlocks = value;
            }
        }

        // Used to determine if blocks are destructible
        public bool BlocksDestructionEnabled
        {
            get
            {
                if (MySession.Static.Settings.DestructibleBlocks)
                {
                    return m_destructibleBlocks;
                }
                else
                {
                    return false;
                }
            }
        }

        internal List<MyBlockGroup> BlockGroups = new List<MyBlockGroup>();
        private static List<MyObjectBuilder_BlockGroup> m_tmpBlockGroups = new List<MyObjectBuilder_BlockGroup>();
        internal MyCubeGridOwnershipManager m_ownershipManager;

        public Sandbox.Game.Entities.Blocks.MyProjector Projector;

        /// <summary>
        /// players that have at least one block in cube grid
        /// </summary>
        public List<long> SmallOwners
        {
            get
            {
                return m_ownershipManager.SmallOwners;
            }
        }

        /// <summary>
        /// players that have the maximum number of functional blocks in cube grid
        /// </summary>
        public List<long> BigOwners
        {
            get
            {
                return m_ownershipManager.BigOwners;
            }
        }

        public MyCubeSize GridSizeEnum
        {
            get { return m_gridSizeEnum; }
            set
            {
                m_gridSizeEnum = value;
                GridSize = MyDefinitionManager.Static.GetCubeSize(value);
            }
        }

        public new MyGridPhysics Physics { get { return (MyGridPhysics)base.Physics; } set { base.Physics = value; } }

        //public int HavokCollisionSystemID { get; private set; }

        public event Action<MySlimBlock> OnBlockAdded;
        public event Action<MySlimBlock> OnBlockRemoved;
        //Called when ownership recalculation is actually done
        public event Action<MyCubeGrid> OnBlockOwnershipChanged;
        public event Action<MyCubeGrid, MyCubeGrid> OnGridSplit;

        internal event Action<MyGridLogicalGroupData> AddedToLogicalGroup;
        internal event Action RemovedFromLogicalGroup;

        internal event Action<int> OnHavokSystemIDChanged;

        public static int GridCounter
        {
            get;
            private set;
        }

        public int BlocksCount { get { return m_cubeBlocks.Count; } }
        public HashSet<MySlimBlock> CubeBlocks { get { return m_cubeBlocks; } }
        public event Action<MyCubeGrid> OnGridChanged;
        public bool CreatePhysics;
        public void RaiseGridChanged()
        {
            if (OnGridChanged != null)
            {
                OnGridChanged(this);
            }
        }

        private bool m_smallToLargeConnectionsInitialized = false;
        private bool m_enableSmallToLargeConnections = true;
        internal bool EnableSmallToLargeConnections { get { return m_enableSmallToLargeConnections;  } }

        // Flag if SI should check connectivity of the grid
        internal bool TestDynamic = false;

        internal new MyRenderComponentCubeGrid Render
        {
            get { return (MyRenderComponentCubeGrid)base.Render; }
            set { base.Render = value; }
        }

        public MyTerminalBlock MainCockpit = null;

        public bool HasMainCockpit()
        {
            return MainCockpit != null;
        }

        public bool IsMainCockpit(MyTerminalBlock cockpit)
        {
            return MainCockpit == cockpit;
        }

        public void SetMainCockpit(MyTerminalBlock cockpit)
        {
            MainCockpit = cockpit;
        }

        public MyCubeGrid() :
            this(MyCubeSize.Large)
        {
            Render = new MyRenderComponentCubeGrid();
            Render.NeedsDraw = true;

            PositionComp = new MyCubeGridPosition();
        }

        private MyCubeGrid(MyCubeSize gridSize)
        {
            GridSizeEnum = gridSize;
            GridSize = MyDefinitionManager.Static.GetCubeSize(gridSize);
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME; //because of debug draw

            Skeleton = new MyGridSkeleton();

            GridCounter++;

            AddDebugRenderComponent(new MyDebugRenderComponentCubeGrid(this));

            OnPhysicsChanged += delegate(MyEntity entity)
            {
                MyPhysics.RemoveDestructions(entity);
            };

            if (MyFakes.ASSERT_CHANGES_IN_SIMULATION)
            {
                OnPhysicsChanged += e => Debug.Assert(!MyPhysics.InsideSimulation, "Physics change inside simulation!");
                OnGridSplit += (g1, g2) => Debug.Assert(!MyPhysics.InsideSimulation, "Physics change inside simulation!");
            }
        }

        private void CreateSystems()
        {
            ProfilerShort.Begin("CubeGrid.CreateSystems()");

            GridSystems = (MyCubeGridSystems)Activator.CreateInstance(m_gridSystemsType, this);

            ProfilerShort.End();
        }


        #region Overrides

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            InitInternal(objectBuilder, rebuildGrid: true);
        }

        [Conditional("DEBUG")]
        private void AssertNonPublicBlock(MyObjectBuilder_CubeBlock block)
        {
            MyCubeBlockDefinition definition;
            var upgBlock = UpgradeCubeBlock(block, out definition);
            var compound = upgBlock as MyObjectBuilder_CompoundCubeBlock;
            if (compound != null)
            {
                foreach (var b in compound.Blocks)
                {
                    MyCubeBlockDefinition def;
                    Debug.Assert(!MyDefinitionManager.Static.TryGetCubeBlockDefinition(b.GetId(), out def) || def.Public || def.IsGeneratedBlock, "Non-public block definition being added to world: " + def.Id);
                }
            }
            else if (definition != null)
            {
                Debug.Assert(definition.Public, "Non-public block definition being added to world: " + definition.Id);
            }
        }

        [Conditional("DEBUG")]
        private void AssertNonPublicBlocks(MyObjectBuilder_CubeGrid builder)
        {
            foreach (var block in builder.CubeBlocks)
            {
                AssertNonPublicBlock(block);
            }
        }

        private bool RemoveNonPublicBlock(MyObjectBuilder_CubeBlock block)
        {
            MyCubeBlockDefinition definition;
            var upgBlock = UpgradeCubeBlock(block, out definition);
            var compound = upgBlock as MyObjectBuilder_CompoundCubeBlock;
            if (compound != null)
            {
                MyCubeBlockDefinition def;
                compound.Blocks = compound.Blocks.Where(s => !MyDefinitionManager.Static.TryGetCubeBlockDefinition(s.GetId(), out def) || def.Public || def.IsGeneratedBlock).ToArray();
                return compound.Blocks.Length == 0;
            }
            else if (definition != null && !definition.Public)
            {
                return true;
            }
            return false;
        }

        private void RemoveNonPublicBlocks(MyObjectBuilder_CubeGrid builder)
        {
            builder.CubeBlocks.RemoveAll(s => RemoveNonPublicBlock(s));
        }

        private void InitInternal(MyObjectBuilder_EntityBase objectBuilder, bool rebuildGrid)
        {
            var lst = new List<MyDefinitionId>();
            SyncFlag = true;
            base.Init(objectBuilder);

            Init(null, null, null, null, null);

            var builder = (MyObjectBuilder_CubeGrid)objectBuilder;

            if (MyFakes.ASSERT_NON_PUBLIC_BLOCKS)
                AssertNonPublicBlocks(builder);

            if (MyFakes.REMOVE_NON_PUBLIC_BLOCKS)
                RemoveNonPublicBlocks(builder);

            Render.CreateAdditionalModelGenerators(builder != null ? builder.GridSizeEnum : MyCubeSize.Large);

            CreateSystems();

            if (builder != null)
            {
                if (builder.IsStatic)
                    StaticGrids.Add(this);
                CreatePhysics = builder.CreatePhysics;
                m_enableSmallToLargeConnections = builder.EnableSmallToLargeConnections;
                GridSizeEnum = builder.GridSizeEnum;

                GridSystems.BeforeBlockDeserialization(builder);

                m_cubes.Clear();
                m_cubeBlocks.Clear();

                m_tmpOccupiedCockpits.Clear();
                foreach (var cubeBlock in builder.CubeBlocks)
                {
                    Debug.Assert(cubeBlock.IntegrityPercent > 0.0f, "Block is in inconsistent state in grid initialization");
                    var block = AddBlock(cubeBlock, false);
                    Debug.Assert(block != null, "Block was not added");

                    if (block != null)
                    {
                        if (block.FatBlock is MyCompoundCubeBlock)
                        {
                            foreach (var b in (block.FatBlock as MyCompoundCubeBlock).GetBlocks())
                                if (!lst.Contains(b.BlockDefinition.Id))
                                    lst.Add(b.BlockDefinition.Id);
                        }
                        else
                            if (!lst.Contains(block.BlockDefinition.Id))
                                lst.Add(block.BlockDefinition.Id);

                        if (block.FatBlock is MyCockpit)
                        {
                            var cockpit = block.FatBlock as MyCockpit;
                            if (cockpit.Pilot != null)
                                m_tmpOccupiedCockpits.Add(cockpit);
                        }
                    }
                }

                GridSystems.AfterBlockDeserialization();

                if (builder.Skeleton != null)
                {
                    // After adding blocks!
                    Skeleton.Deserialize(builder.Skeleton, GridSize, GridSize);
                }

                Render.RenderData.SetBasePositionHint(Min * GridSize - GridSize);
                if (rebuildGrid)
                    RebuildGrid();

                ProfilerShort.Begin("Block groups");
                foreach (var groupBuilder in builder.BlockGroups)
                    AddGroup(groupBuilder);
                ProfilerShort.End();

                if (Physics != null)
                {
                    Physics.LinearVelocity = builder.LinearVelocity;
                    Physics.AngularVelocity = builder.AngularVelocity;
                    if (!IsStatic)
                        Physics.Shape.BlocksConnectedToWorld.Clear();
                    if(MyPerGameSettings.InventoryMass)
                        m_inventoryMassDirty = true;
                }

                XSymmetryPlane = builder.XMirroxPlane;
                YSymmetryPlane = builder.YMirroxPlane;
                ZSymmetryPlane = builder.ZMirroxPlane;
                XSymmetryOdd = builder.XMirroxOdd;
                YSymmetryOdd = builder.YMirroxOdd;
                ZSymmetryOdd = builder.ZMirroxOdd;

                GridSystems.Init(builder);

                if (builder.DisplayName == null)
                    DisplayName = MakeCustomName();
                else
                    DisplayName = builder.DisplayName;

                m_destructibleBlocks = builder.DestructibleBlocks;

                if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
                {
                    m_ownershipManager = new MyCubeGridOwnershipManager();
                    m_ownershipManager.Init(this);
                }
            }

            Render.CastShadows = true;
            Render.NeedsResolveCastShadow = false;

            if (MyStructuralIntegrity.Enabled)
            {
                // Requires scene information (checking fixed blocks) so wait until scene is created.
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }

            // This is deffered here so that the pilot takes control of the full grid
            foreach (var cockpit in m_tmpOccupiedCockpits)
            {
                cockpit.GiveControlToPilot();
            }
            m_tmpOccupiedCockpits.Clear();
        }

        private static MyCubeGrid CreateForSplit(MyCubeGrid originalGrid, long newEntityId)
        {
            var builder = MyObjectBuilderSerializer.CreateNewObject(typeof(MyObjectBuilder_CubeGrid)) as MyObjectBuilder_CubeGrid;
            builder.EntityId = newEntityId;
            builder.GridSizeEnum = originalGrid.GridSizeEnum;
            builder.IsStatic = originalGrid.IsStatic;
            builder.PersistentFlags = originalGrid.Render.PersistentFlags;
            builder.PositionAndOrientation = new MyPositionAndOrientation(originalGrid.WorldMatrix);

            // We have to call init manually to prevent the grid from being rebuilt with zero blocks, which would immediately mark it for close
            var newGrid = MyEntities.CreateFromObjectBuilderNoinit(builder) as MyCubeGrid;

            if (newGrid == null) return null;

            newGrid.InitInternal(builder, rebuildGrid: false);
            return newGrid;
        }
        
        // RemoveSplit and CreateSplit should behave the same for the old grid. They only differ in the fact that
        // CreateSplit creates a new grid from the removed blocks, whereas the first method only removes the blocks
        // from the old grid (this is used for splits without physics - e.g. interior lights)

        public static void RemoveSplit(MyCubeGrid originalGrid, List<MySlimBlock> blocks, int offset, int count, bool sync = true)
        {
            ProfilerShort.Begin("RemoveBlockInternal");
            for (int i = offset; i < offset + count; i++)
            {
                var block = blocks[i];
                if (block == null)
                    continue;

                if (block.FatBlock != null)
                    originalGrid.Hierarchy.RemoveChild(block.FatBlock);

                originalGrid.RemoveBlockInternal(block, close: true, markDirtyDisconnects: false);
                originalGrid.Physics.AddDirtyBlock(block);
            }
            ProfilerShort.End();

            originalGrid.RemoveEmptyBlockGroups();

            if (sync == true)
            {
                Debug.Assert(Sync.IsServer);
                if (!Sync.IsServer) return;

                originalGrid.SyncObject.AnnounceRemoveSplit(blocks);
                return;
            }
        }

        public static void CreateSplit(MyCubeGrid originalGrid, List<MySlimBlock> blocks, bool sync = true, long newEntityId = 0)
        {
            ProfilerShort.Begin("Init grid");
            var newGrid = MyCubeGrid.CreateForSplit(originalGrid, newEntityId);

            ProfilerShort.End();

            if (newGrid == null) return;

            Vector3 oldCenterOfMass = originalGrid.Physics.CenterOfMassWorld;

            MyEntities.Add(newGrid);
            MyCubeGrid.MoveBlocks(originalGrid, newGrid, blocks, 0, blocks.Count);
            newGrid.RebuildGrid();

            if (originalGrid.IsStatic && MySession.Static.EnableStationVoxelSupport)
            {
                newGrid.TestDynamic = true;
                originalGrid.TestDynamic = true;
              
            }

            newGrid.Physics.AngularVelocity = originalGrid.Physics.AngularVelocity;
            newGrid.Physics.LinearVelocity = originalGrid.Physics.GetVelocityAtPoint(newGrid.Physics.CenterOfMassWorld);

            // CH: TODO: (Optimization) recalculate the original grid only when all splits are done. This will have to be synced by extra message
            originalGrid.Physics.UpdateShape();
            Vector3 velocityAtNewCOM = Vector3.Cross(originalGrid.Physics.AngularVelocity, originalGrid.Physics.CenterOfMassWorld - oldCenterOfMass);
            originalGrid.Physics.LinearVelocity = originalGrid.Physics.LinearVelocity + velocityAtNewCOM;

            if (originalGrid.OnGridSplit != null)
            {
                originalGrid.OnGridSplit(originalGrid, newGrid);
            }

            if (sync == true)
            {
                Debug.Assert(Sync.IsServer);
                if (!Sync.IsServer) return;

                originalGrid.SyncObject.AnnounceCreateSplit(blocks, newGrid.EntityId);
                return;
            }
        }

        /// <summary>
        /// SplitBlocks list can contain null when received from network
        /// </summary>
        public static void CreateSplits(MyCubeGrid originalGrid, List<MySlimBlock> splitBlocks, List<MyDisconnectHelper.Group> groups, bool sync = true)
        {
            Vector3 oldCenterOfMass = originalGrid.Physics.CenterOfMassWorld;

            try
            {
                if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS)
                {
                    ProfilerShort.Begin("BeforeGridSplit_SmallToLargeGridConnetivity");
                    MyCubeGridSmallToLargeConnection.Static.BeforeGridSplit_SmallToLargeGridConnectivity(originalGrid);
                    ProfilerShort.End();
                }

                // Create new grids, move blocks
                ProfilerShort.Begin("Create grids and move");
                for (int i = 0; i < groups.Count; i++)
                {
                    CreateSplitForGroup(originalGrid, splitBlocks, ref groups.GetInternalArray()[i]);
                }
                ProfilerShort.End();

                // Update old grid shape
                ProfilerShort.Begin("Update original grid shape");
                originalGrid.Physics.UpdateShape();
                ProfilerShort.End();

                // Rebuild new grids
                foreach (var newGrid in originalGrid.m_tmpGrids)
                {
                    ProfilerShort.Begin("Update new grid shape");
                    newGrid.RebuildGrid();
                    if (originalGrid.IsStatic && MySession.Static.EnableStationVoxelSupport)
                    {
                        newGrid.TestDynamic = true;
                        originalGrid.TestDynamic = true;

                    }
                    newGrid.Physics.AngularVelocity = originalGrid.Physics.AngularVelocity;
                    newGrid.Physics.LinearVelocity = originalGrid.Physics.GetVelocityAtPoint(newGrid.Physics.CenterOfMassWorld);
                    ProfilerShort.End();
                }

                // Update old grid velocity
                Vector3 velocityAtNewCOM = Vector3.Cross(originalGrid.Physics.AngularVelocity, originalGrid.Physics.CenterOfMassWorld - oldCenterOfMass);
                originalGrid.Physics.LinearVelocity = originalGrid.Physics.LinearVelocity + velocityAtNewCOM;

                if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS)
                {
                    ProfilerShort.Begin("AfterGridSplit_SmallToLargeGridConnetivity");
                    MyCubeGridSmallToLargeConnection.Static.AfterGridSplit_SmallToLargeGridConnectivity(originalGrid, originalGrid.m_tmpGrids);
                    ProfilerShort.End();
                }

                var handler = originalGrid.OnGridSplit;
                if (handler != null)
                {
                    ProfilerShort.Begin("Handle OnGridSplit");
                    foreach (var newGrid in originalGrid.m_tmpGrids)
                    {
                        handler(originalGrid, newGrid);
                    }
                    ProfilerShort.End();
                }
            }
            finally
            {
                originalGrid.m_tmpGrids.Clear();
            }

            if (sync)
            {
                Debug.Assert(Sync.IsServer);
                if (!Sync.IsServer) return;

                originalGrid.SyncObject.AnnounceCreateSplits(splitBlocks, groups);

            }
        }

        private static void CreateSplitForGroup(MyCubeGrid originalGrid, List<MySlimBlock> splitBlocks, ref MyDisconnectHelper.Group group)
        {
            // In voxels test
            if (!originalGrid.IsStatic && Sync.IsServer && group.IsValid)
            {
                ProfilerShort.Begin("IsInVoxels");
                int inVoxels = 0;
                for (int bi = group.FirstBlockIndex; bi < group.FirstBlockIndex + group.BlockCount; bi++)
                {
                    if (MyDisconnectHelper.IsDestroyedInVoxels(splitBlocks[bi]))
                    {
                        inVoxels++;
                        if ((inVoxels / (float)group.BlockCount) > 0.4f) // 40% of blocks in voxels
                        {
                            group.IsValid = false;
                            break;
                        }
                    }
                }
                ProfilerShort.End();
            }

            // Can have physics test
            group.IsValid = group.IsValid && CanHavePhysics(splitBlocks, group.FirstBlockIndex, group.BlockCount);

            Debug.Assert(Sync.IsServer || !(group.IsValid && group.EntityId == 0), "Invalid split entity id");

            if (group.BlockCount == 1 && splitBlocks[group.FirstBlockIndex] != null && splitBlocks[group.FirstBlockIndex].FatBlock is MyFracturedBlock)
            {
                group.IsValid = false;
                if(Sync.IsServer)
                    MyDestructionHelper.CreateFracturePiece(splitBlocks[group.FirstBlockIndex].FatBlock as MyFracturedBlock, true);
            }

            if (group.IsValid)
            {
                ProfilerShort.Begin("Init grid");
                var newGrid = MyCubeGrid.CreateForSplit(originalGrid, group.EntityId);
                ProfilerShort.End();

                if (newGrid != null)
                {
                    ProfilerShort.Begin("Move blocks");
                    originalGrid.m_tmpGrids.Add(newGrid);
                    MyEntities.Add(newGrid);
                    MyCubeGrid.MoveBlocks(originalGrid, newGrid, splitBlocks, group.FirstBlockIndex, group.BlockCount);
                    group.EntityId = newGrid.EntityId;
                    ProfilerShort.End();
                }
                else
                {
                    Debug.Fail("Split grid not created for unknown reason");
                    group.IsValid = false;
                }
            }

            if (!group.IsValid)
            {
                RemoveSplit(originalGrid, splitBlocks, group.FirstBlockIndex, group.BlockCount, false);
            }

            Debug.Assert(!(group.IsValid && group.EntityId == 0), "Entity id is 0, but grid is marked as valid");
        }

        private void AddGroup(MyObjectBuilder_BlockGroup groupBuilder)
        {
            if (groupBuilder.Blocks.Count == 0)
                return;

            MyBlockGroup group = new MyBlockGroup(this);
            group.Init(groupBuilder);
            BlockGroups.Add(group);
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_CubeGrid ob = (MyObjectBuilder_CubeGrid)base.GetObjectBuilder(copy);
            GetObjectBuilderInternal(ob, copy);
            return ob;
        }

        private void GetObjectBuilderInternal(MyObjectBuilder_CubeGrid ob, bool copy)
        {
            ob.GridSizeEnum = GridSizeEnum;
            if (ob.Skeleton == null)
                ob.Skeleton = new List<BoneInfo>();
            ob.Skeleton.Clear();
            Skeleton.Serialize(ob.Skeleton, GridSize, this);

            ob.IsStatic = StaticGrids.Contains(this);

            ob.CubeBlocks.Clear();
            foreach (var block in m_cubeBlocks)
            {
                MyObjectBuilder_CubeBlock objectBuilder = null;
                if (copy)
                    objectBuilder = (MyObjectBuilder_CubeBlock)block.GetCopyObjectBuilder();
                else
                    objectBuilder = (MyObjectBuilder_CubeBlock)block.GetObjectBuilder();
                Debug.Assert(objectBuilder != null, "Could not get object builder for cube block");
                if (objectBuilder != null)
                    ob.CubeBlocks.Add(objectBuilder);
            }

            System.Diagnostics.Debug.Assert(ob.CubeBlocks.Count > 0);

            ob.PersistentFlags = this.Render.PersistentFlags;

            if (Physics != null)
            {
                ob.LinearVelocity = Physics.LinearVelocity;
                ob.AngularVelocity = Physics.AngularVelocity;
            }
            else
            {
                ob.LinearVelocity = Vector3.Zero;
                ob.AngularVelocity = Vector3.Zero;
            }
            ob.XMirroxPlane = XSymmetryPlane;
            ob.YMirroxPlane = YSymmetryPlane;
            ob.ZMirroxPlane = ZSymmetryPlane;
            ob.XMirroxOdd = XSymmetryOdd;
            ob.YMirroxOdd = YSymmetryOdd;
            ob.ZMirroxOdd = ZSymmetryOdd;

            ob.BlockGroups.Clear();
            foreach (var group in BlockGroups)
            {
                ob.BlockGroups.Add(group.GetObjectBuilder());
            }

            ob.DisplayName = DisplayName;
            ob.DestructibleBlocks = DestructibleBlocks;

            GridSystems.GetObjectBuilder(ob);
        }

        internal void HavokSystemIDChanged(int id)
        {
            if (OnHavokSystemIDChanged != null)
                OnHavokSystemIDChanged(id);
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (MyStructuralIntegrity.Enabled)
            {
                CreateStructuralIntegrity();
            }
            base.UpdateOnceBeforeFrame();

            if (MyFakes.ENABLE_GRID_SYSTEM_UPDATE || MyFakes.ENABLE_GRID_SYSTEM_ONCE_BEFORE_FRAME_UPDATE)
            {
                GridSystems.UpdateOnceBeforeFrame();
            }
        }

        public void CreateStructuralIntegrity()
        {
            if (m_gridSizeEnum == MyCubeSize.Large)
            {
                StructuralIntegrity = new MyStructuralIntegrity(this);
            }
        }

        public void CloseStructuralIntegrity()
        {
            if (StructuralIntegrity != null)
            {
                StructuralIntegrity.Close();
                StructuralIntegrity = null;
            }
        }

        public override void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("Lazy updates");
            DoLazyUpdates();
            ProfilerShort.End();

            if (MyFakes.ENABLE_GRID_SYSTEM_UPDATE)
            {
                ProfilerShort.Begin("Grid systems");
                GridSystems.UpdateBeforeSimulation();
                ProfilerShort.End();
            }

            ProfilerShort.Begin("Base update");
            base.UpdateBeforeSimulation();
            ProfilerShort.End();

            ProfilerShort.Begin("Thrash removal");
            if (Sync.IsServer && IsTrash())
            {
                SyncObject.SendCloseRequest();
            }

            if (Physics != null)
            {
                Physics.UpdateShape();
            }

            ProfilerShort.End();
        }

        protected static float GetLineWidthForGizmo(IMyGizmoDrawableObject block, BoundingBox box)
        {
            box.GetCorners(m_gizmoCorners);
            //we don't want to gizmo line be smaller in distance, so we need to adjust line width according to distance from camera
            float minDistance = m_gizmoMaxDistanceFromCamera;

            foreach (var corner in m_gizmoCorners)
            {
                minDistance = (float)Math.Min(minDistance, Math.Abs(World.MySector.MainCamera.GetDistanceWithFOV(Vector3.Transform(block.GetPositionInGrid() + corner, block.GetWorldMatrix()))));
            }
            Vector3 FieldSize = box.Max - box.Min;
            //draw transparent box adjusts witdh based on box size, we don't want this, so we need to inverse this operation
            float minFieldSize = MathHelper.Max(1, MathHelper.Min(MathHelper.Min(FieldSize.X, FieldSize.Y), FieldSize.Z));
            return (minDistance * m_gizmoDrawLineScale) / minFieldSize;
        }

        public bool IsGizmoDrawingEnabled()
        {
            return ShowSenzorGizmos || ShowGravityGizmos || ShowAntennaGizmos;
        }

        public override void PrepareForDraw()
        {
            base.PrepareForDraw();

            if (m_dirtyRegion.IsDirty)
            {
                UpdateDirty();
            }
            else
            { // Hack so that buffered update of dirty cells is performed at some point.
                Render.RebuildDirtyCells();
            }

            GridSystems.PrepareForDraw();

            //gravity and senzor gizmos needs to be drawn even when object / parent grid is not visible
            if (IsGizmoDrawingEnabled())
            {
                foreach (var block in m_cubeBlocks)
                {
                    if ((block.FatBlock is IMyGizmoDrawableObject))
                    {
                        DrawObjectGizmo(block);
                    }
                }
            }
        }

        private static void DrawObjectGizmo(MySlimBlock block)
        {
            IMyGizmoDrawableObject drawObject = block.FatBlock as IMyGizmoDrawableObject;
            if (drawObject.CanBeDrawed())
            {
                Color gizmoColor = drawObject.GetGizmoColor();
                MatrixD worldMatrix = drawObject.GetWorldMatrix();

                BoundingBox? gizmoBoundingBox = drawObject.GetBoundingBox();

                if (null != gizmoBoundingBox)
                {
                    float gizmoLineWidth = GetLineWidthForGizmo(drawObject, gizmoBoundingBox.Value);
                    BoundingBoxD box = (BoundingBoxD)gizmoBoundingBox.Value;
                    Graphics.MySimpleObjectDraw.DrawTransparentBox(ref worldMatrix, ref box, ref gizmoColor, Graphics.MySimpleObjectRasterizer.SolidAndWireframe, 1, gizmoLineWidth);
                }
                else
                {
                    float radius = drawObject.GetRadius();
                    float distanceToObject = (float)MySector.MainCamera.GetDistanceWithFOV(worldMatrix.Translation);
                    float gizmoDistanceToCamera = (float)(radius - MySector.MainCamera.GetDistanceWithFOV(worldMatrix.Translation));
                    float thickness = m_gizmoDrawLineScale * Math.Min(m_gizmoMaxDistanceFromCamera, Math.Abs(gizmoDistanceToCamera));
                    int projectionId = -1;
                    Graphics.MySimpleObjectDraw.DrawTransparentSphere(ref worldMatrix, radius, ref gizmoColor, Graphics.MySimpleObjectRasterizer.SolidAndWireframe, 20, null, null, thickness, projectionId);

                    if (drawObject.EnableLongDrawDistance() && MyFakes.ENABLE_LONG_DISTANCE_GIZMO_DRAWING)
                    {
                        m_viewProjection.CameraPosition = MySector.MainCamera.Position;
                        m_viewProjection.View = MySector.MainCamera.ViewMatrix;
                        m_viewProjection.Viewport = MySector.MainCamera.Viewport;
                        m_viewProjection.DepthRead = true;


                        float aspectRatio = m_viewProjection.Viewport.Width / m_viewProjection.Viewport.Height;
                        m_viewProjection.Projection = Matrix.CreatePerspectiveFieldOfView(MySector.MainCamera.FieldOfView, aspectRatio, 1, 100);
                        m_viewProjection.Projection.M33 = -1;
                        m_viewProjection.Projection.M34 = -1;
                        m_viewProjection.Projection.M43 = 0;
                        m_viewProjection.Projection.M44 = 0;

                        projectionId = 10;
                        VRageRender.MyRenderProxy.AddBillboardViewProjection(projectionId, m_viewProjection);

                        Graphics.MySimpleObjectDraw.DrawTransparentSphere(ref worldMatrix, radius, ref gizmoColor, Graphics.MySimpleObjectRasterizer.SolidAndWireframe, 20, null, null, thickness, projectionId);
                    }
                }
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            if (MyFakes.ENABLE_GRID_SYSTEM_UPDATE)
            {
                GridSystems.UpdateBeforeSimulation10();
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (MyFakes.ENABLE_GRID_SYSTEM_UPDATE)
            {
                GridSystems.UpdateBeforeSimulation100();
            }
        }

        bool m_inventoryMassDirty;
        internal void SetInventoryMassDirty()
        {
            m_inventoryMassDirty = true;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            foreach (var generator in AdditionalModelGenerators)
                generator.UpdateAfterSimulation();

            SyncObject.SendRemovedBlocks();

            if (!CanHavePhysics())
            {
                Close();
                return;
            }

            if (Sync.IsServer)
            {
                if (Physics != null && Physics.GetFracturedBlocks().Count > 0)
                {
                    EnableGenerators(false);
                    foreach (var info in Physics.GetFracturedBlocks())
                    {
                        CreateFracturedBlock(info);
                    }
                    EnableGenerators(true);
                }
            }

            StepStructuralIntegrity();

            if (TestDynamic)
            {
                if (!MyCubeGrid.ShouldBeStatic(this) && IsStatic)
                {
                    ConvertToDynamic();
                }
                TestDynamic = false;
            }

            DoLazyUpdates();

            if (Physics != null && Physics.Enabled)
            {
                if (m_inventoryMassDirty)
                {
                    m_inventoryMassDirty = false;
                    Physics.Shape.UpdateMassFromInventories(m_cubeBlocks, Physics);
                }

                if (IsStatic == false)
                {
                    Physics.RigidBody.Gravity = MyGravityProviderSystem.CalculateGravityInPointForGrid(PositionComp.GetPosition());
                }

                if (Physics.RigidBody2 != null)
                {
                    if (Physics.RigidBody2.LinearVelocity != Physics.RigidBody.LinearVelocity)
                        Physics.RigidBody2.LinearVelocity = Physics.RigidBody.LinearVelocity;

                    if (Physics.RigidBody2.AngularVelocity != Physics.RigidBody.AngularVelocity)
                        Physics.RigidBody2.AngularVelocity = Physics.RigidBody.AngularVelocity;

                    if (Physics.RigidBody2.CenterOfMassLocal != Physics.RigidBody.CenterOfMassLocal)
                        Physics.RigidBody2.CenterOfMassLocal = Physics.RigidBody.CenterOfMassLocal;
                }
            }

        }

        private void StepStructuralIntegrity()
        {
            if (Physics == null || Physics.HavokWorld == null)
            {
                return;
            }
            ProfilerShort.Begin("MyCubeGrid.StructuralIntegrity.Update");

            if (StructuralIntegrity == null && MyStructuralIntegrity.Enabled)
                CreateStructuralIntegrity();

            if (StructuralIntegrity != null)
            {
                StructuralIntegrity.Update(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
            }

            ProfilerShort.End();
        }

        public void ResetStructuralIntegrity()
        {
            if (StructuralIntegrity != null)
            {
                StructuralIntegrity = null;
            }
        }

        internal void RemoveGroup(MyBlockGroup group)
        {
            BlockGroups.Remove(group);

            GridSystems.RemoveGroup(group);
        }

        internal void AddGroup(MyBlockGroup group)
        {
            foreach (var g in BlockGroups)
                if (g.Name.CompareTo(group.Name) == 0)
                {
                    BlockGroups.Remove(g);
                    if (group.CubeGrid != this)
                        group.Blocks.AddList(g.Blocks);
                    break;
                }
            group.CubeGrid = this;
            BlockGroups.Add(group);

            GridSystems.AddGroup(group);
        }

        internal void OnAddedToGroup(MyGridLogicalGroupData group)
        {
            ProfilerShort.Begin("MyCubeGrid.OnAddedToGroup");

            GridSystems.OnAddedToGroup(group);

            if (AddedToLogicalGroup != null)
                AddedToLogicalGroup(group);

            ProfilerShort.End();
        }

        internal void OnRemovedFromGroup(MyGridLogicalGroupData group)
        {
            ProfilerShort.Begin("MyCubeGride.OnRemovedFromGroup");

            GridSystems.OnRemovedFromGroup(group);

            if (RemovedFromLogicalGroup != null)
                RemovedFromLogicalGroup();

            ProfilerShort.End();
        }

        internal void OnAddedToGroup(MyGridPhysicalGroupData groupData)
        {
            GridSystems.OnAddedToGroup(groupData);
        }

        internal void OnRemovedFromGroup(MyGridPhysicalGroupData group)
        {
            GridSystems.OnRemovedFromGroup(group);
        }

        /// <summary>
        /// Reduces the control of the current group if the current grid is the one that the player is sitting in
        /// </summary>
        private void TryReduceGroupControl()
        {
            var controller = Sync.Players.GetEntityController(this);
            if (controller != null && controller.ControlledEntity is MyCockpit)
            {
                var cockpit = controller.ControlledEntity as MyCockpit;
                if (cockpit.CubeGrid == this)
                {
                    var logicalGroups = MyCubeGridGroups.Static.Logical;
                    var group = logicalGroups.GetGroup(this);
                    if (group != null) // CH:TODO: During exitting to the main menu, the group can be null according to some crash logs. Find out why
                        foreach (var node in group.Nodes)
                        {
                            if (node.NodeData == this) continue;

                            // CH: This is to catch a nullref (unrelated to the previous comment)
                            if (MySession.Static == null) MyLog.Default.WriteLine("MySession.Static was null");
                            else if (MySession.Static.SyncLayer == null) MyLog.Default.WriteLine("MySession.Static.SyncLayer was null");
                            else if (Sync.Clients == null) MyLog.Default.WriteLine("Sync.Clients was null");

                            Sync.Players.TryReduceControl(cockpit, node.NodeData);
                        }
                }
            }
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            MyCubeGridGroups.Static.AddNode(GridLinkTypeEnum.Logical, this);
            MyCubeGridGroups.Static.AddNode(GridLinkTypeEnum.Physical, this);
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);
            MyCubeGridGroups.Static.RemoveNode(GridLinkTypeEnum.Physical, this);
            MyCubeGridGroups.Static.RemoveNode(GridLinkTypeEnum.Logical, this);
        }

        protected override void BeforeDelete()
        {
            if (IsStatic)
                StaticGrids.Remove(this);

            SyncObject.SendRemovedBlocks();

            m_cubes.Clear();

            // This is here because of groups, not better way to do it
            // also network sync is disabled for terminal systems and groups are unlinked here
            MyEntities.Remove(this);
            UnregisterBlocksBeforeClose();

            Render.CloseModelGenerators();

            base.BeforeDelete();

            GridCounter--;
        }

        private void UnregisterBlocks(IEnumerable<MySlimBlock> cubeBlocks)
        {
            foreach (var block in cubeBlocks)
            {
                if (block.FatBlock != null)
                {
                    GridSystems.UnregisterFromSystems(block.FatBlock);
                }
            }
        }

        private void UnregisterBlocksBeforeClose()
        {
            GridSystems.BeforeGridClose();

            UnregisterBlocks(m_cubeBlocks);

            GridSystems.AfterGridClose();
        }

        internal override bool GetIntersectionWithLine(ref LineD line, out MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            t = null;

            RayCastCells(line.From, line.To, m_cacheRayCastCells);
            if (m_cacheRayCastCells.Count == 0)
                return false;

            foreach (Vector3I hit in m_cacheRayCastCells)
            {
                if (m_cubes.ContainsKey(hit))
                {
                    var cube = m_cubes[hit];
                    GetBlockIntersection(cube, ref line, out t, flags);

                    if (t.HasValue)
                        break;
                }
            }

            return t.HasValue;
        }

        internal bool GetIntersectionWithLine(ref LineD line, out MyIntersectionResultLineTriangleEx? t, out MySlimBlock slimBlock, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            t = null;
            slimBlock = null;

            RayCastCells(line.From, line.To, m_cacheRayCastCells);
            if (m_cacheRayCastCells.Count == 0)
                return false;

            foreach (Vector3I hit in m_cacheRayCastCells)
            {
                if (m_cubes.ContainsKey(hit))
                {
                    var cube = m_cubes[hit];
                    GetBlockIntersection(cube, ref line, out t, flags);

                    if (t.HasValue)
                    {
                        slimBlock = cube.CubeBlock;
                        break;
                    }
                }
            }

            if (slimBlock != null)
            {
                if (slimBlock.FatBlock is MyCompoundCubeBlock)
                {
                    MyCompoundCubeBlock compoundBlock = slimBlock.FatBlock as MyCompoundCubeBlock;
                    ListReader<MySlimBlock> slimBlocksInCompound = compoundBlock.GetBlocks();
                    double distanceSquaredInCompound = double.MaxValue;
                    MySlimBlock slimBlockInCompound = null;

                    for (int i = 0; i < slimBlocksInCompound.Count; ++i)
                    {
                        MySlimBlock cmpSlimBlock = slimBlocksInCompound.ItemAt(i);
                        MyIntersectionResultLineTriangleEx? intersectionTriResult;
                        if (cmpSlimBlock.FatBlock.GetIntersectionWithLine(ref line, out intersectionTriResult) && intersectionTriResult != null)
                        {
                            Vector3D startToIntersection = intersectionTriResult.Value.IntersectionPointInWorldSpace - line.From;
                            double instrDistanceSq = startToIntersection.LengthSquared();
                            if (instrDistanceSq < distanceSquaredInCompound)
                            {
                                distanceSquaredInCompound = instrDistanceSq;
                                slimBlockInCompound = cmpSlimBlock;
                            }
                        }
                    }

                    slimBlock = slimBlockInCompound;
                }
            }

            return t.HasValue;
        }

        public override bool GetIntersectionWithSphere(ref BoundingSphereD sphere)
        {
            ProfilerShort.Begin("CubeGrid.GetIntersectionWithSphere()");
            try
            {
                BoundingBoxD box = new BoundingBoxD(sphere.Center - new Vector3D(sphere.Radius), sphere.Center + new Vector3D(sphere.Radius));

                var invee = MatrixD.Invert(WorldMatrix);
                box = box.Transform(ref invee);
                Vector3 min = box.Min;
                Vector3 max = box.Max;

                Vector3I start = new Vector3I((int)Math.Round(min.X / GridSize), (int)Math.Round(min.Y / GridSize), (int)Math.Round(min.Z / GridSize));
                Vector3I end = new Vector3I((int)Math.Round(max.X / GridSize), (int)Math.Round(max.Y / GridSize), (int)Math.Round(max.Z / GridSize));

                Vector3I startIt = Vector3I.Min(start, end);
                Vector3I endIt = Vector3I.Max(start, end);

                for (int i = startIt.X; i <= endIt.X; i++)
                {
                    for (int j = startIt.Y; j <= endIt.Y; j++)
                    {
                        for (int k = startIt.Z; k <= endIt.Z; k++)
                        {
                            if (m_cubes.ContainsKey(new Vector3I(i, j, k)))
                            {
                                var cube = m_cubes[new Vector3I(i, j, k)];

                                if (cube.CubeBlock.FatBlock == null || cube.CubeBlock.FatBlock.Model == null)
                                {
                                    if (cube.CubeBlock.BlockDefinition.CubeDefinition.CubeTopology == MyCubeTopology.Box)
                                        return true;

                                    foreach (var part in cube.Parts)
                                    {
                                        MatrixD worldMatrix = part.InstanceData.LocalMatrix * WorldMatrix;

                                        MatrixD inv = Matrix.Invert(worldMatrix);

                                        Vector3D transformedPos = Vector3D.Transform(sphere.Center, inv);
                                        BoundingSphereD bs = new BoundingSphereD(transformedPos, sphere.Radius);

                                        foreach (var triangleIndices in part.Model.Triangles)
                                        {
                                            MyTriangle_Vertexes triangle = new MyTriangle_Vertexes();

                                            triangle.Vertex0 = part.Model.GetVertex(triangleIndices.I0);
                                            triangle.Vertex1 = part.Model.GetVertex(triangleIndices.I1);
                                            triangle.Vertex2 = part.Model.GetVertex(triangleIndices.I2);

                                            MyPlane plane = new MyPlane(ref triangle);

                                            if (MyUtils.GetSphereTriangleIntersection(ref bs, ref plane, ref triangle).HasValue)
                                            {
                                                return true;
                                            }
                                        }
                                    }

                                    //Cannot return because more cubes can be in the line..
                                    // return false;
                                }
                                else
                                {
                                    MatrixD worldMatrix = cube.CubeBlock.FatBlock.WorldMatrix;

                                    MatrixD inv = Matrix.Invert(worldMatrix);

                                    Vector3D transformedPos = Vector3D.Transform(sphere.Center, inv);
                                    BoundingSphereD bs = new BoundingSphere(transformedPos, (float)sphere.Radius);

                                    bool intersected = cube.CubeBlock.FatBlock.Model.GetTrianglePruningStructure().GetIntersectionWithSphere(cube.CubeBlock.FatBlock, ref sphere);
                                    if (intersected)
                                        return intersected;
                                }

                                //Cannot return because more cubes can be in the line..
                                // return true;
                            }
                        }
                    }
                }

                return false;
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        public override string ToString()
        {
            var stat = IsStatic ? "S" : "D";
            var size = GridSizeEnum.ToString();

            return "Grid_" + stat + "_" + size + "_" + m_cubeBlocks.Count + " {" + EntityId.ToString("X8") + "}";
        }

        protected override MySyncEntity OnCreateSync()
        {
            var result = new MySyncGrid(this);
            result.BlocksBuiltAreaRequest += BuildBlocksAreaRequest;
            result.BlocksBuiltAreaSuccess += BuildBlocksAreaSuccess;

            result.BlocksRazeAreaRequest += RazeBlocksAreaRequest;
            result.BlocksRazeAreaSuccess += RazeBlocksAreaSuccess;

            result.BlocksBuilt += BuildBlocksSuccess;
            result.BlocksRazed += RazeBlocksSuccess;
            result.BlocksColored += ColorBlockSuccess;
            result.RazedBlockInCompoundBlock += RazeBlockInCompoundBlockSuccess;
            result.BlocksRemovedWithGenerator += BlocksRemovedWithGenerator;
            result.BlocksRemovedWithoutGenerator += BlocksRemovedWithoutGenerator;
            result.BlocksDestroyed += BlocksDestroyed;
            result.BlocksDeformed += BlocksDeformed;
            result.BlockIntegrityChanged += BlockIntegrityChanged;
            result.BlockStockpileChanged += BlockStockpileChanged;

            result.AfterBlocksBuilt += AfterBuildBlocksSuccess;

            result.BlockBuilt += BuildBlockSuccess;
            result.AfterBlockBuilt += AfterBuildBlockSuccess;

            // TODO: Better get rid of this
            result.UpdatesOnlyOnServer = false;
            return result;
        }

        #endregion

        public bool IsTrash()
        {
            // Too far-away grids are removed no matter what (if the world is limited)
            Vector3D pos = Physics != null ? Physics.CenterOfMassWorld : WorldMatrix.Translation;
            if (!MyEntities.IsInsideWorld(pos)) return true;

            if (!MySession.Static.Settings.RemoveTrash || !MyFakes.ENABLE_TRASH_REMOVAL) return false;

            // Grids with enough cubes are not trash
            if (m_cubeBlocks.Count > 8)
                return false;

            // Static or accelerating grids are not trash
            if (Physics == null || Physics.AngularAcceleration.AbsMin() != 0 || Physics.LinearAcceleration.AbsMin() != 0)
                return false;

            // Not moving grids are not trash
            if (Physics.LinearVelocity.AbsMax() < 0.001f)
                return false;

            if (!GridSystems.IsTrash())
                return false;

            // Grids with medbays are not trash
            // CH: TODO: Ideally, this would somehow call SE-specific code to remove reference to medbays
            int medbayNum;
            if (BlocksCounters.TryGetValue(typeof(MyObjectBuilder_MedicalRoom), out medbayNum) && medbayNum > 0)
                return false;

            // Other grids are trash when they are far enough
            float sphereRadius = this.PositionComp.LocalAABB.HalfExtents.AbsMax();
            bool farFromPlayers = true;
            foreach (var controller in Sync.Players.GetOnlinePlayers())
            {
                if (controller.Identity.Character != null)
                {
                    var dist = controller.Identity.Character.Entity.WorldMatrix.Translation - pos;
                    var arcRadius = Math.Atan2(sphereRadius, dist.Length());
                    if (arcRadius > MIN_TRASH_ARC_RADIUS)
                    {
                        farFromPlayers = false;
                        break;
                    }
                }
            }
            if (farFromPlayers) return true;

            return false;
        }

        public Vector3I WorldToGridInteger(Vector3D coords)
        {
            Vector3D localCoords = Vector3D.Transform(coords, PositionComp.WorldMatrixNormalizedInv);
            localCoords /= GridSize;
            return Vector3I.Round(localCoords);
        }

        public Vector3D GridIntegerToWorld(Vector3I gridCoords)
        {
            Vector3D retval = (Vector3D)(Vector3)gridCoords;
            retval *= GridSize;
            return Vector3D.Transform(retval, WorldMatrix);
        }

        public Vector3D GridIntegerToWorld(Vector3D gridCoords)
        {
            Vector3D retval = gridCoords;
            retval *= GridSize;
            return Vector3D.Transform(retval, WorldMatrix);
        }

        public Vector3I LocalToGridInteger(Vector3 localCoords)
        {
            localCoords /= GridSize;
            return Vector3I.Round(localCoords);
        }

        public bool CanAddCubes(Vector3I min, Vector3I max)
        {
            Vector3I current = min;
            for (var it = new Vector3I.RangeIterator(ref min, ref max); it.IsValid(); it.GetNext(out current))
            {
                if (m_cubes.ContainsKey(current))
                    return false;
            }
            return true;
        }

        public bool CanAddCubes(Vector3I min, Vector3I max, MyBlockOrientation? orientation, MyCubeBlockDefinition definition)
        {
            if (MyFakes.ENABLE_COMPOUND_BLOCKS && definition != null)
            {
                Vector3I current = min;
                for (var it = new Vector3I.RangeIterator(ref min, ref max); it.IsValid(); it.GetNext(out current))
                {
                    if (!CanAddCube(current, orientation, definition))
                        return false;
                }
                return true;
            }

            return CanAddCubes(min, max);
        }

        public bool CanAddCube(Vector3I pos, MyBlockOrientation? orientation, MyCubeBlockDefinition definition)
        {
            if (MyFakes.ENABLE_COMPOUND_BLOCKS && definition != null)
            {
                if (definition.CompoundTemplates != null)
                {
                    MyCube cube;
                    if (m_cubes.TryGetValue(pos, out cube))
                    {
                        MyCompoundCubeBlock cmpBlock = cube.CubeBlock.FatBlock as MyCompoundCubeBlock;
                        if (cmpBlock != null)
                        {
                            return cmpBlock.CanAddBlock(definition, orientation);
                        }

                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return !CubeExists(pos);
                }
            }
            else
            {
                return !CubeExists(pos);
            }
        }

        public void ClearSymmetries()
        {
            XSymmetryPlane = null;
            YSymmetryPlane = null;
            ZSymmetryPlane = null;
        }

        public bool IsTouchingAnyNeighbor(Vector3I min, Vector3I max)
        {
            {
                var minMinusX = min; minMinusX.X -= 1;
                var maxMinusX = max; maxMinusX.X = minMinusX.X;
                if (!CanAddCubes(minMinusX, maxMinusX))
                    return true;
            }
            {
                var minMinusY = min; minMinusY.Y -= 1;
                var maxMinusY = max; maxMinusY.Y = minMinusY.Y;
                if (!CanAddCubes(minMinusY, maxMinusY))
                    return true;
            }
            {
                var minMinusZ = min; minMinusZ.Z -= 1;
                var maxMinusZ = max; maxMinusZ.Z = minMinusZ.Z;
                if (!CanAddCubes(minMinusZ, maxMinusZ))
                    return true;
            }

            {
                var maxPlusX = max; maxPlusX.X += 1;
                var minPlusX = min; minPlusX.X = maxPlusX.X;
                if (!CanAddCubes(minPlusX, maxPlusX))
                    return true;
            }
            {
                var maxPlusY = max; maxPlusY.Y += 1;
                var minPlusY = min; minPlusY.Y = maxPlusY.Y;
                if (!CanAddCubes(minPlusY, maxPlusY))
                    return true;
            }
            {
                var maxPlusZ = max; maxPlusZ.Z += 1;
                var minPlusZ = min; minPlusZ.Z = maxPlusZ.Z;
                if (!CanAddCubes(minPlusZ, maxPlusZ))
                    return true;
            }

            return false;
        }

        public bool CanPlaceBlock(Vector3I min, Vector3I max, MyBlockOrientation orientation, MyCubeBlockDefinition definition)
        {
            if (!CanAddCubes(min, max, orientation, definition))
                return false;

            var gridSettings = MyPerGameSettings.BuildingSettings.GetGridPlacementSettings(this);
            return TestPlacementAreaCube(this, ref gridSettings, min, max, orientation, definition, ignoredEntity: this);
        }

        public void SetCubeDirty(Vector3I pos)
        {
            m_dirtyRegion.AddCube(pos);
            var block = GetCubeBlock(pos);
            if (block != null)
            {
                Physics.AddDirtyBlock(block);
            }
        }

        public void SetBlockDirty(MySlimBlock cubeBlock)
        {
            Vector3I cube = cubeBlock.Min;
            for (var it = new Vector3I.RangeIterator(ref cubeBlock.Min, ref cubeBlock.Max); it.IsValid(); it.GetNext(out cube))
            {
                m_dirtyRegion.AddCube(cube);
            }
        }

        public void DebugDrawRange(Vector3I min, Vector3I max)
        {
            Vector3I currentMin = min;
            for (var it = new Vector3I.RangeIterator(ref min, ref max); it.IsValid(); it.GetNext(out currentMin))
            {
                var currentMax = currentMin + 1;

                var obb = new MyOrientedBoundingBoxD(
                    currentMin * GridSize,
                    new Vector3(0.5f * GridSize),
                    Quaternion.Identity);
                obb.Transform(WorldMatrix);
                VRageRender.MyRenderProxy.DebugDrawOBB(obb, Color.White, 0.5f, true, false);
            }
        }

        public void DebugDrawPositions(List<Vector3I> positions)
        {
            foreach (var currentMin in positions)
            {
                var currentMax = currentMin + 1;

                var obb = new MyOrientedBoundingBoxD(
                    currentMin * GridSize,
                    new Vector3(0.5f * GridSize),
                    Quaternion.Identity);
                obb.Transform(WorldMatrix);
                VRageRender.MyRenderProxy.DebugDrawOBB(obb, Color.White.ToVector3(), 0.5f, true, false);
            }
        }

        private MyObjectBuilder_CubeBlock UpgradeCubeBlock(MyObjectBuilder_CubeBlock block, out MyCubeBlockDefinition blockDefinition)
        {
            var defId = block.GetId();

            if (MyFakes.ENABLE_COMPOUND_BLOCKS)
            {
                if (block is MyObjectBuilder_CompoundCubeBlock)
                {
                    MyObjectBuilder_CompoundCubeBlock compoundBock = block as MyObjectBuilder_CompoundCubeBlock;
                    blockDefinition = MyCompoundCubeBlock.GetCompoundCubeBlockDefinition();
                    if (blockDefinition == null)
                        return null;
                    // Check if compound template was removed from definition (then create block without compound shell).
                    if (compoundBock.Blocks.Length == 1)
                    {
                        MyObjectBuilder_CubeBlock innerBlock = compoundBock.Blocks[0];
                        MyCubeBlockDefinition innerBlockDefinition;
                        if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(innerBlock.GetId(), out innerBlockDefinition))
                        {
                            if (innerBlockDefinition.CompoundTemplates == null || innerBlockDefinition.CompoundTemplates.Length == 0)
                            {
                                blockDefinition = innerBlockDefinition;
                                return innerBlock;
                            }
                        }
                    }

                    return block;
                }
                else
                {
                    // Check added compound to definition (means that compound must be created and block set inside)
                    if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out blockDefinition))
                    {
                        if (blockDefinition.CompoundTemplates != null && blockDefinition.CompoundTemplates.Length > 0)
                        {
                            MyObjectBuilder_CompoundCubeBlock compoundCBBuilder = MyCompoundCubeBlock.CreateBuilder(block);
                            MyCubeBlockDefinition compoundBlockDefinition = MyCompoundCubeBlock.GetCompoundCubeBlockDefinition();
                            if (compoundBlockDefinition != null)
                            {
                                blockDefinition = compoundBlockDefinition;
                                return compoundCBBuilder;
                            }
                        }
                    }
                }
            }

            // Upgrading manually as ChangeType causes stack overflow in protobuf-net.
            if (block is MyObjectBuilder_Ladder)
            {
                var passage = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Passage>(block.SubtypeName);

                passage.BlockOrientation = block.BlockOrientation;
                passage.BuildPercent = block.BuildPercent;
                passage.EntityId = block.EntityId;
                passage.IntegrityPercent = block.IntegrityPercent;
                passage.Min = block.Min;

                blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Passage), block.SubtypeId));
                block = passage;
                return block;
            }

            MyObjectBuilder_CubeBlock newBlock = block;

            //defId = new MyDefinitionId(defId.TypeId, "LargeBlockArmorSlope");

            string[] variants = new string[] { "Red", "Yellow", "Blue", "Green", "Black", "White", "Gray" };
            Vector3[] colors = new Vector3[] { MyRenderComponentBase.OldRedToHSV, MyRenderComponentBase.OldYellowToHSV, MyRenderComponentBase.OldBlueToHSV, MyRenderComponentBase.OldGreenToHSV, MyRenderComponentBase.OldBlackToHSV, MyRenderComponentBase.OldWhiteToHSV, MyRenderComponentBase.OldGrayToHSV };

            // Upgrade object builders which were block and now are specific
            if (!MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out blockDefinition))
            {
                newBlock = FindDefinitionUpgrade(block, out blockDefinition);
                if (newBlock == null)
                {
                    // Try convert old variants
                    for (int i = 0; i < variants.Length; i++)
                    {
                        if (defId.SubtypeName.EndsWith(variants[i], StringComparison.InvariantCultureIgnoreCase))
                        {
                            string shortName = defId.SubtypeName.Substring(0, defId.SubtypeName.Length - variants[i].Length);
                            var newDefId = new MyDefinitionId(defId.TypeId, shortName);
                            if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(newDefId, out blockDefinition))
                            {
                                newBlock = block;
                                newBlock.ColorMaskHSV = colors[i];
                                newBlock.SubtypeName = shortName;
                                return newBlock;
                            }
                        }
                    }
                }

                if (newBlock == null)
                {
                    return null;
                }
            }

            return newBlock;
        }

        private bool CanAddBlock(MyBlockLocation location)
        {
            var blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(location.BlockDefinition);
            if (!MyCubeGrid.CheckConnectivity(this, blockDefinition, ref location.Orientation, ref location.CenterPos)) return false;

            Vector3I min = location.Min, max;
            MyBlockOrientation ori = new MyBlockOrientation(ref location.Orientation);
            MySlimBlock.ComputeMax(blockDefinition, ori, ref min, out max);
            if (!CanAddCubes(min, max)) return false;

            return true;
        }

        private MySlimBlock AddBlock(MyObjectBuilder_CubeBlock objectBuilder, bool testMerge)
        {
            ProfilerShort.Begin("MyCubeGrid.AddBlock(...)");

            try
            {
                if (Skeleton == null)
                    Skeleton = new MyGridSkeleton();

                MyCubeBlockDefinition blockDefinition;
                objectBuilder = UpgradeCubeBlock(objectBuilder, out blockDefinition);

                if (objectBuilder == null)
                {
                    return null;
                }

                try
                {
                    return AddCubeBlock(objectBuilder, testMerge, blockDefinition);
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("ERROR while adding cube " + blockDefinition.DisplayNameText.ToString() + ": " + e.ToString());
                    return null;
                }
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        private MySlimBlock AddCubeBlock(MyObjectBuilder_CubeBlock objectBuilder, bool testMerge, MyCubeBlockDefinition blockDefinition)
        {
            Vector3I min = objectBuilder.Min, max;
            MySlimBlock.ComputeMax(blockDefinition, objectBuilder.BlockOrientation, ref min, out max);
            if (!CanAddCubes(min, max))
            {
                return null;
            }

            var objectBlock = MyCubeBlockFactory.CreateCubeBlock(objectBuilder);
            MySlimBlock cubeBlock = objectBlock as MySlimBlock;
            if (cubeBlock == null)
                cubeBlock = new MySlimBlock();

            cubeBlock.Init(objectBuilder, this, objectBlock as MyCubeBlock);
            cubeBlock.AddNeighbours();

            BoundsInclude(cubeBlock);

            if (cubeBlock.FatBlock != null)
            {
                Hierarchy.AddChild(cubeBlock.FatBlock);

                GridSystems.RegisterInSystems(cubeBlock.FatBlock);

                if (cubeBlock.FatBlock.Render.NeedsDrawFromParent)
                    m_blocksForDraw.Add(cubeBlock.FatBlock);

                MyObjectBuilderType blockType = cubeBlock.BlockDefinition.Id.TypeId;
                if (blockType != typeof(MyObjectBuilder_CubeBlock))
                {
                    if (!BlocksCounters.ContainsKey(blockType))
                        BlocksCounters.Add(blockType, 0);
                    BlocksCounters[blockType]++;
                }
            }

            m_cubeBlocks.Add(cubeBlock);

            MyBlockOrientation blockOrientation = objectBuilder.BlockOrientation;
            Matrix rotationMatrix;
            blockOrientation.GetMatrix(out rotationMatrix);

            //Add cubes
            Vector3I rotatedBlockSize;
            MyCubeGridDefinitions.GetRotatedBlockSize(blockDefinition, ref rotationMatrix, out rotatedBlockSize);

            //integer local center of the cube
            Vector3I center = blockDefinition.Center;

            //integer rotated/world center of the cube
            Vector3I rotatedCenter;
            Vector3I.TransformNormal(ref center, ref rotationMatrix, out rotatedCenter);

            bool blockAddSuccessfull = true;
            Vector3I temp = cubeBlock.Min;
            for (var it = new Vector3I.RangeIterator(ref cubeBlock.Min, ref cubeBlock.Max); it.IsValid(); it.GetNext(out temp))
            {
                blockAddSuccessfull &= AddCube(cubeBlock, ref temp, rotationMatrix, blockDefinition);
            }

            Debug.Assert(blockAddSuccessfull, "Cannot add cube block!");

            if (Physics != null)
            {
                Physics.AddBlock(cubeBlock);
            }

            float boneErrorSquared = MyGridSkeleton.GetMaxBoneError(GridSize);
            boneErrorSquared *= boneErrorSquared;

            Vector3I boneMax = (cubeBlock.Min + Vector3I.One) * Skeleton.BoneDensity;
            Vector3I bonePos = cubeBlock.Min * Skeleton.BoneDensity;
            for (var it = new Vector3I.RangeIterator(ref bonePos, ref boneMax); it.IsValid(); it.GetNext(out bonePos))
            {
                Vector3 boneOffset = Skeleton.GetDefinitionOffsetWithNeighbours(cubeBlock.Min, bonePos, this);

                if (boneOffset.LengthSquared() < boneErrorSquared)
                {
                    Skeleton.Bones.Remove(bonePos);
                }
                else
                {
                    Skeleton.Bones[bonePos] = boneOffset;
                }
            }

            if (cubeBlock.BlockDefinition.Skeleton != null && cubeBlock.BlockDefinition.Skeleton.Count > 0)
            {
                if (Physics != null)
                {
                    for (int i = -1; i <= 1; i++)
                        for (int j = -1; j <= 1; j++)
                            for (int k = -1; k <= 1; k++)
                                SetCubeDirty(new Vector3I(i, j, k) + cubeBlock.Min);
                }
            }

            if (testMerge)
            {
                MyCubeGrid mergedGrid = DetectMerge(cubeBlock, null);
                if (mergedGrid != null && mergedGrid != this)
                {
                    //RK is this really necessary? This should always return the same block (because merged grid uses the same slimblock).
                    cubeBlock = mergedGrid.GetCubeBlock(cubeBlock.Position);
                    // Refresh block generators with currently set block.
                    mergedGrid.AdditionalModelGenerators.ForEach(g => g.BlockAddedToMergedGrid(cubeBlock));
                }
                else
                {
                    if (OnBlockAdded != null)
                        OnBlockAdded(cubeBlock);
                }
            }
            else
            {
                if (OnBlockAdded != null)
                    OnBlockAdded(cubeBlock);
            }
            return cubeBlock;
        }

        public MySlimBlock BuildGeneratedBlock(MyBlockLocation location, Vector3 colorMaskHsv)
        {
            MyDefinitionId blockDefinitionId = location.BlockDefinition;
            MyCubeBlockDefinition blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(blockDefinitionId);
            return BuildBlock(blockDefinition, colorMaskHsv, location.Min, location.Orientation, location.Owner, location.EntityId, null);
        }

        public void BuildBlock(Vector3 colorMaskHsv, MyBlockLocation location, MyObjectBuilder_CubeBlock blockObjectBuilder, long builderEntityId)
        {
            SyncObject.BuildBlock(colorMaskHsv, location, blockObjectBuilder, builderEntityId);
        }

        /// <summary>
        /// Network friendly alternative for building block
        /// </summary>
        public void BuildBlocks(long buildBy, ref MyBlockBuildArea area)
        {
            SyncObject.BuildBlocks(buildBy, ref area);
        }

        /// <summary>
        /// Builds many same blocks, used when building lines or planes.
        /// </summary>
        public void BuildBlocks(Vector3 colorMaskHsv, HashSet<MyBlockLocation> locations, long builderEntityId)
        {
            SyncObject.BuildBlocks(colorMaskHsv, locations, builderEntityId);
        }

        /// <summary>
        /// Server method, builds blocks and notifies clients
        /// </summary>
        private void BuildBlocksAreaRequest(ref MyCubeGrid.MyBlockBuildArea area, long ownerId)
        {
            try
            {
                GetValidBuildOffsets(ref area, m_tmpBuildOffsets, m_tmpBuildFailList);
                MyCubeGrid.CheckAreaConnectivity(this, ref area, m_tmpBuildOffsets, m_tmpBuildFailList);

                int entityIdSeed = MyRandom.Instance.CreateRandomSeed();

                SyncObject.BuildBlocksSuccess(ref area, m_tmpBuildFailList, ownerId, entityIdSeed);
                BuildBlocksArea(ref area, m_tmpBuildOffsets, ownerId, entityIdSeed);
            }
            finally
            {
                m_tmpBuildOffsets.Clear();
                m_tmpBuildFailList.Clear();
            }
        }


        //private static void OnPlaceBlockInternal(MySyncGrid sync, ref PlaceBlockMsg msg, MyPlayer sender, bool server)
        //{
        //    MyCharacter builder = sender.Controller.ControlledEntity.Entity as MyCharacter;
        //    if (buidler == null)
        //    {
        //        Debug.Fail("Received incorrect place block message. Character entity does not exist.");
        //        return;
        //    }
        //    MyDefinitionBase definition = new MyDefinitionBase();
        //    MyDefinitionManager.Static.TryGetDefinition(msg.BlockLocation.BlockDefinition, out definition);
        //    MyCubeBlockDefinition blockDefinition = definition as MyCubeBlockDefinition;

        //    Debug.Assert(blockDefinition != null, "Received incorrect place block message. Could not find block definition.");
        //    if (blockDefinition == null) return;

        //    MyInventory inventory = builder.GetInventory();
        //    if (server && !inventory.ContainItems(1m, blockDefinition.Components[0].Definition.Id))
        //    {
        //        Debug.Assert(false, "Character does not have enough components to build the block");
        //        return;
        //    }

        //    var block = sync.Entity.BuildBlock(blockDefinition, ColorExtensions.UnpackHSVFromUint(msg.ColorMaskHsv), msg.BlockLocation, inventory);
        //    Debug.Assert(block != null, "Could not build a new block.");
        //    if (block == null) return;

        //    m_tmpBuildList.Clear();
        //    m_tmpBuildList.Add(msg.BlockLocation);

        //    //First send blocks build, then call OnBuildSuccess because it can already send messages for that block i.e. Attach for motors
        //    if (server)
        //        Sync.Layer.SendMessageToAll(msg, MyTransportMessageEnum.Success);

        //    var handler = sync.AfterBlocksBuilt;
        //    if (handler != null) handler(m_tmpBuildList);
        //}
        private void BuildBlocksAreaSuccess(ref MyCubeGrid.MyBlockBuildArea area, int entityIdSeed, HashSet<Vector3UByte> failList, long ownerId)
        {
            try
            {
                GetAllBuildOffsetsExcept(ref area, failList, m_tmpBuildOffsets);
                BuildBlocksArea(ref area, m_tmpBuildOffsets, ownerId, entityIdSeed);
            }
            finally
            {
                m_tmpBuildOffsets.Clear();
            }
        }

        private void BuildBlocksArea(ref MyCubeGrid.MyBlockBuildArea area, List<Vector3UByte> validOffsets, long ownerId, int entityIdSeed)
        {
            var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(area.DefinitionId) as MyCubeBlockDefinition;
            if (definition == null)
            {
                Debug.Fail("Block definition not found");
                return;
            }
            ProfilerShort.Begin("BuildBlocksArea");

            Quaternion orientation = Base6Directions.GetOrientation(area.OrientationForward, area.OrientationUp);
            Vector3I stepDir = area.StepDelta;

            try
            {
                bool successSound = false;
                // This must be here to maintain determinism, on server this goes through hashset which changes order
                validOffsets.Sort(Vector3UByte.Comparer);
                using (MyRandom.Instance.PushSeed(entityIdSeed))
                {
                    foreach (var offset in validOffsets)
                    {
                        Vector3I center = area.PosInGrid + offset * stepDir;
                        var block = BuildBlock(definition, ColorExtensions.UnpackHSVFromUint(area.ColorMaskHSV), center + area.BlockMin, orientation, ownerId, MyEntityIdentifier.AllocateId(), null, null, false, false);
                        if (block != null)
                        {
                            successSound = true;
                            m_tmpBuildSuccessBlocks.Add(block);
                        }
                    }
                }

                var bb = BoundingBox.CreateInvalid();
                foreach (var b in m_tmpBuildSuccessBlocks)
                {
                    if (b.FatBlock == null)
                        continue;
                    ProfilerShort.Begin("OnBuildSuccess");
                    b.FatBlock.OnBuildSuccess(ownerId);
                    ProfilerShort.End();
                    bb.Include(b.FatBlock.PositionComp.LocalAABB);
                }
                if (m_tmpBuildSuccessBlocks.Count > 0)
                {
                    var worldBB = bb.Transform(PositionComp.WorldMatrix);
                    var entities = MyEntities.GetEntitiesInAABB(ref worldBB);
                    foreach (var b in m_tmpBuildSuccessBlocks)
                        DetectMerge(b, null, entities);
                    entities.Clear();
                    m_tmpBuildSuccessBlocks[0].PlayConstructionSound(MyIntegrityChangeEnum.ConstructionBegin);
                    UpdateGridAABB();
                }
                if (MySession.LocalPlayerId == ownerId)
                    if (successSound)
                        MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);
                    else
                        MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
            }
            finally
            {
                m_tmpBuildSuccessBlocks.Clear();
                ProfilerShort.End();
            }
        }

        private void GetAllBuildOffsetsExcept(ref MyCubeGrid.MyBlockBuildArea area, HashSet<Vector3UByte> exceptList, List<Vector3UByte> resultOffsets)
        {
            Vector3UByte offset;
            for (offset.X = 0; offset.X < area.BuildAreaSize.X; offset.X++)
            {
                for (offset.Y = 0; offset.Y < area.BuildAreaSize.Y; offset.Y++)
                {
                    for (offset.Z = 0; offset.Z < area.BuildAreaSize.Z; offset.Z++)
                    {
                        if (!exceptList.Contains(offset))
                        {
                            resultOffsets.Add(offset);
                        }
                    }
                }
            }
        }

        private void GetValidBuildOffsets(ref MyCubeGrid.MyBlockBuildArea area, List<Vector3UByte> resultOffsets, HashSet<Vector3UByte> resultFailList)
        {
            Vector3I stepDir = area.StepDelta;
            MyBlockOrientation ori = new MyBlockOrientation(area.OrientationForward, area.OrientationUp);
            var blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(area.DefinitionId);

            Vector3UByte offset;
            for (offset.X = 0; offset.X < area.BuildAreaSize.X; offset.X++)
            {
                for (offset.Y = 0; offset.Y < area.BuildAreaSize.Y; offset.Y++)
                {
                    for (offset.Z = 0; offset.Z < area.BuildAreaSize.Z; offset.Z++)
                    {
                        Vector3I pos = area.PosInGrid + offset * stepDir;

                        if (CanPlaceBlock(pos + area.BlockMin, pos + area.BlockMax, ori, blockDefinition))
                        {
                            resultOffsets.Add(offset);
                        }
                        else
                        {
                            resultFailList.Add(offset);
                        }
                    }
                }
            }
        }

        private void BuildBlocksSuccess(Vector3 colorMaskHsv, HashSet<MyBlockLocation> locations, HashSet<MyBlockLocation> resultBlocks, MyEntity builder)
        {
            bool cubeProcessed = true;
            Debug.Assert(MySession.Static.CreativeMode || locations.Count <= 1, "Trying to build multiple blocks in survival.");

            while (locations.Count > 0 && cubeProcessed)
            {
                cubeProcessed = false;

                foreach (MyBlockLocation location in locations)
                {
                    var orientation = location.Orientation;
                    var center = location.CenterPos;

                    MyCubeBlockDefinition blockDefinition;
                    MyDefinitionManager.Static.TryGetCubeBlockDefinition(location.BlockDefinition, out blockDefinition);

                    if (blockDefinition == null)
                    {
                        Debug.Fail("Invalid block definition");
                        return;
                    }

                    var ori = new MyBlockOrientation(ref orientation);
                    // If we are on the server, we perform various checks. Clients on the other hand just build the blocks
                    if ( !Sync.IsServer ||
                            (
                                CanPlaceBlock(location.Min, location.Max, ori, blockDefinition) &&
                                MyCubeGrid.CheckConnectivity(this, blockDefinition, ref orientation, ref center)
                            )
                        )
                    {
                        var block = BuildBlock(blockDefinition, colorMaskHsv, location.Min, orientation, location.Owner, location.EntityId, builder);

                        if (block != null)
                        {
                            var resultLocation = location;
                            resultBlocks.Add(resultLocation);
                            block.PlayConstructionSound(MyIntegrityChangeEnum.ConstructionBegin);
                        }
                        cubeProcessed = true;
                        locations.Remove(location);
                        break;
                    }
                }
            }
        }

        private void BuildBlockSuccess(Vector3 colorMaskHsv, MyBlockLocation location, MyObjectBuilder_CubeBlock objectBuilder, ref MyBlockLocation? resultBlock, MyEntity builder)
        {
            var orientation = location.Orientation;
            var center = location.CenterPos;

            MyCubeBlockDefinition blockDefinition;
            MyDefinitionManager.Static.TryGetCubeBlockDefinition(location.BlockDefinition, out blockDefinition);

            if (blockDefinition == null)
            {
                Debug.Fail("Invalid block definition");
                return;
            }

            MyBlockOrientation ori = new MyBlockOrientation(ref location.Orientation);
            if (CanPlaceBlock(location.Min, location.Max, ori, blockDefinition) && MyCubeGrid.CheckConnectivity(this, blockDefinition, ref orientation, ref center))
            {
                var block = BuildBlock(blockDefinition, colorMaskHsv, location.Min, orientation, location.Owner, location.EntityId, builder, objectBuilder);

                if (block != null)
                {
                    resultBlock = location;
                    block.PlayConstructionSound(MyIntegrityChangeEnum.ConstructionBegin);
                }
                else
                {
                    resultBlock = null;
                }
            }
        }

        private void AfterBuildBlocksSuccess(HashSet<MyCubeGrid.MyBlockLocation> builtBlocks)
        {
            foreach (var location in builtBlocks)
            {
                AfterBuildBlockSuccess(location);
            }
        }

        private void AfterBuildBlockSuccess(MyBlockLocation builtBlock)
        {
            var block = GetCubeBlock(builtBlock.CenterPos);
            if (block != null && block.FatBlock != null)
            {
                block.FatBlock.OnBuildSuccess(builtBlock.Owner);
            }
        }

        public void RazeBlocks(ref Vector3I pos, ref Vector3UByte size)
        {
            SyncObject.RazeBlocksArea(ref pos, ref size);
        }

        void RazeBlocksAreaRequest(ref Vector3I pos, ref Vector3UByte size)
        {
            try
            {
                Vector3UByte offset;
                for (offset.X = 0; offset.X <= size.X; offset.X++)
                    for (offset.Y = 0; offset.Y <= size.Y; offset.Y++)
                        for (offset.Z = 0; offset.Z <= size.Z; offset.Z++)
                        {
                            var loc = pos + (Vector3I)offset;
                            MySlimBlock slimBlock = GetCubeBlock(loc);
                            if (slimBlock == null || (slimBlock.FatBlock != null && slimBlock.FatBlock.IsSubBlock))
                                m_tmpBuildFailList.Add(offset);
                        }

                SyncObject.RazeBlocksAreaSuccess(ref pos, ref size, m_tmpBuildFailList);
                RazeBlocksAreaSuccess(ref pos, ref size, m_tmpBuildFailList);

            }
            finally
            {
                m_tmpBuildFailList.Clear();
            }
        }

        void RazeBlocksAreaSuccess(ref Vector3I pos, ref Vector3UByte size, HashSet<Vector3UByte> resultFailList)
        {
            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;

            Vector3UByte offset;

            for (offset.X = 0; offset.X <= size.X; offset.X++)
                for (offset.Y = 0; offset.Y <= size.Y; offset.Y++)
                    for (offset.Z = 0; offset.Z <= size.Z; offset.Z++)
                    {
                        if (!resultFailList.Contains(offset))
                        {
                            var loc = pos + (Vector3I)offset;

                            var block = GetCubeBlock(loc);
                            if (block != null)
                            {
                                min = Vector3I.Min(min, block.Min);
                                max = Vector3I.Max(max, block.Max);

                                RemoveBlockInternal(block, close: true);
                                if (block.FatBlock != null)
                                    block.FatBlock.OnRemovedByCubeBuilder();
                            }
                        }
                    }

            Physics.AddDirtyArea(min, max);
        }

        public void RazeBlock(Vector3I position)
        {
            SyncObject.RazeBlock(position);
        }

        /// <summary>
        /// Razes blocks (unbuild)
        /// </summary>
        public void RazeBlocks(List<Vector3I> locations)
        {
            SyncObject.RazeBlocks(locations);
        }

        private void RazeBlocksSuccess(List<Vector3I> locations, List<Vector3I> removedBlocks)
        {
            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;

            foreach (var loc in locations)
            {
                var block = GetCubeBlock(loc);
                if (block != null)
                {
                    removedBlocks.Add(loc);

                    min = Vector3I.Min(min, block.Min);
                    max = Vector3I.Max(max, block.Max);

                    RemoveBlockInternal(block, close: true);
                    if (block.FatBlock != null)
                        block.FatBlock.OnRemovedByCubeBuilder();
                }
            }

            Physics.AddDirtyArea(min, max);
        }

        public void RazeGeneratedBlocks(List<Vector3I> locations)
        {
            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;

            foreach (var loc in locations)
            {
                var block = GetCubeBlock(loc);
                if (block != null)
                {
                    min = Vector3I.Min(min, block.Min);
                    max = Vector3I.Max(max, block.Max);

                    RemoveBlockInternal(block, close: true);
                    if (block.FatBlock != null)
                        block.FatBlock.OnRemovedByCubeBuilder();
                }
            }

            Physics.AddDirtyArea(min, max);
        }

        public void RazeBlockInCompoundBlock(List<Tuple<Vector3I, ushort>> locationsAndIds)
        {
            SyncObject.RazeBlockInCompoundBlock(locationsAndIds);
        }

        private void RazeBlockInCompoundBlockSuccess(List<Tuple<Vector3I, ushort>> locationsAndIds, List<Tuple<Vector3I, ushort>> removedBlocks)
        {
            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;

            foreach (var tuple in locationsAndIds)
            {
                var block = GetCubeBlock(tuple.Item1);
                if (block != null && block.FatBlock is MyCompoundCubeBlock)
                {
                    MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;

                    // Remove block in compound block
                    MySlimBlock blockToRemove = compoundBlock.GetBlock(tuple.Item2);
                    if (blockToRemove != null)
                    {
                        if (compoundBlock.Remove(blockToRemove))
                        {
                            removedBlocks.Add(tuple);

                            min = Vector3I.Min(min, block.Min);
                            max = Vector3I.Max(max, block.Max);

                            if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS && m_enableSmallToLargeConnections)
                            {
                                MyCubeGridSmallToLargeConnection.Static.RemoveBlockSmallToLargeConnection(blockToRemove);
                            }

                            if (OnBlockRemoved != null)
                                OnBlockRemoved(blockToRemove);
                        }
                    }

                    // Remove compound if empty
                    if (compoundBlock.GetBlocksCount() == 0)
                    {
                        RemoveBlockInternal(block, close: true);
                        if (block.FatBlock != null)
                            block.FatBlock.OnRemovedByCubeBuilder();
                    }
                }
            }

            m_dirtyRegion.AddCubeRegion(min, max);
            Physics.AddDirtyArea(min, max);
        }

        public void RazeGeneratedBlocksInCompoundBlock(List<Tuple<Vector3I, ushort>> locationsAndIds)
        {
            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;

            foreach (var tuple in locationsAndIds)
            {
                var block = GetCubeBlock(tuple.Item1);
                if (block != null && block.FatBlock is MyCompoundCubeBlock)
                {
                    MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;

                    // Remove block in compound block
                    MySlimBlock blockToRemove = compoundBlock.GetBlock(tuple.Item2);
                    if (blockToRemove != null)
                    {
                        if (compoundBlock.Remove(blockToRemove))
                        {
                            min = Vector3I.Min(min, block.Min);
                            max = Vector3I.Max(max, block.Max);

                            if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS && m_enableSmallToLargeConnections)
                            {
                                MyCubeGridSmallToLargeConnection.Static.RemoveBlockSmallToLargeConnection(blockToRemove);
                            }

                            if (OnBlockRemoved != null)
                                OnBlockRemoved(blockToRemove);
                        }
                    }

                    // Remove compound if empty
                    if (compoundBlock.GetBlocksCount() == 0)
                    {
                        RemoveBlockInternal(block, close: true);
                        if (block.FatBlock != null)
                            block.FatBlock.OnRemovedByCubeBuilder();
                    }
                }
            }

            m_dirtyRegion.AddCubeRegion(min, max);
            Physics.AddDirtyArea(min, max);
        }

        public void ColorBlocks(Vector3I min, Vector3I max, Vector3 newHSV, bool playSound)
        {
            ProfilerShort.Begin("MyCubeGrid.ColorBlocks");
            SyncObject.ColorBlocks(min, max, newHSV, playSound);
            ProfilerShort.End();
        }

        private void ColorBlockSuccess(Vector3I min, Vector3I max, Vector3 newHSV, bool playSound)
        {
            ProfilerShort.Begin("MyCubeGrid.ColorBlockSuccess");
            Vector3I temp;
            bool sound = false;
            for (temp.X = min.X; temp.X <= max.X; temp.X++)
                for (temp.Y = min.Y; temp.Y <= max.Y; temp.Y++)
                    for (temp.Z = min.Z; temp.Z <= max.Z; temp.Z++)
                    {
                        var block = GetCubeBlock(temp);
                        if (block != null)
                        {
                            sound |= ChangeColor(block, newHSV);
                        }
                    }

            if (sound && Vector3D.Distance(MySector.MainCamera.Position, Vector3D.Transform(min * GridSize, WorldMatrix)) < 200)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudColorBlock);
            }

            ProfilerShort.End();
        }

        /// <summary>
        /// Builds block without checking connectivity
        /// </summary>
        private MySlimBlock BuildBlock(MyCubeBlockDefinition blockDefinition, Vector3 colorMaskHsv, Vector3I min, Quaternion orientation, long owner, long entityId, MyEntity builderEntity, MyObjectBuilder_CubeBlock blockObjectBuilder = null, bool updateVolume = true, bool testMerge = true)
        {
            ProfilerShort.Begin("BuildBlock");

            MyBlockOrientation blockOrientation = new MyBlockOrientation(ref orientation);
            if (blockObjectBuilder == null)
            {
                blockObjectBuilder = MyCubeGrid.CreateBlockObjectBuilder(blockDefinition, min, blockOrientation, entityId, owner, fullyBuilt: builderEntity == null || !MySession.Static.SurvivalMode);
                blockObjectBuilder.ColorMaskHSV = colorMaskHsv;
            }
            else
            {
                blockObjectBuilder.Min = min;
                blockObjectBuilder.Orientation = orientation;
            }
            MyCubeBuilder.BuildComponent.BeforeCreateBlock(blockDefinition, builderEntity, blockObjectBuilder);

            MySlimBlock block = null;

            if (Sync.IsServer)
            {
                Vector3I position = MySlimBlock.ComputePositionInGrid(new MatrixI(blockOrientation), blockDefinition, min);
                MyCubeBuilder.BuildComponent.GetBlockPlacementMaterials(blockDefinition, position, blockObjectBuilder.BlockOrientation, this);
            }

            if (MyFakes.ENABLE_COMPOUND_BLOCKS && blockDefinition.CompoundTemplates != null && blockDefinition.CompoundTemplates.Length > 0)
            {
                MySlimBlock existingSlimBlock = GetCubeBlock(min);
                MyCompoundCubeBlock compoundBlock = existingSlimBlock != null ? existingSlimBlock.FatBlock as MyCompoundCubeBlock : null;

                if (compoundBlock != null)
                {
                    if (compoundBlock.CanAddBlock(blockDefinition, new MyBlockOrientation(ref orientation)))
                    {
                        // Existing compound block in position
                        var objectBlock = MyCubeBlockFactory.CreateCubeBlock(blockObjectBuilder);
                        block = objectBlock as MySlimBlock;
                        if (block == null)
                            block = new MySlimBlock();

                        block.Init(blockObjectBuilder, this, objectBlock as MyCubeBlock);

                        ushort id;
                        if (compoundBlock.Add(block, out id))
                        {
                            BoundsInclude(block);

                            m_dirtyRegion.AddCube(min);

                            Physics.AddDirtyBlock(existingSlimBlock);

                            if (OnBlockAdded != null)
                                OnBlockAdded(block);
                        }
                    }
                }
                else
                {
                    // Create new compound block in position
                    MyObjectBuilder_CompoundCubeBlock compoundCBBuilder = MyCompoundCubeBlock.CreateBuilder(blockObjectBuilder);
                    block = AddBlock(compoundCBBuilder, testMerge);
                }
            }
            else
            {
                block = AddBlock(blockObjectBuilder, testMerge);
            }
            if (block != null)
            {
                // We have to use block.CubeGrid instead of this because adding the block could have caused a grid merge
                block.CubeGrid.BoundsInclude(block);
                if(updateVolume)
                    block.CubeGrid.UpdateGridAABB();

                if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS && m_enableSmallToLargeConnections)
                    MyCubeGridSmallToLargeConnection.Static.AddBlockSmallToLargeConnection(block);

                if (Sync.IsServer)
                {
                    MyCubeBuilder.BuildComponent.AfterBlockBuild(block, builderEntity);
                }
            }

            ProfilerShort.End();
            return block;
        }

        internal bool AddExplosion(Vector3 pos, MyExplosionTypeEnum type, float radius)
        {
            // We don't want explosion on same cube too soon
            if (!m_explosions.AddInstance(TimeSpan.FromMilliseconds(700), pos, GridSize * 2))
                return false;

            var explosionSphere = new BoundingSphere(pos, radius);
            MyExplosionInfo explosionInfo = new MyExplosionInfo()
            {
                PlayerDamage = 0,
                Damage = 1,
                ExplosionSphere = explosionSphere,
                ExcludedEntity = null,
                ExplosionFlags = MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT,
                ExplosionType = type,
                LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN,
                ParticleScale = Math.Max(GridSizeEnum == MyCubeSize.Large ? 1 : 0.4f, Math.Min(radius / 2, 6)),
                VoxelCutoutScale = 1.0f,
                PlaySound = false,
                CheckIntersections = false,
                VoxelExplosionCenter = explosionSphere.Center,
                Velocity = Physics.RigidBody.LinearVelocity,
            };
            MyExplosions.AddExplosion(ref explosionInfo);

            return true;
        }

        public void ResetBlockSkeleton(MySlimBlock block, bool updateSync = false)
        {
            MultiplyBlockSkeleton(block, 0.0f, updateSync);
        }

        public void MultiplyBlockSkeleton(MySlimBlock block, float factor, bool updateSync = false)
        {
            Debug.Assert(Skeleton != null, "Skeleton null in MultiplyBlockSkeleton!");
            Debug.Assert(Physics != null, "Physics null in MultiplyBlockSkeleton!");

            if (block == null)
                return;

            var min = block.Min * Skeleton.BoneDensity;
            var max = block.Max * Skeleton.BoneDensity + Skeleton.BoneDensity;

            bool bonesChanged = false;

            Vector3I pos;
            for (pos.Z = min.Z; pos.Z <= max.Z; pos.Z++)
            {
                for (pos.Y = min.Y; pos.Y <= max.Y; pos.Y++)
                {
                    for (pos.X = min.X; pos.X <= max.X; pos.X++)
                    {
                        bonesChanged |= Skeleton.MultiplyBone(ref pos, factor, ref block.Min, this);
                    }
                }
            }

            if (bonesChanged)
            {
                if (Sync.IsServer && updateSync)
                {
                    SyncObject.SendBonesMultiplied(block.Position, factor);
                }

                min = block.Min - Vector3I.One;
                max = block.Max + Vector3I.One;
                for (pos.Z = min.Z; pos.Z <= max.Z; pos.Z++)
                {
                    for (pos.Y = min.Y; pos.Y <= max.Y; pos.Y++)
                    {
                        for (pos.X = min.X; pos.X <= max.X; pos.X++)
                        {
                            m_dirtyRegion.AddCube(pos);
                        }
                    }
                }
                Physics.AddDirtyArea(min, max);
            }
        }

        public void AddDirtyBone(Vector3I gridPosition, Vector3I boneOffset)
        {
            Skeleton.Wrap(ref gridPosition, ref boneOffset);

            var cubeOffset = boneOffset - new Vector3I(1, 1, 1);
            Vector3I min = Vector3I.Min(cubeOffset, new Vector3I(0, 0, 0));
            Vector3I max = Vector3I.Max(cubeOffset, new Vector3I(0, 0, 0));

            Vector3I temp = min;
            for (Vector3I.RangeIterator it = new Vector3I.RangeIterator(ref min, ref max); it.IsValid(); it.GetNext(out temp))
                m_dirtyRegion.AddCube(gridPosition + temp);
        }

        public MySlimBlock GetCubeBlock(Vector3I pos)
        {
            MyCube result;
            if (m_cubes.TryGetValue(pos, out result))
            {
                Debug.Assert(result.CubeBlock.FatBlock == null || !result.CubeBlock.FatBlock.Closed);
                return result.CubeBlock;
            }

            return null;
        }

        public T GetFirstBlockOfType<T>()
            where T : MyCubeBlock
        {
            foreach (var cube in m_cubeBlocks)
            {
                if (cube.FatBlock != null && (cube.FatBlock is T))
                    return (cube.FatBlock as T);
            }

            return null;
        }

        /// <summary>
        /// Iterate over all the neighbors of the cube and return when one of them exists.
        /// </summary> 
        public void FixTargetCube(out Vector3I cube, Vector3 fractionalGridPosition)
        {
            cube = Vector3I.Round(fractionalGridPosition);
            fractionalGridPosition += new Vector3(0.5f);
            if (m_cubes.ContainsKey(cube)) return;

            // Calculate distances to all 26 neighbors of the cube
            Vector3 dDown = fractionalGridPosition - cube;
            Vector3 dUp = new Vector3(1.0f) - dDown;
            m_neighborDistances[(int)NeighborOffsetIndex.XDOWN] = dDown.X;
            m_neighborDistances[(int)NeighborOffsetIndex.XUP] = dUp.X;
            m_neighborDistances[(int)NeighborOffsetIndex.YDOWN] = dDown.Y;
            m_neighborDistances[(int)NeighborOffsetIndex.YUP] = dUp.Y;
            m_neighborDistances[(int)NeighborOffsetIndex.ZDOWN] = dDown.Z;
            m_neighborDistances[(int)NeighborOffsetIndex.ZUP] = dUp.Z;
            Vector3 dDown2 = dDown * dDown;
            Vector3 dUp2 = dUp * dUp;
            m_neighborDistances[(int)NeighborOffsetIndex.XDOWN_YDOWN] = (float)Math.Sqrt(dDown2.X + dDown2.Y);
            m_neighborDistances[(int)NeighborOffsetIndex.XDOWN_YUP] = (float)Math.Sqrt(dDown2.X + dUp2.Y);
            m_neighborDistances[(int)NeighborOffsetIndex.XUP_YDOWN] = (float)Math.Sqrt(dUp2.X + dDown2.Y);
            m_neighborDistances[(int)NeighborOffsetIndex.XUP_YUP] = (float)Math.Sqrt(dUp2.X + dUp2.Y);
            m_neighborDistances[(int)NeighborOffsetIndex.XDOWN_ZDOWN] = (float)Math.Sqrt(dDown2.X + dDown2.Z);
            m_neighborDistances[(int)NeighborOffsetIndex.XDOWN_ZUP] = (float)Math.Sqrt(dDown2.X + dUp2.Z);
            m_neighborDistances[(int)NeighborOffsetIndex.XUP_ZDOWN] = (float)Math.Sqrt(dUp2.X + dDown2.Z);
            m_neighborDistances[(int)NeighborOffsetIndex.XUP_ZUP] = (float)Math.Sqrt(dUp2.X + dUp2.Z);
            m_neighborDistances[(int)NeighborOffsetIndex.YDOWN_ZDOWN] = (float)Math.Sqrt(dDown2.Y + dDown2.Z);
            m_neighborDistances[(int)NeighborOffsetIndex.YDOWN_ZUP] = (float)Math.Sqrt(dDown2.Y + dUp2.Z);
            m_neighborDistances[(int)NeighborOffsetIndex.YUP_ZDOWN] = (float)Math.Sqrt(dUp2.Y + dDown2.Z);
            m_neighborDistances[(int)NeighborOffsetIndex.YUP_ZUP] = (float)Math.Sqrt(dUp2.Y + dUp2.Z);
            Vector3 dDown3 = dDown2 * dDown;
            Vector3 dUp3 = dUp2 * dUp;
            m_neighborDistances[(int)NeighborOffsetIndex.XDOWN_YDOWN_ZDOWN] = (float)Math.Pow(dDown3.X + dDown3.Y + dDown3.Z, 1.0 / 3.0);
            m_neighborDistances[(int)NeighborOffsetIndex.XDOWN_YDOWN_ZUP] = (float)Math.Pow(dDown3.X + dDown3.Y + dUp3.Z, 1.0 / 3.0);
            m_neighborDistances[(int)NeighborOffsetIndex.XDOWN_YUP_ZDOWN] = (float)Math.Pow(dDown3.X + dUp3.Y + dDown3.Z, 1.0 / 3.0);
            m_neighborDistances[(int)NeighborOffsetIndex.XDOWN_YUP_ZUP] = (float)Math.Pow(dDown3.X + dUp3.Y + dUp3.Z, 1.0 / 3.0);
            m_neighborDistances[(int)NeighborOffsetIndex.XUP_YDOWN_ZDOWN] = (float)Math.Pow(dUp3.X + dDown3.Y + dDown3.Z, 1.0 / 3.0);
            m_neighborDistances[(int)NeighborOffsetIndex.XUP_YDOWN_ZUP] = (float)Math.Pow(dUp3.X + dDown3.Y + dUp3.Z, 1.0 / 3.0);
            m_neighborDistances[(int)NeighborOffsetIndex.XUP_YUP_ZDOWN] = (float)Math.Pow(dUp3.X + dUp3.Y + dDown3.Z, 1.0 / 3.0);
            m_neighborDistances[(int)NeighborOffsetIndex.XUP_YUP_ZUP] = (float)Math.Pow(dUp3.X + dUp3.Y + dUp3.Z, 1.0 / 3.0);

            // Bubble sort the face neighbors by distance
            for (int i = 0; i < 25; ++i)
            {
                for (int j = 0; j < 25 - i; ++j)
                {
                    float distFirst = m_neighborDistances[(int)m_neighborOffsetIndices[j]];
                    float distSecond = m_neighborDistances[(int)m_neighborOffsetIndices[j + 1]];
                    if (distFirst > distSecond)
                    {
                        NeighborOffsetIndex swap = m_neighborOffsetIndices[j];
                        m_neighborOffsetIndices[j] = m_neighborOffsetIndices[j + 1];
                        m_neighborOffsetIndices[j + 1] = swap;
                    }
                }
            }

            // Find the first existing neighbor by distance
            Vector3I offset = new Vector3I();
            for (int i = 0; i < m_neighborOffsets.Count; ++i)
            {
                offset = m_neighborOffsets[(int)m_neighborOffsetIndices[i]];
                if (m_cubes.ContainsKey(cube + offset))
                {
                    cube = cube + offset;
                    //break;
                    return;
                }
            }
        }

        public HashSet<MySlimBlock> GetBlocks()
        {
            return m_cubeBlocks;
        }

        public MyFatBlockReader<T> GetFatBlocks<T>()
            where T : MyCubeBlock
        {
            return new MyFatBlockReader<T>(this);
        }

        /// <summary>
        /// Returns true when grid have at least one block which has physics (lights has no physics)
        /// </summary>
        public bool CanHavePhysics()
        {
            foreach (var block in m_cubeBlocks)
            {
                if (block.HasPhysics)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true when grid have at least one block which has physics (lights has no physics)
        /// </summary>
        public static bool CanHavePhysics(List<MySlimBlock> blocks, int offset, int count)
        {
            for (int i = offset; i < offset + count; i++)
            {
                var block = blocks[i];
                if (block != null && block.HasPhysics)
                    return true;
            }
            return false;
        }

        private void RebuildGrid()
        {
            // No physical cubes, close grid
            if (!CanHavePhysics())
            {
                Close();
                return;
            }

            ProfilerShort.Begin("Rebuild grid");

            ProfilerShort.Begin("Recalc bounds");
            RecalcBounds();
            ProfilerShort.End();

            ProfilerShort.Begin("Remove redundant parts");
            RemoveRedundantParts();
            ProfilerShort.End();

            ProfilerShort.Begin("Close grid physics");
            if (this.Physics != null)
            {
                this.Physics.Close();
                this.Physics = null;
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Create grid physics");
            if (CreatePhysics)
            {
                Physics = new MyGridPhysics(this);
                RaisePhysicsChanged();

                if (!Sync.IsServer && MyPerGameSettings.GridRBFlagOnClients == RigidBodyFlag.RBF_KINEMATIC)
                    Physics.RigidBody.UpdateMotionType(HkMotionType.Keyframed);
            }
            ProfilerShort.End();

            ProfilerShort.End();
        }

        public void ConvertToDynamic()
        {
            Debug.Assert(IsStatic);
            if (!IsStatic || Physics == null) return;

            if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS && m_enableSmallToLargeConnections)
            {
                MyCubeGridSmallToLargeConnection.Static.GridConvertedToDynamic(this);
            }

            MyCubeGrid.StaticGrids.Remove(this);
            Physics.Close();
            Physics = null;

            Physics = new MyGridPhysics(this);
            foreach (var block in GetBlocks())
            {
                Physics.AddBlock(block);
                if (block.FatBlock != null)
                {
                    if (block.FatBlock is MyLandingGear)
                    {
                        (block.FatBlock as MyLandingGear).EnqueueRetryLock();
                    }
                    else if (block.FatBlock is MyMotorStator)
                    {
                        (block.FatBlock as MyMotorStator).Detach();
                    }
                    else if (block.FatBlock is MyMotorRotor)
                    {
                        var rotor = (block.FatBlock as MyMotorRotor);
                        if (rotor.Stator != null)
                            rotor.Stator.Detach();
                    }
                    else if (block.FatBlock is MyShipConnector  && (block.FatBlock as MyShipConnector).InConstraint)
                    {
                        (block.FatBlock as MyShipConnector).Detach();
                    }
                }
            }

            RaisePhysicsChanged();
        }

        public void DoDamage(float damage, MyHitInfo hitInfo, Vector3? localPos = null)
        {
            Debug.Assert(Sync.IsServer);
            Vector3I cubePos;
            if (localPos.HasValue)
                FixTargetCube(out cubePos, localPos.Value / GridSize);
            else
                FixTargetCube(out cubePos, Vector3D.Transform(hitInfo.Position, PositionComp.WorldMatrixInvScaled) / GridSize);

            var cube = GetCubeBlock(cubePos);
            //Debug.Assert(cube != null, "Cannot find block for damage!");
            if (cube != null)
            {
                ApplyDestructionDeformation(cube, damage, hitInfo);
            }
        }

        public void ApplyDestructionDeformation(MySlimBlock block, float damage = 1f, MyHitInfo? hitInfo = null)
        {
            if (MyPerGameSettings.Destruction)
            {
                Debug.Assert(hitInfo.HasValue, "Destruction needs additional info");
                (block as IMyDestroyableObject).DoDamage(damage, MyDamageType.Unknown, true, hitInfo);
            }
            else
            {
                Debug.Assert(Sandbox.Game.Multiplayer.Sync.IsServer, "ApplyDestructionDeformation is supposed to be only server method");
                SyncObject.EnqueueDestructionDeformationBlock(block.Position);
                ApplyDestructionDeformationInternal(block, true, damage);
            }
        }

        private float ApplyDestructionDeformationInternal(MySlimBlock block, bool sync, float damage = 1f)
        {
            if (!BlocksDestructionEnabled)
                return 0;

            m_totalBoneDisplacement = 0.0f;

            // TODO: Optimization. Cache bone changes (moves) and apply them only at the end
            ProfilerShort.Begin("Update corner bones");
            Vector3I minCube = Vector3I.MaxValue;
            Vector3I maxCube = Vector3I.MinValue;
            bool changed = false;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    changed = changed | MoveCornerBones(block.Min, new Vector3I(x, 0, z), ref minCube, ref maxCube);
                    changed = changed | MoveCornerBones(block.Min, new Vector3I(x, z, 0), ref minCube, ref maxCube);
                    changed = changed | MoveCornerBones(block.Min, new Vector3I(0, x, z), ref minCube, ref maxCube);
                }
            }
            if (changed)
            {
                m_dirtyRegion.AddCubeRegion(minCube, maxCube);
            }
            ProfilerShort.End();

            m_deformationRng.SetSeed(block.Position.GetHashCode());

            float angleDeviation = MathHelper.PiOver4 / 2;

            ProfilerShort.Begin("Update thin bones");
            float maxLinearDeviation = GridSize / 2.0f;
            var cubePos = block.Min;
            Vector3I min, max;
            for (int i = 0; i < 3; i++) // Three axes
            {
                min = Vector3I.MaxValue;
                max = Vector3I.MinValue;

                changed = false;
                changed |= ApplyTable(cubePos, MyCubeGridDeformationTables.ThinUpper[i], ref min, ref max, m_deformationRng, maxLinearDeviation, angleDeviation);
                changed |= ApplyTable(cubePos, MyCubeGridDeformationTables.ThinLower[i], ref min, ref max, m_deformationRng, maxLinearDeviation, angleDeviation);

                if (changed)
                {
                    // One more bone to make sure to hit incident cubes
                    min -= Vector3I.One;
                    max += Vector3I.One;
                    minCube = cubePos;
                    maxCube = cubePos;
                    Skeleton.Wrap(ref minCube, ref min);
                    Skeleton.Wrap(ref maxCube, ref max);
                    m_dirtyRegion.AddCubeRegion(minCube, maxCube);
                }
            }
            ProfilerShort.End();

            if (sync)
            {
                (block as IMyDestroyableObject).DoDamage(m_totalBoneDisplacement * GridSize * 10.0f * damage, MyDamageType.Deformation, true);
            }
            return m_totalBoneDisplacement;
        }

        /// <summary>
        /// Removes destroyed block, applies damage and deformation to close blocks
        /// Won't update physics!
        /// </summary>
        public void RemoveDestroyedBlock(MySlimBlock block)
        {
            if (!Sync.IsServer)
                return;

            SyncObject.EnqueueDestroyedBlock(block.Position);
            RemoveDestroyedBlockInternal(block);

            if (!CanHavePhysics())
            {
                Close();
            }
        }

        private void RemoveDestroyedBlockInternal(MySlimBlock block)
        {
            ApplyDestructionDeformationInternal(block, false);
            (block as IMyDestroyableObject).OnDestroy();
            RemoveBlockInternal(block, close: true);
        }

        private bool ApplyTable(Vector3I cubePos, MyCubeGridDeformationTables.DeformationTable table, ref Vector3I dirtyMin, ref Vector3I dirtyMax, MyRandom random, float maxLinearDeviation, float angleDeviation)
        {
            if (!m_cubes.ContainsKey(cubePos + table.Normal))
            {
                Vector3I boneOffset;
                Vector3 clamp;

                m_tmpBoneSet.Clear();
                GetExistingBones(cubePos * Skeleton.BoneDensity + table.MinOffset, cubePos * Skeleton.BoneDensity + table.MaxOffset, m_tmpBoneSet);
                foreach (var offset in table.OffsetTable)
                {
                    if (m_tmpBoneSet.ContainsKey(cubePos * Skeleton.BoneDensity + offset.Key))
                    {
                        boneOffset = offset.Key;
                        clamp = new Vector3(GridSize / 2 + random.NextFloat(-GridSize / 10, GridSize / 10));
                        Vector3 moveDirection = random.NextDeviatingVector(offset.Value, angleDeviation) * random.NextFloat(1, maxLinearDeviation);
                        MoveBone(ref cubePos, ref boneOffset, ref moveDirection, ref clamp);
                    }
                }
                dirtyMin = Vector3I.Min(dirtyMin, table.MinOffset);
                dirtyMax = Vector3I.Max(dirtyMax, table.MaxOffset);
                return true;
            }
            return false;
        }


        private void BlocksRemovedWithGenerator(List<Vector3I> blocksToRemove)
        {
            EnableGenerators(true, true);

            BlocksRemoved(blocksToRemove);
        }

        private void BlocksRemovedWithoutGenerator(List<Vector3I> blocksToRemove)
        {
            EnableGenerators(false, true);

            BlocksRemoved(blocksToRemove);
        }


        /// <summary>
        /// Client only method, not called on server
        /// </summary>
        private void BlocksRemoved(List<Vector3I> blocksToRemove)
        {
            foreach (var pos in blocksToRemove)
            {
                var block = GetCubeBlock(pos);
                if (block != null)
                {
                    RemoveBlockInternal(block, close: true);
                    Physics.AddDirtyBlock(block);
                }
            }

            if (!CanHavePhysics())
            {
                Close();
                return;
            }
        }

        private void BlocksDestroyed(List<Vector3I> blockToDestroy)
        {
            foreach (var pos in blockToDestroy)
            {
                var block = GetCubeBlock(pos);
                if (block != null)
                {
                    RemoveDestroyedBlockInternal(block);
                    Physics.AddDirtyBlock(block);
                }
            }

            if (!CanHavePhysics())
            {
                Close();
                return;
            }
        }

        private void BlocksDeformed(List<Vector3I> blockToDestroy)
        {
            foreach (var pos in blockToDestroy)
            {
                var block = GetCubeBlock(pos);
                if (block != null)
                {
                    ApplyDestructionDeformationInternal(block, false);
                    Physics.AddDirtyBlock(block);
                }
            }
        }

		private void BlockIntegrityChanged(Vector3I pos, ushort subBlockId, float buildIntegrity, float integrity, MyIntegrityChangeEnum integrityChangeType, long grinderOwner)
        {
			MyCompoundCubeBlock compoundBlock = null;
            var block = GetCubeBlock(pos);
			if (block != null)
				compoundBlock = block.FatBlock as MyCompoundCubeBlock;
			if (compoundBlock != null)
				block = compoundBlock.GetBlock(subBlockId);
            //Debug.Assert(block != null, "Attempting to change integrity of a non-existent block!");

            if (block != null)
            {
                block.SetIntegrity(buildIntegrity, integrity, integrityChangeType, grinderOwner);
            }
        }

        private void BlockStockpileChanged(Vector3I pos, ushort subBlockId, List<MyStockpileItem> items)
        {
            var block = GetCubeBlock(pos);
          
			MyCompoundCubeBlock compoundBlock = null;
			if (block != null)
				compoundBlock = block.FatBlock as MyCompoundCubeBlock;
			if (compoundBlock != null)
				block = compoundBlock.GetBlock(subBlockId);

			Debug.Assert(block != null, "Attempting to change stockpile of a non-existent block!");

            if (block != null)
            {
                block.ChangeStockpile(items);
            }
        }

        /// <summary>
        /// Removes block, should be used only by server or on server request
        /// </summary>
        private void RemoveBlockInternal(MySlimBlock block, bool close, bool markDirtyDisconnects = true)
        {
            if (!m_cubeBlocks.Contains(block))
            {
                Debug.Fail("Block being removed twice");
                return;
            }

            RenderData.RemoveDecals(block.Position);

            ProfilerShort.Begin("Remove terminal block");
            var terminalBlock = block.FatBlock as MyTerminalBlock;
            if (terminalBlock != null)
            {
                for (int i = 0; i < BlockGroups.Count; i++)
                {
                    var group = BlockGroups[i];
                    if (group.Blocks.Contains(terminalBlock))
                        group.Blocks.Remove(terminalBlock);
                    if (group.Blocks.Count == 0)
                    {
                        RemoveGroup(group);
                        i--;
                    }
                }
            }
            ProfilerShort.End();

            bool removed;
            ProfilerShort.Begin("Remove cubes");
            Vector3I temp = block.Min;
            for (Vector3I.RangeIterator it = new Vector3I.RangeIterator(ref block.Min, ref block.Max); it.IsValid(); it.GetNext(out temp))
            {
                removed = RemoveCube(temp);
                Debug.Assert(removed, "Cube to remove was not found");
            }
            ProfilerShort.End();

            ProfilerShort.Begin("RemoveBlockEdges");
            RemoveBlockEdges(block);
            ProfilerShort.End();

            if (block.FatBlock != null)
            {
                if (BlocksCounters.ContainsKey(block.BlockDefinition.Id.TypeId))
                    BlocksCounters[block.BlockDefinition.Id.TypeId]--;
                ProfilerShort.Begin("Unregister");
                GridSystems.UnregisterFromSystems(block.FatBlock);
                ProfilerShort.End();

                if (close)
                {
                    ProfilerShort.Begin("Mark for close");
                    block.FatBlock.Close();
                    ProfilerShort.End();
                }
                else
                {
                    ProfilerShort.Begin("Remove child");
                    Hierarchy.RemoveChild(block.FatBlock);
                    ProfilerShort.End();
                }

                ProfilerShort.Begin("UnregisterForDraw");
                if (block.FatBlock.Render.NeedsDrawFromParent)
                    m_blocksForDraw.Remove(block.FatBlock);
                ProfilerShort.End();
            }

            if (MyStructuralIntegrity.Enabled && StructuralIntegrity != null)
            {
                StructuralIntegrity.RemoveBlock(block);
            }

            ProfilerShort.Begin("Remove Neighbours");
            block.RemoveNeighbours();
            ProfilerShort.End();

            ProfilerShort.Begin("Remove");
            m_cubeBlocks.Remove(block);
            ProfilerShort.End();

            if (markDirtyDisconnects)
                m_disconnectsDirty = true;

            Vector3I cube = block.Min;
            for (Vector3I.RangeIterator it = new Vector3I.RangeIterator(ref block.Min, ref block.Max); it.IsValid(); it.GetNext(out cube))
                Skeleton.MarkCubeRemoved(ref cube);

            ProfilerShort.Begin("OnBlockRemoved");

            if (block.FatBlock != null && block.FatBlock.IDModule != null)
                ChangeOwner(block.FatBlock, block.FatBlock.IDModule.Owner, 0);

            if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS && m_enableSmallToLargeConnections)
            {
                ProfilerShort.Begin("CheckRemovedBlockSmallToLargeConnection");
                MyCubeGridSmallToLargeConnection.Static.RemoveBlockSmallToLargeConnection(block);
                ProfilerShort.End();
            }

            if (OnBlockRemoved != null)
                OnBlockRemoved(block);
            ProfilerShort.End();
        }

        public void RemoveBlock(MySlimBlock block, bool updatePhysics = false)
        {
            // Client cannot remove blocks, only server
            if (!Sync.IsServer)
                return;

            if (!m_cubeBlocks.Contains(block))
            {
                Debug.Fail("Block being removed twice");
                return;
            }

            SyncObject.EnqueueRemovedBlock(block.Min, m_generatorsEnabled);
            RemoveBlockInternal(block, close: true);

            if (updatePhysics)
            {
                this.Physics.AddDirtyBlock(block);
            }

            if (!CanHavePhysics())
            {
                Close();
                return;
            }
        }

        public void UpdateBlockNeighbours(MySlimBlock block)
        {
            if (!m_cubeBlocks.Contains(block))
            {
                Debug.Assert(false, "Grid does not contain the given block!");
                return;
            }

            block.RemoveNeighbours();
            block.AddNeighbours();
            m_disconnectsDirty = true;
        }

        /// <summary>
        /// Returns cube corner which is closest to position
        /// </summary>
        public Vector3 GetClosestCorner(Vector3I gridPos, Vector3 position)
        {
            return gridPos * GridSize - Vector3.SignNonZero(gridPos * GridSize - position) * GridSize / 2;
        }

        public void DetectDisconnectsAfterFrame()
        {
            m_disconnectsDirty = true;
        }

        // Test method to detect disconnects
        // OPTIMIZE: Create tree from cubes, tree node will be cube or cube cycle (cubes circularly connected cubes)
        // Branch detachment = disconnect
        // When cycle is broken, it will be expanded to branch/subtree
        private void DetectDisconnects()
        {
            Debug.Assert(m_disconnectsDirty, "Do not call unless needed to (after block was removed).");
            if (!MyFakes.DETECT_DISCONNECTS)
                return;

            if (m_cubes.Count == 0)
                return;

            if (!Sync.IsServer)
                return;

            MyPerformanceCounter.PerCameraDrawRead.CustomTimers.Remove("Mount points");
            MyPerformanceCounter.PerCameraDrawWrite.CustomTimers.Remove("Mount points");

            MyPerformanceCounter.PerCameraDrawRead.CustomTimers.Remove("Disconnect");
            MyPerformanceCounter.PerCameraDrawWrite.CustomTimers.Remove("Disconnect");
            MyPerformanceCounter.PerCameraDrawRead.StartTimer("Disconnect");
            MyPerformanceCounter.PerCameraDrawWrite.StartTimer("Disconnect");

            ProfilerShort.Begin("Detect disconnects");
            m_disconnectHelper.Disconnect(this);
            m_disconnectsDirty = false;
            ProfilerShort.End();

            MyPerformanceCounter.PerCameraDrawRead.StopTimer("Disconnect");
            MyPerformanceCounter.PerCameraDrawWrite.StopTimer("Disconnect");
        }

        public bool CubeExists(Vector3I pos)
        {
            return m_cubes.ContainsKey(pos);
        }

        public void UpdateDirty()
        {
            ProfilerShort.Begin("Update dirty");

            ProfilerShort.Begin("Update parts");
            foreach (var pos in m_dirtyRegion.Cubes)
            {
                UpdateParts(pos);
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Update edges");
            foreach (var pos in m_dirtyRegion.Cubes)
            {
                var block = GetCubeBlock(pos);
                //Don't add edges for projections (dithering < 0)
                if (block != null && block.ShowParts && MyFakes.ENABLE_EDGES)
                {
                    if (block.Dithering >= 0f)
                    {
                        // Edges are filtered (to remove duplicates) in render data
                        AddBlockEdges(block);
                    }
                    else
                    {
                        RemoveBlockEdges(block);
                    }
                }
                if (block != null && block.FatBlock != null)
                {
                    if (block.FatBlock.Render.NeedsDrawFromParent)
                        m_blocksForDraw.Add(block.FatBlock);
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Update groups");
            UpdateInstanceData();
            ProfilerShort.End();

            m_dirtyRegion.Clear();

            ProfilerShort.End();
        }

        public bool IsDirty()
        {
            return m_dirtyRegion.IsDirty;
        }
        public void UpdateInstanceData()
        {
            ProfilerShort.Begin("Rebuild dirty cells");
            Render.RebuildDirtyCells();
            PositionComp.UpdateWorldVolume();
            ProfilerShort.End();
        }

        /// <summary>
        /// Add new cube in the grid
        /// </summary>
        /// <param name="block"></param>
        /// <param name="pos"></param>
        /// <param name="rotation"></param>
        /// <param name="cubeBlockDefinition"></param>
        /// <returns>false if add failed (can be caused be block structure change during the development</returns>
        private bool AddCube(MySlimBlock block, ref Vector3I pos, Matrix rotation, MyCubeBlockDefinition cubeBlockDefinition)
        {
            MyCube c = new MyCube();

            c.Parts = MyCubeGrid.GetCubeParts(cubeBlockDefinition, pos, rotation, GridSize);
            c.CubeBlock = block;

            if (!m_cubes.ContainsKey(pos))
                m_cubes.Add(pos, c);
            else
                return false; //whole block will be removed

            m_dirtyRegion.AddCube(pos);

            return true;
        }

        private MyCube CreateCube(MySlimBlock block, Vector3I pos, Matrix rotation, MyCubeBlockDefinition cubeBlockDefinition)
        {
            MyCube c = new MyCube();
            c.Parts = MyCubeGrid.GetCubeParts(cubeBlockDefinition, pos, rotation, GridSize);
            c.CubeBlock = block;
            return c;
        }

        //Temp public for debugging
        public bool ChangeColor(MySlimBlock block, Vector3 newHSV)
        {
            ProfilerShort.Begin("MyCubeGrid.ChangeColor");
            try
            {
                if (block.ColorMaskHSV == newHSV)
                    return false;
                block.ColorMaskHSV = newHSV;
                block.UpdateVisual();
                return true;
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        private void UpdatePartInstanceData(MyCubePart part, Vector3I cubePos)
        {
            MyCube cube;
            m_cubes.TryGetValue(cubePos, out cube);
            MySlimBlock block = cube.CubeBlock as MySlimBlock;
            if (block != null)
            {
                part.InstanceData.SetColorMaskHSV(new Vector4(block.ColorMaskHSV, block.Dithering));
            }
            {
                Debug.Assert(part.Model.BoneMapping != null, String.Format("Bones not set for model '{0}', add BoneGridSize tag to XML and rebuild model", part.Model.AssetName));

                if (part.Model.BoneMapping != null)
                {
                    var orientation = part.InstanceData.LocalMatrix.GetOrientation();
                    bool enableSkinning = false;

                    part.InstanceData.BoneRange = GridSize;

                    for (int index = 0; index < Math.Min(part.Model.BoneMapping.Length, 9); index++)
                    {
                        // Bone offset, from 0,0,0 to 2,2,2 inclusive
                        var boneOffset = part.Model.BoneMapping[index];

                        // -1,-1,-1 to 1,1,1
                        Vector3 centered = boneOffset * 1.0f - Vector3.One;

                        var transformedOffset = Vector3I.Round(Vector3.Transform(centered * 1.0f, orientation) + Vector3.One);
                        Vector3 bonePos = Skeleton.GetBone(cubePos, transformedOffset);

                        var byteBone = Vector3UByte.Normalize(bonePos, GridSize);

                        if (!Vector3UByte.IsMiddle(byteBone))
                            enableSkinning = true;

                        // We need to copy bone anyway, because even when this bone is zero, other bones may not be
                        part.InstanceData[index] = byteBone;
                    }
                    part.InstanceData.EnableSkinning = enableSkinning;
                }
            }
        }

        private void UpdateParts(Vector3I pos)
        {
            MyCube cube;
            bool exists = m_cubes.TryGetValue(pos, out cube);
            if (exists && cube.CubeBlock.ShowParts) // Cube exists
            {
                var gridDefinition = cube.CubeBlock.BlockDefinition;
                MyTileDefinition[] tiles = MyCubeGridDefinitions.GetCubeTiles(gridDefinition);

                Matrix orientation;
                cube.CubeBlock.Orientation.GetMatrix(out orientation);

                if (Skeleton.IsDeformed(pos, 0.004f * GridSize, this, false))
                {
                    RemoveBlockEdges(cube.CubeBlock);
                }

                for (int i = 0; i < cube.Parts.Length; i++)
                {
                    UpdatePartInstanceData(cube.Parts[i], pos);

                    // When part is already present, it doesn't mind, hashset can handle that
                    Render.RenderData.AddCubePart(cube.Parts[i]);

                    var tile = tiles[i];

                    if (tile.IsEmpty)
                        continue;

                    var normal = Vector3.TransformNormal(tile.Normal, orientation);
                    var up = Vector3.TransformNormal(tile.Up, orientation);

                    // Only axis aligned direction apply
                    if (!Base6Directions.IsBaseDirection(ref normal))
                        continue;

                    // Find neighbour in direction of normal
                    var neighbour = pos + Vector3I.Round(normal);

                    MyCube neighbourCube;
                    if (m_cubes.TryGetValue(neighbour, out neighbourCube) && neighbourCube.CubeBlock.ShowParts)
                    {
                        Matrix neighbourOrientation;
                        neighbourCube.CubeBlock.Orientation.GetMatrix(out neighbourOrientation);

                        var neighbourDefinition = neighbourCube.CubeBlock.BlockDefinition;
                        MyTileDefinition[] neighbourTiles = MyCubeGridDefinitions.GetCubeTiles(neighbourDefinition);

                        for (int j = 0; j < neighbourCube.Parts.Length; j++)
                        {
                            var neighbourTile = neighbourTiles[j];
                            if (neighbourTile.IsEmpty)
                                continue;

                            var neighbourNormal = Vector3.TransformNormal(neighbourTile.Normal, neighbourOrientation);

                            if ((normal + neighbourNormal).LengthSquared() < 0.001f)
                            {
                                // Different dithering = add tile, same dithering = remove tile
                                if (neighbourCube.CubeBlock.Dithering != cube.CubeBlock.Dithering)
                                {
                                    // Doesn't matter if already added, hashset handle that
                                    Render.RenderData.AddCubePart(neighbourCube.Parts[j]);
                                }
                                else
                                {
                                    bool isAnyFull = false;
                                    if (neighbourTile.FullQuad && !tile.IsRounded)
                                    {
                                        Render.RenderData.RemoveCubePart(cube.Parts[i]);
                                        isAnyFull = true;
                                    }
                                    if (tile.FullQuad && !neighbourTile.IsRounded)
                                    {
                                        Render.RenderData.RemoveCubePart(neighbourCube.Parts[j]);
                                        isAnyFull = true;
                                    }

                                    if (!isAnyFull && (neighbourTile.Up * tile.Up).LengthSquared() > 0.001f)
                                    {
                                        var neighbourUp = Vector3.TransformNormal(neighbourTile.Up, neighbourOrientation);
                                        if ((neighbourUp - up).LengthSquared() < 0.001f)
                                        {
                                            if (!tile.IsRounded || neighbourTile.IsRounded)
                                            {
                                                Render.RenderData.RemoveCubePart(cube.Parts[i]);
                                            }
                                            if (tile.IsRounded || !neighbourTile.IsRounded)
                                            {
                                                Render.RenderData.RemoveCubePart(neighbourCube.Parts[j]);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            else // Cubes was probably removed
            {
                if (exists)
                {
                    foreach (var part in cube.Parts)
                    {
                        Render.RenderData.RemoveCubePart(part);
                    }
                }

                // Foreach neighbour, add shared tile
                foreach (var dir in Base6Directions.Directions)
                {
                    var neighbour = pos + Vector3I.Round(dir);

                    MyCube neighbourCube;
                    if (m_cubes.TryGetValue(neighbour, out neighbourCube) && neighbourCube.CubeBlock.ShowParts)
                    {
                        Matrix neighbourOrientation;
                        neighbourCube.CubeBlock.Orientation.GetMatrix(out neighbourOrientation);

                        var neighbourDefinition = neighbourCube.CubeBlock.BlockDefinition;
                        MyTileDefinition[] neighbourTiles = MyCubeGridDefinitions.GetCubeTiles(neighbourDefinition);

                        for (int j = 0; j < neighbourCube.Parts.Length; j++)
                        {
                            var neighbourTile = neighbourTiles[j];
                            var neighbourNormal = Vector3.TransformNormal(neighbourTile.Normal, neighbourOrientation);

                            if ((dir + neighbourNormal).LengthSquared() < 0.001f)
                            {
                                // Doesn't matter if already added, hashset handle that
                                Render.RenderData.AddCubePart(neighbourCube.Parts[j]);
                            }
                        }
                    }
                }
            }
        }

        private void RemoveRedundantParts()
        {
            foreach (var pair in m_cubes)
            {
                UpdateParts(pair.Key);
            }
        }

        private void BoundsInclude(MySlimBlock block)
        {
            //Debug.Assert(block != null, "Block cannot be null");

            if (block != null)
            {
                m_min = Vector3I.Min(m_min, block.Min);
                m_max = Vector3I.Max(m_max, block.Max);
            }
        }

        private void BoundsIncludeUpdateAABB(MySlimBlock block)
        {
            BoundsInclude(block);
            UpdateGridAABB();
        }

        private void RecalcBounds()
        {
            m_min = Vector3I.MaxValue;
            m_max = Vector3I.MinValue;

            if (m_cubes.Count > 0)
            {
                foreach (var c in m_cubes)
                {
                    m_min = Vector3I.Min(m_min, c.Key);
                    m_max = Vector3I.Max(m_max, c.Key);
                }
            }

            UpdateGridAABB();
        }

        private void UpdateGridAABB()
        {
            PositionComp.LocalAABB = new BoundingBox(m_min * GridSize - new Vector3(GridSize / 2.0f), m_max * GridSize + new Vector3(GridSize / 2.0f));
        }

        private void ResetSkeleton()
        {
            Skeleton = new MyGridSkeleton();
        }

        /// <summary>
        /// For moving bones in corner, offset must contain two one's (positive or negative) and one zero
        /// </summary>
        private bool MoveCornerBones(Vector3I cubePos, Vector3I offset, ref Vector3I minCube, ref Vector3I maxCube)
        {
            var absOffset = Vector3I.Abs(offset);
            var shift = Vector3I.Shift(absOffset);
            Vector3I a = offset * shift;
            Vector3I b = offset * Vector3I.Shift(shift);
            Vector3 clamp = new Vector3(GridSize / 2);

            bool exists = m_cubes.ContainsKey(cubePos + offset);
            exists &= m_cubes.ContainsKey(cubePos + a);
            exists &= m_cubes.ContainsKey(cubePos + b);

            // Three in corner exists, do bone transform
            if (exists)
            {
                var normal = new Vector3I(1, 1, 1) - absOffset;
                var centerBone = new Vector3I(1, 1, 1);

                var targetBone = centerBone + offset;
                var upper = targetBone + normal;
                var lower = targetBone - normal;

                Vector3 moveDirection = -offset * MyGridConstants.CORNER_BONE_MOVE_DISTANCE * GridSize; // 1m per axis in direction

                // The bones here exists
                MoveBone(ref cubePos, ref targetBone, ref moveDirection, ref clamp);
                MoveBone(ref cubePos, ref upper, ref moveDirection, ref clamp);
                MoveBone(ref cubePos, ref lower, ref moveDirection, ref clamp);

                minCube = Vector3I.Min(Vector3I.Min(cubePos, minCube), cubePos + offset - normal);
                maxCube = Vector3I.Max(Vector3I.Max(cubePos, maxCube), cubePos + offset + normal);
            }
            return exists;
        }

        public void GetExistingBones(Vector3I boneMin, Vector3I boneMax, Dictionary<Vector3I, MySlimBlock> resultSet)
        {
            Vector3I cubeMin = Vector3I.Floor((boneMin - Vector3I.One) / (float)Skeleton.BoneDensity);
            Vector3I cubeMax = Vector3I.Ceiling((boneMax - Vector3I.One) / (float)Skeleton.BoneDensity);

            resultSet.Clear();
            Vector3I cube, boneBase, bone;
            for (cube.X = cubeMin.X; cube.X <= cubeMax.X; cube.X++)
                for (cube.Y = cubeMin.Y; cube.Y <= cubeMax.Y; cube.Y++)
                    for (cube.Z = cubeMin.Z; cube.Z <= cubeMax.Z; cube.Z++)
                    {
                        MyCube cubeInst;
                        if (m_cubes.TryGetValue(cube, out cubeInst) && cubeInst.CubeBlock.UsesDeformation)
                        {
                            // TODO: Optimize to one cycle (precalculate position)
                            boneBase = cube * Skeleton.BoneDensity;
                            for (bone.X = 0; bone.X <= Skeleton.BoneDensity; bone.X++)
                                for (bone.Y = 0; bone.Y <= Skeleton.BoneDensity; bone.Y++)
                                    for (bone.Z = 0; bone.Z <= Skeleton.BoneDensity; bone.Z++)
                                    {
                                        resultSet[boneBase + bone] = cubeInst.CubeBlock;
                                    }
                        }
                    }
        }

        private void MoveBone(ref Vector3I cubePos, ref Vector3I boneOffset, ref Vector3 moveDirection, ref Vector3 clamp)
        {
            m_totalBoneDisplacement += moveDirection.Length();
            var pos = cubePos * Skeleton.BoneDensity + boneOffset;
            var boneValue = Skeleton[pos] + moveDirection;
            Skeleton[pos] = Vector3.Clamp(boneValue, -clamp, clamp);
        }

        private bool RemoveCube(Vector3I pos)
        {
            MyCube cube;
            if (m_cubes.TryGetValue(pos, out cube))
            {
                foreach (var p in cube.Parts)
                {
                    Render.RenderData.RemoveCubePart(p);
                }
                m_cubes.Remove(pos);
                m_dirtyRegion.AddCube(pos);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to merge this grid with any other grid
        /// Merges grids only when both are static
        /// Returns the merged grid (which does not necessarily have to be this grid)
        /// </summary>
        public MyCubeGrid DetectMerge(MySlimBlock block, MyCubeGrid ignore = null, List<MyEntity> nearEntities = null)
        {
            if (!this.IsStatic)
                return null;

            MyCubeGrid retval = null;

            ProfilerShort.Begin("Test merge");

            BoundingBoxD aabb = (BoundingBoxD)new BoundingBox(block.Min * GridSize - GridSize / 2, block.Max * GridSize + GridSize / 2);
            // Inflate by half cube, so it will intersect for sure when there's anything
            aabb.Inflate(GridSize / 2);
            aabb = aabb.Transform(WorldMatrix);
            bool clearNearEntities = false;
            if (nearEntities == null)
            {
                clearNearEntities = true;
                nearEntities = MyEntities.GetEntitiesInAABB(ref aabb);
            }
            for (int i = 0; i < nearEntities.Count; i++)
            {
                Vector3I gridOffset;

                var grid = nearEntities[i] as MyCubeGrid;
                var mergingGrid = retval != null ? retval : this;

                if (grid != null && grid != this && grid.Physics != null && grid.Physics.Enabled && grid != ignore && grid.IsStatic && grid.GridSizeEnum == mergingGrid.GridSizeEnum
                    && mergingGrid.IsMergePossible_Static(block, grid, out gridOffset))
                {
                    MyCubeGrid localMergedGrid = mergingGrid.MergeGrid_Static(grid, gridOffset);
                    if (localMergedGrid != null)
                        retval = localMergedGrid;
                }
            }

            if(clearNearEntities)
                nearEntities.Clear(); // We don't want to hold references to objects and keep them alive!

            ProfilerShort.End();

            return retval;
        }

        /// <param name="gridOffset">Offset of second grid</param>
        private bool IsMergePossible_Static(MySlimBlock block, MyCubeGrid gridToMerge, out Vector3I gridOffset)
        {
            //Debug.Assert(this.WorldMatrix.Up == Vector3D.Up && this.WorldMatrix.Forward == Vector3D.Forward, "This grid must have identity rotation");
            //Debug.Assert(gridToMerge.WorldMatrix.Up == Vector3D.Up && gridToMerge.WorldMatrix.Forward == Vector3D.Forward, "Grid to merge must have identity rotation");

           
            gridOffset = Vector3I.Round((gridToMerge.PositionComp.GetPosition() - this.PositionComp.GetPosition()) / GridSize);

            MatrixD otherMatrix = gridToMerge.PositionComp.WorldMatrix.GetOrientation();
            if (this.PositionComp.WorldMatrix.GetOrientation().EqualsFast(ref otherMatrix) == false)
            {
                return false;
            }

            var blockPosInSecondGrid = block.Position - gridOffset;
            Quaternion blockOrientation;
            block.Orientation.GetQuaternion(out blockOrientation);
            return CheckConnectivity(gridToMerge, block.BlockDefinition, ref blockOrientation, ref blockPosInSecondGrid);
        }

        public MatrixI CalculateMergeTransform(MyCubeGrid gridToMerge, Vector3I gridOffset)
        {
            Vector3 fw = (Vector3)Vector3D.TransformNormal(gridToMerge.WorldMatrix.Forward, this.PositionComp.WorldMatrixNormalizedInv);
            Vector3 up = (Vector3)Vector3D.TransformNormal(gridToMerge.WorldMatrix.Up, this.PositionComp.WorldMatrixNormalizedInv);
            Base6Directions.Direction fwDir = Base6Directions.GetClosestDirection(fw);
            Base6Directions.Direction upDir = Base6Directions.GetClosestDirection(up);
            if (upDir == fwDir) upDir = Base6Directions.GetPerpendicular(fwDir);
            MatrixI transform = new MatrixI(ref gridOffset, fwDir, upDir);
            return transform;
        }

        public bool CanMergeCubes(MyCubeGrid gridToMerge, Vector3I gridOffset)
        {
            MatrixI transform = CalculateMergeTransform(gridToMerge, gridOffset);
            foreach (var cubesEntry in gridToMerge.m_cubes)
            {
                Vector3I position = Vector3I.Transform(cubesEntry.Key, transform);
                if (this.m_cubes.ContainsKey(position))
                {
                    MySlimBlock localBlock = GetCubeBlock(position);
                    if (localBlock != null && localBlock.FatBlock is MyCompoundCubeBlock)
                    {
                        MyCompoundCubeBlock localCompoundBlock = localBlock.FatBlock as MyCompoundCubeBlock;

                        MySlimBlock blockInMergeGrid = gridToMerge.GetCubeBlock(cubesEntry.Key);
                        if (blockInMergeGrid.FatBlock is MyCompoundCubeBlock)
                        {
                            MyCompoundCubeBlock compoundInMergeGrid = blockInMergeGrid.FatBlock as MyCompoundCubeBlock;

                            bool canMerge = true;

                            foreach (var blockInCompund in compoundInMergeGrid.GetBlocks())
                            {
                                Base6Directions.Direction forward = transform.GetDirection(blockInCompund.Orientation.Forward);
                                Base6Directions.Direction up = transform.GetDirection(blockInCompund.Orientation.Up);
                                MyBlockOrientation newOrientation = new MyBlockOrientation(forward, up);

                                if (!localCompoundBlock.CanAddBlock(blockInCompund.BlockDefinition, newOrientation))
                                {
                                    canMerge = false;
                                    break;
                                }
                            }

                            if (canMerge)
                                continue;
                        }
                        else
                        {
                            Base6Directions.Direction forward = transform.GetDirection(blockInMergeGrid.Orientation.Forward);
                            Base6Directions.Direction up = transform.GetDirection(blockInMergeGrid.Orientation.Up);
                            MyBlockOrientation newOrientation = new MyBlockOrientation(forward, up);

                            if (localCompoundBlock.CanAddBlock(blockInMergeGrid.BlockDefinition, newOrientation))
                                continue;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        private MyCubeGrid MergeGrid_Static(MyCubeGrid gridToMerge, Vector3I gridOffset)
        {
            Debug.Assert(this.IsStatic && gridToMerge.IsStatic, "Grids to merge must be static");

            // Always merge smaller grid to larger
            if (this.BlocksCount < gridToMerge.BlocksCount)
            {
                return gridToMerge.MergeGrid_Static(this, -gridOffset);
            }

            MatrixI transform = new MatrixI(gridOffset, Base6Directions.Direction.Forward, Base6Directions.Direction.Up);
            return MergeGridInternal(gridToMerge, ref transform);
        }

        public MyCubeGrid MergeGrid_MergeBlock(MyCubeGrid gridToMerge, Vector3I gridOffset)
        {
            Debug.Assert(Sync.IsServer);

            // Always merge smaller grid to larger
            if ((this.BlocksCount < gridToMerge.BlocksCount && !this.IsStatic) || (!this.IsStatic && gridToMerge.IsStatic))
            {
                return null;
            }

            MatrixI transform = CalculateMergeTransform(gridToMerge, gridOffset);
            SyncObject.MergeGrid(gridToMerge, ref transform);
            return MergeGridInternal(gridToMerge, ref transform);
        }

        public MyCubeGrid MergeGrid_CopyPaste(MyCubeGrid gridToMerge, MatrixI mergeTransform)
        {
            Debug.Assert(Sync.IsServer);

            SyncObject.MergeGrid(gridToMerge, ref mergeTransform);
            return MergeGridInternal(gridToMerge, ref mergeTransform, false);
        }

        public MyCubeGrid MergeGridInternal(MyCubeGrid gridToMerge, ref MatrixI transform, bool disableBlockGenerators = true)
        {
            ProfilerShort.Begin("MergeGridInternal");

            if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS)
                MyCubeGridSmallToLargeConnection.Static.BeforeGridMerge_SmallToLargeGridConnectivity(this, gridToMerge);

            MoveBlocksAndClose(gridToMerge, this, transform, disableBlockGenerators: disableBlockGenerators);

            // Update AABB, physics, dirty blocks
            UpdateGridAABB();

            if (Physics != null)
            {
                // We need to update physics immediatelly because of landing gears
                Physics.UpdateShape();
            }

            UpdateDirty();

            if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS)
                MyCubeGridSmallToLargeConnection.Static.AfterGridMerge_SmallToLargeGridConnectivity(this);

            ProfilerShort.End();
            return this;
        }

        public void ChangeGridOwnership(long playerId, MyOwnershipShareModeEnum shareMode)
        {
            Debug.Assert(Sync.IsServer, "Changing grid ownership from the client");
            if (!Sync.IsServer) return;

            MySyncGrid.ChangeGridOwner(this, playerId, shareMode);
        }

        private static void MoveBlocks(MyCubeGrid from, MyCubeGrid to, List<MySlimBlock> cubeBlocks, int offset, int count)
        {
            ProfilerShort.Begin("MoveBlocks");

            from.EnableGenerators(false, true);
            to.EnableGenerators(false, true);

            try
            {

                m_tmpBlockGroups.Clear();
                foreach (var group in from.BlockGroups)
                {
                    // CH: TODO: This is to catch a nullref. Remove when not needed
                    if (group == null) MySandboxGame.Log.WriteLine("group in from.BlockGroups was null");

                    m_tmpBlockGroups.Add(group.GetObjectBuilder());
                }

                ProfilerShort.Begin("RemoveBlockInternal");
                for (int i = offset; i < offset + count; i++)
                {
                    var block = cubeBlocks[i];
                    if (block == null) continue;

                    if (block.FatBlock != null)
                        from.Hierarchy.RemoveChild(block.FatBlock);

                    from.RemoveBlockInternal(block, close: false, markDirtyDisconnects: false);
                }
                ProfilerShort.End();

                ProfilerShort.Begin("AddDirtyBlock");
                if (from.Physics != null)
                {
                    for (int i = offset; i < offset + count; i++)
                    {
                        var block = cubeBlocks[i];
                        if (block == null) continue;

                        from.Physics.AddDirtyBlock(block);
                    }
                    /*from.Physics.UpdateShape();
                    from.RaisePhysicsChanged();*/
                }
                ProfilerShort.End();

                ProfilerShort.Begin("Add block & Copy skeleton");
                for (int i = offset; i < offset + count; i++)
                {
                    var block = cubeBlocks[i];
                    if (block == null) continue;

                    // CH: TODO: This is to catch a nullref. Remove when not needed
                    if (from.Skeleton == null) MySandboxGame.Log.WriteLine("from.Skeleton was null");

                    to.AddBlockInternal(block);
                    from.Skeleton.CopyTo(to.Skeleton, block.Position, block.Position);
                }
                ProfilerShort.End();

                ProfilerShort.Begin("Block groups");
                foreach (var groupBuilder in m_tmpBlockGroups)
                {
                    var group = new MyBlockGroup(to);

                    // CH: TODO: This is to catch a nullref. Remove when not needed
                    if (group == null) MySandboxGame.Log.WriteLine("group was null");
                    if (groupBuilder == null) MySandboxGame.Log.WriteLine("groupBuilder was null");

                    group.Init(groupBuilder);
                    to.AddGroup(group);
                }
                m_tmpBlockGroups.Clear();

                from.RemoveEmptyBlockGroups();

                ProfilerShort.End();
            }
            finally
            {
                from.EnableGenerators(true, true);
                to.EnableGenerators(true, true);

                ProfilerShort.End();
            }
        }

        private void RemoveEmptyBlockGroups()
        {
            for (int i = 0; i < BlockGroups.Count; i++)
            {
                var group = BlockGroups[i];
                if (group.Blocks.Count == 0)
                {
                    RemoveGroup(group);
                    i--;
                }
            }
        }

        /// <summary>
        /// Used only when all blocks of grid are moved.
        /// Moving only some blocks is unsupported now.
        /// </summary>
        static void MoveBlocksAndClose(MyCubeGrid from, MyCubeGrid to, MatrixI transform, bool disableBlockGenerators = true)
        {
            ProfilerShort.Begin("MoveBlocksAndClose");

            if (disableBlockGenerators)
            {
                from.EnableGenerators(false, true);
                to.EnableGenerators(false, true);
            }

            ProfilerShort.Begin("MoveGroups");
            while (from.BlockGroups.Count > 0)
            {
                var group = from.BlockGroups[0];
                to.AddGroup(group);
                from.RemoveGroup(group);
            }
            ProfilerShort.End();

            ProfilerShort.Begin("MyEntities.Remove(grid)");
            MyEntities.Remove(from);
            ProfilerShort.End();

            // Unregister from systems (after removing all blocks from scene)
            ProfilerShort.Begin("UnregisterBlocks");
            from.UnregisterBlocksBeforeClose();
            ProfilerShort.End();

            // Remove from scene
            ProfilerShort.Begin("RemoveBlocksFromScene");
            foreach (var block in from.m_cubeBlocks)
            {
                if (block.FatBlock != null)
                    from.Hierarchy.RemoveChild(block.FatBlock, false);

                ProfilerShort.Begin("Remove Neighbours");
                block.RemoveNeighbours();
                ProfilerShort.End();
            }
            ProfilerShort.End();

            ProfilerShort.Begin("ClosePhysics");
            if (from.Physics != null)
            {
                from.Physics.Close();
                from.Physics = null;
                from.RaisePhysicsChanged();
            }
            ProfilerShort.End();

            // Add blocks
            ProfilerShort.Begin("AddBlocks");
            foreach (var block in from.m_cubeBlocks)
            {
                ProfilerShort.Begin("TransformBlock");
                block.Transform(ref transform);
                ProfilerShort.End();

                ProfilerShort.Begin("AddBlockInternal");
                to.AddBlockInternal(block);
                ProfilerShort.End();
            }
            ProfilerShort.End();

            ProfilerShort.Begin("CopySkeleton");
            from.Skeleton.CopyTo(to.Skeleton, transform, to);
            ProfilerShort.End();

            if (disableBlockGenerators)
            {
                from.EnableGenerators(true, true);
                to.EnableGenerators(true, true);
            }

            ProfilerShort.Begin("ClearAndClose");
            from.m_blocksForDraw.Clear();
            from.m_cubeBlocks.Clear();
            from.m_cubes.Clear();
            from.Close();
            ProfilerShort.End();

            ProfilerShort.End();
        }

        /// <summary>
        /// Adds the block to the grid. The block's position and orientation in the grid should be set elsewhere
        /// </summary>
        private void AddBlockInternal(MySlimBlock block)
        {
            block.CubeGrid = this;

            // Try merge compound blocks together.
            if (MyFakes.ENABLE_COMPOUND_BLOCKS && block.FatBlock is MyCompoundCubeBlock)
            {
                MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                MySlimBlock existingSlimBlock = GetCubeBlock(block.Min);
                MyCompoundCubeBlock existingCompoundBlock = existingSlimBlock != null ? existingSlimBlock.FatBlock as MyCompoundCubeBlock : null;

                if (existingCompoundBlock != null)
                {
                    bool added = false;

                    compoundBlock.UpdateWorldMatrix();

                    List<MySlimBlock> blocksAdded = new List<MySlimBlock>();
                    foreach (var blockInCompound in compoundBlock.GetBlocks())
                    {
                        ushort id;
                        if (existingCompoundBlock.Add(blockInCompound, out id))
                        {
                            BoundsInclude(blockInCompound);

                            m_dirtyRegion.AddCube(blockInCompound.Min);

                            Physics.AddDirtyBlock(existingSlimBlock);

                            blocksAdded.Add(blockInCompound);

                            added = true;
                        }
                    }

                    foreach (var blockToRemove in blocksAdded)
                    {
                        compoundBlock.Remove(blockToRemove, false);
                    }

                    if (added)
                    {
                        if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS && m_enableSmallToLargeConnections)
                            MyCubeGridSmallToLargeConnection.Static.AddBlockSmallToLargeConnection(block);

                        if (OnBlockAdded != null)
                        {
                            foreach (var blockAdded in blocksAdded)
                            {
                                OnBlockAdded(blockAdded);
                            }
                        }
                    }

                    return;
                }
            }

            m_cubeBlocks.Add(block);

            ProfilerShort.Begin("AddNeighbors");
            block.AddNeighbours();
            ProfilerShort.End();

            BoundsInclude(block);
            if (block.FatBlock != null)
            {
                ProfilerShort.Begin("FatBlock");
                block.FatBlock.UpdateWorldMatrix();

                // CH:TODO: This would actually be better than setting WorldMatrix to local matrix, but it causes inaccurate fat block
                // positions for some reasons. Find out why.
                //AddChildWithMatrix(block.FatBlock, ref local);
                Hierarchy.AddChild(block.FatBlock, false);
                GridSystems.RegisterInSystems(block.FatBlock);

                if (block.FatBlock.Render.NeedsDrawFromParent)
                    m_blocksForDraw.Add(block.FatBlock);

                MyObjectBuilderType blockType = block.BlockDefinition.Id.TypeId;
                if (blockType != typeof(MyObjectBuilder_CubeBlock))
                {
                    if (!BlocksCounters.ContainsKey(blockType))
                        BlocksCounters.Add(blockType, 0);
                    BlocksCounters[blockType]++;
                }
                ProfilerShort.End();
            }

            ProfilerShort.Begin("AddCubes");
            MyBlockOrientation blockOrientation = block.Orientation;
            Matrix rotationMatrix;
            blockOrientation.GetMatrix(out rotationMatrix);

            bool blockAddSuccessfull = true;
            Vector3I temp = new Vector3I();
            for (temp.X = block.Min.X; temp.X <= block.Max.X; temp.X++)
                for (temp.Y = block.Min.Y; temp.Y <= block.Max.Y; temp.Y++)
                    for (temp.Z = block.Min.Z; temp.Z <= block.Max.Z; temp.Z++)
                    {
                        blockAddSuccessfull &= AddCube(block, ref temp, rotationMatrix, block.BlockDefinition);
                    }

            Debug.Assert(blockAddSuccessfull, "Cannot add block!");
            ProfilerShort.End();

            if (Physics != null)
            {
                ProfilerShort.Begin("Physics.AddBlock");
                Physics.AddBlock(block);
                ProfilerShort.End();
            }

            if (block.FatBlock != null)
            {
                ProfilerShort.Begin("Owner");
                ChangeOwner(block.FatBlock, 0, block.FatBlock.OwnerId);
                ProfilerShort.End();
            }

            if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS && m_enableSmallToLargeConnections && blockAddSuccessfull)
            {
                ProfilerShort.Begin("CheckAddedBlockSmallToLargeConnection");
                MyCubeGridSmallToLargeConnection.Static.AddBlockSmallToLargeConnection(block);
                ProfilerShort.End();
            }

            ProfilerShort.Begin("OnBlockAdded");
            if (OnBlockAdded != null)
                OnBlockAdded(block);
            ProfilerShort.End();
        }

        private bool IsDamaged(Vector3I blockPos, Vector3I boneOffset, float epsilon = 0.04f)
        {
            return !MyUtils.IsZero(Skeleton.GetBone(blockPos, boneOffset), epsilon * GridSize);
        }

        private void AddBlockEdges(MySlimBlock block)
        {
            Vector3 position = block.Position * GridSize;
            Matrix blockOrientation;
            block.Orientation.GetMatrix(out blockOrientation);

            Matrix blockTransformMatrix = blockOrientation;
            blockTransformMatrix.Translation = position;

            var definition = block.BlockDefinition;
            if (definition.BlockTopology == MyBlockTopology.Cube)
            {
                if (!definition.CubeDefinition.ShowEdges)
                    return;
                var info = MyCubeGridDefinitions.GetTopologyInfo(definition.CubeDefinition.CubeTopology);

                foreach (var edge in info.Edges)
                {
                    Vector3 point0 = Vector3.Transform(edge.Point0, blockOrientation);
                    Vector3 point1 = Vector3.Transform(edge.Point1, blockOrientation);
                    Vector3 middle = (point0 + point1) * 0.5f;

                    if (IsDamaged(block.Position, Vector3I.Round(point0) + Vector3I.One) ||
                        IsDamaged(block.Position, Vector3I.Round(middle) + Vector3I.One) ||
                        IsDamaged(block.Position, Vector3I.Round(point1) + Vector3I.One))
                        continue;

                    point0 = Vector3.Transform(edge.Point0 * GridSize * 0.5f, blockTransformMatrix);
                    point1 = Vector3.Transform(edge.Point1 * GridSize * 0.5f, blockTransformMatrix);

                    Vector3 normal0 = Vector3.Transform(info.Tiles[edge.Side0].Normal, blockTransformMatrix.GetOrientation());
                    Vector3 normal1 = Vector3.Transform(info.Tiles[edge.Side1].Normal, blockTransformMatrix.GetOrientation());

                    // Saturation and Value is offset, from -1 to 1, it must be normalized
                    var hsvNormalized = block.ColorMaskHSV;
                    hsvNormalized.Y = (hsvNormalized.Y + 1) / 2;
                    hsvNormalized.Z = (hsvNormalized.Z + 1) / 2;

                    Render.RenderData.AddEdgeInfo(ref point0, ref point1, ref normal0, ref normal1, new Color(hsvNormalized), block);
                }
            }
        }

        private void RemoveBlockEdges(MySlimBlock block)
        {
            Vector3 position = block.Position * GridSize;
            Matrix blockTransformMatrix;
            block.Orientation.GetMatrix(out blockTransformMatrix);
            blockTransformMatrix.Translation = position;

            var definition = block.BlockDefinition;
            if (definition.BlockTopology == MyBlockTopology.Cube)
            {
                var info = MyCubeGridDefinitions.GetTopologyInfo(definition.CubeDefinition.CubeTopology);
                Vector3 point0, point1;
                foreach (var edge in info.Edges)
                {
                    point0 = Vector3.Transform(edge.Point0 * GridSize * 0.5f, blockTransformMatrix);
                    point1 = Vector3.Transform(edge.Point1 * GridSize * 0.5f, blockTransformMatrix);

                    Render.RenderData.RemoveEdgeInfo(point0, point1, block);
                }
            }
        }

        public void RequestFillStockpile(Vector3I blockPosition, MyInventory fromInventory)
        {
            SyncObject.RequestFillStockpile(blockPosition, fromInventory);
        }

        public void RequestSetToConstruction(Vector3I blockPosition, MyInventory fromInventory)
        {
            SyncObject.RequestSetToConstruction(blockPosition, fromInventory);
        }

        private void DoLazyUpdates()
        {
            if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS && m_enableSmallToLargeConnections && !m_smallToLargeConnectionsInitialized)
            {
                MyCubeGridSmallToLargeConnection.Static.AddGridSmallToLargeConnection(this);
            }
            m_smallToLargeConnectionsInitialized = true;

            ProfilerShort.Begin("Send dirty bones");
            if (BonesToSend.InputCount > 0 && m_bonesSendCounter++ > 10) // Only increment counter when there's something waiting
            {
                m_bonesSendCounter = 0;
                var segments = BonesToSend.FindSegments(MyVoxelSegmentationType.Simple);
                foreach (var seg in segments)
                {
                    SyncObject.SendDirtyBones(seg.Min, seg.Max, Skeleton);
                }
                BonesToSend.ClearInput();
            }
            ProfilerShort.End();

            foreach (var block in m_blocksForDamageApplication)
                block.ApplyAccumulatedDamage();
            m_blocksForDamageApplication.Clear();

            if (m_disconnectsDirty)
            {
                DetectDisconnects();
            }

            Skeleton.RemoveUnusedBones(this);

            if (m_ownershipManager.NeedRecalculateOwners)
            {
                m_ownershipManager.RecalculateOwners();
                m_ownershipManager.NeedRecalculateOwners = false;

                var handler = OnBlockOwnershipChanged;
                if (handler != null)
                    handler(this);
            }
        }

        internal void AddForDamageApplication(MySlimBlock block)
        {
            m_blocksForDamageApplication.Add(block);
        }

        internal void RemoveFromDamageApplication(MySlimBlock block)
        {
            m_blocksForDamageApplication.Remove(block);
        }

        #region Ray casting, intersections and overlaps

        //For ModAPI, they dont have havok yet
        public bool GetLineIntersectionExactGrid(ref LineD line, ref Vector3I position, ref double distanceSquared)
        {
            return GetLineIntersectionExactGrid(ref line, ref position, ref distanceSquared, null);
        }
        public bool GetLineIntersectionExactGrid(ref LineD line, ref Vector3I position, ref double distanceSquared, MyPhysics.HitInfo? hitInfo = null)
        {
            RayCastCells(line.From, line.To, m_cacheRayCastCells, havokWorld: true);
            if (m_cacheRayCastCells.Count == 0)
                return false;

            if (hitInfo.HasValue)
                m_tmpHitList.Add(hitInfo.Value);
            else
                MyPhysics.CastRay(line.From, line.To, m_tmpHitList, MyPhysics.ObjectDetectionCollisionLayer);

            bool result = false;
            for (int i = 0; i < m_cacheRayCastCells.Count; i++)
            {
                Vector3I hit = m_cacheRayCastCells[i];
                MyCube cube;
                m_cubes.TryGetValue(hit, out cube);
                double distSq = double.MaxValue;

                if (cube == null || cube.CubeBlock.FatBlock == null || !cube.CubeBlock.FatBlock.BlockDefinition.UseModelIntersection)
                {
                    if (m_tmpHitList.Count > 0)
                    {
                        int j = 0;
                        if (MySession.ControlledEntity != null)
                            while (j < m_tmpHitList.Count - 1 && m_tmpHitList[j].HkHitInfo.Body.GetEntity() == MySession.ControlledEntity.Entity)
                                j++;

                        if (m_tmpHitList[j].HkHitInfo.Body.GetEntity() != this)
                            continue;
                        var bias = new Vector3(GridSize, GridSize, GridSize) / 2;
                        var locPos = Vector3D.Transform(m_tmpHitList[j].Position, MatrixD.Invert(WorldMatrix));
                        var blockPos = hit * GridSize;
                        var dir = locPos - blockPos;
                        var max = dir.Max() > Math.Abs(dir.Min()) ? dir.Max() : dir.Min();
                        dir.X = dir.X == max ? max > 0 ? 1 : -1 : 0;
                        dir.Y = dir.Y == max ? max > 0 ? 1 : -1 : 0;
                        dir.Z = dir.Z == max ? max > 0 ? 1 : -1 : 0;
                        locPos -= dir * 0.06f;

                        if (Vector3D.Max(locPos, blockPos - bias) == locPos && Vector3D.Min(locPos, blockPos + bias) == locPos)
                        {
                            if (cube == null)
                            {
                                Vector3I nearest;
                                FixTargetCube(out nearest, locPos / GridSize);
                                if (!m_cubes.TryGetValue(nearest, out cube))
                                    continue;
                                hit = nearest;
                            }

                            distSq = Vector3D.DistanceSquared(line.From, m_tmpHitList[j].Position);// Vector3.Transform(hit*GridSize, WorldMatrix));
                            if (distSq < distanceSquared)
                            {
                                position = hit;
                                distanceSquared = distSq;
                                result = true;
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    MyIntersectionResultLineTriangleEx? intersection;
                    GetBlockIntersection(cube, ref line, out intersection, IntersectionFlags.ALL_TRIANGLES);
                    if (intersection.HasValue)
                        distSq = Vector3.DistanceSquared(line.From, intersection.Value.IntersectionPointInWorldSpace);
                }
                if (distSq < distanceSquared)
                {
                    distanceSquared = distSq;
                    position = hit;
                    result = true;
                }
            }

            m_tmpHitList.Clear();
            return result;
        }

        private void GetBlockIntersection(MyCube cube, ref LineD line, out MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags)
        {
            if (cube.CubeBlock.FatBlock != null)
            {
                if (cube.CubeBlock.FatBlock is MyCompoundCubeBlock)
                {
                    MyCompoundCubeBlock compound = cube.CubeBlock.FatBlock as MyCompoundCubeBlock;
                    MyIntersectionResultLineTriangleEx? closestHit = null;
                    double closestDistance = double.MaxValue;

                    foreach (var block in compound.GetBlocks())
                    {
                        //model block
                        Matrix local;
                        block.Orientation.GetMatrix(out local);
                        Vector3 modelOffset;
                        Vector3.TransformNormal(ref block.BlockDefinition.ModelOffset, ref local, out modelOffset);

                        local.Translation = block.Position * GridSize + modelOffset;
                        MatrixD invLocal = MatrixD.Invert(block.FatBlock.WorldMatrix);

                        t = block.FatBlock.ModelCollision.GetTrianglePruningStructure().GetIntersectionWithLine(this, ref line, ref invLocal, flags);

                        if (t == null && block.FatBlock.Subparts != null)
                        {
                            foreach (var subpart in block.FatBlock.Subparts)
                            {
                                invLocal = MatrixD.Invert(subpart.Value.WorldMatrix);
                                t = subpart.Value.ModelCollision.GetTrianglePruningStructure().GetIntersectionWithLine(this, ref line, ref invLocal, flags);
                                if (t != null)
                                    break;
                            }
                        }

                        if (t != null)
                        {
                            MyIntersectionResultLineTriangleEx correctIntersection = t.Value;

                            var hitPoint = Vector3D.Transform(t.Value.IntersectionPointInObjectSpace, block.FatBlock.WorldMatrix);
                            var distance = Vector3D.Distance(hitPoint, line.From);

                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                correctIntersection.IntersectionPointInObjectSpace = Vector3.Transform(t.Value.IntersectionPointInObjectSpace, local);
                                correctIntersection.IntersectionPointInWorldSpace = Vector3D.Transform(t.Value.IntersectionPointInObjectSpace, block.FatBlock.WorldMatrix);
                                correctIntersection.NormalInObjectSpace = Vector3.TransformNormal(t.Value.NormalInObjectSpace, local);
                                correctIntersection.NormalInWorldSpace = (Vector3)Vector3D.TransformNormal(correctIntersection.NormalInObjectSpace, WorldMatrix);
                                closestHit = correctIntersection;
                            }
                        }
                    }

                    t = closestHit;
                }
                else
                {
                    //model block
                    Matrix local;
                    cube.CubeBlock.Orientation.GetMatrix(out local);
                    Vector3 modelOffset;
                    Vector3.TransformNormal(ref cube.CubeBlock.BlockDefinition.ModelOffset, ref local, out modelOffset);

                    local.Translation = cube.CubeBlock.Position * GridSize + modelOffset;
                    MatrixD invLocal = MatrixD.Invert(cube.CubeBlock.FatBlock.WorldMatrix);

                    t = cube.CubeBlock.FatBlock.ModelCollision.GetTrianglePruningStructure().GetIntersectionWithLine(this, ref line, ref invLocal, flags);

                    if (t == null && cube.CubeBlock.FatBlock.Subparts != null)
                    {
                        foreach (var subpart in cube.CubeBlock.FatBlock.Subparts)
                        {
                            invLocal = MatrixD.Invert(subpart.Value.WorldMatrix);
                            t = subpart.Value.ModelCollision.GetTrianglePruningStructure().GetIntersectionWithLine(this, ref line, ref invLocal, flags);
                            if (t != null)
                                break;
                        }
                    }

                    if (t != null)
                    {
                        MyIntersectionResultLineTriangleEx correctIntersection = t.Value;
                        correctIntersection.IntersectionPointInObjectSpace = Vector3.Transform(t.Value.IntersectionPointInObjectSpace, local);
                        correctIntersection.IntersectionPointInWorldSpace = Vector3.Transform(t.Value.IntersectionPointInObjectSpace, cube.CubeBlock.FatBlock.WorldMatrix);
                        correctIntersection.NormalInObjectSpace = Vector3.TransformNormal(t.Value.NormalInObjectSpace, local);
                        correctIntersection.NormalInWorldSpace = Vector3.TransformNormal(correctIntersection.NormalInObjectSpace, WorldMatrix);
                        t = correctIntersection;
                    }
                }
            }
            else
            {
                //cube block
                MyIntersectionResultLineTriangleEx? closestHit = null;
                float closestDistance = float.MaxValue;
                Vector3? closestHitpoint = null;

                foreach (var cubepart in cube.Parts)
                {
                    MatrixD world = cubepart.InstanceData.LocalMatrix * WorldMatrix;
                    MatrixD invWorld = MatrixD.Invert(world);

                    t = cubepart.Model.GetTrianglePruningStructure().GetIntersectionWithLine(this, ref line, ref invWorld, flags);
                    if (t != null)
                    {
                        MyIntersectionResultLineTriangleEx correctIntersection = t.Value;

                        var hitPoint = Vector3.Transform(t.Value.IntersectionPointInObjectSpace, world);
                        float distance = Vector3.Distance(hitPoint, line.From);

                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            correctIntersection.IntersectionPointInObjectSpace = Vector3.Transform(t.Value.IntersectionPointInObjectSpace, cubepart.InstanceData.LocalMatrix);
                            correctIntersection.IntersectionPointInWorldSpace = Vector3.Transform(correctIntersection.IntersectionPointInObjectSpace, WorldMatrix);
                            correctIntersection.NormalInObjectSpace = Vector3.TransformNormal(t.Value.NormalInObjectSpace, cubepart.InstanceData.LocalMatrix);
                            correctIntersection.NormalInWorldSpace = Vector3.TransformNormal(correctIntersection.NormalInObjectSpace, WorldMatrix);
                            closestHitpoint = correctIntersection.IntersectionPointInWorldSpace;
                            closestHit = correctIntersection;
                        }
                    }
                }

                t = closestHit;
            }
        }

        public static bool GetLineIntersection(ref LineD line, out MyCubeGrid grid, out Vector3I position, out double distanceSquared)
        {
            grid = default(MyCubeGrid);
            position = default(Vector3I);
            distanceSquared = float.MaxValue;

            MyEntities.OverlapAllLineSegment(ref line, m_lineOverlapList);
            foreach (var e in m_lineOverlapList)
            {
                MyCubeGrid testGrid = e.Element as MyCubeGrid;
                if (testGrid != null)
                {
                    var castResult = testGrid.RayCastBlocks(line.From, line.To);
                    if (castResult.HasValue)
                    {
                        Vector3 closestCorner = testGrid.GetClosestCorner(castResult.Value, line.From);
                        float testDistSq = (float)Vector3D.DistanceSquared(line.From, Vector3D.Transform(closestCorner, testGrid.WorldMatrix));
                        if (testDistSq < distanceSquared)
                        {
                            distanceSquared = testDistSq;
                            grid = testGrid;
                            position = castResult.Value;
                        }
                    }
                }
            }

            m_lineOverlapList.Clear();
            return grid != null;
        }

        public static bool GetLineIntersectionExact(ref LineD line, out MyCubeGrid grid, out Vector3I position, out double distanceSquared)
        {
            grid = default(MyCubeGrid);
            position = default(Vector3I);
            distanceSquared = float.MaxValue;

            double distance = double.MaxValue;

            MyEntities.OverlapAllLineSegment(ref line, m_lineOverlapList);
            foreach (var e in m_lineOverlapList)
            {
                MyCubeGrid testGrid = e.Element as MyCubeGrid;
                if (testGrid != null)
                {
                    MySlimBlock slimBlock;
                    double dst;
                    Vector3D? intersectedObjectPos = testGrid.GetLineIntersectionExactAll(ref line, out dst, out slimBlock);
                    if (intersectedObjectPos != null && dst < distance)
                    {
                        grid = testGrid;
                        distance = dst;
                    }
                }
            }

            m_lineOverlapList.Clear();
            return grid != null;
        }

        /// <summary>
        /// Returns closest line (in world space) intersection with all cubes. 
        /// </summary>
        public Vector3D? GetLineIntersectionExactAll(ref LineD line, out double distance, out MySlimBlock intersectedBlock)
        {
            intersectedBlock = null;
            distance = float.MaxValue;

            Vector3I? cubePosRes = null;

            Vector3I cubePos = Vector3I.Zero;
            double cubeDst = double.MaxValue;
            if (GetLineIntersectionExactGrid(ref line, ref cubePos, ref cubeDst))
            {
                cubeDst = (float)Math.Sqrt(cubeDst);
                cubePosRes = cubePos;
            }

            if (cubePosRes != null)
            {
                distance = cubeDst;

                intersectedBlock = GetCubeBlock(cubePosRes.Value);
                if (intersectedBlock == null)
                    return null;

                return cubePos;
            }

            return null;
        }

        public void GetBlocksInsideSphere(ref BoundingSphereD sphere, HashSet<MySlimBlock> blocks)
        {
            blocks.Clear();

            BoundingBoxD box = new BoundingBoxD(sphere.Center - new Vector3D(sphere.Radius), sphere.Center + new Vector3D(sphere.Radius));

            box = box.Transform(this.PositionComp.WorldMatrixNormalizedInv);
            Vector3D min = box.Min;
            Vector3D max = box.Max;
            Vector3I start = new Vector3I((int)Math.Round(min.X / GridSize), (int)Math.Round(min.Y / GridSize), (int)Math.Round(min.Z / GridSize));
            Vector3I end = new Vector3I((int)Math.Round(max.X / GridSize), (int)Math.Round(max.Y / GridSize), (int)Math.Round(max.Z / GridSize));

            Vector3I startIt = Vector3I.Min(start, end);
            Vector3I endIt = Vector3I.Max(start, end);

            var localSphere = new BoundingSphereD(box.Center, sphere.Radius);

            for (int i = startIt.X; i <= endIt.X; i++)
            {
                for (int j = startIt.Y; j <= endIt.Y; j++)
                {
                    for (int k = startIt.Z; k <= endIt.Z; k++)
                    {
                        if (m_cubes.ContainsKey(new Vector3I(i, j, k)))
                        {
                            var block = m_cubes[new Vector3I(i, j, k)].CubeBlock;

                            BoundingBoxD blockBox = new BoundingBoxD(block.Min * GridSize - GridSize / 2, block.Max * GridSize + GridSize / 2);
                            if (blockBox.Intersects(localSphere))
                            {
                                blocks.Add(block);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Optimized version where spheres are sorted from smallest to largest
        /// </summary>
        /// <param name="sphere1"></param>
        /// <param name="sphere2"></param>
        /// <param name="sphere3"></param>
        /// <param name="blocks1"></param>
        /// <param name="blocks2"></param>
        /// <param name="blocks3"></param>
        /// <param name="respectDeformationRatio"></param>
        /// <param name="detectionBlockHalfSize"></param>
        /// <param name="invWorldGrid"></param>
        public void GetBlocksInsideSpheres(
            ref BoundingSphereD sphere1,
            ref BoundingSphereD sphere2,
            ref BoundingSphereD sphere3,
            HashSet<MySlimBlock> blocks1,
            HashSet<MySlimBlock> blocks2,
            HashSet<MySlimBlock> blocks3,
            bool respectDeformationRatio, float detectionBlockHalfSize, ref MatrixD invWorldGrid)
        {
            blocks1.Clear();
            blocks2.Clear();
            blocks3.Clear();

            m_processedBlocks.Clear();

            BoundingBoxD box = new BoundingBoxD(sphere3.Center - new Vector3D(sphere3.Radius), sphere3.Center + new Vector3D(sphere3.Radius));

            box = box.Transform(ref invWorldGrid);
            Vector3D min = box.Min;
            Vector3D max = box.Max;
            Vector3D center;
            Vector3D.Transform(ref sphere3.Center, ref invWorldGrid, out center);

            Vector3I start = new Vector3I((int)Math.Round(min.X / GridSize), (int)Math.Round(min.Y / GridSize), (int)Math.Round(min.Z / GridSize));
            Vector3I end = new Vector3I((int)Math.Round(max.X / GridSize), (int)Math.Round(max.Y / GridSize), (int)Math.Round(max.Z / GridSize));

            Vector3I startIt = Vector3I.Min(start, end);
            Vector3I endIt = Vector3I.Max(start, end);

            var halfSize = new Vector3(detectionBlockHalfSize);
            var localSphere1 = new BoundingSphereD(center, sphere1.Radius);
            var localSphere2 = new BoundingSphereD(center, sphere2.Radius);
            var localSphere3 = new BoundingSphereD(center, sphere3.Radius);

            int cellsToCheck = (endIt.X - startIt.X) * (endIt.Y - startIt.Y) * (endIt.Z - startIt.Z);
            if (cellsToCheck < m_cubes.Count)
            {
                for (int i = startIt.X; i <= endIt.X; i++)
                {
                    for (int j = startIt.Y; j <= endIt.Y; j++)
                    {
                        for (int k = startIt.Z; k <= endIt.Z; k++)
                        {
                            if (m_cubes.ContainsKey(new Vector3I(i, j, k)))
                            {
                                var block = m_cubes[new Vector3I(i, j, k)].CubeBlock;

                                if (block.FatBlock != null && m_processedBlocks.Contains(block.FatBlock))
                                {
                                    continue;
                                }

                                m_processedBlocks.Add(block.FatBlock);

                                if (respectDeformationRatio)
                                {
                                    localSphere1.Radius = sphere1.Radius * block.DeformationRatio;
                                    localSphere2.Radius = sphere2.Radius * block.DeformationRatio;
                                    localSphere3.Radius = sphere3.Radius * block.DeformationRatio;
                                }

                                BoundingBox blockBox;

                                if (block.FatBlock != null)
                                {
                                    blockBox = new BoundingBox(block.Min * GridSize - GridSize / 2, block.Max * GridSize + GridSize / 2);
                                }
                                else
                                {
                                    blockBox = new BoundingBox(block.Position * GridSize - halfSize, block.Position * GridSize + halfSize);
                                }

                                if (blockBox.Intersects(localSphere1))
                                {
                                    blocks1.Add(block);
                                }
                                else
                                    if (blockBox.Intersects(localSphere2))
                                    {
                                        blocks2.Add(block);
                                    }
                                    else
                                        if (blockBox.Intersects(localSphere3))
                                        {
                                            blocks3.Add(block);
                                        }
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var cube in m_cubes.Values)
                {
                    var block = cube.CubeBlock;
                    if (block.FatBlock != null && m_processedBlocks.Contains(block.FatBlock))
                    {
                        continue;
                    }

                    m_processedBlocks.Add(block.FatBlock);

                    if (respectDeformationRatio)
                    {
                        localSphere1.Radius = sphere1.Radius * block.DeformationRatio;
                        localSphere2.Radius = sphere2.Radius * block.DeformationRatio;
                        localSphere3.Radius = sphere3.Radius * block.DeformationRatio;
                    }

                    BoundingBox blockBox;

                    if (block.FatBlock != null)
                    {
                        blockBox = new BoundingBox(block.Min * GridSize - GridSize / 2, block.Max * GridSize + GridSize / 2);
                    }
                    else
                    {
                        blockBox = new BoundingBox(block.Position * GridSize - halfSize, block.Position * GridSize + halfSize);
                    }

                    if (blockBox.Intersects(localSphere1))
                    {
                        blocks1.Add(block);
                    }
                    else
                        if (blockBox.Intersects(localSphere2))
                        {
                            blocks2.Add(block);
                        }
                        else
                            if (blockBox.Intersects(localSphere3))
                            {
                                blocks3.Add(block);
                            }
                }
            }

            m_processedBlocks.Clear();
        }

        /// <summary>
        /// Obtains all blocks intersected by raycast
        /// </summary>
        internal HashSet<MyCube> RayCastBlocksAll(Vector3 worldStart, Vector3 worldEnd)
        {
            RayCastCells(worldStart, worldEnd, m_cacheRayCastCells);
            HashSet<MyCube> result = new HashSet<MyCube>();
            foreach (var i in m_cacheRayCastCells)
            {
                if (m_cubes.ContainsKey(i))
                {
                    result.Add(m_cubes[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Obtains all blocks intersected by raycast
        /// </summary>
        internal List<MyCube> RayCastBlocksAllOrdered(Vector3 worldStart, Vector3 worldEnd)
        {
            RayCastCells(worldStart, worldEnd, m_cacheRayCastCells);
            List<MyCube> result = new List<MyCube>();
            foreach (var i in m_cacheRayCastCells)
            {
                if (m_cubes.ContainsKey(i) && !result.Contains(m_cubes[i]))
                {
                    result.Add(m_cubes[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Obtains position of first hit block.
        /// </summary>
        public Vector3I? RayCastBlocks(Vector3D worldStart, Vector3D worldEnd)
        {
            RayCastCells(worldStart, worldEnd, m_cacheRayCastCells);
            foreach (var i in m_cacheRayCastCells)
            {
                if (m_cubes.ContainsKey(i))
                {
                    return i;
                }
            }
            return null;
        }

        /// <summary>
        /// Obtains positions of grid cells regardless of whether these cells are taken up by blocks or not.
        /// </summary>
        public void RayCastCells(Vector3D worldStart, Vector3D worldEnd, List<Vector3I> outHitPositions, Vector3I? gridSizeInflate = null, bool havokWorld = false)
        {
            MatrixD invWorld = MatrixD.Invert(WorldMatrix);
            Vector3D localStart, localEnd;
            Vector3D.Transform(ref worldStart, ref invWorld, out localStart);
            Vector3D.Transform(ref worldEnd, ref invWorld, out localEnd);

            //We need move the line, because MyGridIntersection calculates the center of the box in the corner
            var offset = new Vector3D(GridSize * 0.5f);
            localStart += offset;
            localEnd += offset;

            var min = Min - Vector3I.One;
            var max = Max + Vector3I.One;
            if (gridSizeInflate.HasValue)
            {
                min -= gridSizeInflate.Value;
                max += gridSizeInflate.Value;
            }

            outHitPositions.Clear();
            if (havokWorld)
                MyGridIntersection.CalculateHavok(outHitPositions, GridSize, localStart, localEnd, min, max);
            else
                MyGridIntersection.Calculate(outHitPositions, GridSize, localStart, localEnd, min, max);
        }

        /// <summary>
        /// Obtains positions of static grid cells regardless of whether these cells are taken up by blocks or not. Usefull when placing block on voxel.
        /// </summary>
        public static void RayCastStaticCells(Vector3D worldStart, Vector3D worldEnd, List<Vector3I> outHitPositions, float gridSize, Vector3I? gridSizeInflate = null, bool havokWorld = false)
        {
            Vector3D localStart = worldStart;
            Vector3D localEnd = worldEnd;

            //We need move the line, because MyGridIntersection calculates the center of the box in the corner
            var offset = new Vector3D(gridSize * 0.5f);
            localStart += offset;
            localEnd += offset;

            var min = - Vector3I.One;
            var max = Vector3I.One;
            if (gridSizeInflate.HasValue)
            {
                min -= gridSizeInflate.Value;
                max += gridSizeInflate.Value;
            }

            outHitPositions.Clear();
            if (havokWorld)
                MyGridIntersection.CalculateHavok(outHitPositions, gridSize, localStart, localEnd, min, max);
            else
                MyGridIntersection.Calculate(outHitPositions, gridSize, localStart, localEnd, min, max);
        }


        ///// <summary>
        ///// Finds children which overlap box given by start and end (which do not have to be min and max).
        ///// Coordinates are given in grid coordinates (integer indices).
        ///// </summary>
        //private void GetOverlappedBlocks(Vector3I minI, Vector3I maxI, HashSet<MySlimBlock> outOverlappedBlocks)
        //{
        //    Debug.Assert(outOverlappedBlocks != null); 

        //    var tmp = new Vector3I();

        //    for (tmp.Z = minI.Z; tmp.Z <= maxI.Z; ++tmp.Z)
        //        for (tmp.Y = minI.Y; tmp.Y <= maxI.Y; ++tmp.Y)
        //            for (tmp.X = minI.X; tmp.X <= maxI.X; ++tmp.X)
        //            {
        //                var block = GetCubeBlock(tmp);
        //                if (block != null)
        //                    outOverlappedBlocks.Add(block);
        //            }
        //}

        #endregion

        #region Nested types

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MyBlockBuildArea
        {
            // 34B in total
            public DefinitionIdBlit DefinitionId; // 6B
            public uint ColorMaskHSV;
            public Vector3I PosInGrid; // Could be Vector3S, we'll save another 6B
            public Vector3B BlockMin;
            public Vector3B BlockMax;
            public Vector3UByte BuildAreaSize;
            public Vector3B StepDelta;
            public Base6Directions.Direction OrientationForward;
            public Base6Directions.Direction OrientationUp;
        }

        [ProtoContract]
        public struct MyBlockLocation
        {
            [ProtoMember]
            public Vector3I Min;

            [ProtoMember]
            public Vector3I Max; // Will be obsolete

            [ProtoMember]
            public Vector3I CenterPos; // Will be obsolete

            [ProtoMember]
            public Quaternion Orientation; // Will be different

            [ProtoMember]
            public long EntityId;

            [ProtoMember]
            public DefinitionIdBlit BlockDefinition;

            [ProtoMember]
            public long Owner;

            public MyBlockLocation(MyDefinitionId blockDefinition, Vector3I min, Vector3I max, Vector3I center, Quaternion orientation, long entityId, long owner)
            {
                BlockDefinition = blockDefinition;
                Min = min;
                Max = max;
                CenterPos = center;
                Orientation = orientation;
                EntityId = entityId;
                Owner = owner;
            }
        }

        private enum NeighborOffsetIndex
        {
            XUP = 0,
            XDOWN = 1,
            YUP = 2,
            YDOWN = 3,
            ZUP = 4,
            ZDOWN = 5,
            XUP_YUP = 6,
            XUP_YDOWN = 7,
            XDOWN_YUP = 8,
            XDOWN_YDOWN = 9,
            YUP_ZUP = 10,
            YUP_ZDOWN = 11,
            YDOWN_ZUP = 12,
            YDOWN_ZDOWN = 13,
            XUP_ZUP = 14,
            XUP_ZDOWN = 15,
            XDOWN_ZUP = 16,
            XDOWN_ZDOWN = 17,
            XUP_YUP_ZUP = 18,
            XUP_YUP_ZDOWN = 19,
            XUP_YDOWN_ZUP = 20,
            XUP_YDOWN_ZDOWN = 21,
            XDOWN_YUP_ZUP = 22,
            XDOWN_YUP_ZDOWN = 23,
            XDOWN_YDOWN_ZUP = 24,
            XDOWN_YDOWN_ZDOWN = 25
        }

        public enum MyIntegrityChangeEnum
        {
            Damage,
            ConstructionBegin,
            ConstructionEnd,
            ConstructionProcess
        }

        private struct MyNeighbourCachedBlock
        {
            public Vector3I Position;
            public MyCubeBlockDefinition BlockDefinition;
            public MyBlockOrientation Orientation;

            public override int GetHashCode()
            {
                return Position.GetHashCode();
            }
        }

        /// <summary>
        /// Used when calculating index of added block. Might not be count of
        /// blocks since removal of a block does not decrement this. Key is numerical
        /// ID of cube block definition.
        /// </summary>
        public class BlockTypeCounter
        {
            private Dictionary<MyDefinitionId, int> m_countById = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);

            internal int GetNextNumber(MyDefinitionId blockType)
            {
                int idCounter = 0;
                m_countById.TryGetValue(blockType, out idCounter);
                ++idCounter;
                m_countById[blockType] = idCounter;
                return idCounter;
            }
        }

        #endregion

        void IMyGridConnectivityTest.GetConnectedBlocks(Vector3I minI, Vector3I maxI, Dictionary<Vector3I, ConnectivityResult> outOverlappedCubeBlocks)
        {
            Debug.Assert(outOverlappedCubeBlocks != null);

            var tmp = new Vector3I();

            for (tmp.Z = minI.Z; tmp.Z <= maxI.Z; ++tmp.Z)
                for (tmp.Y = minI.Y; tmp.Y <= maxI.Y; ++tmp.Y)
                    for (tmp.X = minI.X; tmp.X <= maxI.X; ++tmp.X)
                    {
                        var block = GetCubeBlock(tmp);
                        if (block != null)
                            outOverlappedCubeBlocks[block.Position] = new ConnectivityResult() { Definition = block.BlockDefinition, FatBlock = block.FatBlock, Orientation = block.Orientation, Position = block.Position };
                    }


        }

        private string MakeCustomName()
        {
            StringBuilder output = new StringBuilder();
            int mod = 10000;
            long prefix = MyMath.Mod(EntityId, mod);

            string name = null;
            if (IsStatic)
            {
                MyTexts.TryGet(MySpaceTexts.CustomShipName_Platform, out name);
            }
            else
            {
                switch (GridSizeEnum)
                {
                    case Common.ObjectBuilders.MyCubeSize.Small: MyTexts.TryGet(MySpaceTexts.CustomShipName_SmallShip, out name); break;
                    case Common.ObjectBuilders.MyCubeSize.Large: MyTexts.TryGet(MySpaceTexts.CustomShipName_LargeShip, out name); break;
                }
            }
            output.Append(name ?? "Grid").Append(" ").Append(prefix.ToString());
            return output.ToString();
        }

        public void ChangeOwner(MyCubeBlock block, long oldOwner, long newOwner)
        {
            if (!MyFakes.ENABLE_TERMINAL_PROPERTIES)
                return;

            m_ownershipManager.ChangeBlockOwnership(block, oldOwner, newOwner);

        }

        public void RecalculateOwners()
        {
            if (!MyFakes.ENABLE_TERMINAL_PROPERTIES)
                return;

            m_ownershipManager.RecalculateOwners();
        }

        public void UpdateOwnership(long ownerId, bool isFunctional)
        {
            if (!MyFakes.ENABLE_TERMINAL_PROPERTIES)
                return;

            m_ownershipManager.UpdateOnFunctionalChange(ownerId, isFunctional);
        }

        class MyCubeGridPosition : MyPositionComponent
        {
            MyCubeGrid m_grid;

            public override void OnAddedToContainer()
            {
                base.OnAddedToContainer();
                m_grid = Container.Entity as MyCubeGrid;
            }

            public override BoundingBox LocalAABBHr
            {
                get
                {
                    Matrix worldInv = WorldMatrixNormalizedInv;

                    BoundingBox localAABBHr = LocalAABB;

                    foreach (var node in MyCubeGridGroups.Static.Logical.GetGroup(m_grid).Nodes)
                    {
                        if (node.NodeData != m_grid)
                        {
                            BoundingBox box = node.NodeData.PositionComp.LocalAABB.Transform((Matrix)(node.NodeData.PositionComp.WorldMatrix * worldInv));
                            localAABBHr = localAABBHr.Include(box);
                        }
                    }

                    return localAABBHr;
                }
            }

        }

        public MyFracturedBlock CreateFracturedBlock(MyObjectBuilder_FracturedBlock fracturedBlockBuilder, Vector3I position)
        {
            ProfilerShort.Begin("CreateFractureBlockBuilder");
            var defId = new Definitions.MyDefinitionId(typeof(MyObjectBuilder_FracturedBlock), "FracturedBlockLarge");
            var def = Sandbox.Definitions.MyDefinitionManager.Static.GetCubeBlockDefinition(defId);
            var bPos = position;
            MyCube oldBlock;
            if (m_cubes.TryGetValue(bPos, out oldBlock))
                RemoveBlockInternal(oldBlock.CubeBlock, close: true);

            var sb = AddBlock(fracturedBlockBuilder, false); //BuildBlock(def, Vector3.Zero, bPos, Quaternion.Identity, 0, 0, null);
            System.Diagnostics.Debug.Assert(sb != null, "Error created fractured block!");
            if (sb != null)
            {
                var fb = sb.FatBlock as MyFracturedBlock;
              

                fb.Render.UpdateRenderObject(true);
                ProfilerShort.End();
                UpdateBlockNeighbours(fb.SlimBlock);

                return fb;
            }

            return null;
        }

        private MyFracturedBlock CreateFracturedBlock(MyFracturedBlock.Info info)// Havok.HkdBreakableShape shape, Vector3I pos)
        {
            ProfilerShort.Begin("CreateFractureBlock");
            var defId = new Definitions.MyDefinitionId(typeof(MyObjectBuilder_FracturedBlock), "FracturedBlockLarge");
            var def = Sandbox.Definitions.MyDefinitionManager.Static.GetCubeBlockDefinition(defId);
            var bPos = info.Position;// m_grid.WorldToGridInteger(worldMatrix.Translation);
            MyCube oldBlock;
            if (m_cubes.TryGetValue(bPos, out oldBlock))
                RemoveBlock(oldBlock.CubeBlock, false);

            var blockObjectBuilder = MyCubeGrid.CreateBlockObjectBuilder(def, bPos, new MyBlockOrientation(ref  Quaternion.Identity), 0, 0, true);
            blockObjectBuilder.ColorMaskHSV = Vector3.Zero;

            var sb = AddBlock(blockObjectBuilder, false); //BuildBlock(def, Vector3.Zero, bPos, Quaternion.Identity, 0, 0, null);
            if (sb == null)
            {
                Debug.Fail("Fracture block not added");
                info.Shape.RemoveReference();
                ProfilerShort.End();
                return null;
            }
            var fb = sb.FatBlock as MyFracturedBlock;
            fb.OriginalBlocks = info.OriginalBlocks;
            fb.Orientations = info.Orientations;

            ProfilerShort.Begin("SetRenderData");
            fb.SetDataFromHavok(info.Shape, info.Compound);
            ProfilerShort.End();

            fb.Render.UpdateRenderObject(true);
            ProfilerShort.End();
            UpdateBlockNeighbours(fb.SlimBlock);

            if (Sync.IsServer)
            {
                MySyncDestructions.CreateFracturedBlock((MyObjectBuilder_FracturedBlock)fb.GetObjectBuilderCubeBlock(), this.EntityId, bPos);
            }

            return fb;
        }

        bool m_generatorsEnabled = true;
        public void EnableGenerators(bool enable, bool fromServer = false)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer || fromServer, "Cannot change this on client");

            if (Sync.IsServer || fromServer)
            {

                if (m_generatorsEnabled != enable)
                {
                    AdditionalModelGenerators.ForEach(g => g.EnableGenerator(enable));
                    m_generatorsEnabled = enable;

                    //if (Sync.IsServer)
                    //    MySyncDestructions.EnableGenerators(this, enable);
                }
            }
        }

        // Returns generating block (not compound block, but block inside) from generated one.
        public MySlimBlock GetGeneratingBlock(MySlimBlock generatedBlock)
        {
			if (generatedBlock == null)
				return null;

            foreach (var generator in AdditionalModelGenerators) 
            {
                var generatingBlock = generator.GetGeneratingBlock(generatedBlock);
                if (generatingBlock != null)
                    return generatingBlock;
            }

            return null;
        }
    }
}
