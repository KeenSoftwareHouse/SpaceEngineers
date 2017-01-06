#region Using

using Havok;
using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
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
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Sandbox.Game.EntityComponents;
using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Game.Replication;
using Sandbox.Game.GameSystems.CoordinateSystem;
using VRage.Game.Entity;
using VRage.Game.Models;
using Sandbox.Game.Weapons;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Game.Gui;
using Sandbox.Game.SessionComponents.Clipboard;
using VRage.Audio;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using ParallelTasks;
using VRage.Game.Entity.EntityComponents;
using VRage.Profiler;
using VRage.Sync;
using VRage.Library.Collections;

#endregion

namespace Sandbox.Game.Entities
{
    class MySyncGridThrustState
    {
        public Vector3B LastSendState;
        public int SleepFrames = 0;

        public bool ShouldSend(Vector3B newThrust)
        {
            if (SleepFrames > 4 && LastSendState != newThrust)
            {
                SleepFrames = 0;
                LastSendState = newThrust;
                return true;
            }
            else
            {
                SleepFrames++;
                return false;
            }
        }
    }
    /// <summary>
    /// Grid - small ship, large ship, station
    /// Cubes (armor, walls...) are merge and rendered by this entity
    /// Blocks (turret, thrusts...) are rendered as child entities
    /// </summary>
    [StaticEventOwner]
    [MyEntityType(typeof(MyObjectBuilder_CubeGrid))]
    public partial class MyCubeGrid : MyEntity, IMyGridConnectivityTest, IMyEventProxy
    {
        static MyCubeGridHitInfo m_hitInfoTmp;

        static HashSet<MyCubeGrid.MyBlockLocation> m_tmpBuildList = new HashSet<MyCubeGrid.MyBlockLocation>();
        static List<Vector3I> m_tmpPositionListReceive = new List<Vector3I>();
        static List<Vector3I> m_tmpPositionListSend = new List<Vector3I>();

        private List<Vector3I> m_removeBlockQueueWithGenerators = new List<Vector3I>();
        private List<Vector3I> m_removeBlockQueueWithoutGenerators = new List<Vector3I>();

        private List<Vector3I> m_destroyBlockQueue = new List<Vector3I>();
        private List<Vector3I> m_destructionDeformationQueue = new List<Vector3I>();

        private List<MyCubeGrid.BlockPositionId> m_destroyBlockWithIdQueueWithGenerators = new List<MyCubeGrid.BlockPositionId>();
        private List<MyCubeGrid.BlockPositionId> m_destroyBlockWithIdQueueWithoutGenerators = new List<MyCubeGrid.BlockPositionId>();

        private List<MyCubeGrid.BlockPositionId> m_removeBlockWithIdQueueWithGenerators = new List<MyCubeGrid.BlockPositionId>();
        private List<MyCubeGrid.BlockPositionId> m_removeBlockWithIdQueueWithoutGenerators = new List<MyCubeGrid.BlockPositionId>();
        static List<byte> m_boneByteList = new List<byte>();
        private List<long> m_tmpBlockIdList = new List<long>();

        Vector3 m_gravity = Vector3.Zero;
        private MySyncGridThrustState m_thrustState = new MySyncGridThrustState();

        public event Action<SyncBase> SyncPropertyChanged
        {
            add { SyncType.PropertyChanged += value; }
            remove { SyncType.PropertyChanged -= value; }
        }

        public readonly SyncType SyncType;

        readonly Sync<bool> m_handBrakeSync;

        static List<MyObjectBuilder_CubeGrid> m_recievedGrids = new List<MyObjectBuilder_CubeGrid>();

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

        /// <summary>
        /// Value used by MoveCornerBones() to precalculate the bones displacement distance.
        /// A performance boost is gained because we can then avoid having to use Vector3.Length() which means we don't need sqrt() each time.
        /// </summary>
        private static float m_precalculatedCornerBonesDisplacementDistance = 0;

        internal MyVoxelSegmentation BonesToSend = new MyVoxelSegmentation();
        private int m_bonesSendCounter = 0;

        private MyDirtyRegion m_dirtyRegion = new MyDirtyRegion();
        private int m_updateDirtyCounter;
        private MyCubeSize m_gridSizeEnum;
        private Vector3I m_min = Vector3I.MaxValue;
        private Vector3I m_max = Vector3I.MinValue;
        private readonly Dictionary<Vector3I, MyCube> m_cubes = new Dictionary<Vector3I, MyCube>(1024);

        /// <summary>
        /// This caches if grid can have physics, once set to false, it stays false and grid is eventually closed.
        /// </summary>
        private bool m_canHavePhysics = true;
        private readonly HashSet<MySlimBlock> m_cubeBlocks = new HashSet<MySlimBlock>();
        private List<MyCubeBlock> m_fatBlocks = new List<MyCubeBlock>(100);
        private MyLocalityGrouping m_explosions = new MyLocalityGrouping(MyLocalityGrouping.GroupingMode.Overlaps);

        public HashSetReader<Vector3I> DirtyBlocks { get { return new HashSetReader<Vector3I>(m_dirtyRegion.Cubes); } }

        public MyCubeGridRenderData RenderData { get { return Render.RenderData; } }

        private HashSet<MyCubeBlock> m_processedBlocks = new HashSet<MyCubeBlock>();
        private HashSet<MyCubeBlock> m_blocksForDraw = new HashSet<MyCubeBlock>();
        private List<MyCubeGrid> m_tmpGrids = new List<MyCubeGrid>();
        public HashSet<MyCubeBlock> BlocksForDraw { get { return m_blocksForDraw; } }
        private bool m_disconnectsDirty;
        private bool m_blocksForDamageApplicationDirty;
        private bool m_boundsDirty = false;
        private int m_lastUpdatedDirtyBounds = 0;
        private HashSet<MySlimBlock> m_blocksForDamageApplication = new HashSet<MySlimBlock>();
        private List<MySlimBlock> m_blocksForDamageApplicationCopy = new List<MySlimBlock>();
        internal MyStructuralIntegrity StructuralIntegrity { get; private set; }

        private HashSet<Vector3UByte> m_tmpBuildFailList = new HashSet<Vector3UByte>();
        private List<Vector3UByte> m_tmpBuildOffsets = new List<Vector3UByte>();
        private List<MySlimBlock> m_tmpBuildSuccessBlocks = new List<MySlimBlock>();

        static List<Vector3I> m_tmpBlockPositions = new List<Vector3I>();
        static List<MySlimBlock> m_tmpBlockListReceive = new List<MySlimBlock>();

        public bool IsSplit { get; set; }

        [ThreadStatic]
        private static List<MyCockpit> m_tmpOccupiedCockpitsPerThread;
        [ThreadStatic]
        private static List<MyObjectBuilder_BlockGroup> m_tmpBlockGroupsPerThread;

        private static List<MyCockpit> m_tmpOccupiedCockpits { get { return MyUtils.Init(ref m_tmpOccupiedCockpitsPerThread); } }
        private static List<MyObjectBuilder_BlockGroup> m_tmpBlockGroups { get { return MyUtils.Init(ref m_tmpBlockGroupsPerThread); } }

        public List<IMyBlockAdditionalModelGenerator> AdditionalModelGenerators { get { return Render.AdditionalModelGenerators; } }
        public bool HasShipSoundEvents = false;
        public int NumberOfReactors = 0;
        public float GridGeneralDamageModifier = 1f;

        internal MyGridSkeleton Skeleton;
        public readonly BlockTypeCounter BlockCounter = new BlockTypeCounter();

        public MyCubeGridSystems GridSystems { get; private set; }

        public Dictionary<MyObjectBuilderType, int> BlocksCounters = new Dictionary<MyObjectBuilderType, int>();

        private const float m_gizmoMaxDistanceFromCamera = 100.0f;
        private const float m_gizmoDrawLineScale = 0.002f;

        private bool m_isStatic;
        public bool IsStatic
        {
            get { return m_isStatic; }
            private set
            {
                if (m_isStatic != value)
                {
                    m_isStatic = value;
                    NotifyIsStaticChanged(m_isStatic);
                }
            }
        }

        public float GridSize { get; private set; }
        public float GridScale { get; private set; } // PARODY
        public float GridSizeHalf { get; private set; }
        public Vector3 GridSizeHalfVector { get; private set; }

        /// <summary>
        /// Reciprocal of gridsize
        /// </summary>
        public float GridSizeR { get; private set; }
        public Vector3I Min { get { return m_min; } }
        public Vector3I Max { get { return m_max; } }

        public Vector3I? XSymmetryPlane = null;
        public Vector3I? YSymmetryPlane = null;
        public Vector3I? ZSymmetryPlane = null;
        public bool XSymmetryOdd = false;
        public bool YSymmetryOdd = false;
        public bool ZSymmetryOdd = false;

        /// <summary>
        /// Indicates if a grid coresponds to a respawn ship/cart.
        /// </summary>
        private readonly Sync<bool> m_isRespawnGrid;

        /// <summary>
        /// Gets or sets indication if a grid coresponds to a respawn ship/cart.
        /// </summary>
        public bool IsRespawnGrid { get { return m_isRespawnGrid; } set { m_isRespawnGrid.Value = value; } }

        /// <summary>
        /// Grid play time with player. Used by respawn ship. 
        /// </summary>
        public int m_playedTime;

        public bool ControlledFromTurret = false;

        private readonly Sync<bool> m_destructibleBlocks;

        // Used for UI & Sync
        public bool DestructibleBlocks
        {
            get
            {
                return m_destructibleBlocks;
            }
            set
            {
                m_destructibleBlocks.Value = value;
            }
        }

        private Sync<bool> m_editable;
        //Defines if blocks can be build/removed from this grid
        //CubeBuilder reads this flag and controls the building Add/Remove block functions still work
        //TODO: unify Add/Remove block calls and integrate this flag, beware destroyed blocks can use RemoveBlock too
        public bool Editable
        {
            get { return m_editable; } 
            set { m_editable.ValidateAndSet(value); }
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
        internal MyCubeGridOwnershipManager m_ownershipManager;

        public Sandbox.Game.Entities.Blocks.MyProjectorBase Projector;

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
                GridSizeHalf = GridSize / 2;
                GridSizeHalfVector = new Vector3(GridSizeHalf);
                GridSizeR = 1 / GridSize;
            }
        }

        public new MyGridPhysics Physics { get { return (MyGridPhysics)base.Physics; } set { base.Physics = value; } }

        //public int HavokCollisionSystemID { get; private set; }

        public event Action<MySlimBlock> OnBlockAdded;
        public event Action<MySlimBlock> OnBlockRemoved;
        public event Action<MySlimBlock> OnBlockIntegrityChanged;
        public event Action<MySlimBlock> OnBlockClosed;
        public event Action<MyCubeGrid> OnAuthorshipChanged;

        internal void NotifyBlockAdded(MySlimBlock block)
        {
            if (OnBlockAdded != null)
                OnBlockAdded(block);

            GridSystems.OnBlockAdded(block);
        }

        internal void NotifyBlockRemoved(MySlimBlock block)
        {
            if (OnBlockRemoved != null)
                OnBlockRemoved(block);

            if (MyVisualScriptLogicProvider.BlockDestroyed != null)
                MyVisualScriptLogicProvider.BlockDestroyed(block.FatBlock != null ? block.FatBlock.Name : string.Empty, Name, block.BlockDefinition.Id.TypeId.ToString(), block.BlockDefinition.Id.SubtypeName);

            MyCubeGrids.NotifyBlockDestroyed(this, block);

            GridSystems.OnBlockRemoved(block);
        }

        internal void NotifyBlockClosed(MySlimBlock block)
        {
            if (OnBlockClosed != null)
                OnBlockClosed(block);
        }

        internal void NotifyBlockIntegrityChanged(MySlimBlock block)
        {
            if (OnBlockIntegrityChanged != null)
                OnBlockIntegrityChanged(block);

            GridSystems.OnBlockIntegrityChanged(block);
        }

        //Called when ownership recalculation is actually done
        public event Action<MyCubeGrid> OnBlockOwnershipChanged;
        internal void NotifyBlockOwnershipChange(MyCubeGrid cubeGrid)
        {
            if (OnBlockOwnershipChanged != null)
                OnBlockOwnershipChanged(cubeGrid);

            GridSystems.OnBlockOwnershipChanged(cubeGrid);
        }

        public event Action<bool> OnIsStaticChanged;
        internal void NotifyIsStaticChanged(bool newIsStatic)
        {
            if (OnIsStaticChanged != null)
                OnIsStaticChanged(newIsStatic);
        }

        public event Action<MyCubeGrid, MyCubeGrid> OnGridSplit;

        internal event Action<MyGridLogicalGroupData> AddedToLogicalGroup;
        internal event Action RemovedFromLogicalGroup;

        public event Action<int> OnHavokSystemIDChanged;//jn: not nice

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

        private static readonly HashSet<MyResourceSinkComponent> m_tmpSinks = new HashSet<MyResourceSinkComponent>();

        private static List<LocationIdentity> m_tmpLocationsAndIdsSend = new List<LocationIdentity>();
        private static List<Tuple<Vector3I, ushort>> m_tmpLocationsAndIdsReceive = new List<Tuple<Vector3I, ushort>>();

        private bool m_smallToLargeConnectionsInitialized = false;
        internal bool SmallToLargeConnectionsInitialized { get { return m_smallToLargeConnectionsInitialized; } }
        private bool m_enableSmallToLargeConnections = true;
        internal bool EnableSmallToLargeConnections { get { return m_enableSmallToLargeConnections; } }

        // Flag if SI should check connectivity of the grid
        // PM: It got more advanced when started to be used to test dynamic generally
        internal enum MyTestDynamicReason
        {
            NoReason,
            GridCopied,
            GridSplit
        }
        internal MyTestDynamicReason TestDynamic = MyTestDynamicReason.NoReason;

        internal new MyRenderComponentCubeGrid Render
        {
            get { return (MyRenderComponentCubeGrid)base.Render; }
            set { base.Render = value; }
        }

        // Flag if the world matrix has been changed this frame (flag cleared in UpdateAfterSimulation)
        private bool m_worldPositionChanged;
        // Flag if the grid has any model generators (for performance reasons)
        private bool m_hasAdditionalModelGenerators;

        public MyTerminalBlock MainCockpit = null;
        public MyTerminalBlock MainRemoteControl = null;

        // Cached map from multiblock id to info, not saved.
        private Dictionary<int, MyCubeGridMultiBlockInfo> m_multiBlockInfos;

        /// <summary>
        /// Local coord system under which this cube exists. (its id)
        /// </summary>
        public long LocalCoordSystem { get; set; }

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

        public bool HasMainRemoteControl()
        {
            return MainRemoteControl != null;
        }

        public bool IsMainRemoteControl(MyTerminalBlock remoteControl)
        {
            return MainRemoteControl == remoteControl;
        }
        public void SetMainRemoteControl(MyTerminalBlock remoteControl)
        {
            MainRemoteControl = remoteControl;
        }

        /// <summary>
        /// Return how much fat blocks defined by T is pressent in grid.
        /// </summary>
        /// <typeparam name="T">Type of Fatblock</typeparam>
        /// <returns></returns>
        public int GetFatBlockCount<T>() where T : MyCubeBlock
        {
            int count = 0;
            foreach (var block in GetFatBlocks())
                if (block is T)
                    count++;

            return count;
        }

        public MyCubeGrid() :
            this(MyCubeSize.Large)
        {
            GridScale = 1;
            Render = new MyRenderComponentCubeGrid();
            Render.NeedsDraw = true;

            PositionComp = new MyCubeGridPosition();

            Hierarchy.QueryAABBImpl = QueryAABB;
            Hierarchy.QuerySphereImpl = QuerySphere;
            Hierarchy.QueryLineImpl = QueryLine;

            Components.Add(new MyGridTargeting());

#if !XB1 // !XB1_SYNC_NOREFLECTION
            SyncType = SyncHelpers.Compose(this);
#else // XB1
            SyncType = new SyncType(new List<SyncBase>());
            m_handBrakeSync = SyncType.CreateAndAddProp<bool>();
            m_isRespawnGrid = SyncType.CreateAndAddProp<bool>();
            m_destructibleBlocks = SyncType.CreateAndAddProp<bool>();
#endif // XB1

            m_handBrakeSync.ValueChanged +=  (x)=> HandBrakeChanged();
        }

        private MyCubeGrid(MyCubeSize gridSize)
        {
            GridScale = 1;
            GridSizeEnum = gridSize;
            GridSize = MyDefinitionManager.Static.GetCubeSize(gridSize);
            GridSizeHalf = GridSize / 2;
            GridSizeHalfVector = new Vector3(GridSizeHalf);
            GridSizeR = 1 / GridSize;
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME; //because of debug draw

            Skeleton = new MyGridSkeleton();

            GridCounter++;

            AddDebugRenderComponent(new MyDebugRenderComponentCubeGrid(this));

            if (MyPerGameSettings.Destruction)
            {
                OnPhysicsChanged += delegate(MyEntity entity)
                {
                    MyPhysics.RemoveDestructions(entity);
                };
            }

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
            // parody
            var builder = (MyObjectBuilder_CubeGrid)objectBuilder;
            if (builder != null)
                GridSizeEnum = builder.GridSizeEnum;

            GridScale = MyDefinitionManager.Static.GetCubeSize(GridSizeEnum) / MyDefinitionManager.Static.GetCubeSizeOriginal(GridSizeEnum);
            base.Init(objectBuilder);

            Init(null, null, null, null, null);
            m_destructibleBlocks.Value = builder.DestructibleBlocks;

            if (MyFakes.ASSERT_NON_PUBLIC_BLOCKS)
                AssertNonPublicBlocks(builder);

            if (MyFakes.REMOVE_NON_PUBLIC_BLOCKS)
                RemoveNonPublicBlocks(builder);

            Render.CreateAdditionalModelGenerators(builder != null ? builder.GridSizeEnum : MyCubeSize.Large);
            m_hasAdditionalModelGenerators = AdditionalModelGenerators.Count > 0;

            CreateSystems();

            if (builder != null)
            {
                IsStatic = builder.IsStatic;
                CreatePhysics = builder.CreatePhysics;
                m_enableSmallToLargeConnections = builder.EnableSmallToLargeConnections;
                GridSizeEnum = builder.GridSizeEnum;
                Editable = builder.Editable;

                GridSystems.BeforeBlockDeserialization(builder);

                m_cubes.Clear();
                m_cubeBlocks.Clear();
                m_fatBlocks.Clear();

                m_tmpOccupiedCockpits.Clear();

                for (int i = 0; i<builder.CubeBlocks.Count; i++)
                {
                    MyObjectBuilder_CubeBlock cubeBlock = builder.CubeBlocks[i];

                    Debug.Assert(cubeBlock.IntegrityPercent > 0.0f, "Block is in inconsistent state in grid initialization");
                    var block = AddBlock(cubeBlock, false);
                    //Debug.Assert(block != null, "Block was not added");

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
                    if (MyPerGameSettings.InventoryMass)
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

                if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
                {
                    m_ownershipManager = new MyCubeGridOwnershipManager();
                    m_ownershipManager.Init(this);
                }

                if (Hierarchy != null)
                    Hierarchy.OnChildRemoved += Hierarchy_OnChildRemoved;
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
            //UpdateDirty();

            if (MyFakes.ENABLE_MULTIBLOCK_PART_IDS)
            {
                PrepareMultiBlockInfos();
            }

            IsRespawnGrid = builder.IsRespawnGrid;
            m_playedTime = builder.playedTime;
            GridGeneralDamageModifier = builder.GridGeneralDamageModifier;
            LocalCoordSystem = builder.LocalCoordSys;
        }

        void Hierarchy_OnChildRemoved(IMyEntity obj)
        {
            m_fatBlocks.Remove(obj as MyCubeBlock);
        }

        private static MyCubeGrid CreateForSplit(MyCubeGrid originalGrid, long newEntityId)
        {
            var builder = MyObjectBuilderSerializer.CreateNewObject(typeof(MyObjectBuilder_CubeGrid)) as MyObjectBuilder_CubeGrid;
            if (builder == null)
            {
                Debug.Fail("CreateForSplit builder shouldn't be null! Original Grid info: " + originalGrid.ToString());
                MyLog.Default.WriteLine("CreateForSplit builder shouldn't be null! Original Grid info: " + originalGrid.ToString());
                return null;
            }
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
                if (blocks.Count <= i)
                {
                    continue;
                }
                var block = blocks[i];
                if (block == null)
                    continue;

                if (block.FatBlock != null)
                    originalGrid.Hierarchy.RemoveChild(block.FatBlock);

                bool oldEnabled = originalGrid.EnableGenerators(false, true);
                originalGrid.RemoveBlockInternal(block, close: true, markDirtyDisconnects: false);
                originalGrid.EnableGenerators(oldEnabled, true);

                originalGrid.Physics.AddDirtyBlock(block);
            }
            ProfilerShort.End();

            originalGrid.RemoveEmptyBlockGroups();

            if (sync == true)
            {
                Debug.Assert(Sync.IsServer);
                if (!Sync.IsServer) return;

                originalGrid.AnnounceRemoveSplit(blocks);
                return;
            }
        }

        public MyCubeGrid SplitByPlane(PlaneD plane)
        {
            m_tmpSlimBlocks.Clear();
            MyCubeGrid grid = null;

            PlaneD localPlane = PlaneD.Transform(plane, PositionComp.WorldMatrixNormalizedInv);
            foreach (var block in GetBlocks())
            {
                BoundingBoxD box = new BoundingBoxD(block.Min * GridSize, block.Max * GridSize);
                box.Inflate(GridSize/2);
                if (box.Intersects(localPlane) == PlaneIntersectionType.Back)
                    m_tmpSlimBlocks.Add(block);
            }

            if (m_tmpSlimBlocks.Count != 0)
            {
                grid = CreateSplit(this, m_tmpSlimBlocks);
                m_tmpSlimBlocks.Clear();
            }

            return grid;
        }

        public static MyCubeGrid CreateSplit(MyCubeGrid originalGrid, List<MySlimBlock> blocks, bool sync = true, long newEntityId = 0)
        {
            ProfilerShort.Begin("Init grid");
            var newGrid = MyCubeGrid.CreateForSplit(originalGrid, newEntityId);

            ProfilerShort.End();

            if (newGrid == null) return null;

            Vector3 oldCenterOfMass = originalGrid.Physics.CenterOfMassWorld;

            MyEntities.Add(newGrid);
            MyCubeGrid.MoveBlocks(originalGrid, newGrid, blocks, 0, blocks.Count);
            newGrid.RebuildGrid();
            if (!newGrid.IsStatic)
            {
                newGrid.SetInventoryMassDirty();
                newGrid.Physics.UpdateMass();
            }

            if (originalGrid.IsStatic)
            {
                newGrid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;
                //GR: Always testing dynamic for original grid (can be any of the 2 grids)
                //if (!MySession.Static.EnableConvertToStation)
                originalGrid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;
            }

            newGrid.Physics.AngularVelocity = originalGrid.Physics.AngularVelocity;
            newGrid.Physics.LinearVelocity = originalGrid.Physics.GetVelocityAtPoint(newGrid.Physics.CenterOfMassWorld);

            // CH: TODO: (Optimization) recalculate the original grid only when all splits are done. This will have to be synced by extra message
            originalGrid.UpdatePhysicsShape();
            if (!originalGrid.IsStatic)
            {
                originalGrid.SetInventoryMassDirty();
                originalGrid.Physics.UpdateMass();
            }
            Vector3 velocityAtNewCOM = Vector3.Cross(originalGrid.Physics.AngularVelocity, originalGrid.Physics.CenterOfMassWorld - oldCenterOfMass);
            originalGrid.Physics.LinearVelocity = originalGrid.Physics.LinearVelocity + velocityAtNewCOM;

            if (originalGrid.OnGridSplit != null)
            {
                originalGrid.OnGridSplit(originalGrid, newGrid);
            }

            if (sync == true)
            {
                Debug.Assert(Sync.IsServer);
                if (!Sync.IsServer) return newGrid;

                m_tmpBlockPositions.Clear();
                foreach (var block in blocks)
                {
                    m_tmpBlockPositions.Add(block.Position);
                }
                MyMultiplayer.RemoveForClientIfIncomplete(originalGrid);
                MyMultiplayer.RaiseEvent(originalGrid, x => x.CreateSplit_Implementation, m_tmpBlockPositions, newGrid.EntityId);
            }

            return newGrid;
        }

        [Event, Reliable, Broadcast]
        public void CreateSplit_Implementation(List<Vector3I> blocks, long newEntityId)
        {
            m_tmpBlockListReceive.Clear();
            foreach (var position in blocks)
            {
                var block = GetCubeBlock(position);
                Debug.Assert(block != null, "Block was null when trying to create a grid split. Desync?");
                if (block == null)
                {
                    MySandboxGame.Log.WriteLine("Block was null when trying to create a grid split. Desync?");
                    continue;
                }

                m_tmpBlockListReceive.Add(block);
            }

            MyCubeGrid.CreateSplit(this, m_tmpBlockListReceive, sync: false, newEntityId: newEntityId);
            m_tmpBlockListReceive.Clear();
        }

        /// <summary>
        /// SplitBlocks list can contain null when received from network
        /// </summary>
        public static void CreateSplits(MyCubeGrid originalGrid, List<MySlimBlock> splitBlocks, List<MyDisconnectHelper.Group> groups, bool sync = true)
        {
            if (originalGrid == null || originalGrid.Physics == null || groups == null || splitBlocks == null)
            {
                return;
            }

            Vector3 oldCenterOfMass = originalGrid.Physics.CenterOfMassWorld;

            try
            {
                if (MyCubeGridSmallToLargeConnection.Static != null)
                {
                    ProfilerShort.Begin("BeforeGridSplit_SmallToLargeGridConnetivity");
                    MyCubeGridSmallToLargeConnection.Static.BeforeGridSplit_SmallToLargeGridConnectivity(originalGrid);
                    ProfilerShort.End();
                }

                // Create new grids, move blocks
                ProfilerShort.Begin("Create grids and move");
                var array = groups.GetInternalArray();
                for (int i = 0; i < groups.Count; i++)
                {
                    CreateSplitForGroup(originalGrid, splitBlocks, ref array[i]);
                }
                ProfilerShort.End();

                // Update old grid shape
                ProfilerShort.Begin("Update original grid shape");
                originalGrid.UpdatePhysicsShape();
                ProfilerShort.End();

                // Rebuild new grids
                foreach (var newGrid in originalGrid.m_tmpGrids)
                {
                    ProfilerShort.Begin("Update new grid shape");
                    newGrid.RebuildGrid();
                    if (originalGrid.IsStatic)
                    {
                        if (!MySession.Static.Settings.StationVoxelSupport)
                        {
                            newGrid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;
                            originalGrid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;
                        }
                    }

                    newGrid.Physics.AngularVelocity = originalGrid.Physics.AngularVelocity;
                    newGrid.Physics.LinearVelocity = originalGrid.Physics.GetVelocityAtPoint(newGrid.Physics.CenterOfMassWorld);
                    ProfilerShort.End();
                }

                // Update old grid velocity
                Vector3 velocityAtNewCOM = Vector3.Cross(originalGrid.Physics.AngularVelocity, originalGrid.Physics.CenterOfMassWorld - oldCenterOfMass);
                originalGrid.Physics.LinearVelocity = originalGrid.Physics.LinearVelocity + velocityAtNewCOM;

                if (MyCubeGridSmallToLargeConnection.Static != null)
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

                foreach (var newGrid in originalGrid.m_tmpGrids)
                {
                    newGrid.GridSystems.UpdatePower();
                }

                if (sync)
                {
                    Debug.Assert(Sync.IsServer);
                    if (!Sync.IsServer) return;

                    MyMultiplayer.RemoveForClientIfIncomplete(originalGrid);
                    m_tmpBlockPositions.Clear();
                    foreach (var block in splitBlocks)
                    {
                        m_tmpBlockPositions.Add(block.Position);
                    }

                    MyMultiplayer.RaiseEvent(originalGrid, x => x.CreateSplits_Implementation, m_tmpBlockPositions, groups);

                    foreach (var newGrid in originalGrid.m_tmpGrids)
                    {
                        newGrid.IsSplit = true;
                        MyMultiplayer.ReplicateImmediatelly(MyExternalReplicable.FindByObject(newGrid), MyExternalReplicable.FindByObject(originalGrid));
                        newGrid.IsSplit = false;
                    }
                }
            }
            finally
            {
                originalGrid.m_tmpGrids.Clear();
            }
        }

        [Event, Reliable, Broadcast]
        public void CreateSplits_Implementation(List<Vector3I> blocks, List<MyDisconnectHelper.Group> groups)
        {

            // Its already to late. Server send info he wants this to be closed so there is no need to process.
            // Its not perfect solution, but due to the fact that close can come before split, there is no other choice for now.
            if (this.MarkedForClose)
                return;

            m_tmpBlockListReceive.Clear();

            // groups should start from index 0 and they should not have any holes in indexes
            Debug.Assert(groups[0].FirstBlockIndex == 0 && blocks.Count == groups[groups.Count - 1].FirstBlockIndex + groups[groups.Count - 1].BlockCount);

            for (int gr = 0; gr < groups.Count; gr++)
            {
                var group = groups[gr];
                int groupBlockCount = group.BlockCount;
                for (int i = group.FirstBlockIndex; i < group.FirstBlockIndex + group.BlockCount; i++)
                {
                    var block = GetCubeBlock(blocks[i]);
                    Debug.Assert(block != null, "Block was null when trying to create a grid split. Desync?");
                    if (block == null)
                    {
                        MySandboxGame.Log.WriteLine("Block was null when trying to create a grid split. Desync?");
                        // block is missing - some desync
                        --groupBlockCount;
                        if (groupBlockCount == 0)
                            group.IsValid = false;
                    }
                    // Note null block can be added (to avoid changing of group's FirstBlockIndex and BlockCount when block is not found)!
                    m_tmpBlockListReceive.Add(block);
                }
                groups[gr] = group;
            }

            Debug.Assert(blocks.Count == m_tmpBlockListReceive.Count);

            //foreach (var position in blocks)
            //{
            //    var block = GetCubeBlock(position);
            //    Debug.Assert(block != null, "Block was null when trying to create a grid split. Desync?");
            //    if (block == null)
            //    {
            //        MySandboxGame.Log.WriteLine("Block was null when trying to create a grid split. Desync?");
            //        continue;
            //    }
            //    m_tmpBlockListReceive.Add(block);
            //}

            MyCubeGrid.CreateSplits(this, m_tmpBlockListReceive, groups, false);
            m_tmpBlockListReceive.Clear();
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

            if (group.BlockCount == 1 && splitBlocks.Count > group.FirstBlockIndex && splitBlocks[group.FirstBlockIndex] != null)
            {
                var firstBlock = splitBlocks[group.FirstBlockIndex];
                if (firstBlock.FatBlock is MyFracturedBlock)
                {
                    group.IsValid = false;
                    if (Sync.IsServer)
                        MyDestructionHelper.CreateFracturePiece(firstBlock.FatBlock as MyFracturedBlock, true);
                }
                else if (firstBlock.FatBlock != null && firstBlock.FatBlock.Components.Has<MyFractureComponentBase>())
                {
                    group.IsValid = false;
                    if (Sync.IsServer)
                    {
                        var component = firstBlock.GetFractureComponent();
                        if (component != null)
                            MyDestructionHelper.CreateFracturePiece(component, true);
                    }
                }
                else if (firstBlock.FatBlock is MyCompoundCubeBlock)
                {
                    var compound = firstBlock.FatBlock as MyCompoundCubeBlock;
                    bool allFractureComponent = true;
                    foreach (var blockInCompound in compound.GetBlocks())
                    {
                        allFractureComponent &= blockInCompound.FatBlock.Components.Has<MyFractureComponentBase>();
                        if (!allFractureComponent)
                            break;
                    }

                    if (allFractureComponent)
                    {
                        group.IsValid = false;
                        if (Sync.IsServer)
                        {
                            foreach (var blockInCompound in compound.GetBlocks())
                            {
                                var component = blockInCompound.GetFractureComponent();
                                if (component != null)
                                    MyDestructionHelper.CreateFracturePiece(component, true);
                            }
                        }
                    }
                }
            }

            if (group.IsValid)
            {
                ProfilerShort.Begin("Init grid");
                var newGrid = MyCubeGrid.CreateForSplit(originalGrid, group.EntityId);
                newGrid.DebugCreatedBy = MyEventContext.Current.IsLocallyInvoked ? DebugCreatedBy.LocalSplit : DebugCreatedBy.ServerSplit;
                ProfilerShort.End();

                if (newGrid != null)
                {
                    ProfilerShort.Begin("Move blocks");
                    originalGrid.m_tmpGrids.Add(newGrid);
                    MyEntities.Add(newGrid);
                    MyCubeGrid.MoveBlocks(originalGrid, newGrid, splitBlocks, group.FirstBlockIndex, group.BlockCount);
                    group.EntityId = newGrid.EntityId;
                    ProfilerShort.End();

                    if (newGrid.IsStatic && Sync.IsServer)
                    {
                        MatrixD worldMat = newGrid.WorldMatrix;
                        bool result = MyCoordinateSystem.Static.IsLocalCoordSysExist(ref worldMat, newGrid.GridSize);
                        if (newGrid.GridSizeEnum == MyCubeSize.Large)
                        {
                        if (result)
                        {
                            MyCoordinateSystem.Static.RegisterCubeGrid(newGrid);
                }
                else
                {
                            MyCoordinateSystem.Static.CreateCoordSys(newGrid, MyCubeBuilder.CubeBuilderDefinition.BuildingSettings.StaticGridAlignToCenter, true);
                        }
                    }
                    }

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

            ob.IsStatic = IsStatic;
            ob.Editable = Editable;

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

            if(copy)
                ob.Name = null;

            ob.BlockGroups.Clear();
            foreach (var group in BlockGroups)
            {
                ob.BlockGroups.Add(group.GetObjectBuilder());
            }

            ob.DisplayName = DisplayName;
            ob.DestructibleBlocks = DestructibleBlocks;

            ob.IsRespawnGrid = IsRespawnGrid;
            ob.playedTime = m_playedTime;
            ob.GridGeneralDamageModifier = GridGeneralDamageModifier;
            ob.LocalCoordSys = LocalCoordSystem;

            GridSystems.GetObjectBuilder(ob);
        }

        internal void HavokSystemIDChanged(int id)
        {
            if (OnHavokSystemIDChanged != null)
                OnHavokSystemIDChanged(id);
        }

        private void UpdatePhysicsShape()
        {
            Physics.UpdateShape();
        }

        private void UpdateGravity()
        {
            ProfilerShort.Begin("Gravity");
            if (IsStatic == false && Physics != null && Physics.Enabled && !Physics.IsWelded)
            {
                if (Physics.DisableGravity <= 0)
                {
                    RecalculateGravity();
                }
                else
                    Physics.DisableGravity--;
            }
            ProfilerShort.End();
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

            ActivatePhysics();
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
            MySimpleProfiler.Begin("Grid");
            if (MyFakes.ENABLE_GRID_SYSTEM_UPDATE)
            {
                ProfilerShort.Begin("Grid systems");
                GridSystems.UpdateBeforeSimulation();
                ProfilerShort.End();
            }

            ProfilerShort.Begin("Update generators");
            if (m_hasAdditionalModelGenerators)
            {
                // Generators must be updated before grid splits.
                foreach (var generator in AdditionalModelGenerators)
                    generator.UpdateBeforeSimulation();
            }

            ProfilerShort.BeginNextBlock("Lazy updates");
            DoLazyUpdates();


            ProfilerShort.BeginNextBlock("Base update");
            base.UpdateBeforeSimulation();


            if (Physics != null)
            {
                UpdatePhysicsShape();
            }

            //MyRenderProxy.DebugDrawAxis(PositionComp.WorldMatrix, 500, false);
            //MyRenderProxy.DebugDrawText3D(PositionComp.WorldMatrix.Translation, (PositionComp.WorldMatrix.Translation - MySector.MainCamera.Position).Length().ToString(), Color.White, 0.7f, false);


            ProfilerShort.End();
            MySimpleProfiler.End("Grid");
        }

        protected static float GetLineWidthForGizmo(IMyGizmoDrawableObject block, BoundingBox box)
        {
            //we don't want to gizmo line be smaller in distance, so we need to adjust line width according to distance from camera
            float minDistance = m_gizmoMaxDistanceFromCamera;

            foreach (var corner in box.Corners)
            {
                minDistance = (float)Math.Min(minDistance, Math.Abs(World.MySector.MainCamera.GetDistanceFromPoint(Vector3.Transform(block.GetPositionInGrid() + corner, block.GetWorldMatrix()))));
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
            ProfilerShort.Begin("Base");
            base.PrepareForDraw();
            ProfilerShort.End();
            if (m_dirtyRegion.IsDirty)
            {
                ProfilerShort.Begin("Update dirty");
                // Never update more often than every 10th frame. Otherwise gatling guns can force update every frame causing performance issues. Tweak if necessary
                if (m_updateDirtyCounter <= 0)
                {
                    UpdateDirty();
                    m_updateDirtyCounter = 10;
                }
                ProfilerShort.End();
            }
            if (m_updateDirtyCounter > 0)
            {
                m_updateDirtyCounter--;
            }

            ProfilerShort.Begin("Grid systems - PrepareForDraw");
            GridSystems.PrepareForDraw();
            ProfilerShort.End();

            //gravity and senzor gizmos needs to be drawn even when object / parent grid is not visible
            ProfilerShort.Begin("Gizmo");
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
            ProfilerShort.End();
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
                    MySimpleObjectDraw.DrawTransparentBox(ref worldMatrix, ref box, ref gizmoColor, MySimpleObjectRasterizer.SolidAndWireframe, 1, gizmoLineWidth);
                }
                else
                {
                    float radius = drawObject.GetRadius();
                    float distanceToObject = (float)MySector.MainCamera.GetDistanceFromPoint(worldMatrix.Translation);
                    float gizmoDistanceToCamera = (float)(radius - MySector.MainCamera.GetDistanceFromPoint(worldMatrix.Translation));
                    float thickness = m_gizmoDrawLineScale * Math.Min(m_gizmoMaxDistanceFromCamera, Math.Abs(gizmoDistanceToCamera));
                    int projectionId = -1;
                    MySimpleObjectDraw.DrawTransparentSphere(ref worldMatrix, radius, ref gizmoColor, MySimpleObjectRasterizer.SolidAndWireframe, 20, null, null, thickness, projectionId);

                    if (drawObject.EnableLongDrawDistance() && MyFakes.ENABLE_LONG_DISTANCE_GIZMO_DRAWING)
                    {
                        MyBillboardViewProjection viewProjection;
                        viewProjection.CameraPosition = MySector.MainCamera.Position;
                        viewProjection.View = MySector.MainCamera.ViewMatrix;
                        viewProjection.ViewAtZero = default(Matrix);
                        viewProjection.Viewport = MySector.MainCamera.Viewport;
                        viewProjection.DepthRead = true;


                        float aspectRatio = viewProjection.Viewport.Width / viewProjection.Viewport.Height;
                        viewProjection.Projection = Matrix.CreatePerspectiveFieldOfView(MySector.MainCamera.FieldOfView, aspectRatio, 1, 100);
                        viewProjection.Projection.M33 = -1;
                        viewProjection.Projection.M34 = -1;
                        viewProjection.Projection.M43 = 0;
                        viewProjection.Projection.M44 = 0;

                        projectionId = 10;
                        VRageRender.MyRenderProxy.AddBillboardViewProjection(projectionId, viewProjection);

                        MySimpleObjectDraw.DrawTransparentSphere(ref worldMatrix, radius, ref gizmoColor, MySimpleObjectRasterizer.SolidAndWireframe, 20, null, null, thickness, projectionId);
                    }
                }
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            MySimpleProfiler.Begin("Grid");
            base.UpdateBeforeSimulation10();

            if (MyFakes.ENABLE_GRID_SYSTEM_UPDATE)
            {
                GridSystems.UpdateBeforeSimulation10();
            }
            MySimpleProfiler.End("Grid");
        }

        public override void UpdateBeforeSimulation100()
        {
            MySimpleProfiler.Begin("Grid");
            base.UpdateBeforeSimulation100();

            if (MyFakes.ENABLE_GRID_SYSTEM_UPDATE)
            {
                GridSystems.UpdateBeforeSimulation100();
            }
            MySimpleProfiler.End("Grid");
        }

        bool m_inventoryMassDirty;
        internal void SetInventoryMassDirty()
        {
            m_inventoryMassDirty = true;
        }

        public int GetCurrentMass(MyCharacter pilot = null)
        {
            int baseMass;
            return GetCurrentMass(out baseMass, pilot);
        }

        public int GetCurrentMass(out int baseMass, MyCharacter pilot = null)
        {
            baseMass = 0;
            int currentMass = 0;
            var physicalGroup = MyCubeGridGroups.Static.Physical.GetGroup(this);
            if (physicalGroup != null)
            {
                foreach (var node in physicalGroup.Nodes)
                {
                    var cubeGrid = node.NodeData;
                    if (cubeGrid == null || cubeGrid.Physics == null || cubeGrid.Physics.Shape == null)
                        continue;

                    var massProperties = cubeGrid.Physics.Shape.MassProperties;
                    var baseMassProperties = cubeGrid.Physics.Shape.BaseMassProperties;
                    if (IsStatic || !massProperties.HasValue || !baseMassProperties.HasValue)
                        continue;

                    float gridTotalMass = massProperties.Value.Mass;
                    float gridBaseMass = baseMassProperties.Value.Mass;
                    float inventoryMass = gridTotalMass - gridBaseMass;
                    baseMass += (int)gridBaseMass;
                    currentMass += (int)(gridBaseMass + inventoryMass * MySession.Static.Settings.InventorySizeMultiplier - (pilot != null ? pilot.BaseMass : 0.0f));
                }
            }
            return currentMass;
        }

        public override void UpdateAfterSimulation100()
        {
            MySimpleProfiler.Begin("Grid");
            base.UpdateAfterSimulation100();
            //trash removal outside world doesnt work in update before simulation becaouse m_worldPositionChanged is set during simulation and unset
            //in update after so it was allways false in update before
            UpdateGravity();

            if (MyFakes.ENABLE_BOUNDINGBOX_SHRINKING && m_boundsDirty)
            {
                var elapsedTime = MySandboxGame.TotalTimeInMilliseconds - m_lastUpdatedDirtyBounds;
                if (elapsedTime > 30000)
                {
                    ProfilerShort.Begin("Recalculate bounds lazily");
                    RecalcBounds();
                    m_boundsDirty = false;
                    m_lastUpdatedDirtyBounds = MySandboxGame.TotalTimeInMilliseconds;
                    if (GridSystems.GasSystem != null)
                        GridSystems.GasSystem.OnCubeGridShrinked();
                    ProfilerShort.End();
                }
            }

            if (MyFakes.ENABLE_GRID_SYSTEM_UPDATE)
            {
                ProfilerShort.Begin("GridSystems");
                GridSystems.UpdateAfterSimulation100();
                ProfilerShort.End();
            }

            MySimpleProfiler.End("Grid");
        }

        public override void UpdateAfterSimulation()
        {
            MySimpleProfiler.Begin("Grid");
            base.UpdateAfterSimulation();

            ProfilerShort.Begin("Update Shape");
            //welding mass props debug
            //if(MySession.Static.ControlledEntity.Entity.GetTopMostParent() == this)
            //{
            //    MyRenderProxy.DebugDrawText2D(new Vector2(300, 300), Physics.RigidBody.InverseInertiaTensor.Scale.ToString(), Color.White, 0.75f);
            //    MyRenderProxy.DebugDrawText2D(new Vector2(300, 330), Physics.RigidBody.InertiaTensor.Scale.ToString(), Color.White, 0.75f);
            //    MyRenderProxy.DebugDrawText2D(new Vector2(300, 360), Physics.RigidBody.Mass.ToString(), Color.White, 0.75f);
            //    var pos = new Vector2(300, 380);
            //    MyRenderProxy.DebugDrawText2D(pos, Physics.WeldInfo.MassElement.Properties.InertiaTensor.Scale.ToString(), Color.White, 0.75f);
            //    pos.Y += 20;
            //    foreach(var child in Physics.WeldInfo.Children)
            //    {
            //        MyRenderProxy.DebugDrawText2D(pos, child.WeldInfo.MassElement.Properties.InertiaTensor.Scale.ToString(), Color.White, 0.75f);
            //        pos.Y += 20;
            //    }
            //}

            if (m_hasAdditionalModelGenerators)
            {
                foreach (var generator in AdditionalModelGenerators)
                    generator.UpdateAfterSimulation();
            }

            SendRemovedBlocks();
            SendRemovedBlocksWithIds();

            if (!CanHavePhysics())
            {
                m_worldPositionChanged = false;
                if (Sync.IsServer)
                    Close();
                ProfilerShort.End();
                MySimpleProfiler.End("Grid");
                return;
            }

            if (Sync.IsServer)
            {
                if (MyFakes.ENABLE_FRACTURE_COMPONENT)
                {
                    if (Physics != null)
                    {
                        if (Physics.GetFractureBlockComponents().Count > 0)
                        {
                            try
                            {
                                foreach (var info in Physics.GetFractureBlockComponents())
                                    CreateFractureBlockComponent(info);
                            }
                            finally
                            {
                                Physics.ClearFractureBlockComponents();
                            }
                        }

                        Physics.CheckLastDestroyedBlockFracturePieces();
                    }
                }
                else
                {
                    if (Physics != null && Physics.GetFracturedBlocks().Count > 0)
                    {
                        bool oldEnabled = EnableGenerators(false);
                        foreach (var info in Physics.GetFracturedBlocks())
                        {
                            CreateFracturedBlock(info);
                        }
                        EnableGenerators(oldEnabled);
                    }
                }
            }

            StepStructuralIntegrity();

            if (TestDynamic != MyTestDynamicReason.NoReason)
            {
                if (!MyCubeGrid.ShouldBeStatic(this, TestDynamic) && IsStatic)
                {
                    ConvertToDynamic();
                }
                TestDynamic = MyCubeGrid.MyTestDynamicReason.NoReason;
            }

            DoLazyUpdates();

            if (Physics != null && Physics.Enabled)
            {
                if (!Physics.IsWelded)
                {
                    Physics.RigidBody.Gravity = m_gravity;
                }

                if (m_inventoryMassDirty)
                {
                    m_inventoryMassDirty = false;
                    Physics.Shape.UpdateMassFromInventories(m_cubeBlocks, Physics);	// TODO: MK: Store inventory blocks in cubegrid and only check those
                }

                if (Physics.RigidBody2 != null)
                {
                    /*
                    //No need, we use ApplyHardKeyFrame utility
                    if (Physics.RigidBody2.LinearVelocity != Physics.RigidBody.LinearVelocity)
                        Physics.RigidBody2.LinearVelocity = Physics.RigidBody.LinearVelocity;
                                      
                    if (Physics.RigidBody2.AngularVelocity != Physics.RigidBody.AngularVelocity)
                       Physics.RigidBody2.AngularVelocity = Physics.RigidBody.AngularVelocity;
                    */
                                      
                    if (Physics.RigidBody2.CenterOfMassLocal != Physics.RigidBody.CenterOfMassLocal)
                        Physics.RigidBody2.CenterOfMassLocal = Physics.RigidBody.CenterOfMassLocal;
                }
            }

            m_worldPositionChanged = false;
            ProfilerShort.End();
            MySimpleProfiler.End("Grid");
        }

        private void CreateFractureBlockComponent(MyFractureComponentBase.Info info)
        {
            if (info.Entity.MarkedForClose)
                return;

            Debug.Assert(!info.Entity.Components.Has<MyFractureComponentBase>());

            MyFractureComponentCubeBlock fractureComponent = new MyFractureComponentCubeBlock();
            info.Entity.Components.Add<MyFractureComponentBase>(fractureComponent);
            fractureComponent.SetShape(info.Shape, info.Compound);

            if (Sync.IsServer)
            {
                MyCubeBlock cubeBlock = info.Entity as MyCubeBlock;
                if (cubeBlock != null)
                {
                    MyCubeGridSmallToLargeConnection.Static.RemoveBlockSmallToLargeConnection(cubeBlock.SlimBlock);

                    var existingSlimBlock = cubeBlock.CubeGrid.GetCubeBlock(cubeBlock.Position);
                    MyCompoundCubeBlock compoundBlock = existingSlimBlock != null ? existingSlimBlock.FatBlock as MyCompoundCubeBlock : null;
                    if (compoundBlock != null)
                    {
                        ushort? compoundId = compoundBlock.GetBlockId(cubeBlock.SlimBlock);
                        if (compoundId != null)
                        {
                            var builder = (MyObjectBuilder_FractureComponentBase)fractureComponent.Serialize();
                            MySyncDestructions.CreateFractureComponent(cubeBlock.CubeGrid.EntityId, cubeBlock.Position, compoundId.Value, builder);
                        }
                    }
                    else
                    {
                        var builder = (MyObjectBuilder_FractureComponentBase)fractureComponent.Serialize();
                        MySyncDestructions.CreateFractureComponent(cubeBlock.CubeGrid.EntityId, cubeBlock.Position, 0xFFFF, builder);
                    }

                    cubeBlock.SlimBlock.ApplyDestructionDamage(fractureComponent.GetIntegrityRatioFromFracturedPieceCounts());
                }
            }
        }

        private void StepStructuralIntegrity()
        {
            if (!MyStructuralIntegrity.Enabled || Physics == null || Physics.HavokWorld == null)
            {
                return;
            }
            ProfilerShort.Begin("MyCubeGrid.StructuralIntegrity.Update");

            if (StructuralIntegrity == null)
                CreateStructuralIntegrity();

            if (StructuralIntegrity != null)
            {
                StructuralIntegrity.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
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

        internal void RemoveGroupByName(string name)
        {
            int index = BlockGroups.FindIndex(g => g.Name.CompareTo(name) == 0);
            if (index == -1)
                return;

            MyBlockGroup group = BlockGroups[index];
            BlockGroups.RemoveAt(index);
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
            // On clients grids can be replicated only by replication
            //if (!Sync.IsServer)
            //{
            //Debug.Assert(DebugCreatedBy == DebugCreatedBy.FromServer || DebugCreatedBy == DebugCreatedBy.Clipboard || DebugCreatedBy == DebugCreatedBy.ServerSplit, "Creating grid locally on client, invalid in MP!");
            //}

            base.OnAddedToScene(source);
            MyCubeGridGroups.Static.AddNode(GridLinkTypeEnum.Logical, this);
            MyCubeGridGroups.Static.AddNode(GridLinkTypeEnum.Physical, this);
            RecalculateGravity();
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);
            MyCubeGridGroups.Static.RemoveNode(GridLinkTypeEnum.Physical, this);
            MyCubeGridGroups.Static.RemoveNode(GridLinkTypeEnum.Logical, this);
        }

        protected override void BeforeDelete()
        {
            SendRemovedBlocks();
            SendRemovedBlocksWithIds();

            m_cubes.Clear();
            m_targetingList.Clear();
            if (MyFakes.ENABLE_NEW_SOUNDS && MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS_QUICK_UPDATE)
                MyEntity3DSoundEmitter.UpdateEntityEmitters(true, false, false);

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

        public override bool GetIntersectionWithLine(ref LineD line, out MyIntersectionResultLineTriangleEx? tri, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            bool ret = GetIntersectionWithLine(ref line, ref m_hitInfoTmp, flags);
            tri = m_hitInfoTmp.Triangle;
            return ret;
        }

        public bool GetIntersectionWithLine(ref LineD line, ref MyCubeGridHitInfo info, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            if (info == null)
                info = new MyCubeGridHitInfo();

            info.Reset();

            RayCastCells(line.From, line.To, m_cacheRayCastCells);
            if (m_cacheRayCastCells.Count == 0)
                return false;

            foreach (Vector3I hit in m_cacheRayCastCells)
            {
                if (m_cubes.ContainsKey(hit))
                {
                    var cube = m_cubes[hit];
                    MyIntersectionResultLineTriangleEx? tri;
                    int cubePartIndex;
                    GetBlockIntersection(cube, ref line, flags, out tri, out cubePartIndex);

                    if (tri.HasValue)
                    {
                        info.Position = cube.CubeBlock.Position;
                        info.Triangle = tri.Value;
                        info.CubePartIndex = cubePartIndex;
                        return true;
                    }
                }
            }

            return false;
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
                    int cubePartIndex;
                    GetBlockIntersection(cube, ref line, flags, out t, out cubePartIndex);

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
                        VRage.Game.Models.MyIntersectionResultLineTriangleEx? intersectionTriResult;
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
                box = box.TransformFast(ref invee);
                Vector3 min = box.Min;
                Vector3 max = box.Max;

                Vector3I start = new Vector3I((int)Math.Round(min.X * GridSizeR), (int)Math.Round(min.Y * GridSizeR), (int)Math.Round(min.Z * GridSizeR));
                Vector3I end = new Vector3I((int)Math.Round(max.X * GridSizeR), (int)Math.Round(max.Y * GridSizeR), (int)Math.Round(max.Z * GridSizeR));

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
                                            MyTriangle_Vertices triangle = new MyTriangle_Vertices();

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

        #endregion
        public bool IsTrash()
        {
            var keepReason = MyTrashRemoval.GetTrashState(this, MyTrashRemoval.PreviewSettings);
            return keepReason == MyTrashRemovalFlags.None;
        }

        public Vector3I WorldToGridInteger(Vector3D coords)
        {
            Vector3D localCoords = Vector3D.Transform(coords, PositionComp.WorldMatrixNormalizedInv);
            localCoords *= GridSizeR;
            return Vector3I.Round(localCoords);
        }

        public Vector3D WorldToGridScaledLocal(Vector3D coords)
        {
            Vector3D localCoords = Vector3D.Transform(coords, PositionComp.WorldMatrixNormalizedInv);
            localCoords *= GridSizeR;
            return localCoords;
        }

        public static Vector3D GridIntegerToWorld(float gridSize, Vector3I gridCoords, MatrixD worldMatrix)
        {
            Vector3D retval = (Vector3D)(Vector3)gridCoords;
            retval *= gridSize;
            return Vector3D.Transform(retval, worldMatrix);
        }

        public Vector3D GridIntegerToWorld(Vector3I gridCoords)
        {
            return GridIntegerToWorld(GridSize, gridCoords, WorldMatrix);
        }

        public Vector3D GridIntegerToWorld(Vector3D gridCoords)
        {
            Vector3D retval = gridCoords;
            retval *= GridSize;
            return Vector3D.Transform(retval, WorldMatrix);
        }

        public Vector3I LocalToGridInteger(Vector3 localCoords)
        {
            localCoords *= GridSizeR;
            return Vector3I.Round(localCoords);
        }

        public bool CanAddCubes(Vector3I min, Vector3I max)
        {
            Vector3I current = min;
            for (var it = new Vector3I_RangeIterator(ref min, ref max); it.IsValid(); it.GetNext(out current))
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
                for (var it = new Vector3I_RangeIterator(ref min, ref max); it.IsValid(); it.GetNext(out current))
                {
                    if (!CanAddCube(current, orientation, definition))
                        return false;
                }
                return true;
            }

            return CanAddCubes(min, max);
        }

        public bool CanAddCube(Vector3I pos, MyBlockOrientation? orientation, MyCubeBlockDefinition definition, bool ignoreSame = false)
        {
            if (MyFakes.ENABLE_COMPOUND_BLOCKS && definition != null)
            {
                if (!CubeExists(pos))
                    return true;

                MySlimBlock block = GetCubeBlock(pos);
                if (block != null)
                {
                    var cmpBlock = block.FatBlock as MyCompoundCubeBlock;
                    if (cmpBlock != null)
                        return cmpBlock.CanAddBlock(definition, orientation, ignoreSame: ignoreSame);
                }

                return false;
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

        public bool CanPlaceBlock(Vector3I min, Vector3I max, MyBlockOrientation orientation, MyCubeBlockDefinition definition, int? ignoreMultiblockId = null, bool ignoreFracturedPieces = false)
        {
            var gridSettings = MyCubeBuilder.CubeBuilderDefinition.BuildingSettings.GetGridPlacementSettings(GridSizeEnum, this.IsStatic);
            return CanPlaceBlock(min, max, orientation, definition, ref gridSettings, ignoreMultiblockId: ignoreMultiblockId, ignoreFracturedPieces: ignoreFracturedPieces);
        }

        public bool CanPlaceBlock(Vector3I min, Vector3I max, MyBlockOrientation orientation, MyCubeBlockDefinition definition, ref MyGridPlacementSettings gridSettings,
            int? ignoreMultiblockId = null, bool ignoreFracturedPieces = false)
        {
            ProfilerShort.Begin("CanAddCubes");
            if (!CanAddCubes(min, max, orientation, definition))
            {
                ProfilerShort.End();
                return false;
            }

            if (MyFakes.ENABLE_MULTIBLOCKS && MyFakes.ENABLE_MULTIBLOCK_CONSTRUCTION)
            {
                ProfilerShort.BeginNextBlock("CanAddOtherBlockInMultiBlock");
                if (!CanAddOtherBlockInMultiBlock(min, max, orientation, definition, ignoreMultiblockId))
                {
                    ProfilerShort.End();
                    return false;
                }
            }

            ProfilerShort.BeginNextBlock("TestPlacementAreaCube");
            bool canPlaceBlock = TestPlacementAreaCube(this, ref gridSettings, min, max, orientation, definition, ignoredEntity: this, ignoreFracturedPieces: ignoreFracturedPieces);
            ProfilerShort.End();
            return canPlaceBlock;
        }

        /// <summary>
        /// Determines whether newly placed blocks still fit within block limits set by server
        /// </summary>
        private bool IsWithinWorldLimits(long ownerID, int blocksToBuild, string name)
        {
            if (!MySession.Static.EnableBlockLimits) return true;

            var identity = MySession.Static.Players.TryGetIdentity(ownerID);
            bool withinLimits = true;
            withinLimits &= MySession.Static.MaxGridSize == 0 || BlocksCount + blocksToBuild <= MySession.Static.MaxGridSize;
            if (MySession.Static.MaxBlocksPerPlayer != 0 && identity != null) {
                withinLimits &= identity.BlocksBuilt + blocksToBuild <= MySession.Static.MaxBlocksPerPlayer + identity.BlockLimitModifier;
            }
            short typeLimit = MySession.Static.GetBlockTypeLimit(name);
            int typeBuilt;
            if (identity != null && typeLimit > 0)
            {
                withinLimits &= (identity.BlockTypeBuilt.TryGetValue(name, out typeBuilt) ? typeBuilt : 0) + blocksToBuild <= typeLimit;
            }
            return withinLimits;
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
            for (var it = new Vector3I_RangeIterator(ref cubeBlock.Min, ref cubeBlock.Max); it.IsValid(); it.GetNext(out cube))
            {
                m_dirtyRegion.AddCube(cube);
            }
        }

        public void DebugDrawRange(Vector3I min, Vector3I max)
        {
            Vector3I currentMin = min;
            for (var it = new Vector3I_RangeIterator(ref min, ref max); it.IsValid(); it.GetNext(out currentMin))
            {
                var currentMax = currentMin + 1;

                var obb = new MyOrientedBoundingBoxD(
                    currentMin * GridSize,
                    GridSizeHalfVector,
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
                    GridSizeHalfVector,
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
                            if (!MyCompoundCubeBlock.IsCompoundEnabled(innerBlockDefinition))
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
                        if (MyCompoundCubeBlock.IsCompoundEnabled(blockDefinition))
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

        private MySlimBlock AddBlock(MyObjectBuilder_CubeBlock objectBuilder, bool testMerge)
        {
            ProfilerShort.Begin("MyCubeGrid.AddBlock(...)");

            try
            {
                if (Skeleton == null)
                    Skeleton = new MyGridSkeleton();

                MyCubeBlockDefinition blockDefinition;
                ProfilerShort.Begin("UpgradeCubeBlock");
                objectBuilder = UpgradeCubeBlock(objectBuilder, out blockDefinition);
                ProfilerShort.End();

                if (objectBuilder == null)
                {
                    return null;
                }

                if (MyFakes.THROW_LOADING_ERRORS)
                {
                    ProfilerShort.Begin("AddCubeBlock 1");
                    MySlimBlock block = AddCubeBlock(objectBuilder, testMerge, blockDefinition);
                    ProfilerShort.End();
                    return block;
                }
                else
                {
                    try
                    {
                        ProfilerShort.Begin("AddCubeBlock 2");
                        MySlimBlock block = AddCubeBlock(objectBuilder, testMerge, blockDefinition);
                        ProfilerShort.End();
                        return block;
                    }
                    catch (Exception e)
                    {
                        MyLog.Default.WriteLine("ERROR while adding cube " + blockDefinition.DisplayNameText.ToString() + ": " + e.ToString());
                        return null;
                    }
                }
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        private MySlimBlock AddCubeBlock(MyObjectBuilder_CubeBlock objectBuilder, bool testMerge, MyCubeBlockDefinition blockDefinition)
        {
            //Debug.Assert(false, "AddCubeBlock");
            ProfilerShort.Begin("CanAddCubes");
            
            Vector3I min = objectBuilder.Min, max;
            MySlimBlock.ComputeMax(blockDefinition, objectBuilder.BlockOrientation, ref min, out max);
            if (!CanAddCubes(min, max))
            {
                ProfilerShort.End();
                return null;
            }

            ProfilerShort.BeginNextBlock("CreateCubeBlock");
            var objectBlock = MyCubeBlockFactory.CreateCubeBlock(objectBuilder);
            MySlimBlock cubeBlock = objectBlock as MySlimBlock;
            if (cubeBlock == null)
                cubeBlock = new MySlimBlock();

            ProfilerShort.BeginNextBlock("Init");
            cubeBlock.Init(objectBuilder, this, objectBlock as MyCubeBlock);

            ProfilerShort.BeginNextBlock("MyCompoundCubeBlock");
            if (cubeBlock.FatBlock is MyCompoundCubeBlock)
            {
                if ((cubeBlock.FatBlock as MyCompoundCubeBlock).GetBlocksCount() == 0)
                {
                    ProfilerShort.End();
                    return null;
            }
            }

            ProfilerShort.BeginNextBlock("HookMultiplayer");
            cubeBlock.FatBlock.HookMultiplayer();

            ProfilerShort.BeginNextBlock("AddNeighbours");
            cubeBlock.AddNeighbours();

            ProfilerShort.BeginNextBlock("BoundsInclude");
            BoundsInclude(cubeBlock);

            ProfilerShort.BeginNextBlock("Fatblock");
            if (cubeBlock.FatBlock != null)
            {
                ProfilerShort.Begin("Hierarchy.AddChild");
                Hierarchy.AddChild(cubeBlock.FatBlock);

                ProfilerShort.BeginNextBlock("GridSystems.RegisterInSystems");
                GridSystems.RegisterInSystems(cubeBlock.FatBlock);

                ProfilerShort.BeginNextBlock("NeedsDrawFromParent");
                if (cubeBlock.FatBlock.Render.NeedsDrawFromParent)
                {
                    m_blocksForDraw.Add(cubeBlock.FatBlock);
                    cubeBlock.FatBlock.Render.NeedsDraw = false; //blocks shouldnt be drawn on their own at all?
                }

                ProfilerShort.BeginNextBlock("Misc");
                MyObjectBuilderType blockType = cubeBlock.BlockDefinition.Id.TypeId;
                if (blockType != typeof(MyObjectBuilder_CubeBlock))
                {
                    if (!BlocksCounters.ContainsKey(blockType))
                        BlocksCounters.Add(blockType, 0);
                    BlocksCounters[blockType]++;
                }
                ProfilerShort.End();
            }

            ProfilerShort.BeginNextBlock("Add");
            m_cubeBlocks.Add(cubeBlock);
            if (cubeBlock.FatBlock != null)
                m_fatBlocks.Add(cubeBlock.FatBlock);

            ProfilerShort.BeginNextBlock("Misc 1");
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
            ProfilerShort.BeginNextBlock("AddCube");
            for (var it = new Vector3I_RangeIterator(ref cubeBlock.Min, ref cubeBlock.Max); it.IsValid(); it.GetNext(out temp))
            {
                blockAddSuccessfull &= AddCube(cubeBlock, ref temp, rotationMatrix, blockDefinition);
            }

            Debug.Assert(blockAddSuccessfull, "Cannot add cube block!");

            ProfilerShort.BeginNextBlock("Physics.AddBlock");
            if (Physics != null)
            {
                Physics.AddBlock(cubeBlock);
            }

            ProfilerShort.BeginNextBlock("Update Skeleton");
            float boneErrorSquared = MyGridSkeleton.GetMaxBoneError(GridSize);
            boneErrorSquared *= boneErrorSquared;

            Vector3I boneMax = (cubeBlock.Min + Vector3I.One) * MyGridSkeleton.BoneDensity;
            Vector3I bonePos = cubeBlock.Min * MyGridSkeleton.BoneDensity;
            for (var it = new Vector3I_RangeIterator(ref bonePos, ref boneMax); it.IsValid(); it.GetNext(out bonePos))
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

            ProfilerShort.BeginNextBlock("Make skeleton dirty");
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

            if (MyFakes.ENABLE_MULTIBLOCK_PART_IDS)
            {
                ProfilerShort.BeginNextBlock("ENABLE_MULTIBLOCK_PART_IDS");
                AddMultiBlockInfo(cubeBlock);
            }

            if (testMerge)
            {
                ProfilerShort.BeginNextBlock("testMerge");
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
                    NotifyBlockAdded(cubeBlock);
                }
            }
            else
            {
                NotifyBlockAdded(cubeBlock);
            }
            ProfilerShort.End();
            cubeBlock.AddAuthorship();
            if (cubeBlock.FatBlock is MyReactor)
                NumberOfReactors++;
            return cubeBlock;
        }

        public void EnqueueDestructionDeformationBlock(Vector3I position)
        {
            if (Sync.IsServer)
            {
                m_destructionDeformationQueue.Add(position);
            }
        }

        public void EnqueueDestroyedBlock(Vector3I position)
        {
            if (Sync.IsServer)
            {
                m_destroyBlockQueue.Add(position);
            }
        }

        public void EnqueueRemovedBlock(Vector3I position, bool generatorsEnabled)
        {
            if (Sync.IsServer)
            {
                if (generatorsEnabled)
                    m_removeBlockQueueWithGenerators.Add(position);
                else
                    m_removeBlockQueueWithoutGenerators.Add(position);
            }
        }

        public void SendRemovedBlocks()
        {
            if (m_removeBlockQueueWithGenerators.Count > 0 || m_destroyBlockQueue.Count > 0 || m_destructionDeformationQueue.Count > 0 || m_removeBlockQueueWithoutGenerators.Count > 0)
            {
                MyMultiplayer.RaiseEvent(this, x => x.RemovedBlocks, m_removeBlockQueueWithGenerators, m_destroyBlockQueue, m_destructionDeformationQueue, m_removeBlockQueueWithoutGenerators);

                m_removeBlockQueueWithGenerators.Clear();
                m_removeBlockQueueWithoutGenerators.Clear();
                m_destroyBlockQueue.Clear();
                m_destructionDeformationQueue.Clear();
            }
        }

        [Event, Reliable, Broadcast]
        void RemovedBlocks(List<Vector3I> locationsWithGenerator, List<Vector3I> destroyLocations, List<Vector3I> DestructionDeformationLocation, List<Vector3I> LocationsWithoutGenerator)
        {
            if (destroyLocations.Count > 0)
            {
                BlocksDestroyed(destroyLocations);
            }
            if (locationsWithGenerator.Count > 0)
            {
                BlocksRemovedWithGenerator(locationsWithGenerator);
            }
            if (LocationsWithoutGenerator.Count > 0)
            {
                BlocksRemovedWithoutGenerator(LocationsWithoutGenerator);
            }
            if (DestructionDeformationLocation.Count > 0)
            {
                BlocksDeformed(DestructionDeformationLocation);
            }
        }

        /// <summary>
        /// Server method, adds removed block with compound id into queue
        /// </summary>
        public void EnqueueRemovedBlockWithId(Vector3I position, ushort? compoundId, bool generatorsEnabled)
        {
            if (Sync.IsServer)
            {
                var blockPositionId = new MyCubeGrid.BlockPositionId() { Position = position, CompoundId = compoundId ?? 0xFFFFFFFF };
                if (generatorsEnabled)
                    m_removeBlockWithIdQueueWithGenerators.Add(blockPositionId);
                else
                    m_removeBlockWithIdQueueWithoutGenerators.Add(blockPositionId);
            }
        }



        public void EnqueueDestroyedBlockWithId(Vector3I position, ushort? compoundId, bool generatorEnabled)
        {
            if (Sync.IsServer)
            {
                if (generatorEnabled)
                    m_destroyBlockWithIdQueueWithGenerators.Add(new MyCubeGrid.BlockPositionId() { Position = position, CompoundId = compoundId ?? 0xFFFFFFFF });
                else
                    m_destroyBlockWithIdQueueWithoutGenerators.Add(new MyCubeGrid.BlockPositionId() { Position = position, CompoundId = compoundId ?? 0xFFFFFFFF });
            }
        }

        public void SendRemovedBlocksWithIds()
        {
            if (m_removeBlockWithIdQueueWithGenerators.Count > 0 || m_removeBlockWithIdQueueWithoutGenerators.Count > 0 || m_destroyBlockWithIdQueueWithGenerators.Count > 0
                || m_destroyBlockWithIdQueueWithoutGenerators.Count > 0)
            {
                MyMultiplayer.RaiseEvent(this, x => x.RemovedBlocksWithIds, m_removeBlockWithIdQueueWithGenerators, m_destroyBlockWithIdQueueWithGenerators,
                    m_destroyBlockWithIdQueueWithoutGenerators, m_removeBlockWithIdQueueWithoutGenerators);

                m_removeBlockWithIdQueueWithGenerators.Clear();
                m_removeBlockWithIdQueueWithoutGenerators.Clear();
                m_destroyBlockWithIdQueueWithGenerators.Clear();
                m_destroyBlockWithIdQueueWithoutGenerators.Clear();
            }
        }

        [Event, Reliable, Broadcast]
        void RemovedBlocksWithIds(List<BlockPositionId> removeBlockWithIdQueueWithGenerators, List<BlockPositionId> destroyBlockWithIdQueueWithGenerators, List<BlockPositionId> destroyBlockWithIdQueueWithoutGenerators, List<BlockPositionId> removeBlockWithIdQueueWithoutGenerators)
        {
            if (destroyBlockWithIdQueueWithGenerators.Count > 0)
            {
                BlocksWithIdDestroyedWithGenerator(destroyBlockWithIdQueueWithGenerators);
            }
            if (destroyBlockWithIdQueueWithoutGenerators.Count > 0)
            {
                BlocksWithIdDestroyedWithoutGenerator(destroyBlockWithIdQueueWithoutGenerators);
            }
            if (removeBlockWithIdQueueWithGenerators.Count > 0)
            {
                BlocksWithIdRemovedWithGenerator(removeBlockWithIdQueueWithGenerators);
            }
            if (removeBlockWithIdQueueWithoutGenerators.Count > 0)
            {
                BlocksWithIdRemovedWithoutGenerator(removeBlockWithIdQueueWithoutGenerators);
            }
        }

        /// <summary>
        /// Remove all blocks from the grid built by specific player
        /// </summary>
        [Event, Reliable, Server, Broadcast]
        public void RemoveBlocksBuiltByID(long identityID)
        {
            foreach (var block in FindBlocksBuiltByID(identityID))
            {
                RemoveBlock(block);
            }
        }

        /// <summary>
        /// Transfer all blocks built by a specific player to another player
        /// </summary>
        [Event, Reliable, Server, Broadcast]
        public void TransferBlocksBuiltByID(long oldOwner, long newOwner)
        {
            foreach (var block in FindBlocksBuiltByID(oldOwner))
            {
                block.TransferAuthorship(newOwner);
            }
            if (OnAuthorshipChanged != null)
                OnAuthorshipChanged(this);
        }

        /// <summary>
        /// Send a message to a player who is supposed to receive blocks built by a player
        /// </summary>
        [Event, Reliable, Server]
        public void SendTransferRequestMessage(long oldOwner, long newOwner, ulong newOwnerSteamId)
        {
            if (MyEventContext.Current.IsLocallyInvoked)
            {
                ReceiveTransferRequestMessage(oldOwner, newOwner);
            }
            else
            {
                MyMultiplayer.RaiseEvent(this, x => x.ReceiveTransferRequestMessage, oldOwner, newOwner, targetEndpoint: new EndpointId(newOwnerSteamId));
            }
        }

        [Event, Client]
        private void ReceiveTransferRequestMessage(long oldOwner, long newOwner)
        {
            var identity = MySession.Static.Players.TryGetIdentity(oldOwner);
            var messageBox = Sandbox.Graphics.GUI.MyGuiSandbox.CreateMessageBox(
                        styleEnum: Graphics.GUI.MyMessageBoxStyleEnum.Info,
                        buttonType: Sandbox.Graphics.GUI.MyMessageBoxButtonsType.YES_NO,
                        messageText: new StringBuilder().AppendFormat(MyTexts.GetString(MyCommonTexts.MessageBoxTextConfirmAcceptTransferGrid), new object[] { identity.DisplayName, identity.BlocksBuiltByGrid[this].ToString(), DisplayName }),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                        canHideOthers: false,
                        callback: (result) =>
                        {
                            if (result == Sandbox.Graphics.GUI.MyGuiScreenMessageBox.ResultEnum.YES)
                            {
                                MyMultiplayer.RaiseEvent(this, x => x.TransferBlocksBuiltByID, oldOwner, newOwner);
                            }
                        });
            Sandbox.Graphics.GUI.MyGuiSandbox.AddScreen(messageBox);
        }

        /// <summary>
        /// Find all blocks built by a specific player. May be public if really needed.
        /// </summary>
        private HashSet<MySlimBlock> FindBlocksBuiltByID(long identityID)
        {
            var builtBlocks = new HashSet<MySlimBlock>();
            foreach (var block in m_cubeBlocks)
            {
                if (block.BuiltBy == identityID)
                    builtBlocks.Add(block);
            }
            return builtBlocks;
        }

        /// <summary>
        /// Find out whether a player blocks are supposed to be trasfered to has enough free blocks to accept them
        /// </summary>
        public bool IsTransferBlocksBuiltByIDPossible(long oldOwner, long newOwner)
        {
            var oldIdentity = MySession.Static.Players.TryGetIdentity(oldOwner);
            var newIdentity = MySession.Static.Players.TryGetIdentity(newOwner);

            if (oldIdentity == null || newIdentity == null)
                return false;

            int blocksNum;
            if (!oldIdentity.BlocksBuiltByGrid.TryGetValue(this, out blocksNum))
                return false;

            if (blocksNum > MySession.Static.MaxBlocksPerPlayer + newIdentity.BlockLimitModifier)
                return false;

            Dictionary<string, short> builtBlocksByType = new Dictionary<string, short>(MySession.Static.BlockTypeLimits);
            foreach (var block in FindBlocksBuiltByID(oldOwner))
            {
                if (builtBlocksByType.ContainsKey(block.BlockDefinition.BlockPairName))
                {
                    builtBlocksByType[block.BlockDefinition.BlockPairName]--;
                    if (builtBlocksByType[block.BlockDefinition.BlockPairName] < 0)
                        return false;
                }
            }
            return true;
        }

        public MySlimBlock BuildGeneratedBlock(MyBlockLocation location, Vector3 colorMaskHsv)
        {
            MyDefinitionId blockDefinitionId = location.BlockDefinition;
            MyCubeBlockDefinition blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(blockDefinitionId);
            Quaternion orientation;
            location.Orientation.GetQuaternion(out orientation);
            return BuildBlock(blockDefinition, colorMaskHsv, location.Min, orientation, location.Owner, location.EntityId, null);
        }

        [Event, Reliable, Server]
        public void BuildBlockRequest(uint colorMaskHsv, MyBlockLocation location, [DynamicObjectBuilder] MyObjectBuilder_CubeBlock blockObjectBuilder, long builderEntityId, bool instantBuild, long ownerId)
        {
            MyEntity builder = null;
            MyEntities.TryGetEntityById(builderEntityId, out builder);

            bool isAdmin = (MyEventContext.Current.IsLocallyInvoked || MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value));
            if (builder == null && isAdmin == false && MySession.Static.CreativeMode == false)
            {
                return;
            }

            using (MyMultiplayer.PauseReplication())
            {
                MyCubeGrid.MyBlockLocation? builtBlock = null;

                MyCubeBlockDefinition blockDefinition;
                MyDefinitionManager.Static.TryGetCubeBlockDefinition(location.BlockDefinition, out blockDefinition);
                MyBlockOrientation ori = location.Orientation;

                Quaternion orientation;
                location.Orientation.GetQuaternion(out orientation);

                var mountPoints = blockDefinition.GetBuildProgressModelMountPoints(MyComponentStack.NewBlockIntegrity);

                int? ignoreMultiBlockId = blockObjectBuilder != null && blockObjectBuilder.MultiBlockId != 0 ? (int?)blockObjectBuilder.MultiBlockId : null;
                var center = location.CenterPos;

                if (CanPlaceBlock(location.Min, location.Max, ori, blockDefinition, ignoreMultiblockId: ignoreMultiBlockId)
                    && MyCubeGrid.CheckConnectivity(this, blockDefinition, mountPoints, ref orientation, ref center))
                {
                BuildBlockSuccess(ColorExtensions.UnpackHSVFromUint(colorMaskHsv), location, blockObjectBuilder, ref builtBlock, builder, isAdmin && instantBuild, ownerId);

                if (builtBlock.HasValue)
                {
                    MyMultiplayer.RaiseEvent(this, x => x.BuildBlockSucess, colorMaskHsv, location, blockObjectBuilder, builderEntityId, isAdmin && instantBuild, ownerId);
                    AfterBuildBlockSuccess(builtBlock.Value);
                }
            }
        }
        }

        [Event, Reliable, Broadcast]
        public void BuildBlockSucess(uint colorMaskHsv, MyBlockLocation location, [DynamicObjectBuilder] MyObjectBuilder_CubeBlock blockObjectBuilder, long builderEntityId, bool instantBuild, long ownerId)
        {
            MyEntity builder = null;
            MyEntities.TryGetEntityById(builderEntityId, out builder);

            MyCubeGrid.MyBlockLocation? builtBlock = null;

            BuildBlockSuccess(ColorExtensions.UnpackHSVFromUint(colorMaskHsv), location, blockObjectBuilder, ref builtBlock, builder, instantBuild, ownerId);

            if (builtBlock.HasValue)
            {
                AfterBuildBlockSuccess(builtBlock.Value);
            }
            else
            { 
        }
        }

        /// <summary>
        /// Network friendly alternative for building block
        /// </summary>
        public void BuildBlocks(ref MyBlockBuildArea area, long builderEntityId, long ownerId)
        {
            if (!IsWithinWorldLimits(ownerId, area.BuildAreaSize.X * area.BuildAreaSize.Y * area.BuildAreaSize.Z, MyDefinitionManager.Static.GetCubeBlockDefinition(area.DefinitionId).BlockPairName))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                MyHud.Notifications.Add(MyNotificationSingletons.ShipOverLimits);
                return;
            }
            bool isAdmin = MySession.Static.CreativeToolsEnabled(Sync.MyId);
            MyMultiplayer.RaiseEvent(this, x => x.BuildBlocksAreaRequest, area, builderEntityId, isAdmin, ownerId);
        }

        /// <summary>
        /// Builds many same blocks, used when building lines or planes.
        /// </summary>
        public void BuildBlocks(Vector3 colorMaskHsv, HashSet<MyBlockLocation> locations, long builderEntityId, long ownerId)
        {
            string name = MyDefinitionManager.Static.GetCubeBlockDefinition(locations.First().BlockDefinition).BlockPairName;
            if (!IsWithinWorldLimits(ownerId, locations.Count, name))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                MyHud.Notifications.Add(MyNotificationSingletons.ShipOverLimits);
                return;
            }
            bool isAdmin = MySession.Static.CreativeToolsEnabled(Sync.MyId);
            MyMultiplayer.RaiseEvent(this, x => x.BuildBlocksRequest, colorMaskHsv.PackHSVToUint(), locations, builderEntityId, isAdmin, ownerId);
        }

        [Event, Reliable, Server]
        void BuildBlocksRequest(uint colorMaskHsv, HashSet<MyBlockLocation> locations, long builderEntityId, bool instantBuild, long ownerId)
        {
            if (!MySession.Static.CreativeMode && !MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                instantBuild = false;
            }
            m_tmpBuildList.Clear();
            Debug.Assert(m_tmpBuildList != locations, "The build block message was received via loopback using the temporary build list. This causes erasing ot the message.");

            MyEntity builder = null;
            MyEntities.TryGetEntityById(builderEntityId, out builder);

            MyCubeBuilder.BuildComponent.GetBlocksPlacementMaterials(locations, this);
            bool isAdmin = MyEventContext.Current.IsLocallyInvoked || MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value);

            if (builder == null && isAdmin == false && MySession.Static.CreativeMode == false && MyFinalBuildConstants.IS_OFFICIAL)
            {
                return;
            }

            if (!MyCubeBuilder.BuildComponent.HasBuildingMaterials(builder) && isAdmin == false)
            {
                return;
            }

            string name = MyDefinitionManager.Static.GetCubeBlockDefinition(locations.First().BlockDefinition).BlockPairName;
            if (!IsWithinWorldLimits(ownerId, locations.Count, name))
            {
                return;
            }

            using (MyMultiplayer.PauseReplication())
            {

                Vector3 unpackedColorMask = ColorExtensions.UnpackHSVFromUint(colorMaskHsv);
                BuildBlocksSuccess(unpackedColorMask, locations, m_tmpBuildList, builder, isAdmin && instantBuild, ownerId);

                if (m_tmpBuildList.Count > 0)
                {
                    MyMultiplayer.RaiseEvent(this, x => x.BuildBlocksClient, colorMaskHsv, m_tmpBuildList, builderEntityId, isAdmin && instantBuild, ownerId);
                }

            }

            AfterBuildBlocksSuccess(m_tmpBuildList);
        }

        [Event, Reliable, Broadcast]
        public void BuildBlocksClient(uint colorMaskHsv, HashSet<MyBlockLocation> locations, long builderEntityId, bool instantBuilt, long ownerId)
        {
            m_tmpBuildList.Clear();
            Debug.Assert(m_tmpBuildList != locations, "The build block message was received via loopback using the temporary build list. This causes erasing ot the message.");
            MyEntity builder = null;
            MyEntities.TryGetEntityById(builderEntityId, out builder);
            BuildBlocksSuccess(ColorExtensions.UnpackHSVFromUint(colorMaskHsv), locations, m_tmpBuildList, builder, instantBuilt, ownerId);
            AfterBuildBlocksSuccess(m_tmpBuildList);
        }

        [Event, Reliable, Server]
        private void BuildBlocksAreaRequest(MyCubeGrid.MyBlockBuildArea area, long builderEntityId, bool instantBuild, long ownerId)
        {
            if (!MySession.Static.CreativeMode && !MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                instantBuild = false;
            }
            try
            {
                bool isAdmin = MyEventContext.Current.IsLocallyInvoked || MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value);

                if (ownerId == 0 && isAdmin == false && MySession.Static.CreativeMode == false && MyFinalBuildConstants.IS_OFFICIAL)
                {
                    return;
                }

                if (!IsWithinWorldLimits(ownerId, area.BuildAreaSize.X * area.BuildAreaSize.Y * area.BuildAreaSize.Z, MyDefinitionManager.Static.GetCubeBlockDefinition(area.DefinitionId).BlockPairName))
                {
                    return;
                }

                var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(area.DefinitionId) as MyCubeBlockDefinition;
                if (definition == null)
                {
                    Debug.Fail("Block definition not found");
                    return;
                }

                int amount = area.BuildAreaSize.X * area.BuildAreaSize.Y * area.BuildAreaSize.Z;
                MyCubeBuilder.BuildComponent.GetBlockAmountPlacementMaterials(definition, amount);
                MyEntity builder = null;
                MyEntities.TryGetEntityById(builderEntityId, out builder);
                if (!MyCubeBuilder.BuildComponent.HasBuildingMaterials(builder, true) && isAdmin == false)
                {
                    return;
                }

                long owner = isAdmin && instantBuild ? 0 : ownerId;

                GetValidBuildOffsets(ref area, m_tmpBuildOffsets, m_tmpBuildFailList);
                MyCubeGrid.CheckAreaConnectivity(this, ref area, m_tmpBuildOffsets, m_tmpBuildFailList);

                int entityIdSeed = MyRandom.Instance.CreateRandomSeed();

                MyMultiplayer.RaiseEvent(this, x => x.BuildBlocksAreaClient, area, entityIdSeed, m_tmpBuildFailList, builderEntityId, isAdmin, owner);

                BuildBlocksArea(ref area, m_tmpBuildOffsets, builderEntityId, isAdmin, owner, entityIdSeed);
            }
            finally
            {
                m_tmpBuildOffsets.Clear();
                m_tmpBuildFailList.Clear();
            }
        }

        [Event, Reliable, Broadcast]
        private void BuildBlocksAreaClient(MyCubeGrid.MyBlockBuildArea area, int entityIdSeed, HashSet<Vector3UByte> failList, long builderEntityId, bool isAdmin, long ownerId)
        {
            try
            {
                GetAllBuildOffsetsExcept(ref area, failList, m_tmpBuildOffsets);
                BuildBlocksArea(ref area, m_tmpBuildOffsets, builderEntityId, isAdmin, ownerId, entityIdSeed);
            }
            finally
            {
                m_tmpBuildOffsets.Clear();
            }
        }

        private void BuildBlocksArea(ref MyCubeGrid.MyBlockBuildArea area, List<Vector3UByte> validOffsets, long builderEntityId, bool isAdmin, long ownerId, int entityIdSeed)
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

            MyEntity builderEntity = null;
            MyEntities.TryGetEntityById(builderEntityId, out builderEntity);

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
                        var block = BuildBlock(definition, ColorExtensions.UnpackHSVFromUint(area.ColorMaskHSV), center + area.BlockMin, orientation, ownerId, MyEntityIdentifier.AllocateId(), builderEntity, null, false, false, isAdmin);
                        if (block != null)
                        {
                            successSound = true;
                            m_tmpBuildSuccessBlocks.Add(block);
                            if (ownerId == MySession.Static.LocalPlayerId)
                                MySession.Static.TotalBlocksCreated++;
                        }
                    }
                }

                var worldBB = BoundingBoxD.CreateInvalid();
                foreach (var b in m_tmpBuildSuccessBlocks)
                {
                    BoundingBoxD blockWorldAAABB;
                    b.GetWorldBoundingBox(out blockWorldAAABB);
                    worldBB.Include(blockWorldAAABB);

                    if (b.FatBlock == null)
                        continue;
                    ProfilerShort.Begin("OnBuildSuccess");
                    b.FatBlock.OnBuildSuccess(ownerId);
                    ProfilerShort.End();
                   
                }
                if (m_tmpBuildSuccessBlocks.Count > 0)
                {
                    if (IsStatic && Sync.IsServer)
                    {
                        var entities = MyEntities.GetEntitiesInAABB(ref worldBB);
                        foreach (var b in m_tmpBuildSuccessBlocks)
                            DetectMerge(b, null, entities);
                        entities.Clear();
                    }
                    m_tmpBuildSuccessBlocks[0].PlayConstructionSound(MyIntegrityChangeEnum.ConstructionBegin);
                    UpdateGridAABB();
                }
                if (MySession.Static.LocalPlayerId == ownerId)
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

        private void BuildBlocksSuccess(Vector3 colorMaskHsv, HashSet<MyBlockLocation> locations, HashSet<MyBlockLocation> resultBlocks, MyEntity builder, bool instantBuilt, long ownerId)
        {
            bool cubeProcessed = true;

            while (locations.Count > 0 && cubeProcessed)
            {
                cubeProcessed = false;

                foreach (MyBlockLocation location in locations)
                {
                    Quaternion orientation;
                    location.Orientation.GetQuaternion(out orientation);
                    var center = location.CenterPos;

                    MyCubeBlockDefinition blockDefinition;
                    MyDefinitionManager.Static.TryGetCubeBlockDefinition(location.BlockDefinition, out blockDefinition);

                    if (blockDefinition == null)
                    {
                        Debug.Fail("Invalid block definition");
                        return;
                    }

                    var mountPoints = blockDefinition.GetBuildProgressModelMountPoints(MyComponentStack.NewBlockIntegrity);
                    // If we are on the server, we perform various checks. Clients on the other hand just build the blocks
                    // TODO: Refactor
                    if (!Sync.IsServer || CanPlaceWithConnectivity(location, ref orientation, ref center, blockDefinition, mountPoints))
                    {
                        var block = BuildBlock(blockDefinition, colorMaskHsv, location.Min, orientation, location.Owner, location.EntityId, builder, testMerge: false, buildAsAdmin: instantBuilt);

                        if (block != null)
                        {
                            ChangeBlockOwner(instantBuilt, block, ownerId);
                            var resultLocation = location;
                            resultBlocks.Add(resultLocation);
                        }
                        cubeProcessed = true;
                        locations.Remove(location);
                        break;
                    }
                }
            }
        }

        private bool CanPlaceWithConnectivity(MyBlockLocation location, ref Quaternion orientation, ref Vector3I center, MyCubeBlockDefinition blockDefinition, MyCubeBlockDefinition.MountPoint[] mountPoints)
        {
            return CanPlaceBlock(location.Min, location.Max, location.Orientation, blockDefinition)
                && MyCubeGrid.CheckConnectivity(this, blockDefinition, mountPoints, ref orientation, ref center);
        }

        private void BuildBlockSuccess(Vector3 colorMaskHsv, MyBlockLocation location, MyObjectBuilder_CubeBlock objectBuilder, ref MyBlockLocation? resultBlock, MyEntity builder, bool instantBuilt, long ownerId)
        {
            Quaternion orientation;
            location.Orientation.GetQuaternion(out orientation);

            MyCubeBlockDefinition blockDefinition;
            MyDefinitionManager.Static.TryGetCubeBlockDefinition(location.BlockDefinition, out blockDefinition);

            if (blockDefinition == null)
            {
                Debug.Fail("Invalid block definition");
                return;
            }

                var block = BuildBlock(blockDefinition, colorMaskHsv, location.Min, orientation, location.Owner, location.EntityId, instantBuilt ? null: builder, objectBuilder);

                if (block != null)
                {
                    ChangeBlockOwner(instantBuilt, block, ownerId);
                    resultBlock = location;
                    block.PlayConstructionSound(MyIntegrityChangeEnum.ConstructionBegin);
                }
                else
                {
                    resultBlock = null;
                }
            }

        private static void ChangeBlockOwner(bool instantBuilt, MySlimBlock block, long ownerId)
        {
            if (block.FatBlock != null)
            {
                block.FatBlock.ChangeOwner(ownerId, MyOwnershipShareModeEnum.Faction);
            }
        }

        private void AfterBuildBlocksSuccess(HashSet<MyCubeGrid.MyBlockLocation> builtBlocks)
        {
            foreach (var location in builtBlocks)
            {
                AfterBuildBlockSuccess(location);

                MySlimBlock block = this.GetCubeBlock(location.CenterPos);
                // Detect merge after blocks are built.
                MyCubeGrid mergedGrid = DetectMerge(block, null);
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
            MyMultiplayer.RaiseEvent(this, x => x.RazeBlocksAreaRequest, pos, size);
        }

        [Event, Reliable, Server]
        void RazeBlocksAreaRequest(Vector3I pos, Vector3UByte size)
        {
            if (!MySession.Static.CreativeMode && !MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }
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

                MyMultiplayer.RaiseEvent(this, x => x.RazeBlocksAreaSuccess, pos, size, m_tmpBuildFailList);
                RazeBlocksAreaSuccess(pos, size, m_tmpBuildFailList);
            }
            finally
            {
                m_tmpBuildFailList.Clear();
            }
        }

        [Event, Reliable, Broadcast]
        void RazeBlocksAreaSuccess(Vector3I pos, Vector3UByte size, HashSet<Vector3UByte> resultFailList)
        {
            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;

            Vector3UByte offset;

            if (MyFakes.ENABLE_MULTIBLOCKS)
            {
                Debug.Assert(m_tmpSlimBlocks.Count == 0);

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
                                    MyCompoundCubeBlock compound = block.FatBlock as MyCompoundCubeBlock;
                                    if (compound != null)
                                    {
                                        m_tmpSlimBlocks.Clear();
                                        m_tmpSlimBlocks.AddRange(compound.GetBlocks());

                                        foreach (var blockInCompound in m_tmpSlimBlocks)
                                        {
                                            if (blockInCompound.IsMultiBlockPart)
                                            {
                                                m_tmpBlocksInMultiBlock.Clear();

                                                GetBlocksInMultiBlock(blockInCompound.MultiBlockId, m_tmpBlocksInMultiBlock);
                                                RemoveMultiBlocks(ref min, ref max, m_tmpBlocksInMultiBlock);

                                                m_tmpBlocksInMultiBlock.Clear();
                                            }
                                            else
                                            {
                                                ushort? compoundId = compound.GetBlockId(blockInCompound);
                                                if (compoundId != null)
                                                    RemoveBlockInCompound(blockInCompound.Position, compoundId.Value, ref min, ref max);
                                            }
                                        }

                                        m_tmpSlimBlocks.Clear();
                                    }
                                    else
                                    {
                                        if (block.IsMultiBlockPart)
                                        {
                                            m_tmpBlocksInMultiBlock.Clear();

                                            GetBlocksInMultiBlock(block.MultiBlockId, m_tmpBlocksInMultiBlock);
                                            RemoveMultiBlocks(ref min, ref max, m_tmpBlocksInMultiBlock);

                                            m_tmpBlocksInMultiBlock.Clear();
                                        }
                                        else
                                        {
                                            MyFracturedBlock fracturedBlock = block.FatBlock as MyFracturedBlock;
                                            if (fracturedBlock != null && fracturedBlock.MultiBlocks != null && fracturedBlock.MultiBlocks.Count > 0)
                                            {
                                                foreach (var mbpart in fracturedBlock.MultiBlocks)
                                                {
                                                    if (mbpart != null)
                                                    {
                                                        m_tmpBlocksInMultiBlock.Clear();

                                                        MyMultiBlockDefinition mbDef = MyDefinitionManager.Static.TryGetMultiBlockDefinition(mbpart.MultiBlockDefinition);
                                                        Debug.Assert(mbDef != null);
                                                        if (mbDef != null)
                                                        {
                                                            GetBlocksInMultiBlock(mbpart.MultiBlockId, m_tmpBlocksInMultiBlock);
                                                            RemoveMultiBlocks(ref min, ref max, m_tmpBlocksInMultiBlock);
                                                        }

                                                        m_tmpBlocksInMultiBlock.Clear();
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                min = Vector3I.Min(min, block.Min);
                                                max = Vector3I.Max(max, block.Max);

                                                RemoveBlockByCubeBuilder(block);
                                            }
                                        }
                                    }
                                }
                            }
                        }
            }
            else
            {
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

                                    RemoveBlockByCubeBuilder(block);
                                }
                            }
                        }
            }

            if (Physics != null)
                Physics.AddDirtyArea(min, max);
        }

        private void RemoveMultiBlocks(ref Vector3I min, ref Vector3I max, HashSet<Tuple<MySlimBlock, ushort?>> tmpBlocksInMultiBlock)
        {
            foreach (var blockInMultiBlock in tmpBlocksInMultiBlock)
            {
                if (blockInMultiBlock.Item2 != null)
                {
                    RemoveBlockInCompound(blockInMultiBlock.Item1.Position, blockInMultiBlock.Item2.Value, ref min, ref max);
                }
                else
                {
                    min = Vector3I.Min(min, blockInMultiBlock.Item1.Min);
                    max = Vector3I.Max(max, blockInMultiBlock.Item1.Max);

                    RemoveBlockByCubeBuilder(blockInMultiBlock.Item1);
                }
            }
        }

        public void RazeBlock(Vector3I position)
        {
            m_tmpPositionListSend.Clear();
            m_tmpPositionListSend.Add(position);
            RazeBlocks(m_tmpPositionListSend);
        }

        /// <summary>
        /// Razes blocks (unbuild)
        /// </summary>
        public void RazeBlocks(List<Vector3I> locations)
        {
            MyMultiplayer.RaiseEvent(this, x => x.RazeBlocksRequest, locations);
        }

        [Event, Reliable, Server]
        public void RazeBlocksRequest(List<Vector3I> locations)
        {
            m_tmpPositionListReceive.Clear();
            Debug.Assert(m_tmpPositionListReceive != locations, "The raze block message was received via loopback using the same list. This causes erasing of the message.");

            RazeBlocksSuccess(locations, m_tmpPositionListReceive);

            MyMultiplayer.RaiseEvent(this, x => x.RazeBlocksClient, m_tmpPositionListReceive);
        }

        [Event, Reliable, Broadcast]
        public void RazeBlocksClient(List<Vector3I> locations)
        {
            m_tmpPositionListReceive.Clear();
            Debug.Assert(m_tmpPositionListReceive != locations, "The raze block message was received via loopback using the same list. This causes erasing of the message.");

            RazeBlocksSuccess(locations, m_tmpPositionListReceive);
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

                    RemoveBlockByCubeBuilder(block);
                }
            }

            if (Physics != null)
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

                    RemoveBlockByCubeBuilder(block);
                }
            }

            if (Physics != null)
                Physics.AddDirtyArea(min, max);
        }

        private void RazeBlockInCompoundBlockSuccess(List<LocationIdentity> locationsAndIds, List<Tuple<Vector3I, ushort>> removedBlocks)
        {
            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;

            foreach (var identity in locationsAndIds)
            {
                RemoveBlockInCompound(identity.Location, identity.Id, ref min, ref max, removedBlocks);
            }

            m_dirtyRegion.AddCubeRegion(min, max);

            if (Physics != null)
                Physics.AddDirtyArea(min, max);
        }

        private void RemoveBlockInCompound(Vector3I position, ushort compoundBlockId, ref Vector3I min, ref Vector3I max, List<Tuple<Vector3I, ushort>> removedBlocks = null)
        {
            var block = GetCubeBlock(position);
            if (block != null && block.FatBlock is MyCompoundCubeBlock)
            {
                MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                RemoveBlockInCompoundInternal(position, compoundBlockId, ref min, ref max, removedBlocks, block, compoundBlock);
            }
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
                    RemoveBlockInCompoundInternal(tuple.Item1, tuple.Item2, ref min, ref max, null, block, compoundBlock);
                }
            }

            m_dirtyRegion.AddCubeRegion(min, max);
            if (Physics != null)
                Physics.AddDirtyArea(min, max);
        }

        private void RemoveBlockInCompoundInternal(Vector3I position, ushort compoundBlockId, ref Vector3I min, ref Vector3I max, List<Tuple<Vector3I, ushort>> removedBlocks, MySlimBlock block, MyCompoundCubeBlock compoundBlock)
        {
            // Remove block in compound block
            MySlimBlock blockToRemove = compoundBlock.GetBlock(compoundBlockId);
            if (blockToRemove != null)
            {
                if (compoundBlock.Remove(blockToRemove))
                {
                    if (removedBlocks != null)
                        removedBlocks.Add(new Tuple<Vector3I, ushort>(position, compoundBlockId));

                    min = Vector3I.Min(min, block.Min);
                    max = Vector3I.Max(max, block.Max);

                    if (MyCubeGridSmallToLargeConnection.Static != null && m_enableSmallToLargeConnections)
                    {
                        MyCubeGridSmallToLargeConnection.Static.RemoveBlockSmallToLargeConnection(blockToRemove);
                    }

                    NotifyBlockRemoved(blockToRemove);
                }
            }

            // Remove compound if empty
            if (compoundBlock.GetBlocksCount() == 0)
            {
                RemoveBlockByCubeBuilder(block);
            }
        }

        public void RazeGeneratedBlocks(List<MySlimBlock> generatedBlocks)
        {
            ProfilerShort.Begin("MyCubeGrid.RazeGeneratedBlocks");
            m_tmpRazeList.Clear();
            m_tmpLocations.Clear();

            foreach (var generatedBlock in generatedBlocks)
            {
                Debug.Assert(generatedBlock.BlockDefinition.IsGeneratedBlock);
                var parentBlock = GetCubeBlock(generatedBlock.Position);

                if (parentBlock != null)
                {
                    if (parentBlock.FatBlock is MyCompoundCubeBlock)
                    {
                        MyCompoundCubeBlock cb = parentBlock.FatBlock as MyCompoundCubeBlock;
                        ushort? blockId = cb.GetBlockId(generatedBlock);
                        Debug.Assert(blockId != null);
                        if (blockId != null)
                            m_tmpRazeList.Add(new Tuple<Vector3I, ushort>(generatedBlock.Position, blockId.Value));
                    }
                    else
                    {
                        m_tmpLocations.Add(generatedBlock.Position);
                    }
                }
            }

            if (m_tmpLocations.Count > 0)
                RazeGeneratedBlocks(m_tmpLocations);

            if (m_tmpRazeList.Count > 0)
                RazeGeneratedBlocksInCompoundBlock(m_tmpRazeList);

            m_tmpRazeList.Clear();
            m_tmpLocations.Clear();
            ProfilerShort.End();
        }

        /// <summary>
        /// Color block in area. Verry slow.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="newHSV"></param>
        /// <param name="playSound"></param>
        public void ColorBlocks(Vector3I min, Vector3I max, Vector3 newHSV, bool playSound)
        {
            ProfilerShort.Begin("MyCubeGrid.ColorBlocks");
            MyMultiplayer.RaiseEvent(this, x => x.ColorBlockRequest, min, max, newHSV, playSound);
            ProfilerShort.End();
        }

        public void ColorGrid(Vector3 newHSV, bool playSound)
        {
            ProfilerShort.Begin("MyCubeGrid.ColorBlocks");
            MyMultiplayer.RaiseEvent(this, x => x.ColorGridFriendlyRequest, newHSV, playSound);
            ProfilerShort.End();
        }

        [Event, Reliable, Server, Broadcast]
        private void ColorGridFriendlyRequest(Vector3 newHSV,bool playSound)
        {
            ProfilerShort.Begin("MyCubeGrid.ColorBlockRequest");
            bool sound = false;
            foreach (var block in CubeBlocks)
            {
                sound |= ChangeColor(block, newHSV);
            }
            if (playSound && sound)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudColorBlock);
            }
            ProfilerShort.End();
        }

        [Event, Reliable, Server, Broadcast]
        private void ColorBlockRequest(Vector3I min, Vector3I max, Vector3 newHSV, bool playSound)
        {
            ProfilerShort.Begin("MyCubeGrid.ColorBlockRequest");
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

            if (playSound && sound && Vector3D.Distance(MySector.MainCamera.Position, Vector3D.Transform(min * GridSize, WorldMatrix)) < 200)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudColorBlock);
            }

            ProfilerShort.End();
        }

        /// <summary>
        /// Builds block without checking connectivity
        /// </summary>
        private MySlimBlock BuildBlock(MyCubeBlockDefinition blockDefinition, Vector3 colorMaskHsv, Vector3I min, Quaternion orientation, long owner, long entityId, MyEntity builderEntity, MyObjectBuilder_CubeBlock blockObjectBuilder = null, bool updateVolume = true, bool testMerge = true, bool buildAsAdmin = false)
        {
            ProfilerShort.Begin("BuildBlock");

            MyBlockOrientation blockOrientation = new MyBlockOrientation(ref orientation);
            if (blockObjectBuilder == null)
            {
                blockObjectBuilder = MyCubeGrid.CreateBlockObjectBuilder(blockDefinition, min, blockOrientation, entityId, owner, fullyBuilt: builderEntity == null || !MySession.Static.SurvivalMode || buildAsAdmin);
                blockObjectBuilder.ColorMaskHSV = colorMaskHsv;
            }
            else
            {
                blockObjectBuilder.Min = min;
                blockObjectBuilder.Orientation = orientation;
            }
            MyCubeBuilder.BuildComponent.BeforeCreateBlock(blockDefinition, builderEntity, blockObjectBuilder, buildAsAdmin: buildAsAdmin);

            MySlimBlock block = null;

            if (Sync.IsServer)
            {
                Vector3I position = MySlimBlock.ComputePositionInGrid(new MatrixI(blockOrientation), blockDefinition, min);
                MyCubeBuilder.BuildComponent.GetBlockPlacementMaterials(blockDefinition, position, blockObjectBuilder.BlockOrientation, this);
            }

            if (MyFakes.ENABLE_COMPOUND_BLOCKS && MyCompoundCubeBlock.IsCompoundEnabled(blockDefinition))
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
                        block.FatBlock.HookMultiplayer();

                        ushort id;
                        if (compoundBlock.Add(block, out id))
                        {
                            BoundsInclude(block);

                            m_dirtyRegion.AddCube(min);

                            if (Physics != null)
                                Physics.AddDirtyBlock(existingSlimBlock);

                            NotifyBlockAdded(block);
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
                if (updateVolume)
                    block.CubeGrid.UpdateGridAABB();

                if (MyCubeGridSmallToLargeConnection.Static != null && m_enableSmallToLargeConnections)
                    MyCubeGridSmallToLargeConnection.Static.AddBlockSmallToLargeConnection(block);

                if (Sync.IsServer)
                {
                    MyCubeBuilder.BuildComponent.AfterSuccessfulBuild(builderEntity, buildAsAdmin);
                }

                MyCubeGrids.NotifyBlockBuilt(this, block);
            }

            ProfilerShort.End();
            return block;
        }

        internal bool AddExplosion(Vector3 pos, MyExplosionTypeEnum type, float radius, bool createParticles, bool createDebris)
        {
            // We don't want explosion on same cube too soon
            if (!m_explosions.AddInstance(TimeSpan.FromMilliseconds(700), pos, GridSize * 2))
                return false;

            MyExplosionFlags flags = MyExplosionFlags.AFFECT_VOXELS;

            if (createParticles)
            {
                flags |= MyExplosionFlags.CREATE_PARTICLE_EFFECT;
            }

            if (createDebris)
            {
                flags |= MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.CREATE_DECALS;
            }

            var explosionSphere = new BoundingSphere(pos, radius);
            MyExplosionInfo explosionInfo = new MyExplosionInfo()
            {
                PlayerDamage = 0,
                Damage = 1,
                ExplosionSphere = explosionSphere,
                ExcludedEntity = type == MyExplosionTypeEnum.GRID_DESTRUCTION ? this : null,
                ExplosionFlags = flags,
                ExplosionType = type,
                LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN,
                ParticleScale = Math.Max(GridSizeEnum == MyCubeSize.Large ? 1 : 0.4f, Math.Min(radius / 4, 6)),
                VoxelCutoutScale = 1.0f,
                PlaySound = false,
                CheckIntersections = false,
                VoxelExplosionCenter = explosionSphere.Center,
                Velocity = Physics.RigidBody.LinearVelocity,
                ObjectsRemoveDelayInMiliseconds = MyRandom.Instance.Next(250),
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
            if (Skeleton == null)
            {
                MyLog.Default.WriteLine("Skeleton null in MultiplyBlockSkeleton!" + this);
            }
            if (Physics == null)
            {
                MyLog.Default.WriteLine("Physics null in MultiplyBlockSkeleton!" + this);
            }

            if (block == null || Skeleton == null || Physics == null)
                return;

            var min = block.Min * MyGridSkeleton.BoneDensity;
            var max = block.Max * MyGridSkeleton.BoneDensity + MyGridSkeleton.BoneDensity;

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
                    MyMultiplayer.RaiseEvent(this, x => x.OnBonesMultiplied, block.Position, factor);
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
            for (Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref min, ref max); it.IsValid(); it.GetNext(out temp))
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

        public MySlimBlock GetCubeBlock(Vector3I pos, ushort? compoundId)
        {
            if (compoundId == null)
                return GetCubeBlock(pos);

            MyCube result;
            if (m_cubes.TryGetValue(pos, out result))
            {
                Debug.Assert(result.CubeBlock.FatBlock != null && !result.CubeBlock.FatBlock.Closed);

                MyCompoundCubeBlock compound = result.CubeBlock.FatBlock as MyCompoundCubeBlock;
                if (compound != null)
                {
                    return compound.GetBlock(compoundId.Value);
                }
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

        public ListReader<MyCubeBlock> GetFatBlocks()
        {
            return m_fatBlocks;
        }

        public MyFatBlockReader<T> GetFatBlocks<T>()
            where T : MyCubeBlock
        {
            return new MyFatBlockReader<T>(this);
        }

        /// <summary>
        /// Returns true when grid have at least one block which has physics (e.g. interior lights have no physics)
        /// </summary>
        public bool CanHavePhysics()
        {
            if (m_canHavePhysics)
            {
                //TODO Temporary performance improvement - HasPhysics should be cached somehow
                if (MyPerGameSettings.Game == GameEnum.SE_GAME || MyPerGameSettings.Game == GameEnum.VRS_GAME)
                {
                    foreach (var block in m_cubeBlocks)
                    {
                        if (block.HasPhysics)
                            return true;
                    }

                    m_canHavePhysics = false;
                }
                else
                {
                    m_canHavePhysics = m_cubeBlocks.Count > 0;
                }
            }
            return m_canHavePhysics;
        }

        /// <summary>
        /// Returns true when grid have at least one block which has physics (lights has no physics)
        /// </summary>
        public static bool CanHavePhysics(List<MySlimBlock> blocks, int offset, int count)
        {
            if (offset < 0)
            {
                Debug.Fail(String.Format("Negative offset in CanHavePhysics - {0}", offset));
                MySandboxGame.Log.WriteLine(String.Format("Negative offset in CanHavePhysics - {0}", offset));
                // zxc throw here exception or not?
                return false;
            }
            for (int i = offset; i < offset + count; i++)
            {
                if (i >= blocks.Count)
                {
                    break;
                }
                var block = blocks[i];
                if (block != null && block.HasPhysics)
                    return true;
            }
            return false;
        }

        private void RebuildGrid()
        {
            // No physical cubes (closed in UpdateAfterSimulation)
            if (!CanHavePhysics())
            {
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

        [Event,Reliable,Broadcast]
        public void ConvertToDynamic()
        {
            Debug.Assert(IsStatic);
            if (!IsStatic || Physics == null || BlocksCount == 0) return;

            if (MyCubeGridSmallToLargeConnection.Static != null && m_enableSmallToLargeConnections)
            {
                MyCubeGridSmallToLargeConnection.Static.GridConvertedToDynamic(this);
            }

            IsStatic = false;
            MyCubeGridGroups.Static.UpdateDynamicState(this);
            SetInventoryMassDirty();
            Physics.ConvertToDynamic(GridSizeEnum == MyCubeSize.Large);
            RaisePhysicsChanged();
            Physics.RigidBody.AddGravity();
            RecalculateGravity();
        }

        [Event, Reliable, Broadcast]
        public void ConvertToStatic()
        {

            if (Physics.AngularVelocity.LengthSquared() > 0.01 * 0.01 || Physics.LinearVelocity.LengthSquared() > 0.01 * 0.01)
                return;

            IsStatic = true;
            Physics.ConvertToStatic();
            RaisePhysicsChanged();
        }

        public void DoDamage(float damage, MyHitInfo hitInfo, Vector3? localPos = null, long attackerId = 0)
        {
            Debug.Assert(Sync.IsServer);

            if (Sync.IsServer == false)
            {
                return;
            }

            Vector3I cubePos;
            if (localPos.HasValue)
                FixTargetCube(out cubePos, localPos.Value * GridSizeR);
            else
                FixTargetCube(out cubePos, Vector3D.Transform(hitInfo.Position, PositionComp.WorldMatrixInvScaled) * GridSizeR);

            var cube = GetCubeBlock(cubePos);
            //Debug.Assert(cube != null, "Cannot find block for damage!");
            if (cube != null)
            {
                if (MyFakes.ENABLE_FRACTURE_COMPONENT)
                {
                    ushort? compoundId = null;
                    MyCompoundCubeBlock compound = cube.FatBlock as MyCompoundCubeBlock;
                    if (compound != null)
                    {
                        compoundId = Physics.GetContactCompoundId(cube.Position, hitInfo.Position);
                        if (compoundId == null)
                            return;

                        var blockInCompound = compound.GetBlock(compoundId.Value);
                        if (blockInCompound != null)
                            cube = blockInCompound;
                        else
                            return;
                    }
                }

                ApplyDestructionDeformation(cube, damage, hitInfo, attackerId);
            }
        }

        public void ApplyDestructionDeformation(MySlimBlock block, float damage = 1f, MyHitInfo? hitInfo = null, long attackerId = 0)
        {
            if (MyPerGameSettings.Destruction)
            {
                Debug.Assert(hitInfo.HasValue, "Destruction needs additional info");
                (block as IMyDestroyableObject).DoDamage(damage, MyDamageType.Unknown, true, hitInfo, attackerId);
            }
            else
            {
                Debug.Assert(Sandbox.Game.Multiplayer.Sync.IsServer, "ApplyDestructionDeformation is supposed to be only server method");
                EnqueueDestructionDeformationBlock(block.Position);
                ApplyDestructionDeformationInternal(block, true, damage, attackerId);
            }
        }

        private float ApplyDestructionDeformationInternal(MySlimBlock block, bool sync, float damage = 1f, long attackerId = 0)
        {
            if (!BlocksDestructionEnabled)
                return 0;

            // Allow mods to stop deformation
            if (block.UseDamageSystem)
            {
                MyDamageInformation damageInfo = new MyDamageInformation(true, 1f, MyDamageType.Deformation, attackerId);
                MyDamageSystem.Static.RaiseBeforeDamageApplied(block, ref damageInfo);

                if (damageInfo.Amount == 0f)
                    return 0;
            }
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
            float maxLinearDeviation = GridSizeHalf;
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
                float damageAmount = m_totalBoneDisplacement * GridSize * 10.0f * damage;

                MyDamageInformation damageInfo = new MyDamageInformation(false, damageAmount, MyDamageType.Deformation, attackerId);
                if (block.UseDamageSystem)
                    MyDamageSystem.Static.RaiseBeforeDamageApplied(block, ref damageInfo);

                if (damageAmount > 0f)
                    (block as IMyDestroyableObject).DoDamage(damageInfo.Amount, MyDamageType.Deformation, true, attackerId: attackerId);
            }
            return m_totalBoneDisplacement;
        }

        /// <summary>
        /// Removes destroyed block, applies damage and deformation to close blocks
        /// Won't update physics!
        /// </summary>
        public void RemoveDestroyedBlock(MySlimBlock block, long attackerId = 0)
        {
            if (!Sync.IsServer)
                return;

            if (MyFakes.ENABLE_FRACTURE_COMPONENT)
            {
                bool enableGenerators = attackerId != 0;
                bool oldEnabled = EnableGenerators(enableGenerators);

                var existingBlock = GetCubeBlock(block.Position);
                Debug.Assert(existingBlock != null);
                if (existingBlock == null)
                    return;

                if (existingBlock == block)
                {
                    EnqueueDestroyedBlockWithId(block.Position, null, enableGenerators);
                    RemoveDestroyedBlockInternal(block);
                    Physics.AddDirtyBlock(block);
                }
                else
                {
                    var compound = existingBlock.FatBlock as MyCompoundCubeBlock;
                    if (compound != null)
                    {
                        var compoundId = compound.GetBlockId(block);
                        if (compoundId != null)
                        {
                            EnqueueDestroyedBlockWithId(block.Position, compoundId, enableGenerators);
                            RemoveDestroyedBlockInternal(block);
                            Physics.AddDirtyBlock(block);
                        }
                    }
                }

                EnableGenerators(oldEnabled);

                // Change fractures in block into pieces
                var fractureComponent = block.GetFractureComponent();
                if (fractureComponent != null)
                {
                    MyDestructionHelper.CreateFracturePiece(fractureComponent, true);
                }
            }
            else
            {
                EnqueueDestroyedBlock(block.Position);
                RemoveDestroyedBlockInternal(block);
                Physics.AddDirtyBlock(block);
            }
        }

        private void RemoveDestroyedBlockInternal(MySlimBlock block)
        {
            ApplyDestructionDeformationInternal(block, false);
            (block as IMyDestroyableObject).OnDestroy();

            var existingBlock = GetCubeBlock(block.Position);
            if (existingBlock == block)
            {
                RemoveBlockInternal(block, close: true);
            }
            else if (existingBlock != null)
            {
                var compound = existingBlock.FatBlock as MyCompoundCubeBlock;
                if (compound != null)
                {
                    var compoundId = compound.GetBlockId(block);
                    if (compoundId != null)
                    {
                        Vector3I min = Vector3I.MaxValue, max = Vector3I.MinValue;
                        RemoveBlockInCompound(block.Position, compoundId.Value, ref min, ref max);
                    }
                }
            }
        }

        private bool ApplyTable(Vector3I cubePos, MyCubeGridDeformationTables.DeformationTable table, ref Vector3I dirtyMin, ref Vector3I dirtyMax, MyRandom random, float maxLinearDeviation, float angleDeviation)
        {
            ProfilerShort.Begin("ApplyTable");
            if (!m_cubes.ContainsKey(cubePos + table.Normal))
            {
                Vector3I boneOffset;
                Vector3 clamp;

                float gridSizeTenth = GridSize / 10;

                m_tmpBoneSet.Clear();
                ProfilerShort.Begin("GetExistingBones");
                GetExistingBones(cubePos * MyGridSkeleton.BoneDensity + table.MinOffset, cubePos * MyGridSkeleton.BoneDensity + table.MaxOffset, m_tmpBoneSet);

                ProfilerShort.BeginNextBlock("table.OffsetTable");
                foreach (var offset in table.OffsetTable)
                {
                    if (m_tmpBoneSet.ContainsKey(cubePos * MyGridSkeleton.BoneDensity + offset.Key))
                    {
                        boneOffset = offset.Key;
                        clamp = new Vector3(GridSizeHalf - random.NextFloat(0, gridSizeTenth));
                        Vector3 moveDirection = random.NextDeviatingVector(offset.Value, angleDeviation) * random.NextFloat(1, maxLinearDeviation);
                        float length = moveDirection.Length();
                        MoveBone(ref cubePos, ref boneOffset, ref moveDirection, ref length, ref clamp);
                    }
                }
                ProfilerShort.End();
                dirtyMin = Vector3I.Min(dirtyMin, table.MinOffset);
                dirtyMax = Vector3I.Max(dirtyMax, table.MaxOffset);
                ProfilerShort.End();
                return true;
            }
            ProfilerShort.End();
            return false;
        }


        private void BlocksRemovedWithGenerator(List<Vector3I> blocksToRemove)
        {
            bool oldEnabled = EnableGenerators(true, true);

            BlocksRemoved(blocksToRemove);

            EnableGenerators(oldEnabled, true);
        }

        private void BlocksRemovedWithoutGenerator(List<Vector3I> blocksToRemove)
        {
            bool oldEnabled = EnableGenerators(false, true);

            BlocksRemoved(blocksToRemove);

            EnableGenerators(oldEnabled, true);
        }

        void BlocksWithIdRemovedWithGenerator(List<MyCubeGrid.BlockPositionId> blocksToRemove)
        {
            bool oldEnabled = EnableGenerators(true, true);

            BlocksWithIdRemoved(blocksToRemove);

            EnableGenerators(oldEnabled, true);
        }

        void BlocksWithIdRemovedWithoutGenerator(List<MyCubeGrid.BlockPositionId> blocksToRemove)
        {
            bool oldEnabled = EnableGenerators(false, true);

            BlocksWithIdRemoved(blocksToRemove);

            EnableGenerators(oldEnabled, true);
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
        }

        private void BlocksWithIdRemoved(List<MyCubeGrid.BlockPositionId> blocksToRemove)
        {
            foreach (var posAndId in blocksToRemove)
            {
                if (posAndId.CompoundId > ushort.MaxValue)
                {
                    var block = GetCubeBlock(posAndId.Position);
                    if (block != null)
                    {
                        RemoveBlockInternal(block, close: true);
                        Physics.AddDirtyBlock(block);
                    }
                }
                else
                {
                    Vector3I min = Vector3I.MaxValue;
                    Vector3I max = Vector3I.MinValue;

                    RemoveBlockInCompound(posAndId.Position, (ushort)posAndId.CompoundId, ref min, ref max);
                    if (min != Vector3I.MaxValue)
                        Physics.AddDirtyArea(min, max);
                }
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
        }

        private void BlocksWithIdDestroyedWithGenerator(List<MyCubeGrid.BlockPositionId> blocksToRemove)
        {
            bool oldEnabled = EnableGenerators(true, true);

            BlocksWithIdRemoved(blocksToRemove);

            EnableGenerators(oldEnabled, true);
        }

        private void BlocksWithIdDestroyedWithoutGenerator(List<MyCubeGrid.BlockPositionId> blocksToRemove)
        {
            bool oldEnabled = EnableGenerators(false, true);

            BlocksWithIdRemoved(blocksToRemove);

            EnableGenerators(oldEnabled, true);
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

        [Event,Reliable,Broadcast]
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

        [Event, Reliable, Broadcast]
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

        [Event, Reliable, Broadcast]
        private void FractureComponentRepaired(Vector3I pos, ushort subBlockId, long toolOwner)
        {
            Debug.Assert(!Sync.IsServer);

            MyCompoundCubeBlock compoundBlock = null;
            var block = GetCubeBlock(pos);
            if (block != null)
                compoundBlock = block.FatBlock as MyCompoundCubeBlock;

            if (compoundBlock != null)
                block = compoundBlock.GetBlock(subBlockId);

            if (block != null && block.FatBlock != null)
            {
                block.RepairFracturedBlock(toolOwner);
            }
        }

        private void RemoveBlockByCubeBuilder(MySlimBlock block)
        {
            RemoveBlockInternal(block, true);

            if (block.FatBlock != null)
            {
                block.FatBlock.OnRemovedByCubeBuilder();
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

            if (MyFakes.ENABLE_MULTIBLOCK_PART_IDS)
            {
                RemoveMultiBlockInfo(block);
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
            for (Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref block.Min, ref block.Max); it.IsValid(); it.GetNext(out temp))
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
                block.FatBlock.IsBeingRemoved = true;
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

            ProfilerShort.Begin("Remove Neighbours");
            block.RemoveNeighbours();
            ProfilerShort.End();

            ProfilerShort.Begin("Remove Ownership");
            block.RemoveAuthorship();
            if (OnAuthorshipChanged != null)
                OnAuthorshipChanged(this);
            ProfilerShort.End();

            ProfilerShort.Begin("Remove");
            m_cubeBlocks.Remove(block);
            if (block.FatBlock != null)
            {
                if (block.FatBlock is MyReactor)
                    NumberOfReactors--;
                m_fatBlocks.Remove(block.FatBlock);
                block.FatBlock.IsBeingRemoved = false;
            }
            ProfilerShort.End();

            if (markDirtyDisconnects)
                m_disconnectsDirty = true;

            Vector3I cube = block.Min;
            for (Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref block.Min, ref block.Max); it.IsValid(); it.GetNext(out cube))
                Skeleton.MarkCubeRemoved(ref cube);

            ProfilerShort.Begin("OnBlockRemoved");

            if (block.FatBlock != null && block.FatBlock.IDModule != null)
                ChangeOwner(block.FatBlock, block.FatBlock.IDModule.Owner, 0);

            if (MyCubeGridSmallToLargeConnection.Static != null && m_enableSmallToLargeConnections)
            {
                ProfilerShort.Begin("CheckRemovedBlockSmallToLargeConnection");
                MyCubeGridSmallToLargeConnection.Static.RemoveBlockSmallToLargeConnection(block);
                ProfilerShort.End();
            }

            NotifyBlockRemoved(block);

            if (close)
                NotifyBlockClosed(block);

            ProfilerShort.End();

            m_boundsDirty = true;
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

            EnqueueRemovedBlock(block.Min, m_generatorsEnabled);
            RemoveBlockInternal(block, close: true);

            if (updatePhysics)
            {
                this.Physics.AddDirtyBlock(block);
            }
        }

        public void RemoveBlockWithId(MySlimBlock block, bool updatePhysics = false)
        {
            var cb = GetCubeBlock(block.Min);
            if (cb == null)
                return;

            // Prepare block from compound
            MyCompoundCubeBlock compoundBlock = cb.FatBlock as MyCompoundCubeBlock;
            ushort? compoundId = null;
            if (compoundBlock != null)
            {
                compoundId = compoundBlock.GetBlockId(block);
                Debug.Assert(compoundId != null, "Block not found in compound");
                if (compoundId == null)
                    return;
            }

            RemoveBlockWithId(block.Min, compoundId, updatePhysics: updatePhysics);
        }

        public void RemoveBlockWithId(Vector3I position, ushort? compoundId, bool updatePhysics = false)
        {
            // Client cannot remove blocks, only server
            if (!Sync.IsServer)
                return;

            var block = GetCubeBlock(position);
            if (block == null)
                return;

            EnqueueRemovedBlockWithId(block.Min, compoundId, m_generatorsEnabled);
            if (compoundId != null)
            {
                Vector3I min = Vector3I.Zero, max = Vector3I.Zero;
                RemoveBlockInCompound(block.Min, compoundId.Value, ref min, ref max);
            }
            else
            {
                RemoveBlockInternal(block, close: true);
            }

            if (updatePhysics)
            {
                this.Physics.AddDirtyBlock(block);
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
            return gridPos * GridSize - Vector3.SignNonZero(gridPos * GridSize - position) * GridSizeHalf;
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

                    block.UpdateMaxDeformation();
                }
                if (block != null && block.FatBlock != null && block.FatBlock.Render != null)
                {
                    if (block.FatBlock.Render.NeedsDrawFromParent)
                    {
                        m_blocksForDraw.Add(block.FatBlock);
                        block.FatBlock.Render.NeedsDraw = false; //blocks shouldnt be drawn on their own at all?
                    }
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
            //no changes to volume actually made
            //if they were it would be marked dirty
            //PositionComp.UpdateWorldVolume();
            ProfilerShort.End();
        }

        public bool TryGetCube(Vector3I position, out MyCube cube)
        {
            return m_cubes.TryGetValue(position, out cube);
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
            //Debug.Assert(false, "AddCube");
            MyCube c = new MyCube();

            c.Parts = MyCubeGrid.GetCubeParts(cubeBlockDefinition, pos, rotation, GridSize, GridScale);
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
            c.Parts = MyCubeGrid.GetCubeParts(cubeBlockDefinition, pos, rotation, GridSize, GridScale);
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
                block.UpdateVisual(false);          // Don't trigger physics updates for color changes
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
                            var neighbourNormal = Vector3.Normalize(Vector3.TransformNormal(neighbourTile.Normal, neighbourOrientation));

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
            PositionComp.LocalAABB = new BoundingBox(m_min * GridSize - GridSizeHalfVector, m_max * GridSize + GridSizeHalfVector);
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
            Vector3 clamp = GridSizeHalfVector;

            bool exists = m_cubes.ContainsKey(cubePos + offset);
            exists &= m_cubes.ContainsKey(cubePos + a);
            exists &= m_cubes.ContainsKey(cubePos + b);

            // Three in corner exists, do bone transform
            if (exists)
            {
                var normal = Vector3I.One - absOffset;
                var centerBone = Vector3I.One;

                var targetBone = centerBone + offset;
                var upper = targetBone + normal;
                var lower = targetBone - normal;

                Vector3 moveDirection = -offset * MyGridConstants.CORNER_BONE_MOVE_DISTANCE; // 1m per axis in direction

                // Performance update: Instead of moveDirection.Length() we have precomputed the lengths, as the moveDirection is always constant * GridSize because of the constraint that
                // offset always has to contain 2x 1, and 1x 0. Since offset length will thus always be the same, we can treat it as a constant, and save the square root performance call.
                if (m_precalculatedCornerBonesDisplacementDistance <= 0)
                    m_precalculatedCornerBonesDisplacementDistance = moveDirection.Length();
                float displacementLength = m_precalculatedCornerBonesDisplacementDistance;

                // Now accomodate for grid size
                displacementLength *= GridSize;
                moveDirection *= GridSize;

                // The bones here exists
                MoveBone(ref cubePos, ref targetBone, ref moveDirection, ref displacementLength, ref clamp);
                MoveBone(ref cubePos, ref upper, ref moveDirection, ref displacementLength, ref clamp);
                MoveBone(ref cubePos, ref lower, ref moveDirection, ref displacementLength, ref clamp);

                minCube = Vector3I.Min(Vector3I.Min(cubePos, minCube), cubePos + offset - normal);
                maxCube = Vector3I.Max(Vector3I.Max(cubePos, maxCube), cubePos + offset + normal);
            }
            return exists;
        }

        public void GetExistingBones(Vector3I boneMin, Vector3I boneMax, Dictionary<Vector3I, MySlimBlock> resultSet, MyDamageInformation? damageInfo = null)
        {
            Vector3I cubeMin = Vector3I.Floor((boneMin - Vector3I.One) / (float)MyGridSkeleton.BoneDensity);
            Vector3I cubeMax = Vector3I.Ceiling((boneMax - Vector3I.One) / (float)MyGridSkeleton.BoneDensity);
            MyDamageInformation info = damageInfo.HasValue ? damageInfo.Value : default(MyDamageInformation);
            resultSet.Clear();
            Vector3I cube, boneBase;
            MyCube cubeInst;
            for (cube.X = cubeMin.X; cube.X <= cubeMax.X; cube.X++)
                for (cube.Y = cubeMin.Y; cube.Y <= cubeMax.Y; cube.Y++)
                    for (cube.Z = cubeMin.Z; cube.Z <= cubeMax.Z; cube.Z++)
                    {
                        if (m_cubes.TryGetValue(cube, out cubeInst) && cubeInst.CubeBlock.UsesDeformation)
                        {
                            if (cubeInst.CubeBlock.UseDamageSystem && damageInfo.HasValue)
                            {
                                info.Amount = 1;
                                MyDamageSystem.Static.RaiseBeforeDamageApplied(cubeInst.CubeBlock, ref info);
                                if (info.Amount == 0)
                                    continue;
                            }

                            //TODO:perf: in plane of armor this leads to lot of overrides on block edges
                            //3*3 = 9 bones are shared with neighbor - overriden in next iteration
                            boneBase = cube * MyGridSkeleton.BoneDensity;
                            foreach (var offset in Skeleton.BoneOffsets) 
                                resultSet[boneBase + offset] = cubeInst.CubeBlock;
                        }
                    }
        }

        private void MoveBone(ref Vector3I cubePos, ref Vector3I boneOffset, ref Vector3 moveDirection, ref float displacementLength, ref Vector3 clamp)
        {
            m_totalBoneDisplacement += displacementLength;
            var pos = cubePos * MyGridSkeleton.BoneDensity + boneOffset;
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

            if (!Sync.IsServer)
                return null;

            if (block == null)
                return null;

            MyCubeGrid retval = null;

            ProfilerShort.Begin("Test merge");

            BoundingBoxD aabb = (BoundingBoxD)new BoundingBox(block.Min * GridSize - GridSizeHalf, block.Max * GridSize + GridSizeHalf);
            // Inflate by half cube, so it will intersect for sure when there's anything
            aabb.Inflate(GridSizeHalf);
            aabb = aabb.TransformFast(WorldMatrix);
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

                    MyCubeGrid mergeTo = mergingGrid;
                    MyCubeGrid mergeFrom = grid;

                    // Change the merge order if needed.
                    if (grid.BlocksCount > mergingGrid.BlocksCount)
                    {
                        mergeTo = grid;
                        mergeFrom = mergingGrid;
                    }

                    Vector3D gridPosition = mergeFrom.PositionComp.GetPosition();
                    gridPosition = Vector3D.Transform(gridPosition, mergeTo.PositionComp.WorldMatrixNormalizedInv);
                    Vector3I gridOffsetFoo = Vector3I.Round(gridPosition * GridSizeR);

                    MyCubeGrid localMergedGrid = mergeTo.MergeGrid_Static(mergeFrom, gridOffsetFoo, block);

                    if (localMergedGrid != null)
                        retval = localMergedGrid;
                }
            }

            if (clearNearEntities)
                nearEntities.Clear(); // We don't want to hold references to objects and keep them alive!

            ProfilerShort.End();

            return retval;
        }

        /// <param name="gridOffset">Offset of second grid</param>
        private bool IsMergePossible_Static(MySlimBlock block, MyCubeGrid gridToMerge, out Vector3I gridOffset)
        {
            //Debug.Assert(this.WorldMatrix.Up == Vector3D.Up && this.WorldMatrix.Forward == Vector3D.Forward, "This grid must have identity rotation");
            //Debug.Assert(gridToMerge.WorldMatrix.Up == Vector3D.Up && gridToMerge.WorldMatrix.Forward == Vector3D.Forward, "Grid to merge must have identity rotation");

            Vector3D thisMergePos = this.PositionComp.GetPosition();
            thisMergePos = Vector3D.Transform(thisMergePos, gridToMerge.PositionComp.WorldMatrixNormalizedInv);
            gridOffset = -Vector3I.Round((thisMergePos) * GridSizeR);

            if (!IsOrientationsAligned(gridToMerge.WorldMatrix, this.WorldMatrix))
                return false;

            var transform = gridToMerge.CalculateMergeTransform(this, -gridOffset);
            Vector3I blockPosInSecondGrid;
            Vector3I.Transform(ref block.Position, ref transform, out blockPosInSecondGrid);

            MyBlockOrientation newOrientation = MatrixI.Transform(ref block.Orientation, ref transform);
            Quaternion blockOrientation;
            newOrientation.GetQuaternion(out blockOrientation);

            var mountPoints = block.BlockDefinition.GetBuildProgressModelMountPoints(block.BuildLevelRatio);
            return CheckConnectivity(gridToMerge, block.BlockDefinition, mountPoints, ref blockOrientation, ref blockPosInSecondGrid);
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
                                MyBlockOrientation newOrientation = MatrixI.Transform(ref blockInCompund.Orientation, ref transform);

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
                            MyBlockOrientation newOrientation = MatrixI.Transform(ref blockInMergeGrid.Orientation, ref transform);

                            if (localCompoundBlock.CanAddBlock(blockInMergeGrid.BlockDefinition, newOrientation))
                                continue;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        private MyCubeGrid MergeGrid_Static(MyCubeGrid gridToMerge, Vector3I gridOffset, MySlimBlock triggeringMergeBlock)
        {
            Debug.Assert(this.IsStatic && gridToMerge.IsStatic, "Grids to merge must be static");

            // We have to force replicate grids before merging them on client. Otherwise he won't have one of them and fails tragically.
            MyMultiplayer.ReplicateImmediatelly(gridToMerge, this);
            MyMultiplayer.ReplicateImmediatelly(this, gridToMerge);

            MatrixI transform = CalculateMergeTransform(gridToMerge, gridOffset);
  
            // Get transformed position of block that triggered merge.
            Vector3I mergingBlockPos = triggeringMergeBlock.Position;
            if (triggeringMergeBlock.CubeGrid != this)
                mergingBlockPos = Vector3I.Transform(mergingBlockPos, transform);

            MyMultiplayer.RaiseBlockingEvent(this, gridToMerge, x => x.MergeGrid_MergeClient, gridToMerge.EntityId, (SerializableVector3I)transform.Translation, transform.Forward, transform.Up, mergingBlockPos);

            MyCubeGrid newGrid = MergeGridInternal(gridToMerge, ref transform);

            newGrid.AdditionalModelGenerators.ForEach(g => g.BlockAddedToMergedGrid(triggeringMergeBlock));

            return newGrid;
        }

        /// <summary>
        /// Merges grids on client side.
        /// </summary>
        /// <param name="gridId">Grid id to be merge with.</param>
        /// <param name="gridOffset">Grid offset.</param>
        /// <param name="gridForward">Grid forward.</param>
        /// <param name="gridUp">Grid Up.</param>
        /// <param name="mergingBlockPos">Position of block that triggered merge.</param>
        [Event, Reliable, Broadcast, Blocking]
        void MergeGrid_MergeClient(long gridId, SerializableVector3I gridOffset, Base6Directions.Direction gridForward, Base6Directions.Direction gridUp, Vector3I mergingBlockPos)
        {
            MyCubeGrid grid = null;
            bool found = MyEntities.TryGetEntityById<MyCubeGrid>(gridId, out grid);
            Debug.Assert(found, "Grid not found ! please call Dusan");
            if (found)
            {
                MatrixI transform = new MatrixI(gridOffset, gridForward, gridUp);
                MyCubeGrid newGrid = MergeGridInternal(grid, ref transform);

                // Get block that triggered merge and regenerate Model Generators.
                MySlimBlock triggeringMergeBlock = newGrid.GetCubeBlock(mergingBlockPos);
                newGrid.AdditionalModelGenerators.ForEach(g => g.BlockAddedToMergedGrid(triggeringMergeBlock));

            }
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

            MyMultiplayer.ReplicateImmediatelly(gridToMerge, this);
            MyMultiplayer.ReplicateImmediatelly(this, gridToMerge);

            MyMultiplayer.RaiseBlockingEvent(this, gridToMerge, x => x.MergeGrid_MergeBlockClient, gridToMerge.EntityId, (SerializableVector3I)transform.Translation, transform.Forward, transform.Up);
            return MergeGridInternal(gridToMerge, ref transform);
        }

        [Event, Reliable, Broadcast, Blocking]
        void MergeGrid_MergeBlockClient(long gridId, SerializableVector3I gridOffset, Base6Directions.Direction gridForward, Base6Directions.Direction gridUp)
        {
            MyCubeGrid grid = null;
            bool found = MyEntities.TryGetEntityById<MyCubeGrid>(gridId, out grid);
            Debug.Assert(found, "Grid not found ! please call Dusan");
            if (found)
            {
                MatrixI transform = new MatrixI(gridOffset, gridForward, gridUp);
                MergeGridInternal(grid, ref transform);
            }
        }

        MyCubeGrid MergeGridInternal(MyCubeGrid gridToMerge, ref MatrixI transform, bool disableBlockGenerators = true)
        {
            ProfilerShort.Begin("MergeGridInternal");

            if (MyCubeGridSmallToLargeConnection.Static != null)
                MyCubeGridSmallToLargeConnection.Static.BeforeGridMerge_SmallToLargeGridConnectivity(this, gridToMerge);

            MoveBlocksAndClose(gridToMerge, this, transform, disableBlockGenerators: disableBlockGenerators);

            // Update AABB, physics, dirty blocks
            UpdateGridAABB();

            if (Physics != null)
            {
                // We need to update physics immediatelly because of landing gears
                UpdatePhysicsShape();
            }

            if (MyCubeGridSmallToLargeConnection.Static != null)
                MyCubeGridSmallToLargeConnection.Static.AfterGridMerge_SmallToLargeGridConnectivity(this);

            ProfilerShort.End();
            return this;
        }

        public void ChangeGridOwnership(long playerId, MyOwnershipShareModeEnum shareMode)
        {
            Debug.Assert(Sync.IsServer, "Changing grid ownership from the client");
            if (!Sync.IsServer) return;

            ChangeGridOwner(playerId, shareMode);
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
                    /*from.UpdatePhysicsShape();
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
            from.MarkedForClose = true;

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

                // "from" group could have been reinstantiated by OnModifyGroupSuccess
                // side effects: we have to remove it by name
                from.RemoveGroupByName(group.Name.ToString());
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
                block.RemoveAuthorship();
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
            from.m_fatBlocks.Clear();
            from.m_cubes.Clear();
            from.MarkedForClose = false; // TODO: CH, if MarkedForClose is set manually above, it must be reset manually here
            if (Sync.IsServer)
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

                    Debug.Assert(m_tmpSlimBlocks.Count == 0);

                    m_tmpSlimBlocks.Clear();
                    foreach (var blockInCompound in compoundBlock.GetBlocks())
                    {
                        ushort id;
                        if (existingCompoundBlock.Add(blockInCompound, out id))
                        {
                            BoundsInclude(blockInCompound);

                            m_dirtyRegion.AddCube(blockInCompound.Min);

                            Physics.AddDirtyBlock(existingSlimBlock);

                            m_tmpSlimBlocks.Add(blockInCompound);

                            added = true;
                        }
                    }

                    foreach (var blockToRemove in m_tmpSlimBlocks)
                    {
                        compoundBlock.Remove(blockToRemove, merged: true);
                    }

                    if (added)
                    {
                        if (MyCubeGridSmallToLargeConnection.Static != null && m_enableSmallToLargeConnections)
                            MyCubeGridSmallToLargeConnection.Static.AddBlockSmallToLargeConnection(block);

                        foreach (var blockAdded in m_tmpSlimBlocks)
                        {
                            NotifyBlockAdded(blockAdded);
                        }
                    }

                    m_tmpSlimBlocks.Clear();

                    return;
                }
            }

            m_cubeBlocks.Add(block);
            if (block.FatBlock != null)
                m_fatBlocks.Add(block.FatBlock);

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
                {
                    m_blocksForDraw.Add(block.FatBlock);
                    block.FatBlock.Render.NeedsDraw = false; //blocks shouldnt be drawn on their own at all?
                }

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

            if (MyCubeGridSmallToLargeConnection.Static != null && m_enableSmallToLargeConnections && blockAddSuccessfull)
            {
                ProfilerShort.Begin("CheckAddedBlockSmallToLargeConnection");
                MyCubeGridSmallToLargeConnection.Static.AddBlockSmallToLargeConnection(block);
                ProfilerShort.End();
            }

            if (MyFakes.ENABLE_MULTIBLOCK_PART_IDS)
            {
                ProfilerShort.Begin("AddMultiBlockInfo");
                AddMultiBlockInfo(block);
                ProfilerShort.End();
            }

            ProfilerShort.Begin("OnBlockAdded");
            NotifyBlockAdded(block);
            ProfilerShort.End();

            ProfilerShort.Begin("AddAuthorship");
            block.AddAuthorship();
            if (OnAuthorshipChanged != null)
                OnAuthorshipChanged(this);
            ProfilerShort.End();
        }

        private bool IsDamaged(Vector3I bonePos, float epsilon = 0.04f)
        {
            Vector3 bone;
            if(Skeleton.TryGetBone(ref bonePos, out bone))
                return !MyUtils.IsZero(ref bone, epsilon * GridSize);
            return false;
        }

        private void AddBlockEdges(MySlimBlock block)
        {
            var definition = block.BlockDefinition;
            if (definition.BlockTopology == MyBlockTopology.Cube && definition.CubeDefinition != null)
            {
                if (!definition.CubeDefinition.ShowEdges)
                    return;

                Vector3 position = block.Position * GridSize;
                Matrix blockTransformMatrix;
                block.Orientation.GetMatrix(out blockTransformMatrix);

                blockTransformMatrix.Translation = position;

                var info = MyCubeGridDefinitions.GetTopologyInfo(definition.CubeDefinition.CubeTopology);

                var baseBonePos = block.Position * MyGridSkeleton.BoneDensity + Vector3I.One;
                foreach (var edge in info.Edges)
                {
                    Vector3 point0 = Vector3.TransformNormal(edge.Point0, block.Orientation);
                    Vector3 point1 = Vector3.TransformNormal(edge.Point1, block.Orientation);
                    Vector3 middle = (point0 + point1) * 0.5f;

                    if (IsDamaged(baseBonePos + Vector3I.Round(point0)) ||
                        IsDamaged(baseBonePos + Vector3I.Round(middle)) ||
                        IsDamaged(baseBonePos + Vector3I.Round(point1)))
                        continue;

                    point0 = Vector3.Transform(edge.Point0 * GridSizeHalf, ref blockTransformMatrix);
                    point1 = Vector3.Transform(edge.Point1 * GridSizeHalf, ref blockTransformMatrix);

                    Vector3 normal0 = Vector3.TransformNormal(info.Tiles[edge.Side0].Normal, block.Orientation);
                    Vector3 normal1 = Vector3.TransformNormal(info.Tiles[edge.Side1].Normal, block.Orientation);

                    // Saturation and Value is offset, from -1 to 1, it must be normalized
                    var hsvNormalized = block.ColorMaskHSV;
                    hsvNormalized.Y = (hsvNormalized.Y + 1) * 0.5f;
                    hsvNormalized.Z = (hsvNormalized.Z + 1) * 0.5f;

                    Render.RenderData.AddEdgeInfo(ref point0, ref point1, ref normal0, ref normal1, new Color(hsvNormalized), block);
                }
            }
        }

        private void RemoveBlockEdges(MySlimBlock block)
        {
            var definition = block.BlockDefinition;
            if (definition.BlockTopology == MyBlockTopology.Cube && definition.CubeDefinition != null)
            {
                Vector3 position = block.Position * GridSize;
                Matrix blockTransformMatrix;
                block.Orientation.GetMatrix(out blockTransformMatrix);
                blockTransformMatrix.Translation = position;

                var info = MyCubeGridDefinitions.GetTopologyInfo(definition.CubeDefinition.CubeTopology);
                Vector3 point0, point1;
                foreach (var edge in info.Edges)
                {
                    point0 = Vector3.Transform(edge.Point0 * GridSizeHalf, blockTransformMatrix);
                    point1 = Vector3.Transform(edge.Point1 * GridSizeHalf, blockTransformMatrix);

                    Render.RenderData.RemoveEdgeInfo(point0, point1, block);
                }
            }
        }

        private void DoLazyUpdates()
        {
            if (MyCubeGridSmallToLargeConnection.Static != null && !m_smallToLargeConnectionsInitialized && m_enableSmallToLargeConnections)
            {
                // set flag before calling the method AddGridSmallToLargeConnection
                m_smallToLargeConnectionsInitialized = true;
                MyCubeGridSmallToLargeConnection.Static.AddGridSmallToLargeConnection(this);
            }
            m_smallToLargeConnectionsInitialized = true;

            ProfilerShort.Begin("Send dirty bones");
            if (!MyPerGameSettings.Destruction && BonesToSend.InputCount > 0 && m_bonesSendCounter++ > 10) // Only increment counter when there's something waiting
            {
                m_bonesSendCounter = 0;
                if (Sync.IsServer)
                {
                    var segments = BonesToSend.FindSegments(MyVoxelSegmentationType.Simple);
                    foreach (var seg in segments)
                    {
                        SendDirtyBones(seg.Min, seg.Max, Skeleton);
                    }
                }
                BonesToSend.ClearInput();
            }
            ProfilerShort.End();

            if (m_blocksForDamageApplicationDirty)
            {
                // Copy content to temporary, because block can be destroyed in "block.ApplyAccumulatedDamage()" and then it is also removed 
                // from m_blocksForDamageApplication (see MySlimBlock.OnDestroy).
                m_blocksForDamageApplicationCopy.AddHashset(m_blocksForDamageApplication);
                foreach (var block in m_blocksForDamageApplicationCopy)
                {
                    if (block.AccumulatedDamage > 0f)
                    {
                        block.ApplyAccumulatedDamage();
                    }
                }
                m_blocksForDamageApplication.Clear();
                m_blocksForDamageApplicationCopy.Clear();
                m_blocksForDamageApplicationDirty = false;
            }

            if (m_disconnectsDirty)
            {
                DetectDisconnects();
            }

            if (!MyPerGameSettings.Destruction)
                Skeleton.RemoveUnusedBones(this);

            if (m_ownershipManager.NeedRecalculateOwners)
            {
                m_ownershipManager.RecalculateOwners();
                m_ownershipManager.NeedRecalculateOwners = false;

                NotifyBlockOwnershipChange(this);
            }
        }

        internal void AddForDamageApplication(MySlimBlock block)
        {
            m_blocksForDamageApplication.Add(block);
            m_blocksForDamageApplicationDirty = true;
        }

        internal void RemoveFromDamageApplication(MySlimBlock block)
        {
            m_blocksForDamageApplication.Remove(block);
            m_blocksForDamageApplicationDirty = m_blocksForDamageApplication.Count > 0;
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
                MyPhysics.CastRay(line.From, line.To, m_tmpHitList, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);

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
                        if (MySession.Static.ControlledEntity != null)
                            while (j < m_tmpHitList.Count - 1 && m_tmpHitList[j].HkHitInfo.GetHitEntity() == MySession.Static.ControlledEntity.Entity)
                                j++;

                        if (j > 1 && m_tmpHitList[j].HkHitInfo.GetHitEntity() != this)
                            continue;
                        var bias = GridSizeHalfVector;
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
                                FixTargetCube(out nearest, locPos * GridSizeR);
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
                    VRage.Game.Models.MyIntersectionResultLineTriangleEx? intersection;
                    int cubePartIndex;
                    GetBlockIntersection(cube, ref line, IntersectionFlags.ALL_TRIANGLES, out intersection, out cubePartIndex);
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

        private void GetBlockIntersection(MyCube cube, ref LineD line, IntersectionFlags flags, out MyIntersectionResultLineTriangleEx? t, out int cubePartIndex)
        {
            if (cube.CubeBlock.FatBlock != null)
            {
                if (cube.CubeBlock.FatBlock is MyCompoundCubeBlock)
                {
                    MyCompoundCubeBlock compound = cube.CubeBlock.FatBlock as MyCompoundCubeBlock;
                    VRage.Game.Models.MyIntersectionResultLineTriangleEx? closestHit = null;
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
                                MatrixD? cubeWorldMatrix = block.FatBlock.WorldMatrix;
                                TransformCubeToGrid(ref correctIntersection, ref local, ref cubeWorldMatrix);
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
                        MatrixD? cubeWorldMatrix = cube.CubeBlock.FatBlock.WorldMatrix;
                        TransformCubeToGrid(ref correctIntersection, ref local, ref cubeWorldMatrix);
                        t = correctIntersection;
                    }
                }

                cubePartIndex = -1;
            }
            else
            {
                //cube block
                VRage.Game.Models.MyIntersectionResultLineTriangleEx? closestHit = null;
                float closestDistance = float.MaxValue;
                Vector3? closestHitpoint = null;
                int closestCubePartIndex = -1;

                for (int it = 0; it < cube.Parts.Length; it++)
                {
                    MyCubePart cubepart = cube.Parts[it];

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
                            Matrix localMatrix = cubepart.InstanceData.LocalMatrix;
                            MatrixD? cubeWorldMatrix = null;
                            TransformCubeToGrid(ref correctIntersection, ref localMatrix, ref cubeWorldMatrix);
                            closestHitpoint = correctIntersection.IntersectionPointInWorldSpace;
                            closestHit = correctIntersection;
                            closestCubePartIndex = it;
                        }
                    }
                }

                t = closestHit;
                cubePartIndex = closestCubePartIndex;
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
            if (PositionComp == null)
                return;
            BoundingBoxD box = new BoundingBoxD(sphere.Center - new Vector3D(sphere.Radius), sphere.Center + new Vector3D(sphere.Radius));

            box = box.TransformFast(this.PositionComp.WorldMatrixNormalizedInv);
            Vector3D min = box.Min;
            Vector3D max = box.Max;
            Vector3I start = new Vector3I((int)Math.Round(min.X * GridSizeR), (int)Math.Round(min.Y * GridSizeR), (int)Math.Round(min.Z * GridSizeR));
            Vector3I end = new Vector3I((int)Math.Round(max.X * GridSizeR), (int)Math.Round(max.Y * GridSizeR), (int)Math.Round(max.Z * GridSizeR));

            Vector3I startIt = Vector3I.Min(start, end);
            Vector3I endIt = Vector3I.Max(start, end);

            var localSphere = new BoundingSphereD(box.Center, sphere.Radius);
            BoundingBoxD blockBox = new BoundingBoxD();

            if ((endIt - startIt).Volume() < m_cubes.Count)
            {
                Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref startIt, ref endIt);
                var pos = it.Current;
                MyCube cube;
                for (; it.IsValid(); it.GetNext(out pos))
                {
                    if (m_cubes.TryGetValue(pos,out cube))
                    {
                        var block = cube.CubeBlock;
                        blockBox.Min = block.Min*GridSize - GridSizeHalf;
                        blockBox.Max = block.Max*GridSize + GridSizeHalf;
                        if (blockBox.Intersects(localSphere))
                        {
                            blocks.Add(block);
                        }
                    }
                }
            }
            else
            {
                foreach (var value in m_cubes.Values)
                {
                    var block = value.CubeBlock;
                    blockBox.Min = block.Min*GridSize - GridSizeHalf;
                    blockBox.Max = block.Max*GridSize + GridSizeHalf;
                    if (blockBox.Intersects(localSphere))
                    {
                        blocks.Add(block);
                    }
                }
            }
        }

        private void QuerySphere(BoundingSphereD sphere, List<MyEntity> blocks)
        {
            Debug.Assert(!Closed);

            if (PositionComp == null)
                return;

            // CH: Testing code to catch a crash:
            if (Closed) MyLog.Default.WriteLine("Grid was Closed in MyCubeGrid.QuerySphere!");

            if (sphere.Contains(PositionComp.WorldVolume) == ContainmentType.Contains)
            {
                foreach (var fb in m_fatBlocks)
                {
                    Debug.Assert(m_cubeBlocks.Contains(fb.SlimBlock));
                    // Assert commented since this will happen a lot in AI School, scenario 2, when the fixing bot is fixing 
                    //Debug.Assert(!fb.Closed);
                    if(fb.Closed) //TODO:investigate why there is closed block in the grid/m_fatblock list
                        continue; //it is possible to have marked for close block there but not closed
                    blocks.Add(fb);

                    foreach (var child in fb.Hierarchy.Children)
                    {
                        var entity = (MyEntity)child.Entity;
                        if (entity != null)
                            blocks.Add(entity);
                    }
                }
                return;
            }

            BoundingBoxD box = new BoundingBoxD(sphere.Center - new Vector3D(sphere.Radius), sphere.Center + new Vector3D(sphere.Radius));

            box = box.TransformFast(this.PositionComp.WorldMatrixNormalizedInv);
            Vector3D min = box.Min;
            Vector3D max = box.Max;
            Vector3I start = new Vector3I((int)Math.Round(min.X * GridSizeR), (int)Math.Round(min.Y * GridSizeR), (int)Math.Round(min.Z * GridSizeR));
            Vector3I end = new Vector3I((int)Math.Round(max.X * GridSizeR), (int)Math.Round(max.Y * GridSizeR), (int)Math.Round(max.Z * GridSizeR));

            Vector3I startIt = Vector3I.Min(start, end);
            Vector3I endIt = Vector3I.Max(start, end);
            startIt = Vector3I.Max(startIt, Min);
            endIt = Vector3I.Min(endIt, Max);
            if (startIt.X > endIt.X || startIt.Y > endIt.Y || startIt.Z > endIt.Z)
                return;

            Vector3 halfGridSize = new Vector3(0.5f);
            BoundingBox blockBB = new BoundingBox();
            var localSphere = new BoundingSphere((Vector3)box.Center * GridSizeR, (float)sphere.Radius * GridSizeR);
            if ((endIt - startIt).Size > m_cubeBlocks.Count)
            {
                foreach (var fb in m_fatBlocks)
                {
                    Debug.Assert(!fb.Closed);
                    if (fb.Closed) //TODO:investigate why there is closed block in the grid/m_fatblock list
                        continue; //it is possible to have marked for close block there but not closed
                    blockBB.Min = fb.Min - halfGridSize;
                    blockBB.Max = fb.Max + halfGridSize;
                    if (localSphere.Intersects(blockBB))
                    {
                        blocks.Add(fb);

                        foreach (var child in fb.Hierarchy.Children)
                        {
                            var entity = (MyEntity)child.Entity;
                            if (entity != null)
                                blocks.Add(entity);
                        }
                    }
                }
                return;
            }

            MyCube block;
            if (m_tmpQueryCubeBlocks == null)
                m_tmpQueryCubeBlocks = new HashSet<MyEntity>();

            // CH: Testing code to catch a crash:
            if (m_cubes == null) MyLog.Default.WriteLine("m_cubes null in MyCubeGrid.QuerySphere!");

            Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref startIt, ref endIt);
            var pos = it.Current;
            for (; it.IsValid(); it.GetNext(out pos))
            {
                if (m_cubes.TryGetValue(pos, out block) && block.CubeBlock.FatBlock != null)
                {
                    Debug.Assert(!block.CubeBlock.FatBlock.Closed);
                    if (block.CubeBlock.FatBlock.Closed) //TODO:investigate why there is closed block in the grid/m_fatblock list
                        continue; //it is possible to have marked for close block there but not closed
                    if (m_tmpQueryCubeBlocks.Contains(block.CubeBlock.FatBlock))
                        continue;

                    blockBB.Min = block.CubeBlock.Min - halfGridSize;
                    blockBB.Max = block.CubeBlock.Max + halfGridSize;
                    if (localSphere.Intersects(blockBB))
                    {
                        blocks.Add(block.CubeBlock.FatBlock);
                        m_tmpQueryCubeBlocks.Add(block.CubeBlock.FatBlock);

                        foreach (var child in block.CubeBlock.FatBlock.Hierarchy.Children)
                        {
                            var entity = (MyEntity)child.Entity;
                            if (entity != null)
                            {
                                blocks.Add(entity);
                                m_tmpQueryCubeBlocks.Add(entity);
                            }
                        }
                    }
                }
            }
            m_tmpQueryCubeBlocks.Clear();
        }

        /// <summary>
        /// Correct interesection transforming vertices from cube to grid coordinates
        /// </summary>
        private void TransformCubeToGrid(ref MyIntersectionResultLineTriangleEx triangle, ref Matrix cubeLocalMatrix, ref MatrixD? cubeWorldMatrix)
        {
            if (cubeWorldMatrix == null)
            {
                MatrixD gridWorldMatrix = WorldMatrix;
                triangle.IntersectionPointInObjectSpace = Vector3.Transform(triangle.IntersectionPointInObjectSpace, ref cubeLocalMatrix);
                triangle.IntersectionPointInWorldSpace = Vector3.Transform(triangle.IntersectionPointInObjectSpace, gridWorldMatrix);
                triangle.NormalInObjectSpace = Vector3.TransformNormal(triangle.NormalInObjectSpace, ref cubeLocalMatrix);
                triangle.NormalInWorldSpace = Vector3.TransformNormal(triangle.NormalInObjectSpace, gridWorldMatrix);
            }
            else
            {
                Vector3 intersectionLocal = triangle.IntersectionPointInObjectSpace;
                Vector3 normalLocal = triangle.NormalInObjectSpace;

                triangle.IntersectionPointInObjectSpace = Vector3.Transform(intersectionLocal, ref cubeLocalMatrix);
                triangle.IntersectionPointInWorldSpace = Vector3.Transform(intersectionLocal, cubeWorldMatrix.Value);
                triangle.NormalInObjectSpace = Vector3.TransformNormal(normalLocal, ref cubeLocalMatrix);
                triangle.NormalInWorldSpace = Vector3.TransformNormal(normalLocal, cubeWorldMatrix.Value);
            }

            triangle.Triangle.InputTriangle.Transform(ref cubeLocalMatrix);
        }

        private void QueryLine(LineD line, List<MyLineSegmentOverlapResult<MyEntity>> blocks)
        {
            MyLineSegmentOverlapResult<MyEntity> overlap = new MyLineSegmentOverlapResult<MyEntity>();
            BoundingBoxD bb = new BoundingBoxD();
            MatrixD invWorld = PositionComp.WorldMatrixNormalizedInv;//MatrixD.Invert(WorldMatrix);
            Vector3D localStart, localEnd;
            Vector3D.Transform(ref line.From, ref invWorld, out localStart);
            Vector3D.Transform(ref line.To, ref invWorld, out localEnd);

            RayD lineLoc = new RayD(localStart, Vector3D.Normalize(localEnd - localStart));
            MyCube cube;
            RayCastCells(line.From, line.To, m_cacheRayCastCells);
            foreach (var pos in m_cacheRayCastCells)
            {
                if (m_cubes.TryGetValue(pos, out cube) && cube.CubeBlock.FatBlock != null)
                {
                    var block = cube.CubeBlock.FatBlock;
                    overlap.Element = block;
                    bb.Min = block.Min * GridSize - GridSizeHalfVector;
                    bb.Max = block.Max * GridSize + GridSizeHalfVector;
                    var intersect = lineLoc.Intersects(bb);
                    if (!intersect.HasValue)
                        continue;
                    overlap.Distance = intersect.Value;
                    blocks.Add(overlap);
                }
            }
        }

        [ThreadStatic]
        private static HashSet<MyEntity> m_tmpQueryCubeBlocks;

        private void QueryAABB(BoundingBoxD box, List<MyEntity> blocks)
        {
            if (blocks == null)
            {
                Debug.Fail("null blocks ! probably dead entity ?");
                return;
            }

            if (PositionComp == null)
            {
                return;
            }

            if (box.Contains(PositionComp.WorldAABB) == ContainmentType.Contains)
            {
                foreach (var fb in m_fatBlocks)
                {
                    Debug.Assert(m_cubeBlocks.Contains(fb.SlimBlock));
                    Debug.Assert(!fb.Closed,m_fatBlocks.Count+", "+m_cubeBlocks.Count);
                    if (fb.Closed) //TODO:investigate why there is closed block in the grid/m_fatblock list
                        continue; //it is possible to have marked for close block there but not closed

                    blocks.Add(fb);
                    if (fb.Hierarchy != null && fb.Hierarchy.Children != null)
                    {
                        foreach (var child in fb.Hierarchy.Children)
                        {
                            if (child.Container != null)
                            {
                                blocks.Add((MyEntity)child.Container.Entity);
                            }
                            else
                            {
                                System.Diagnostics.Debug.Fail("Child block hasn't container set!");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Fail("Fatblock missing hieararchy component!");
                    }
                }
                return;
            }
            var obb = MyOrientedBoundingBoxD.Create(box, PositionComp.WorldMatrixNormalizedInv);
            obb.Center *= GridSizeR;
            obb.HalfExtent *= GridSizeR;
            box = box.TransformFast(this.PositionComp.WorldMatrixNormalizedInv);
            Vector3D min = box.Min;
            Vector3D max = box.Max;
            Vector3I start = new Vector3I((int)Math.Round(min.X * GridSizeR), (int)Math.Round(min.Y * GridSizeR), (int)Math.Round(min.Z * GridSizeR));
            Vector3I end = new Vector3I((int)Math.Round(max.X * GridSizeR), (int)Math.Round(max.Y * GridSizeR), (int)Math.Round(max.Z * GridSizeR));

            Vector3I startIt = Vector3I.Min(start, end);
            Vector3I endIt = Vector3I.Max(start, end);

            startIt = Vector3I.Max(startIt, Min);
            endIt = Vector3I.Min(endIt, Max);
            if (startIt.X > endIt.X || startIt.Y > endIt.Y || startIt.Z > endIt.Z)
                return;
            Vector3 halfGridSize = new Vector3(0.5f);
            BoundingBoxD blockBB = new BoundingBoxD();

            if ((endIt - startIt).Size > m_cubeBlocks.Count)
            {
                foreach (var fb in m_fatBlocks)
                {
                    Debug.Assert(m_cubeBlocks.Contains(fb.SlimBlock));
                    Debug.Assert(!fb.Closed);
                    if (fb.Closed) //TODO:investigate why there is closed block in the grid/m_fatblock list
                        continue; //it is possible to have marked for close block there but not closed

                    blockBB.Min = fb.Min - halfGridSize;
                    blockBB.Max = fb.Max + halfGridSize;
                    if (obb.Intersects(ref blockBB))
                    {
                        blocks.Add(fb);
                        if (fb.Hierarchy != null && fb.Hierarchy.Children != null)
                        {
                            foreach (var child in fb.Hierarchy.Children)
                            {
                                if (child.Container != null)
                                {
                                    blocks.Add((MyEntity)child.Container.Entity);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.Fail("Child block hasn't container set!");
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.Fail("Fatblock is missing hierarchy component!");
                        }
                    }
                }
                return;
            }

            MyCube block;
            Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref startIt, ref endIt);
            var pos = it.Current;
            if (m_tmpQueryCubeBlocks == null) m_tmpQueryCubeBlocks = new HashSet<MyEntity>();
            for (; it.IsValid(); it.GetNext(out pos))
            {
                System.Diagnostics.Debug.Assert(m_cubes != null, "m_cubes on MyCubeGrid are null!");
                if (m_cubes != null && m_cubes.TryGetValue(pos, out block) && block.CubeBlock.FatBlock != null)
                {
                    var fatBlock = block.CubeBlock.FatBlock;
                    if (m_tmpQueryCubeBlocks.Contains(fatBlock))
                        continue;
                    blockBB.Min = block.CubeBlock.Min - halfGridSize;
                    blockBB.Max = block.CubeBlock.Max + halfGridSize;
                    if (obb.Intersects(ref blockBB))
                    {
                        m_tmpQueryCubeBlocks.Add(fatBlock);
                        blocks.Add(fatBlock);
                        if (fatBlock.Hierarchy != null && fatBlock.Hierarchy.Children != null)
                        {
                            foreach (var child in fatBlock.Hierarchy.Children)
                            {
                                if (child.Container != null)
                                {
                                    blocks.Add((MyEntity)child.Container.Entity);
                                    m_tmpQueryCubeBlocks.Add(fatBlock);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.Fail("child block hasn't container set!");
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.Fail("Fatblock is missing hierarchy component!");
                        }
                    }
                }
            }
            m_tmpQueryCubeBlocks.Clear();
        }

        [ThreadStatic]
        private static HashSet<MySlimBlock> m_tmpQuerySlimBlocks;

        public void GetBlocksIntersectingOBB(BoundingBoxD box, MatrixD boxTransform, List<MySlimBlock> blocks)
        {
            if (blocks == null)
            {
                Debug.Fail("null blocks ! probably dead entity ?");
                return;
            }

            if (PositionComp == null)
            {
                return;
            }

            var obbWorld = MyOrientedBoundingBoxD.Create(box, boxTransform);
            var gridWorldAabb = PositionComp.WorldAABB;
            if (obbWorld.Contains(ref gridWorldAabb) == ContainmentType.Contains)
            {
                foreach (var slimBlock in GetBlocks())
                {
                    if (slimBlock.FatBlock != null)
                    {
                        Debug.Assert(!slimBlock.FatBlock.Closed);
                        if (slimBlock.FatBlock.Closed) //TODO:investigate why there is closed block in the grid/m_fatblock list
                            continue; //it is possible to have marked for close block there but not closed
                    }

                    blocks.Add(slimBlock);
                }

                return;
            }

            var compositeTransform = boxTransform * PositionComp.WorldMatrixNormalizedInv;
            var obb = MyOrientedBoundingBoxD.Create(box, compositeTransform);
            obb.Center *= GridSizeR;
            obb.HalfExtent *= GridSizeR;
            box = box.TransformFast(compositeTransform);
            Vector3D min = box.Min;
            Vector3D max = box.Max;
            Vector3I start = new Vector3I((int)Math.Round(min.X * GridSizeR), (int)Math.Round(min.Y * GridSizeR), (int)Math.Round(min.Z * GridSizeR));
            Vector3I end = new Vector3I((int)Math.Round(max.X * GridSizeR), (int)Math.Round(max.Y * GridSizeR), (int)Math.Round(max.Z * GridSizeR));

            Vector3I startIt = Vector3I.Min(start, end);
            Vector3I endIt = Vector3I.Max(start, end);

            startIt = Vector3I.Max(startIt, Min);
            endIt = Vector3I.Min(endIt, Max);
            if (startIt.X > endIt.X || startIt.Y > endIt.Y || startIt.Z > endIt.Z)
                return;
            Vector3 halfGridSize = new Vector3(0.5f);
            BoundingBoxD blockBB = new BoundingBoxD();

            if ((endIt - startIt).Size > m_cubeBlocks.Count)
            {
                foreach (var slimBlock in GetBlocks())
                {
                    Debug.Assert(!slimBlock.FatBlock.Closed);
                    if (slimBlock.FatBlock.Closed) //TODO:investigate why there is closed block in the grid/m_fatblock list
                        continue; //it is possible to have marked for close block there but not closed

                    blockBB.Min = slimBlock.Min - halfGridSize;
                    blockBB.Max = slimBlock.Max + halfGridSize;
                    if (obb.Intersects(ref blockBB))
                    {
                        blocks.Add(slimBlock);
                    }
                }
                return;
            }

            MyCube block;
            if (m_tmpQuerySlimBlocks == null)
                m_tmpQuerySlimBlocks = new HashSet<MySlimBlock>();
            Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref startIt, ref endIt);
            var pos = it.Current;
            for (; it.IsValid(); it.GetNext(out pos))
            {
                System.Diagnostics.Debug.Assert(m_cubes != null, "m_cubes on MyCubeGrid are null!");
                if (m_cubes != null && m_cubes.TryGetValue(pos, out block) && block.CubeBlock != null)
                {
                    var slimBlock = block.CubeBlock;
                    if (m_tmpQuerySlimBlocks.Contains(slimBlock))
                        continue;
                    blockBB.Min = slimBlock.Min - halfGridSize;
                    blockBB.Max = slimBlock.Max + halfGridSize;
                    if (obb.Intersects(ref blockBB))
                    {
                        m_tmpQuerySlimBlocks.Add(slimBlock);
                        blocks.Add(slimBlock);
                    }
                }
            }
            m_tmpQuerySlimBlocks.Clear();
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

            Vector3D center;
            Vector3D.Transform(ref sphere3.Center, ref invWorldGrid, out center);


            Vector3I startIt = Vector3I.Round((center - sphere3.Radius) * GridSizeR);
            Vector3I endIt = Vector3I.Round((center + sphere3.Radius) * GridSizeR);

            var halfSize = new Vector3(detectionBlockHalfSize);
            var localSphere1 = new BoundingSphereD(center, sphere1.Radius);
            var localSphere2 = new BoundingSphereD(center, sphere2.Radius);
            var localSphere3 = new BoundingSphereD(center, sphere3.Radius);

            int cellsToCheck = (endIt.X - startIt.X) * (endIt.Y - startIt.Y) * (endIt.Z - startIt.Z);
            if (cellsToCheck < m_cubes.Count)
            {
                Vector3I cubePos = new Vector3I();
                MyCube cube;
                for (cubePos.X = startIt.X; cubePos.X <= endIt.X; cubePos.X++)
                {
                    for (cubePos.Y = startIt.Y; cubePos.Y <= endIt.Y; cubePos.Y++)
                    {
                        for (cubePos.Z = startIt.Z; cubePos.Z <= endIt.Z; cubePos.Z++)
                        {
                            if (m_cubes.TryGetValue(cubePos, out cube))
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
                                    blockBox = new BoundingBox(block.Min * GridSize - GridSizeHalf, block.Max * GridSize + GridSizeHalf);
                                }
                                else
                                {
                                    blockBox = new BoundingBox(block.Position * GridSize - halfSize, block.Position * GridSize + halfSize);
                                }

                                //If biggest sphere fails no need to check the rest
                                if (blockBox.Intersects(localSphere3))
                                {
                                    if (blockBox.Intersects(localSphere2))
                                    {
                                        if(blockBox.Intersects(localSphere1))
                                            blocks1.Add(block);
                                        else
                                            blocks2.Add(block);
                                    }
                                    else
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
                        blockBox = new BoundingBox(block.Min * GridSize - GridSizeHalf, block.Max * GridSize + GridSizeHalf);
                    }
                    else
                    {
                        blockBox = new BoundingBox(block.Position * GridSize - halfSize, block.Position * GridSize + halfSize);
                    }

                    //If biggest sphere fails no need to check the rest
                    if (blockBox.Intersects(localSphere3))
                    {
                        if (blockBox.Intersects(localSphere2))
                        {
                            if (blockBox.Intersects(localSphere1))
                                blocks1.Add(block);
                            else
                                blocks2.Add(block);
                        }
                        else
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
        public void RayCastCells(Vector3D worldStart, Vector3D worldEnd, List<Vector3I> outHitPositions, Vector3I? gridSizeInflate = null, bool havokWorld = false, 
            bool clearOutHitPositions = true)
        {
            MatrixD invWorld = PositionComp.WorldMatrixNormalizedInv;//MatrixD.Invert(WorldMatrix);
            Vector3D localStart, localEnd;
            Vector3D.Transform(ref worldStart, ref invWorld, out localStart);
            Vector3D.Transform(ref worldEnd, ref invWorld, out localEnd);

            //We need move the line, because MyGridIntersection calculates the center of the box in the corner
            var offset = GridSizeHalfVector;
            localStart += offset;
            localEnd += offset;

            var min = Min - Vector3I.One;
            var max = Max + Vector3I.One;
            if (gridSizeInflate.HasValue)
            {
                min -= gridSizeInflate.Value;
                max += gridSizeInflate.Value;
            }

            if (clearOutHitPositions)
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

            var min = -Vector3I.One;
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
            public MyBlockOrientation Orientation; // Will be different

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
                Orientation = new MyBlockOrientation(ref orientation);
                EntityId = entityId;
                Owner = owner;
            }
        }

        [ProtoContract]
        public struct BlockPositionId
        {
            [ProtoMember]
            public Vector3I Position;

            [ProtoMember]
            public uint CompoundId;
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
                //name = MyTexts.GetString(MySpaceTexts.CustomShipName_Platform);
                name = MyTexts.GetString(MyCommonTexts.DetailStaticGrid);
            }
            else
            {
                switch (GridSizeEnum)
                {
                    //case Common.ObjectBuilders.MyCubeSize.Small: name = MyTexts.GetString(MySpaceTexts.CustomShipName_SmallShip); break;
                    case MyCubeSize.Small: name = MyTexts.GetString(MyCommonTexts.DetailSmallGrid); break;
                    //case Common.ObjectBuilders.MyCubeSize.Large: name = MyTexts.GetString(MySpaceTexts.CustomShipName_LargeShip); break;
                    case MyCubeSize.Large: name = MyTexts.GetString(MyCommonTexts.DetailLargeGrid); break;
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

            protected override void OnWorldPositionChanged(object source, bool updateChildren)
            {
                m_grid.m_worldPositionChanged = true;
                base.OnWorldPositionChanged(source, updateChildren);
            }

        }

        public MyFracturedBlock CreateFracturedBlock(MyObjectBuilder_FracturedBlock fracturedBlockBuilder, Vector3I position)
        {
            Debug.Assert(!MyFakes.ENABLE_FRACTURE_COMPONENT);
            ProfilerShort.Begin("CreateFractureBlockBuilder");
            var defId = new MyDefinitionId(typeof(MyObjectBuilder_FracturedBlock), "FracturedBlockLarge");
            var def = Sandbox.Definitions.MyDefinitionManager.Static.GetCubeBlockDefinition(defId);
            var bPos = position;
            MyCube oldBlock;
            if (m_cubes.TryGetValue(bPos, out oldBlock))
                RemoveBlockInternal(oldBlock.CubeBlock, close: true);
            fracturedBlockBuilder.CreatingFracturedBlock = true;

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
            Debug.Assert(!MyFakes.ENABLE_FRACTURE_COMPONENT);
            ProfilerShort.Begin("CreateFractureBlock");
            var defId = new MyDefinitionId(typeof(MyObjectBuilder_FracturedBlock), "FracturedBlockLarge");
            var def = Sandbox.Definitions.MyDefinitionManager.Static.GetCubeBlockDefinition(defId);
            var bPos = info.Position;// m_grid.WorldToGridInteger(worldMatrix.Translation);
            MyCube oldBlock;
            if (m_cubes.TryGetValue(bPos, out oldBlock))
                RemoveBlock(oldBlock.CubeBlock, false);

            var blockObjectBuilder = MyCubeGrid.CreateBlockObjectBuilder(def, bPos, new MyBlockOrientation(ref  Quaternion.Identity), 0, 0, true);
            blockObjectBuilder.ColorMaskHSV = Vector3.Zero;
            (blockObjectBuilder as MyObjectBuilder_FracturedBlock).CreatingFracturedBlock = true;

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
            Debug.Assert(info.MultiBlocks == null || info.MultiBlocks.Count == fb.OriginalBlocks.Count);
            fb.MultiBlocks = info.MultiBlocks;

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
        public bool EnableGenerators(bool enable, bool fromServer = false)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer || fromServer, "Cannot change this on client");

            bool oldEnabled = m_generatorsEnabled;

            if (Sync.IsServer || fromServer)
            {
                Debug.Assert(Render is MyRenderComponentCubeGrid, "Invalid Render - cannot access grid generators");
                if (!(Render is MyRenderComponentCubeGrid))
                {
                    m_generatorsEnabled = false;
                    return false;
                }

                if (m_generatorsEnabled != enable)
                {
                    AdditionalModelGenerators.ForEach(g => g.EnableGenerator(enable));
                    m_generatorsEnabled = enable;

                    //if (Sync.IsServer)
                    //    MySyncDestructions.EnableGenerators(this, enable);
                }
            }

            return oldEnabled;
        }

        // Returns generating block (not compound block, but block inside) from generated one. Can return also MyFracturedBlock!
        public MySlimBlock GetGeneratingBlock(MySlimBlock generatedBlock)
        {
            if (generatedBlock == null || !generatedBlock.BlockDefinition.IsGeneratedBlock)
                return null;

            foreach (var generator in AdditionalModelGenerators)
            {
                var generatingBlock = generator.GetGeneratingBlock(generatedBlock);
                if (generatingBlock != null)
                    return generatingBlock;
            }

            return null;
        }

        private static readonly Vector3I[] m_tmpBlockSurroundingOffsets = new Vector3I[]
        {
            new Vector3I(0, 0, 0),
            new Vector3I(1, 0, 0),
            new Vector3I(-1, 0, 0),
            new Vector3I(0, 0, 1),
            new Vector3I(0, 0, -1),
            new Vector3I(1, 0, 1),
            new Vector3I(-1, 0, 1),
            new Vector3I(1, 0, -1),
            new Vector3I(-1, 0, -1),

            new Vector3I(0, 1, 0),
            new Vector3I(1, 1, 0),
            new Vector3I(-1, 1, 0),
            new Vector3I(0, 1, 1),
            new Vector3I(0, 1, -1),
            new Vector3I(1, 1, 1),
            new Vector3I(-1, 1, 1),
            new Vector3I(1, 1, -1),
            new Vector3I(-1, 1, -1),

            new Vector3I(0, -1, 0),
            new Vector3I(1, -1, 0),
            new Vector3I(-1, -1, 0),
            new Vector3I(0, -1, 1),
            new Vector3I(0, -1, -1),
            new Vector3I(1, -1, 1),
            new Vector3I(-1, -1, 1),
            new Vector3I(1, -1, -1),
            new Vector3I(-1, -1, -1),
        };

        /// <summary>
        /// Returns array of generated blocks from given generating block. 
        /// </summary>
        public void GetGeneratedBlocks(MySlimBlock generatingBlock, List<MySlimBlock> outGeneratedBlocks)
        {
            ProfilerShort.Begin("MyCubeGrid.GetGeneratedBlocks");
            Debug.Assert(!(generatingBlock.FatBlock is MyCompoundCubeBlock));

            outGeneratedBlocks.Clear();

            if (generatingBlock == null || (generatingBlock.FatBlock is MyCompoundCubeBlock))
            {
                ProfilerShort.End();
                return;
            }

            if (generatingBlock.BlockDefinition.IsGeneratedBlock || generatingBlock.BlockDefinition.GeneratedBlockDefinitions == null
                || generatingBlock.BlockDefinition.GeneratedBlockDefinitions.Length == 0)
            {
                ProfilerShort.End();
                return;
            }

            foreach (var offset in m_tmpBlockSurroundingOffsets)
            {
                var surroundingBlock = generatingBlock.CubeGrid.GetCubeBlock(generatingBlock.Position + offset);
                if (surroundingBlock == null || surroundingBlock == generatingBlock)
                    continue;

                if (surroundingBlock.FatBlock is MyCompoundCubeBlock)
                {
                    var compoundBlock = surroundingBlock.FatBlock as MyCompoundCubeBlock;

                    foreach (var blockInCompound in compoundBlock.GetBlocks())
                    {
                        if (blockInCompound == generatingBlock || !blockInCompound.BlockDefinition.IsGeneratedBlock)
                            continue;

                        foreach (var generator in AdditionalModelGenerators)
                        {
                            var localGeneratingBlock = generator.GetGeneratingBlock(blockInCompound);
                            if (generatingBlock != localGeneratingBlock)
                                continue;

                            outGeneratedBlocks.Add(blockInCompound);
                        }

                    }
                }
                else
                {
                    if (!surroundingBlock.BlockDefinition.IsGeneratedBlock)
                        continue;

                    foreach (var generator in AdditionalModelGenerators)
                    {
                        var localGeneratingBlock = generator.GetGeneratingBlock(surroundingBlock);
                        if (generatingBlock != localGeneratingBlock)
                            continue;

                        outGeneratedBlocks.Add(surroundingBlock);
                    }
                }
            }
            ProfilerShort.End();
        }

        public void OnIntegrityChanged(MySlimBlock block)
        {
            NotifyBlockIntegrityChanged(block);
        }

        public void PasteBlocksToGrid(List<MyObjectBuilder_CubeGrid> gridsToMerge, long inventoryEntityId, bool multiBlock, bool instantBuild)
        {
            MyMultiplayer.RaiseEvent(this, x => x.PasteBlocksToGridServer_Implementation, gridsToMerge, inventoryEntityId, multiBlock, instantBuild);
        }

        [Event, Reliable, Server]
        private void PasteBlocksToGridServer_Implementation(List<MyObjectBuilder_CubeGrid> gridsToMerge, long inventoryEntityId, bool multiBlock, bool instantBuild)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }
            bool isAdmin = (MyEventContext.Current.IsLocallyInvoked || MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value));

            MyEntities.RemapObjectBuilderCollection(gridsToMerge);
            MatrixI mergeTransform = PasteBlocksServer(gridsToMerge);

            if ((isAdmin && instantBuild) == false)
            {
                MyEntity entity;
                if (MyEntities.TryGetEntityById(inventoryEntityId, out entity) && entity != null)
                {
                    MyInventoryBase buildInventory = MyCubeBuilder.BuildComponent.GetBuilderInventory(entity);
                    if (buildInventory != null)
                    {
                        if (multiBlock)
                        {
                            MyMultiBlockClipboard.TakeMaterialsFromBuilder(gridsToMerge, entity);
                        }
                        else
                        {
                            MyGridClipboard.CalculateItemRequirements(gridsToMerge, m_buildComponents);
                            foreach (var item in m_buildComponents.TotalMaterials)
                            {
                                buildInventory.RemoveItemsOfType(item.Value, item.Key);
                            }
                        }
                    }
                }
            }

            MyMultiplayer.RaiseEvent(this, x => x.PasteBlocksToGridClient_Implementation, gridsToMerge[0], mergeTransform);
        }

        [Event, Reliable, Broadcast]
        private void PasteBlocksToGridClient_Implementation(MyObjectBuilder_CubeGrid gridToMerge, MatrixI mergeTransform)
        {
            PasteBlocksClient(gridToMerge, mergeTransform);
        }

        public void UpdateOxygenAmount(float[] oxygenAmount)
        {
            Debug.Assert(Sync.IsServer);
            Debug.Assert(MySession.Static.Settings.EnableOxygen);
            Debug.Assert(MySession.Static.Settings.EnableOxygenPressurization);
            MyMultiplayer.RaiseEvent(this, x => x.UpdateOxygenAmount_Implementation, oxygenAmount);
        }

        // TODO: Make unreliable!
        [Event, Reliable, Broadcast]
        private void UpdateOxygenAmount_Implementation(float[] oxygenAmount)
        {
            Debug.Assert(MySession.Static.Settings.EnableOxygen);
            Debug.Assert(MySession.Static.Settings.EnableOxygenPressurization);
            if (GridSystems != null && GridSystems.GasSystem != null)
            {
                GridSystems.GasSystem.UpdateOxygenAmount(oxygenAmount);
            }
        }

        private void PasteBlocksClient(MyObjectBuilder_CubeGrid gridToMerge, MatrixI mergeTransform)
        {
            var pastedGrid = MyEntities.CreateFromObjectBuilder(gridToMerge) as MyCubeGrid;
            pastedGrid.SentFromServer = true;
            MyEntities.Add(pastedGrid);
            MySession.Static.TotalBlocksCreated += (uint)pastedGrid.BlocksCount;
            MergeGridInternal(pastedGrid, ref mergeTransform);
        }

        private MatrixI PasteBlocksServer(List<MyObjectBuilder_CubeGrid> gridsToMerge)
        {
            Debug.Assert(Sync.IsServer);
            MyCubeGrid firstEntity = null;
            foreach (var gridbuilder in gridsToMerge)
            {
                var pastedGrid = MyEntities.CreateFromObjectBuilder(gridbuilder) as MyCubeGrid;
                if (firstEntity == null)
                    firstEntity = pastedGrid;
                MyEntities.Add(pastedGrid);
                MySession.Static.TotalBlocksCreated += (uint)pastedGrid.BlocksCount;
            }

            MatrixI mergingTransform = CalculateMergeTransform(firstEntity,
                WorldToGridInteger(firstEntity.PositionComp.GetPosition()));
            MergeGridInternal(firstEntity, ref mergingTransform, false);
            return mergingTransform;
        }

        public static bool CanPasteGrid()
        {
            return MySession.Static.IsCopyPastingEnabled;
        }

        /// <summary>
        /// Returns biggest grid in physical group by AABB volume
        /// </summary>
        public MyCubeGrid GetBiggestGridInGroup()
        {
            MyCubeGrid biggestGridInGroup = this;
            double maxVolume = 0;
            foreach (var grid in MyCubeGridGroups.Static.Physical.GetGroup(this).Nodes)
            {
                var volume = grid.NodeData.PositionComp.WorldAABB.Size.Volume;
                if (volume > maxVolume)
                {
                    maxVolume = volume;
                    biggestGridInGroup = grid.NodeData;
                }
            }
            return biggestGridInGroup;
        }

        public void ConvertFracturedBlocksToComponents()
        {
            Debug.Assert(Sync.IsServer);

            var fracturedBlocks = new List<MyFracturedBlock>();
            foreach (var block in m_cubeBlocks)
            {
                MyFracturedBlock fb = block.FatBlock as MyFracturedBlock;
                if (fb != null)
                    fracturedBlocks.Add(fb);
            }

            bool oldEnabled = EnableGenerators(false);
            try
            {
                foreach (var fb in fracturedBlocks)
                {
                    var convertedBuilder = fb.ConvertToOriginalBlocksWithFractureComponent();

                    RemoveBlockInternal(fb.SlimBlock, true, markDirtyDisconnects: false);

                    if (convertedBuilder != null)
                        AddBlock(convertedBuilder, false);
                }
            }
            finally
            {
                EnableGenerators(oldEnabled);
            }
        }

        public void PrepareMultiBlockInfos()
        {
            Debug.Assert(MyFakes.ENABLE_MULTIBLOCK_PART_IDS);
            foreach (var block in GetBlocks())
                AddMultiBlockInfo(block);
        }

        internal void AddMultiBlockInfo(MySlimBlock block)
        {
            Debug.Assert(MyFakes.ENABLE_MULTIBLOCK_PART_IDS);

            var compoundBlock = block.FatBlock as MyCompoundCubeBlock;
            if (compoundBlock != null)
            {
                foreach (var blockInCompound in compoundBlock.GetBlocks())
                    if (blockInCompound.IsMultiBlockPart)
                        AddMultiBlockInfo(blockInCompound);

                return;
            }

            Debug.Assert(compoundBlock == null);

            if (!block.IsMultiBlockPart)
                return;

            if (m_multiBlockInfos == null)
                m_multiBlockInfos = new Dictionary<int, MyCubeGridMultiBlockInfo>();

            MyCubeGridMultiBlockInfo mbInfo;
            if (!m_multiBlockInfos.TryGetValue(block.MultiBlockId, out mbInfo))
            {
                mbInfo = new MyCubeGridMultiBlockInfo();
                mbInfo.MultiBlockId = block.MultiBlockId;
                mbInfo.MultiBlockDefinition = block.MultiBlockDefinition;
                mbInfo.MainBlockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinitionForMultiBlock(block.MultiBlockDefinition.Id.SubtypeName);
                Debug.Assert(mbInfo.MainBlockDefinition != null);

                m_multiBlockInfos.Add(block.MultiBlockId, mbInfo);
            }

            mbInfo.Blocks.Add(block);
        }

        internal void RemoveMultiBlockInfo(MySlimBlock block)
        {
            Debug.Assert(MyFakes.ENABLE_MULTIBLOCK_PART_IDS);

            if (m_multiBlockInfos == null)
                return;

            var compoundBlock = block.FatBlock as MyCompoundCubeBlock;
            if (compoundBlock != null)
            {
                foreach (var blockInCompound in compoundBlock.GetBlocks())
                    if (blockInCompound.IsMultiBlockPart)
                        RemoveMultiBlockInfo(blockInCompound);

                return;
            }

            Debug.Assert(compoundBlock == null);

            if (!block.IsMultiBlockPart)
                return;

            MyCubeGridMultiBlockInfo mbInfo;
            if (m_multiBlockInfos.TryGetValue(block.MultiBlockId, out mbInfo))
            {
                if (mbInfo.Blocks.Remove(block))
                {
                    if (mbInfo.Blocks.Count == 0)
                    {
                        if (m_multiBlockInfos.Remove(block.MultiBlockId))
                        {
                            if (m_multiBlockInfos.Count == 0)
                                m_multiBlockInfos = null;
                        }
                    }
                }
            }
        }

        public MyCubeGridMultiBlockInfo GetMultiBlockInfo(int multiBlockId)
        {
            if (m_multiBlockInfos != null)
            {
                MyCubeGridMultiBlockInfo info;
                if (m_multiBlockInfos.TryGetValue(multiBlockId, out info))
                    return info;
            }

            return null;
        }

        /// <summary>
        /// Writes multiblocks (compound block and block ID) to outMultiBlocks collection with the same multiblockId.
        /// </summary>
        public void GetBlocksInMultiBlock(int multiBlockId, HashSet<Tuple<MySlimBlock, ushort?>> outMultiBlocks)
        {
            Debug.Assert(multiBlockId != 0);
            if (multiBlockId == 0)
                return;

            var multiBlockInfo = GetMultiBlockInfo(multiBlockId);
            if (multiBlockInfo != null)
            {
                foreach (var multiBlockPart in multiBlockInfo.Blocks)
                {
                    var existingBlock = GetCubeBlock(multiBlockPart.Position);
                    var compoundBlock = existingBlock.FatBlock as MyCompoundCubeBlock;

                    if (compoundBlock != null)
                    {
                        ushort? blockInCompoundId = compoundBlock.GetBlockId(multiBlockPart);
                        Debug.Assert(blockInCompoundId != null);
                        outMultiBlocks.Add(new Tuple<MySlimBlock, ushort?>(existingBlock, blockInCompoundId));
                    }
                    else
                    {
                        outMultiBlocks.Add(new Tuple<MySlimBlock, ushort?>(existingBlock, null));
                    }
                }
            }
        }

        public bool CanAddMultiBlocks(MyCubeGridMultiBlockInfo multiBlockInfo, ref MatrixI transform, List<int> multiBlockIndices)
        {
            MatrixI outBlockRotation;
            foreach (var index in multiBlockIndices)
            {
                Debug.Assert(index < multiBlockInfo.MultiBlockDefinition.BlockDefinitions.Length);
                if (index < multiBlockInfo.MultiBlockDefinition.BlockDefinitions.Length)
                {
                    var blockDefInfo = multiBlockInfo.MultiBlockDefinition.BlockDefinitions[index];
                    MyCubeBlockDefinition blockDef;
                    if (!MyDefinitionManager.Static.TryGetCubeBlockDefinition(blockDefInfo.Id, out blockDef) || blockDef == null)
                        return false;

                    var position = Vector3I.Transform(blockDefInfo.Min, ref transform);

                    var blockDefRotation = new MatrixI(blockDefInfo.Forward, blockDefInfo.Up);
                    MatrixI.Multiply(ref blockDefRotation, ref transform, out outBlockRotation);
                    var blockOrientation = outBlockRotation.GetBlockOrientation();

                    if (!CanPlaceBlock(position, position, blockOrientation, blockDef, ignoreMultiblockId: multiBlockInfo.MultiBlockId, ignoreFracturedPieces: true))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Builds multiblock parts according to specified indices. 
        /// </summary>
        public bool BuildMultiBlocks(MyCubeGridMultiBlockInfo multiBlockInfo, ref MatrixI transform, List<int> multiBlockIndices, long builderEntityId)
        {
            List<MyBlockLocation> locations = new List<MyBlockLocation>();
            List<MyObjectBuilder_CubeBlock> blockBuilders = new List<MyObjectBuilder_CubeBlock>();

            MatrixI outBlockRotation;
            foreach (var index in multiBlockIndices)
            {
                Debug.Assert(index < multiBlockInfo.MultiBlockDefinition.BlockDefinitions.Length);
                if (index < multiBlockInfo.MultiBlockDefinition.BlockDefinitions.Length)
                {
                    var blockDefInfo = multiBlockInfo.MultiBlockDefinition.BlockDefinitions[index];
                    MyCubeBlockDefinition blockDef;
                    if (!MyDefinitionManager.Static.TryGetCubeBlockDefinition(blockDefInfo.Id, out blockDef) || blockDef == null)
                        return false;

                    var position = Vector3I.Transform(blockDefInfo.Min, ref transform);

                    var blockDefRotation = new MatrixI(blockDefInfo.Forward, blockDefInfo.Up);
                    MatrixI.Multiply(ref blockDefRotation, ref transform, out outBlockRotation);
                    var blockOrientation = outBlockRotation.GetBlockOrientation();

                    if (!CanPlaceBlock(position, position, blockOrientation, blockDef, ignoreMultiblockId: multiBlockInfo.MultiBlockId))
                        return false;

                    MyObjectBuilder_CubeBlock blockBuilder = MyObjectBuilderSerializer.CreateNewObject(blockDefInfo.Id) as MyObjectBuilder_CubeBlock;
                    blockBuilder.Orientation = Base6Directions.GetOrientation(blockOrientation.Forward, blockOrientation.Up);
                    blockBuilder.Min = position;
                    blockBuilder.ColorMaskHSV = MyPlayer.SelectedColor;
                    blockBuilder.MultiBlockId = multiBlockInfo.MultiBlockId;
                    blockBuilder.MultiBlockIndex = index;
                    blockBuilder.MultiBlockDefinition = multiBlockInfo.MultiBlockDefinition.Id;

                    blockBuilders.Add(blockBuilder);

                    MyBlockLocation location = new MyBlockLocation();
                    location.Min = position;
                    location.Max = position;
                    location.CenterPos = position;
                    location.Orientation = new MyBlockOrientation(blockOrientation.Forward, blockOrientation.Up);
                    location.BlockDefinition = blockDefInfo.Id;
                    location.EntityId = MyEntityIdentifier.AllocateId();
                    location.Owner = builderEntityId;

                    locations.Add(location);
                }
            }

            Debug.Assert(locations.Count == blockBuilders.Count);

            if (MySession.Static.SurvivalMode)
            {
                MyEntity toolOwner = MyEntities.GetEntityById(builderEntityId);
                if (toolOwner == null)
                    return false;

                HashSet<MyBlockLocation> setLocations = new HashSet<MyBlockLocation>(locations);
                MyCubeBuilder.BuildComponent.GetBlocksPlacementMaterials(setLocations, this);
                if (!MyCubeBuilder.BuildComponent.HasBuildingMaterials(toolOwner))
                    return false;
            }

            for (int i = 0; i < locations.Count && i < blockBuilders.Count; ++i)
            {
                var location = locations[i];
                var blockBuilder = blockBuilders[i];

                MyMultiplayer.RaiseEvent(this, x => x.BuildBlockRequest, MyPlayer.SelectedColor.PackHSVToUint(), location, blockBuilder, builderEntityId,false,MySession.Static.LocalPlayerId);
            }

            return true;
        }

        private bool GetMissingBlocksMultiBlock(int multiblockId, out MyCubeGridMultiBlockInfo multiBlockInfo, out MatrixI transform, List<int> multiBlockIndices)
        {
            transform = default(MatrixI);
            multiBlockInfo = GetMultiBlockInfo(multiblockId);
            Debug.Assert(multiBlockInfo != null);
            if (multiBlockInfo == null)
                return false;

            return multiBlockInfo.GetMissingBlocks(out transform, multiBlockIndices);
        }

        public bool CanAddMissingBlocksInMultiBlock(int multiBlockId)
        {
            try
            {
                MatrixI transform;
                MyCubeGridMultiBlockInfo multiBlockInfo;
                if (!GetMissingBlocksMultiBlock(multiBlockId, out multiBlockInfo, out transform, m_tmpMultiBlockIndices))
                    return false;

                return CanAddMultiBlocks(multiBlockInfo, ref transform, m_tmpMultiBlockIndices);
            }
            finally
            {
                m_tmpMultiBlockIndices.Clear();
            }
        }

        public void AddMissingBlocksInMultiBlock(int multiBlockId, long toolOwnerId)
        {
            Debug.Assert(Sync.IsServer);

            try
            {
                MatrixI transform;
                MyCubeGridMultiBlockInfo multiBlockInfo;
                if (!GetMissingBlocksMultiBlock(multiBlockId, out multiBlockInfo, out transform, m_tmpMultiBlockIndices))
                    return;

                BuildMultiBlocks(multiBlockInfo, ref transform, m_tmpMultiBlockIndices, toolOwnerId);
            }
            finally
            {
                m_tmpMultiBlockIndices.Clear();
            }
        }

        /// <summary>
        /// Checks if the given block can be added to place where multiblock area (note that even if some parts of multiblock are destroyed then they still 
        /// occupy - virtually - its place). 
        /// </summary>
        public bool CanAddOtherBlockInMultiBlock(Vector3I min, Vector3I max, MyBlockOrientation orientation, MyCubeBlockDefinition definition, int? ignoreMultiblockId)
        {
            if (m_multiBlockInfos == null)
                return true;

            foreach (var entry in m_multiBlockInfos)
            {
                if (ignoreMultiblockId == null || ignoreMultiblockId.Value != entry.Key)
                {
                    var mbInfo = entry.Value;
                    if (!mbInfo.CanAddBlock(ref min, ref max, orientation, definition))
                        return false;
                }
            }

            return true;
        }

        public static bool IsGridInCompleteState(MyCubeGrid grid)
        {
            foreach (var block in grid.CubeBlocks)
            {
                if (!block.IsFullIntegrity || block.BuildLevelRatio != 1.0f)
                    return false;
            }

            return true;
        }

        public bool WillRemoveBlockSplitGrid( MySlimBlock testBlock )
        {
            return m_disconnectHelper.TryDisconnect( testBlock );
        }

        /// <param name="position">In world coordinates</param>
        public MySlimBlock GetTargetedBlock(Vector3D position)
        {
            Vector3I blockPos;
            FixTargetCube(out blockPos, Vector3D.Transform(position, PositionComp.WorldMatrixNormalizedInv) * GridSizeR);
            return GetCubeBlock(blockPos);
        }

        #region Multiplayer
        [Event, Reliable, Server]
        public static void TryCreateGrid_Implementation(MyCubeSize cubeSize, bool isStatic, MyPositionAndOrientation position, long inventoryEntityId, bool instantBuild)
        {
            string prefabName;
            bool isAdmin = (MyEventContext.Current.IsLocallyInvoked || MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value));

            MyDefinitionManager.Static.GetBaseBlockPrefabName(cubeSize, isStatic, MySession.Static.CreativeMode || (instantBuild && isAdmin), out prefabName);

            if (prefabName == null)
                return;
            var gridBuilders = MyPrefabManager.Static.GetGridPrefab(prefabName);
            Debug.Assert(gridBuilders != null && gridBuilders.Length > 0);
            if (gridBuilders == null || gridBuilders.Length == 0)
                return;

            foreach (var gridBuilder in gridBuilders)
            {
                gridBuilder.PositionAndOrientation = position;
            }

            MyEntities.RemapObjectBuilderCollection(gridBuilders);

            if ((instantBuild && isAdmin) == false)
            {
                MyEntity inventoryOwner;
                if (MyEntities.TryGetEntityById(inventoryEntityId, out inventoryOwner) && inventoryOwner != null)
                {
                    MyInventoryBase buildInventory = MyCubeBuilder.BuildComponent.GetBuilderInventory(inventoryOwner);
                    if (buildInventory != null)
                    {
                        MyGridClipboard.CalculateItemRequirements(gridBuilders, m_buildComponents);
                        foreach (var item in m_buildComponents.TotalMaterials)
                        {
                            buildInventory.RemoveItemsOfType(item.Value, item.Key);
                        }
                    }
                }
                else if (isAdmin == false && MySession.Static.CreativeMode == false && MyFinalBuildConstants.IS_OFFICIAL)
                {
                    return;
                }
            }

            List<MyCubeGrid> results = new List<MyCubeGrid>();

            foreach (var entity in gridBuilders)
            {
                MySandboxGame.Log.WriteLine("CreateCompressedMsg: Type: " + entity.GetType().Name.ToString() + "  Name: " + entity.Name + "  EntityID: " + entity.EntityId.ToString("X8"));

                // This does not work, because EntityCreation is not supported on separate thread
                //MyEntities.CreateAsync(entity, false, (e) => results.Add((MyCubeGrid)e));
               
                MyCubeGrid grid = MyEntities.CreateFromObjectBuilder(entity) as MyCubeGrid;
                results.Add(grid);
              
                if (instantBuild && isAdmin)
                {
                    ChangeOwnership(inventoryEntityId, grid);
                }
            
                MySandboxGame.Log.WriteLine("Status: Exists(" + MyEntities.EntityExists(entity.EntityId) + ") InScene(" + ((entity.PersistentFlags & MyPersistentEntityFlags2.InScene) == MyPersistentEntityFlags2.InScene) + ")");
            }

            AfterPaste(results, Vector3.Zero, false);
        }

        /// <summary>
        /// Use only for cut request
        /// </summary>
        public void SendGridCloseRequest()
        {
            MyMultiplayer.RaiseStaticEvent(s => OnGridClosedRequest, EntityId);
        }

        [Event, Reliable, Server]
        static void OnGridClosedRequest(long entityId)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }
            MyEntity entity;
            MyEntities.TryGetEntityById(entityId, out entity);
            if (entity == null)
                return;

            foreach (var block in (entity as MyCubeGrid).GetBlocks())
            {
                block.RemoveAuthorship();
                var cockpit = block.FatBlock as MyCockpit;
                if (cockpit != null && cockpit.Pilot != null)
                    cockpit.Use();
            }

            // Test right to closing entity (e.g. is creative mode?)
            if (!entity.MarkedForClose)
                entity.Close(); // close only on server, server uses replication to propagate it to clients
        }

        private class PasteGridData : ParallelTasks.WorkData
        {
            List<MyObjectBuilder_CubeGrid> m_entities;
            bool m_detectDisconnects;
            long m_inventoryEntityId; 
            Vector3 m_objectVelocity;
            bool m_multiBlock;
            bool m_instantBuild;
            List<MyCubeGrid> m_results;
            bool m_canPlaceGrid;
            List<IMyEntity> m_resultIDs;
            bool m_removeScripts;
            //GK: Added minimum event info, because at parallel task completion (OnPasteCompleted) we no longer have MyEventContext.Current
            public readonly EndpointId SenderEndpointId;
            public readonly bool IsLocallyInvoked;

            public PasteGridData(List<MyObjectBuilder_CubeGrid> entities, bool detectDisconnects, long inventoryEntityId, Vector3 objectVelocity, bool multiBlock,
            bool instantBuild, bool shouldRemoveScripts, EndpointId senderEndpointId, bool isLocallyInvoked)
            {
                m_entities = new List<MyObjectBuilder_CubeGrid>(entities);
                m_detectDisconnects = detectDisconnects;
                m_inventoryEntityId = inventoryEntityId;
                m_objectVelocity = objectVelocity;
                m_multiBlock = multiBlock;
                m_instantBuild = instantBuild;
                SenderEndpointId = senderEndpointId;
                IsLocallyInvoked = isLocallyInvoked;
                m_removeScripts = shouldRemoveScripts;
            }

            public void TryPasteGrid()
            {
                bool isAdmin = (MyEventContext.Current.IsLocallyInvoked || MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value));

                bool validBattlePaste = false;

                if (MySession.Static.SurvivalMode && !validBattlePaste && !isAdmin)
                {
                    return;
                }

                //Fix for when the player wants to paste a ship with the same object builder multiple times. Should be done in a less performance heavy way if possible
                for (int i = 0; i < m_entities.Count; i++)
                {
                    m_entities[i] = (MyObjectBuilder_CubeGrid)m_entities[i].Clone();
                }

                MyEntities.RemapObjectBuilderCollection(m_entities);

                if (m_removeScripts)
                {
                    foreach (var grid in m_entities)
                    {
                        foreach (var block in grid.CubeBlocks)
                        {
                            var programmable = block as MyObjectBuilder_MyProgrammableBlock;
                            if (programmable == null)
                                continue;

                            programmable.Program = null;
                        }
                    }
                }

                if ((m_instantBuild && isAdmin) == false)
                {
                    MyEntity inventoryOwner;
                    if (MyEntities.TryGetEntityById(m_inventoryEntityId, out inventoryOwner) && inventoryOwner != null)
                    {
                        // TODO: If there's not enough materials, it should return false!
                        if (m_multiBlock)
                        {
                            MyMultiBlockClipboard.TakeMaterialsFromBuilder(m_entities, inventoryOwner);
                        }
                        else
                        {
                            MyInventoryBase buildInventory = MyCubeBuilder.BuildComponent.GetBuilderInventory(inventoryOwner);
                            if (buildInventory != null)
                            {
                                MyGridClipboard.CalculateItemRequirements(m_entities, m_buildComponents);
                                foreach (var item in m_buildComponents.TotalMaterials)
                                {
                                    buildInventory.RemoveItemsOfType(item.Value, item.Key);
                                }
                            }
                        }
                    }
                }

                m_results = new List<MyCubeGrid>();

                MyEntityIdentifier.LazyInitPerThreadStorage(2048);

                m_canPlaceGrid = true;
                foreach (var entity in m_entities)
                {
                    MySandboxGame.Log.WriteLine("CreateCompressedMsg: Type: " + entity.GetType().Name.ToString() + "  Name: " + entity.Name + "  EntityID: " + entity.EntityId.ToString("X8"));
                    MyCubeGrid grid = MyEntities.CreateFromObjectBuilder(entity, false) as MyCubeGrid;
                    m_results.Add(grid);

                    m_canPlaceGrid &= TestPastedGridPlacement(grid);
                    if (m_canPlaceGrid == false)
                    {
                        break;
                    }

                    if (m_instantBuild && isAdmin)
                    {
                        ChangeOwnership(m_inventoryEntityId, grid);
                    }

                    MySandboxGame.Log.WriteLine("Status: Exists(" + MyEntities.EntityExists(entity.EntityId) + ") InScene(" + ((entity.PersistentFlags & MyPersistentEntityFlags2.InScene) == MyPersistentEntityFlags2.InScene) + ")");
                }

                m_resultIDs = new List<VRage.ModAPI.IMyEntity>();
                MyEntityIdentifier.GetPerThreadEntities(m_resultIDs);
                MyEntityIdentifier.ClearPerThreadEntities();
            }

            private bool TestPastedGridPlacement(MyCubeGrid grid)
            {
                var settings = MyClipboardComponent.ClipboardDefinition.PastingSettings.GetGridPlacementSettings(grid.GridSizeEnum, grid.IsStatic);
                return TestPlacementArea(grid, grid.IsStatic, ref settings, (BoundingBoxD)grid.PositionComp.LocalAABB, !grid.IsStatic);
            }

            public void Callback()
            {
                if (!IsLocallyInvoked)
                {
                    MyMultiplayer.RaiseStaticEvent(s => MyCubeGrid.SendHudNotificationAfterPaste, SenderEndpointId);
                }
                else if (!MySandboxGame.IsDedicated)
                {
                    MyHud.PopRotatingWheelVisible();
                }
                if (m_canPlaceGrid && m_results.Count > 0)
                {
                    foreach (var entity in m_resultIDs)
                    {
                        VRage.ModAPI.IMyEntity foundEntity;
                        MyEntityIdentifier.TryGetEntity(entity.EntityId, out foundEntity);
                        if (foundEntity == null)
                            MyEntityIdentifier.AddEntityWithId(entity);
                        else
                            Debug.Fail("Two threads added the same entity");
                    }

                    foreach (var grid in m_results)
                    {
                        m_canPlaceGrid &= TestPastedGridPlacement(grid);
                    }
                    if (m_canPlaceGrid)
                        AfterPaste(m_results, m_objectVelocity, m_detectDisconnects);
                }
                else
                {
                    foreach (var grid in m_results)
                    {
                        grid.Close();
                    }

                    if (!IsLocallyInvoked)
                    {
                        MyMultiplayer.RaiseStaticEvent(s => MyCubeGrid.ShowPasteFailedOperation, SenderEndpointId);
                    }
                }
            }
        }

        [Event, Reliable, Server]
        public static void TryPasteGrid_Implementation(List<MyObjectBuilder_CubeGrid> entities, bool detectDisconnects, long inventoryEntityId, Vector3 objectVelocity, bool multiBlock,
            bool instantBuild)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.IsCopyPastingEnabledForUser(MyEventContext.Current.Sender.Value)) //GK: Check the correct user that is the one of the current event context
            {
                MyEventContext.ValidationFailed();
                MyMultiplayer.RaiseStaticEvent(s => MyCubeGrid.SendHudNotificationAfterPaste, MyEventContext.Current.Sender);
                return;
            }
            bool shouldRemoveScripts = !MySession.Static.IsUserScripter(MyEventContext.Current.Sender.Value);
            PasteGridData workData = new PasteGridData(entities, detectDisconnects, inventoryEntityId, objectVelocity, multiBlock, instantBuild, shouldRemoveScripts, MyEventContext.Current.Sender, MyEventContext.Current.IsLocallyInvoked);
            Parallel.Start(TryPasteGrid_ImplementationInternal, OnPasteCompleted, workData);
        }

        static void TryPasteGrid_ImplementationInternal(WorkData workData)
        {
            PasteGridData pasteData = workData as PasteGridData;
            if (pasteData == null)
            {
                workData.FlagAsFailed();
                return;
            }

            pasteData.TryPasteGrid();
        }

        static void OnPasteCompleted(WorkData workData)
        {
            PasteGridData pasteData = workData as PasteGridData;
            if (pasteData == null)
            {
                workData.FlagAsFailed();
                return;
            }

            pasteData.Callback();
        }
        
        [Event, Reliable, Client]
        public static void ShowPasteFailedOperation()
        {
            MyHud.Notifications.Add(MyNotificationSingletons.PasteFailed);
        }

        [Event, Reliable, Client]
        public static void SendHudNotificationAfterPaste()
        {
            MyHud.PopRotatingWheelVisible();
        }

        private static void ChangeOwnership(long inventoryEntityId, MyCubeGrid grid)
        {
            MyEntity inventoryOwner;
            if (MyEntities.TryGetEntityById(inventoryEntityId, out inventoryOwner) && inventoryOwner != null)
            {
                MyCharacter character = inventoryOwner as MyCharacter;
                if (character != null)
                {
                    grid.ChangeGridOwner(character.ControllerInfo.Controller.Player.Identity.IdentityId, MyOwnershipShareModeEnum.Faction);
                }
            }
        }

        static void AfterPaste(List<MyCubeGrid> grids, Vector3 objectVelocity, bool detectDisconnects)
        {
            foreach (var pastedGrid in grids)
            {
                if (pastedGrid.IsStatic)
                {
                    pastedGrid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridCopied;
                }

                MyEntities.Add(pastedGrid);

                if (pastedGrid.Physics != null)
                {
                    if (!pastedGrid.IsStatic)
                    {
                        pastedGrid.Physics.LinearVelocity = objectVelocity;
                    }

                    if (!pastedGrid.IsStatic && MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Entity.Physics != null)
                    {
                        if (MySession.Static.ControlledEntity != null)
                            pastedGrid.Physics.AngularVelocity = MySession.Static.ControlledEntity.Entity.Physics.AngularVelocity;
                    }
                }
                else
                    Debug.Fail("Pasted grid without physics!");

                if (detectDisconnects)
                {
                    pastedGrid.DetectDisconnectsAfterFrame();
                }
                MySession.Static.TotalBlocksCreated += (uint)pastedGrid.BlocksCount;

                if (pastedGrid.IsStatic)
                {
                    foreach (var block in pastedGrid.CubeBlocks)
                    {
                        if (pastedGrid.DetectMerge(block) != null)
                            break;
                    }
                }
                pastedGrid.IsReadyForReplication = true;
            }

            MatrixD worldMatrix = grids[0].PositionComp.WorldMatrix;
            // grid[0] because we need only first grid for this
            bool result = MyCoordinateSystem.Static.IsLocalCoordSysExist(ref worldMatrix, grids[0].GridSize);
            if (grids[0].GridSizeEnum == MyCubeSize.Large)
            {
            if (result)
            {
                MyCoordinateSystem.Static.RegisterCubeGrid(grids[0]);
            }
            else
            {
                MyCoordinateSystem.Static.CreateCoordSys(grids[0], MyClipboardComponent.ClipboardDefinition.PastingSettings.StaticGridAlignToCenter, true);
            }
        }
        }

        [Event, Reliable, Broadcast]
        public void RequestConvertToDynamic()
        {
            if (IsStatic)
            {
                this.ConvertToDynamic();
            }
        }

        public void RecalculateGravity()
        {
            m_gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(PositionComp.GetPosition());
            if (MyPerGameSettings.Game == GameEnum.VRS_GAME)
                m_gravity += MyGravityProviderSystem.CalculateArtificialGravityInPoint(PositionComp.GetPosition());
        }

        public void ActivatePhysics()
        {
            if (Physics != null && Physics.Enabled)
            {
                Physics.RigidBody.Activate();
                if (Physics.RigidBody2 != null)
                {
                    Physics.RigidBody2.Activate();
                }
            }
        }

        void SendDirtyBones(Vector3I minBone, Vector3I maxBone, MyGridSkeleton skeleton)
        {
            m_boneByteList.Clear();
            skeleton.SerializePart(minBone, maxBone, GridSize, m_boneByteList);

            if (m_boneByteList.Count > 0)
            {
                MyMultiplayer.RaiseEvent(this, x => x.OnBonesReceived, minBone, maxBone, m_boneByteList);
            }
        }

        [Event, Reliable, Broadcast]
        void OnBonesReceived(Vector3I minBone, Vector3I maxBone, List<byte> boneByteList)
        {
            Skeleton.DeserializePart(minBone, maxBone, GridSize, boneByteList);

            Vector3I minCube = Vector3I.Zero;
            Vector3I maxCube = Vector3I.Zero;

            // To hit incident cubes
            Vector3I min = minBone;// -Vector3I.One;
            Vector3I max = maxBone;// +Vector3I.One;

            Skeleton.Wrap(ref minCube, ref min);
            Skeleton.Wrap(ref maxCube, ref max);

            minCube -= Vector3I.One;
            maxCube += Vector3I.One;

            Vector3I pos;
            for (pos.X = minCube.X; pos.X <= maxCube.X; pos.X++)
            {
                for (pos.Y = minCube.Y; pos.Y <= maxCube.Y; pos.Y++)
                {
                    for (pos.Z = minCube.Z; pos.Z <= maxCube.Z; pos.Z++)
                    {
                        SetCubeDirty(pos);
                    }
                }
            }

        }

        [Event, Reliable, Broadcast]
        void OnBonesMultiplied(Vector3I blockLocation, float multiplier)
        {
            var block = GetCubeBlock(blockLocation);

            Debug.Assert(block != null, "Block was null in OnBonesMultiplied handler!");
            if (block == null) return;

            MultiplyBlockSkeleton(block, multiplier);
        }

        public void SendReflectorState(MyMultipleEnabledEnum value)
        {
            MyMultiplayer.RaiseEvent(this, x => x.RelfectorStateRecived, value);
        }

        [Event, Reliable, Server, Broadcast]
        void RelfectorStateRecived(MyMultipleEnabledEnum value)
        {
            GridSystems.ReflectorLightSystem.ReflectorStateChanged(value);
        }

        public void SendIntegrityChanged(MySlimBlock mySlimBlock, MyIntegrityChangeEnum integrityChangeType, long toolOwner)
        {
            MyMultiplayer.RaiseEvent(this, x => x.BlockIntegrityChanged,mySlimBlock.Position,GetSubBlockId(mySlimBlock), mySlimBlock.BuildIntegrity,mySlimBlock.Integrity, integrityChangeType, toolOwner);
        }

        public void SendStockpileChanged(MySlimBlock mySlimBlock, List<MyStockpileItem> list)
        {
            if (list.Count > 0)
            {
                MyMultiplayer.RaiseEvent(this, x => x.BlockStockpileChanged, mySlimBlock.Position, GetSubBlockId(mySlimBlock), list);
            }
        }

        public void SendFractureComponentRepaired(MySlimBlock mySlimBlock, long toolOwner)
        {
            MyMultiplayer.RaiseEvent(this, x => x.FractureComponentRepaired, mySlimBlock.Position, GetSubBlockId(mySlimBlock), toolOwner);
        }

        ushort GetSubBlockId(MySlimBlock slimBlock)
        {
            var block = slimBlock.CubeGrid.GetCubeBlock(slimBlock.Position);
            MyCompoundCubeBlock compoundBlock = null;
            if (block != null)
                compoundBlock = block.FatBlock as MyCompoundCubeBlock;
            if (compoundBlock != null)
            {
                var subBlockId = compoundBlock.GetBlockId(slimBlock);
                return subBlockId ?? 0;
            }

            return 0;
        }

        public void RequestFillStockpile(Vector3I blockPosition, MyInventory fromInventory)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnStockpileFillRequest, blockPosition, fromInventory.Owner.EntityId, fromInventory.InventoryIdx);
        }

        [Event, Reliable, Server]
        void OnStockpileFillRequest(Vector3I blockPosition, long ownerEntityId, byte inventoryIndex)
        {
            var block = GetCubeBlock(blockPosition);
            Debug.Assert(block != null, "Could not find block whose stockpile fill was requested");
            if (block == null) return;

            MyEntity ownerEntity = null;
            if (!MyEntities.TryGetEntityById(ownerEntityId, out ownerEntity))
            {
                Debug.Assert(false, "Stockpile fill inventory owner entity was null");
                return;
            }

            var owner = (ownerEntity != null && ownerEntity.HasInventory) ? ownerEntity : null;
            Debug.Assert(owner != null, "Stockpile fill inventory owner was not an inventory owner");

            var inventory = owner.GetInventory(inventoryIndex);
            Debug.Assert(inventory != null, "Stockpile fill inventory owner did not have the given inventory");

            block.MoveItemsToConstructionStockpile(inventory as MyInventoryBase);
        }

        public void RequestSetToConstruction(Vector3I blockPosition, MyInventory fromInventory)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnSetToConstructionRequest, blockPosition, fromInventory.Owner.EntityId, fromInventory.InventoryIdx, MySession.Static.LocalPlayerId);
        }

        [Event, Reliable, Server]
        private void OnSetToConstructionRequest(Vector3I blockPosition,long ownerEntityId, byte inventoryIndex,long requestingPlayer)
        {
            var block = GetCubeBlock(blockPosition);
            Debug.Assert(block != null, "Could not find block to set to construction site");
            if (block == null) return;

            block.SetToConstructionSite();

            MyEntity ownerEntity = null;
            if (!MyEntities.TryGetEntityById(ownerEntityId, out ownerEntity))
            {
                Debug.Assert(false, "Set to construction site inventory owner entity was null");
                return;
            }

            var owner = (ownerEntity != null && ownerEntity.HasInventory) ? ownerEntity : null;
            Debug.Assert(owner != null, "Set to construction site inventory owner was not an inventory owner");

            var inventory = owner.GetInventory(inventoryIndex) as MyInventoryBase;
            Debug.Assert(inventory != null, "Set to construction site inventory owner did not have the given inventory");

            block.MoveItemsToConstructionStockpile(inventory);
            block.IncreaseMountLevel(MyWelder.WELDER_AMOUNT_PER_SECOND * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, requestingPlayer);
        }

        public void ChangePowerProducerState(MyMultipleEnabledEnum enabledState, long playerId)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnPowerProducerStateRequest, enabledState, playerId); ;
        }

        public void SendPowerDistributorState(MyMultipleEnabledEnum enabledState, long playerId)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnPowerProducerStateRequest, enabledState, playerId);
        }

        [Event, Reliable, Server,Broadcast]
        private void OnPowerProducerStateRequest(MyMultipleEnabledEnum enabledState, long playerId)
        {
            this.GridSystems.SyncObject_PowerProducerStateChanged(enabledState, playerId);

        }
           
        [Event, Reliable, Server, BroadcastExcept]
        private void OnZeroThrustReceived()
        {
            var thrustComp = Components.Get<MyEntityThrustComponent>();
            if (thrustComp != null)
            {
                thrustComp.ControlThrust = Vector3.Zero;
            }
        }

        public void RequestConversionToShip()
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnConvertedToShipRequest);
        }

        public void RequestConversionToStation()
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnConvertedToStationRequest);
        }

        [Event, Reliable, Server]
        public void OnConvertedToShipRequest()
        { 
            if(IsStatic == false)
            {
                return;
            }
            ConvertToDynamic();
            MyMultiplayer.RaiseEvent(this, x => x.ConvertToDynamic);
        }

        [Event, Reliable, Server]
        public void OnConvertedToStationRequest()
        {
            if (IsStatic)
            {
                return;
            }
            ConvertToStatic();
            MyMultiplayer.RaiseEvent(this, x => x.ConvertToStatic);
        }

        public void ChangeOwnerRequest(MyCubeGrid grid, MyCubeBlock block, long playerId, MyOwnershipShareModeEnum shareMode)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnChangeOwnerRequest,block.EntityId,playerId,shareMode);
        }

        [Event, Reliable, Server]
        void OnChangeOwnerRequest(long blockId, long owner, MyOwnershipShareModeEnum shareMode)
        {
            MyCubeBlock block = null;

            if (MyEntities.TryGetEntityById<MyCubeBlock>(blockId, out block))
            {
                var ownerComp = block.Components.Get<MyEntityOwnershipComponent>();                
                if (Sync.IsServer && block.IDModule != null && ((block.IDModule.Owner == 0) || block.IDModule.Owner == owner || (owner == 0)))
                {
                    OnChangeOwner(blockId, owner, shareMode);
                    MyMultiplayer.RaiseEvent(this, x => x.OnChangeOwner, blockId, owner, shareMode);
                }
                else if (Sync.IsServer && ownerComp != null && (ownerComp.OwnerId == 0 || ownerComp.OwnerId == owner || owner == 0))
                {
                    OnChangeOwner(blockId, owner, shareMode);
                    MyMultiplayer.RaiseEvent(this, x => x.OnChangeOwner, blockId, owner, shareMode);
                }
                else
                {
                    bool shouldHaveOwnership = block.BlockDefinition.ContainsComputer();
                    if (block.UseObjectsComponent != null)
                        shouldHaveOwnership = shouldHaveOwnership || block.UseObjectsComponent.GetDetectors("ownership").Count > 0;
                    if (shouldHaveOwnership)
                        System.Diagnostics.Debug.Fail("Invalid ownership change request!");
                }
            }
        }

        [Event, Reliable, Broadcast]
        void OnChangeOwner(long blockId, long owner, MyOwnershipShareModeEnum shareMode)
        {
            MyCubeBlock block = null;
            if (MyEntities.TryGetEntityById<MyCubeBlock>(blockId, out block))
            {
                block.ChangeOwner(owner, shareMode);
            }
        }

        void HandBrakeChanged()
        {
            GridSystems.WheelSystem.HandBrake = m_handBrakeSync;
        }

        public void SetHandbrakeRequest(bool v)
        {
            m_handBrakeSync.Value = v;
        }

        public void ChangeGridOwner(long playerId, MyOwnershipShareModeEnum shareMode)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnChangeGridOwner, playerId, shareMode);
            OnChangeGridOwner(playerId, shareMode);
        }

        [Event, Reliable, Broadcast]
        void OnChangeGridOwner(long playerId, MyOwnershipShareModeEnum shareMode)
        {
            foreach (var block in GetBlocks())
            {
                if (block.FatBlock != null && block.BlockDefinition.RatioEnoughForOwnership(block.BuildLevelRatio))
                {
                    block.FatBlock.ChangeOwner(playerId, shareMode);
                }
            }
        }

        public void AnnounceRemoveSplit(List<MySlimBlock> blocks)
        {
            m_tmpPositionListSend.Clear();
            foreach (var block in blocks)
            {
                m_tmpPositionListSend.Add(block.Position);
            }
            MyMultiplayer.RaiseEvent(this, x => x.OnRemoveSplit, m_tmpPositionListSend);
        }

        [Event, Reliable, Broadcast]
        void OnRemoveSplit(List<Vector3I> removedBlocks)
        {
            m_tmpPositionListReceive.Clear();
            foreach (var position in removedBlocks)
            {
                var block = GetCubeBlock(position);
                Debug.Assert(block != null, "Block was null when trying to remove a grid split. Desync?");
                if (block == null)
                {
                    MySandboxGame.Log.WriteLine("Block was null when trying to remove a grid split. Desync?");
                    continue;
                }

                m_tmpBlockListReceive.Add(block);
            }

            MyCubeGrid.RemoveSplit(this, m_tmpBlockListReceive, 0, m_tmpBlockListReceive.Count, sync: false);
        }

        public void ChangeDisplayNameRequest(String displayName)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnChangeDisplayNameRequest, displayName);
        }

        [Event, Reliable, Server,Broadcast]
        void OnChangeDisplayNameRequest(String displayName)
        {
            DisplayName = displayName;
        }

        public void ModifyGroup(MyBlockGroup group)
        {
            m_tmpBlockIdList.Clear();
            foreach (var block in group.Blocks)
            {
                m_tmpBlockIdList.Add(block.EntityId);
            }
            MyMultiplayer.RaiseEvent(this, x => x.OnModifyGroupSuccess, group.Name.ToString(), m_tmpBlockIdList);
        }

        [Event, Reliable, Server, BroadcastExcept]
        void OnModifyGroupSuccess(String name,List<long> blocks)
        {
            if (blocks == null || blocks.Count == 0)
            {
                foreach (var group in BlockGroups)
                {
                    if (group.Name.ToString().Equals(name))
                    {
                        RemoveGroup(group);
                        break;
                    }
                }
            }
            else
            {
                MyBlockGroup group = new MyBlockGroup(this);
                group.Name.Clear().Append(name);
                foreach (var blockId in blocks)
                {
                    MyTerminalBlock block = null;
                    if (MyEntities.TryGetEntityById(blockId, out block))
                        group.Blocks.Add(block);
                }
                AddGroup(group);
            }
        }

        public void RazeBlockInCompoundBlock(List<Tuple<Vector3I, ushort>> locationsAndIds)
        {
            ConvertToLocationIdentityList(locationsAndIds, m_tmpLocationsAndIdsSend);
            MyMultiplayer.RaiseEvent(this, x => x.OnRazeBlockInCompoundBlockRequest, m_tmpLocationsAndIdsSend);
        }

        [Event, Reliable, Server]
        private void OnRazeBlockInCompoundBlockRequest(List<LocationIdentity> locationsAndIds)
        {
            OnRazeBlockInCompoundBlock(locationsAndIds);

            if (m_tmpLocationsAndIdsReceive.Count > 0)
            {
                // Broadcast to clients, use result collection
                ConvertToLocationIdentityList(m_tmpLocationsAndIdsReceive, m_tmpLocationsAndIdsSend);
                MyMultiplayer.RaiseEvent(this, x => x.OnRazeBlockInCompoundBlockSuccess, m_tmpLocationsAndIdsSend);
            }
        }

        [Event, Reliable, Broadcast]
        private void OnRazeBlockInCompoundBlockSuccess(List<LocationIdentity> locationsAndIds)
        {
            OnRazeBlockInCompoundBlock(locationsAndIds);
        }

        private void OnRazeBlockInCompoundBlock(List<LocationIdentity> locationsAndIds)
        {
            m_tmpLocationsAndIdsReceive.Clear();
            RazeBlockInCompoundBlockSuccess(locationsAndIds, m_tmpLocationsAndIdsReceive);
        }

        private static void ConvertToLocationIdentityList(List<Tuple<Vector3I, ushort>> locationsAndIdsFrom, List<LocationIdentity> locationsAndIdsTo)
        {
            locationsAndIdsTo.Clear();
            locationsAndIdsTo.Capacity = locationsAndIdsFrom.Count;
            foreach (var tuple in locationsAndIdsFrom)
                locationsAndIdsTo.Add(new LocationIdentity() { Location = tuple.Item1, Id = tuple.Item2 });
        }

        public static void ChangeOwnersRequest(MyOwnershipShareModeEnum shareMode, List<MySingleOwnershipRequest> requests, long requestingPlayer)
        {
            System.Diagnostics.Debug.Assert((int)shareMode >= 0);

            MyMultiplayer.RaiseStaticEvent(s => MyCubeGrid.OnChangeOwnersRequest, shareMode, requests, requestingPlayer);
        }

        [Event, Reliable, Server]
        private static void OnChangeOwnersRequest(MyOwnershipShareModeEnum shareMode, List<MySingleOwnershipRequest> requests, long requestingPlayer)
        {
            MyCubeBlock block = null;
            int c = 0;

            MyIdentity identity = MySession.Static.Players.TryGetIdentity(requestingPlayer);
            MyPlayer player = identity != null ? MyPlayer.GetPlayerFromCharacter(identity.Character) : null;
            if (identity != null && identity.Character != null && player != null && MySession.Static.IsUserSpaceMaster(player.Client.SteamUserId))
                c = requests.Count;

            while (c < requests.Count)
            {
                var request = requests[c];
                if (MyEntities.TryGetEntityById<MyCubeBlock>(request.BlockId, out block))
                {
                    var ownerComp = block.Components.Get<MyEntityOwnershipComponent>();
                    if (Sync.IsServer && block.IDModule != null && ((block.IDModule.Owner == 0) || block.IDModule.Owner == requestingPlayer || (request.Owner == 0)))
                    {
                        c++;
                    }
                    else if (Sync.IsServer && ownerComp != null && (ownerComp.OwnerId == 0 || ownerComp.OwnerId == requestingPlayer || request.Owner == 0))
                    {
                        c++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Fail("Invalid ownership change request!");
                        requests.RemoveAtFast(c);
                    }
                }
                else
                {
                    c++;
                }
            }

            if (requests.Count > 0)
            {
                OnChangeOwnersSuccess(shareMode, requests);
                MyMultiplayer.RaiseStaticEvent(s => MyCubeGrid.OnChangeOwnersSuccess, shareMode, requests);
            }
        }

        [Event, Reliable, Broadcast]
        private static void OnChangeOwnersSuccess(MyOwnershipShareModeEnum shareMode, List<MySingleOwnershipRequest> requests)
        {
            foreach (var request in requests)
            {
                MyCubeBlock block = null;
                if (MyEntities.TryGetEntityById<MyCubeBlock>(request.BlockId, out block))
                {
                    block.ChangeOwner(request.Owner, shareMode);
                }
            }
        }

        [ProtoContract]
        public struct MySingleOwnershipRequest
        {
            [ProtoMember]
            public long BlockId;

            [ProtoMember]
            public long Owner; //PlayerId
        }

        [ProtoContract]
        public struct LocationIdentity
        {
            [ProtoMember]
            public Vector3I Location;

            [ProtoMember]
            public ushort Id;
        }

        #endregion

        public override void SerializeControls(BitStream stream)
        {
            MyShipController shipController = null;
            if (!IsStatic)
            {
                shipController = GridSystems.ControlSystem.GetShipController();
            }

            if (shipController != null)
            {
                stream.WriteBool(true);
                var netState = shipController.GetNetState();
                netState.Serialize(stream);
            }
            else stream.WriteBool(false);
        }

        private MyGridNetState m_lastNetState;
        public override void DeserializeControls(BitStream stream, bool outOfOrder)
        {
            var valid = stream.ReadBool();
            if (valid)
            {
                var netState = new MyGridNetState(stream);
                if (!outOfOrder)
                    m_lastNetState = netState;

                var shipController = GridSystems.ControlSystem.GetShipController();
                if (shipController != null && !shipController.ControllerInfo.IsLocallyControlled())
                    shipController.SetNetState(m_lastNetState);
            }
            else m_lastNetState.Valid = false;
        }

        public override void ApplyLastControls()
        {
            if (m_lastNetState.Valid)
            {
                var shipController = GridSystems.ControlSystem.GetShipController();
                if (shipController != null && !shipController.ControllerInfo.IsLocallyControlled())
                    shipController.SetNetState(m_lastNetState);
            }
        }

        #region VisualScripting

        private List<long> m_targetingList = new List<long>();
        private bool m_targetingListIsWhitelist = false;
        private bool m_usesTargetingList = false;
        public bool UsesTargetingList { get { return m_usesTargetingList; } }

        public void TargetingAddId(long id)
        {
            if (!m_targetingList.Contains(id))
                m_targetingList.Add(id);
            m_usesTargetingList = m_targetingList.Count > 0 || m_targetingListIsWhitelist;
        }

        public void TargetingRemoveId(long id)
        {
            if (m_targetingList.Contains(id))
                m_targetingList.Remove(id);
            m_usesTargetingList = m_targetingList.Count > 0 || m_targetingListIsWhitelist;
        }

        public void TargetingSetWhitelist(bool whitelist)
        {
            m_targetingListIsWhitelist = whitelist;
            m_usesTargetingList = m_targetingList.Count > 0 || m_targetingListIsWhitelist;
        }

        public bool TargetingCanAttackGrid(long id)
        {
            if (m_targetingListIsWhitelist)
                return m_targetingList.Contains(id);
            else
                return !m_targetingList.Contains(id);
        }

        #endregion
    }

    public class MyCubeGridHitInfo
    {
        public MyIntersectionResultLineTriangleEx Triangle;
        public Vector3I Position;
        public int CubePartIndex = -1;

        public void Reset()
        {
            Triangle = new MyIntersectionResultLineTriangleEx();
            Position = new Vector3I();
            CubePartIndex = -1;
        }
    }
}
