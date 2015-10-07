using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_CubeBlock))]
    public partial class MySlimBlock : IMyDestroyableObject
    {
        static MySoundPair CONSTRUCTION_START = new MySoundPair("PrgConstrPh01Start");
        static MySoundPair CONSTRUCTION_PROG = new MySoundPair("PrgConstrPh02Proc");
        static MySoundPair CONSTRUCTION_END = new MySoundPair("PrgConstrPh03Fin");
        static MySoundPair DECONSTRUCTION_START = new MySoundPair("PrgDeconstrPh01Start");
        static MySoundPair DECONSTRUCTION_PROG = new MySoundPair("PrgDeconstrPh02Proc");
        static MySoundPair DECONSTRUCTION_END = new MySoundPair("PrgDeconstrPh03Fin");

        [ThreadStatic]
        static Dictionary<string, int> m_tmpComponentsPerThread;
        [ThreadStatic]
        static List<MyStockpileItem> m_tmpItemListPerThread;
        [ThreadStatic]
        static List<Vector3I> m_tmpCubeNeighboursPerThread;
        [ThreadStatic]
        static List<MySlimBlock> m_tmpBlocksPerThread;

        static Dictionary<string, int> m_tmpComponents { get { return MyUtils.Init(ref m_tmpComponentsPerThread); } }
        static List<MyStockpileItem> m_tmpItemList { get { return MyUtils.Init(ref m_tmpItemListPerThread); } }
        static List<Vector3I> m_tmpCubeNeighbours { get { return MyUtils.Init(ref m_tmpCubeNeighboursPerThread); } }
        static List<MySlimBlock> m_tmpBlocks { get { return MyUtils.Init(ref m_tmpBlocksPerThread); } }

        private float m_accumulatedDamage;
        public float AccumulatedDamage
        {
            get { return m_accumulatedDamage; }
            private set
            {
                m_accumulatedDamage = value;
                if (m_accumulatedDamage > 0)
                    CubeGrid.AddForDamageApplication(this);
            }
        }

        public MyCubeBlock FatBlock
        {
            get;
            private set;
        }

        public MyCubeBlockDefinition BlockDefinition;
        public Vector3I Min;
        public Vector3I Max;
        public MyBlockOrientation Orientation = MyBlockOrientation.Identity;
        public Vector3I Position;
        private MyCubeGrid m_cubeGrid;
        public MyCubeGrid CubeGrid
        {
            get { return m_cubeGrid; }
            set
            {
                if (m_cubeGrid != value)
                {
                    bool wasNull = m_cubeGrid == null;
                    MyCubeGrid oldGrid = m_cubeGrid;
                    m_cubeGrid = value;
                    if (FatBlock != null && !wasNull)
                    {
                        FatBlock.OnCubeGridChanged(oldGrid);
                        if (CubeGridChanged != null)
                            CubeGridChanged(this, oldGrid);
                    }
                }
            }
        }
        public Vector3 ColorMaskHSV;
        public float Dithering = 0; // Per-block dithering

        // Only block which does not use deformation is drill
        public bool UsesDeformation = true;

        // How much block is damaged (bone move distance is multiplied by this)
        public float DeformationRatio = 1.0f;

        public bool ShowParts { get; private set; }
        public bool HasPhysics = true;

        private MyEntity3DSoundEmitter m_soundEmitter;

        private MyComponentStack m_componentStack;
        private MyObjectBuilder_CubeBlock m_objectBuilder;

        private MyConstructionStockpile m_stockpile = null;
        private float m_cachedMaxDeformation;

        /// <summary>
        /// Neighbours which are connected by mount points
        /// </summary>
        public List<MySlimBlock> Neighbours = new List<MySlimBlock>();

        public bool IsFullIntegrity
        {
            get { return m_componentStack.IsFullIntegrity; }
        }

        public float BuildLevelRatio
        {
            get
            {
                return m_componentStack.BuildRatio;
            }
        }

        public float BuildIntegrity
        {
            get { return m_componentStack.BuildIntegrity; }
        }

        public bool IsFullyDismounted
        {
            get
            {
                return m_componentStack.IsFullyDismounted;
            }
        }

        public bool IsDestroyed
        {
            get
            {
                return m_componentStack.IsDestroyed;
            }
        }

        public bool UseDamageSystem { get; private set; }

        public float Integrity
        {
            get
            {
                return m_componentStack.Integrity;
            }
        }

        public float MaxIntegrity
        {
            get
            {
                return m_componentStack.MaxIntegrity;
            }
        }

        public float CurrentDamage
        {
            get
            {
                return BuildIntegrity - Integrity;
            }
        }

        public float DamageRatio
        {
            get
            {
                return 2.0f - m_componentStack.BuildIntegrity / MaxIntegrity;
            }
        }

        public bool StockpileAllocated
        {
            get
            {
                return m_stockpile != null;
            }
        }

        public bool StockpileEmpty
        {
            get
            {
                return !StockpileAllocated || m_stockpile.IsEmpty();
            }
        }

        public bool HasDeformation
        {
            get
            {
                return CubeGrid.Skeleton.IsDeformed(Position, 0.0f, CubeGrid, true);
            }
        }

        public float MaxDeformation
        {
            get
            {
                return m_cachedMaxDeformation;
            }
        }

        public MyComponentStack ComponentStack
        {
            get
            {
                return m_componentStack;
            }
        }

        public event Action<MySlimBlock, MyCubeGrid> CubeGridChanged;

        public float m_lastDamage = 0f;

        public long m_lastAttackerId = 0;

        public MyStringHash m_lastDamageType = MyDamageType.Unknown;

        // Unique identifier
        public int UniqueId
        {
            get;
            private set;
        }

        /// <summary>
        /// Multiblock definition which the block was created from or null.
        /// </summary>
        public MyMultiBlockDefinition MultiBlockDefinition;
        /// <summary>
        /// Multiblock unique identifier (all blocks in a multiblock have the same identifier). 0 means single block (default).
        /// </summary>
        public int MultiBlockId;

        public bool IsMultiBlockPart
        {
            get { return MultiBlockId != 0 && MultiBlockDefinition != null; }
        }


        public MySlimBlock()
        {
            m_soundEmitter = new MyEntity3DSoundEmitter(null);
            UniqueId = MyRandom.Instance.Next();
            UseDamageSystem = true;
        }

        public void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid, MyCubeBlock fatBlock)
        {
            ProfilerShort.Begin("SlimBlock.Init(objectBuilder, ...)");
            Debug.Assert(cubeGrid != null);
            FatBlock = fatBlock;
            m_soundEmitter.Entity = FatBlock;

            if (objectBuilder is MyObjectBuilder_CompoundCubeBlock)
                BlockDefinition = MyCompoundCubeBlock.GetCompoundCubeBlockDefinition();
            else
                BlockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(objectBuilder.GetId());
            m_componentStack = new MyComponentStack(BlockDefinition, objectBuilder.IntegrityPercent, objectBuilder.BuildPercent);

            if (MyCubeGridDefinitions.GetCubeRotationOptions(BlockDefinition) == MyRotationOptionsEnum.None)
            {
                objectBuilder.BlockOrientation = MyBlockOrientation.Identity;
            }

            DeformationRatio = BlockDefinition.DeformationRatio;
            Min = objectBuilder.Min;

            Orientation = objectBuilder.BlockOrientation;
            if (!Orientation.IsValid)
                Orientation = MyBlockOrientation.Identity;

            Debug.Assert(Orientation.IsValid, "Orientation of block is not valid.");

            CubeGrid = cubeGrid;
            ColorMaskHSV = objectBuilder.ColorMaskHSV;

            if (BlockDefinition.CubeDefinition != null)
            {
                //Ensure we have always only one distinct orientation use
                Orientation = MyCubeGridDefinitions.GetTopologyUniqueOrientation(BlockDefinition.CubeDefinition.CubeTopology, Orientation);
            }

            ComputeMax(BlockDefinition, Orientation, ref Min, out Max);
            Position = ComputePositionInGrid(new MatrixI(Orientation), BlockDefinition, Min);

            if (objectBuilder.MultiBlockId != 0 && objectBuilder.MultiBlockDefinition != null)
            {
                MultiBlockDefinition = MyDefinitionManager.Static.TryGetMultiBlockDefinition(objectBuilder.MultiBlockDefinition.Value);
                if (MultiBlockDefinition != null)
                {
                    MultiBlockId = objectBuilder.MultiBlockId;
                }
            }

            UpdateShowParts();

            if (FatBlock == null)
            {
                bool isRenderedAsModel = !String.IsNullOrEmpty(BlockDefinition.Model);
                bool showConstructionModel = BlockDefinition.BlockTopology == MyBlockTopology.Cube && !ShowParts;
                if (isRenderedAsModel || showConstructionModel)
                {
                    FatBlock = new MyCubeBlock();
                    m_soundEmitter.Entity = FatBlock;
                }
            }

            if (FatBlock != null)
            {
                ProfilerShort.Begin("FatBlock.Init(objectBuilder, ...)");
                FatBlock.SlimBlock = this;
                FatBlock.Init(objectBuilder, cubeGrid);
                ProfilerShort.End();
            }

            if (objectBuilder.ConstructionStockpile != null)
            {
                EnsureConstructionStockpileExists();
                m_stockpile.Init(objectBuilder.ConstructionStockpile);
            }
            else if (objectBuilder.ConstructionInventory != null) // Backwards compatibility
            {
                EnsureConstructionStockpileExists();
                m_stockpile.Init(objectBuilder.ConstructionInventory);
            }

            if (FatBlock == null || FatBlock.GetType() == typeof(MyCubeBlock))
                m_objectBuilder = new MyObjectBuilder_CubeBlock();

            if (MyFakes.SHOW_DAMAGE_EFFECTS && FatBlock != null && BlockDefinition.RatioEnoughForDamageEffect(Integrity / MaxIntegrity))
            {//start effect
                if (CurrentDamage > 0)//fix for weird simple blocks having FatBlock - old save?
                {
                    FatBlock.SetDamageEffect(true);
                }

            }

            UpdateMaxDeformation();

            ProfilerShort.End();
        }
        public void ResumeDamageEffect()
        {

            if (MyFakes.SHOW_DAMAGE_EFFECTS && FatBlock != null && BlockDefinition.RatioEnoughForDamageEffect(Integrity / MaxIntegrity))
            {//start effect
                if (CurrentDamage > 0)//fix for weird simple blocks having FatBlock - old save?
                {
                    FatBlock.SetDamageEffect(true);
                }

            }
        }
        /// <summary>
        /// Initializes the orientation of the slim block according to the given forward and up vectors.
        /// Note that the resulting orientation can be different than the supplied orientation due to symmetries.
        /// This function chooses one canonical orientation for all orientations from one symetry equivalent group of orientations.
        /// </summary>
        public void InitOrientation(Base6Directions.Direction Forward, Base6Directions.Direction Up)
        {
            if (MyCubeGridDefinitions.GetCubeRotationOptions(BlockDefinition) == MyRotationOptionsEnum.None)
            {
                Orientation = MyBlockOrientation.Identity;
            }
            else
            {
                Orientation = new MyBlockOrientation(Forward, Up);
            }

            if (BlockDefinition.CubeDefinition != null)
            {
                //Ensure we have always only one distinct orientation use
                Orientation = MyCubeGridDefinitions.GetTopologyUniqueOrientation(BlockDefinition.CubeDefinition.CubeTopology, Orientation);
            }
        }

        /// <summary>
        /// An argument variant of the previous function
        /// </summary>
        public void InitOrientation(MyBlockOrientation orientation)
        {
            if (!orientation.IsValid)
                Orientation = MyBlockOrientation.Identity;

            InitOrientation(orientation.Forward, orientation.Up);
        }

        public void InitOrientation(ref Vector3I forward, ref Vector3I up)
        {
            Debug.Assert(forward.RectangularLength() == 1 && up.RectangularLength() == 1);
            Debug.Assert(Vector3I.Dot(ref forward, ref up) == 0);

            InitOrientation(Base6Directions.GetDirection(forward), Base6Directions.GetDirection(up));
        }

        public MyObjectBuilder_CubeBlock GetObjectBuilder()
        {
            return GetObjectBuilderInternal(copy: false);
        }

        public MyObjectBuilder_CubeBlock GetCopyObjectBuilder()
        {
            return GetObjectBuilderInternal(copy: true);
        }

        private MyObjectBuilder_CubeBlock GetObjectBuilderInternal(bool copy)
        {
            MyObjectBuilder_CubeBlock builder = null;
            if (FatBlock != null)
            {
                builder = FatBlock.GetObjectBuilderCubeBlock(copy);
            }
            else
            {
                builder = (MyObjectBuilder_CubeBlock)MyObjectBuilderSerializer.CreateNewObject(BlockDefinition.Id);
            }

            Debug.Assert(Orientation.IsValid);

            builder.SubtypeName = BlockDefinition.Id.SubtypeName;
            builder.Min = this.Min;
            builder.BlockOrientation = Orientation;
            builder.IntegrityPercent = m_componentStack.Integrity / m_componentStack.MaxIntegrity;
            builder.BuildPercent = m_componentStack.BuildRatio;
            builder.ColorMaskHSV = ColorMaskHSV;

            if (m_stockpile == null || m_stockpile.GetItems().Count == 0)
                builder.ConstructionStockpile = null;
            else
                builder.ConstructionStockpile = m_stockpile.GetObjectBuilder();

            if (IsMultiBlockPart)
            {
                builder.MultiBlockDefinition = MultiBlockDefinition.Id;
                builder.MultiBlockId = MultiBlockId;
            }

            return builder;
        }

        public void AddNeighbours()
        {
            // For each side
            AddNeighbours(Min, new Vector3I(Min.X, Max.Y, Max.Z), -Vector3I.UnitX);
            AddNeighbours(Min, new Vector3I(Max.X, Min.Y, Max.Z), -Vector3I.UnitY);
            AddNeighbours(Min, new Vector3I(Max.X, Max.Y, Min.Z), -Vector3I.UnitZ);
            AddNeighbours(new Vector3I(Max.X, Min.Y, Min.Z), Max, Vector3I.UnitX);
            AddNeighbours(new Vector3I(Min.X, Max.Y, Min.Z), Max, Vector3I.UnitY);
            AddNeighbours(new Vector3I(Min.X, Min.Y, Max.Z), Max, Vector3I.UnitZ);

            if (FatBlock != null)
                FatBlock.OnAddedNeighbours();
        }

        private void AddNeighbours(Vector3I min, Vector3I max, Vector3I normalDirection)
        {
            Vector3I temp;
            for (temp.X = min.X; temp.X <= max.X; temp.X++)
            {
                for (temp.Y = min.Y; temp.Y <= max.Y; temp.Y++)
                {
                    for (temp.Z = min.Z; temp.Z <= max.Z; temp.Z++)
                    {
                        AddNeighbour(temp, normalDirection);
                    }
                }
            }
        }

        private void AddNeighbour(Vector3I pos, Vector3I dir)
        {
            var otherBlock = CubeGrid.GetCubeBlock(pos + dir);

            if (otherBlock != null && otherBlock != this)
            {
                if (MyFakes.ENABLE_COMPOUND_BLOCKS)
                {
                    if (Neighbours.Contains(otherBlock))
                        return;

                    var thisCompound = FatBlock as MyCompoundCubeBlock;
                    var otherCompound = otherBlock.FatBlock as MyCompoundCubeBlock;

                    if (thisCompound != null)
                    {
                        foreach (var thisBlockInCompound in thisCompound.GetBlocks())
                        {
                            var thisMountPoints = thisBlockInCompound.BlockDefinition.GetBuildProgressModelMountPoints(thisBlockInCompound.BuildLevelRatio);

                            if (otherCompound != null)
                            {
                                foreach (var otherBlockInCompound in otherCompound.GetBlocks())
                                {
                                    var otherMountPoints = otherBlockInCompound.BlockDefinition.GetBuildProgressModelMountPoints(otherBlockInCompound.BuildLevelRatio);
                                    if (AddNeighbour(ref dir, thisBlockInCompound, thisMountPoints, otherBlockInCompound, otherMountPoints, this, otherBlock))
                                        return;
                                }
                            }
                            else
                            {
                                var otherMountPoints = otherBlock.BlockDefinition.GetBuildProgressModelMountPoints(otherBlock.BuildLevelRatio);
                                if (AddNeighbour(ref dir, thisBlockInCompound, thisMountPoints, otherBlock, otherMountPoints, this, otherBlock))
                                    return;
                            }
                        }
                    }
                    else
                    {
                        var thisMountPoints = this.BlockDefinition.GetBuildProgressModelMountPoints(BuildLevelRatio);

                        if (otherCompound != null)
                        {
                            foreach (var otherBlockInCompound in otherCompound.GetBlocks())
                            {
                                var otherMountPoints = otherBlockInCompound.BlockDefinition.GetBuildProgressModelMountPoints(otherBlockInCompound.BuildLevelRatio);
                                if (AddNeighbour(ref dir, this, thisMountPoints, otherBlockInCompound, otherMountPoints, this, otherBlock))
                                    return;
                            }
                        }
                        else
                        {
                            var otherMountPoints = otherBlock.BlockDefinition.GetBuildProgressModelMountPoints(otherBlock.BuildLevelRatio);
                            if (AddNeighbour(ref dir, this, thisMountPoints, otherBlock, otherMountPoints, this, otherBlock))
                                return;
                        }
                    }
                }
                else
                {
                    var thisMountPoints = this.BlockDefinition.GetBuildProgressModelMountPoints(BuildLevelRatio);
                    var bMountPoints = otherBlock.BlockDefinition.GetBuildProgressModelMountPoints(otherBlock.BuildLevelRatio);
                    if (MyCubeGrid.CheckMountPointsForSide(this.BlockDefinition, thisMountPoints, ref this.Orientation, ref this.Position, ref dir, otherBlock.BlockDefinition, bMountPoints, ref otherBlock.Orientation, ref otherBlock.Position))
                    {
                        if (this.ConnectionAllowed(ref pos, ref dir, otherBlock) && !Neighbours.Contains(otherBlock))
                        {
                            Debug.Assert(!otherBlock.Neighbours.Contains(this), "Inconsistent neighbours");
                            Neighbours.Add(otherBlock);
                            otherBlock.Neighbours.Add(this);
                        }
                    }
                }
            }
        }

        private static bool AddNeighbour(ref Vector3I dir,
            MySlimBlock thisBlock, MyCubeBlockDefinition.MountPoint[] thisMountPoints,
            MySlimBlock otherBlock, MyCubeBlockDefinition.MountPoint[] otherMountPoints,
            MySlimBlock thisParentBlock, MySlimBlock otherParentBlock)
        {
            if (MyCubeGrid.CheckMountPointsForSide(thisBlock.BlockDefinition, thisMountPoints, ref thisBlock.Orientation,
                ref thisBlock.Position, ref dir, otherBlock.BlockDefinition, otherMountPoints, ref otherBlock.Orientation,
                ref otherBlock.Position))
            {
                if (thisBlock.ConnectionAllowed(ref otherBlock.Position, ref dir, otherBlock))
                {
                    Debug.Assert(!otherParentBlock.Neighbours.Contains(thisParentBlock), "Inconsistent neighbours");
                    // Add parents
                    thisParentBlock.Neighbours.Add(otherParentBlock);
                    otherParentBlock.Neighbours.Add(thisParentBlock);
                    return true;
                }
            }

            return false;
        }

        public List<Vector3I> DisconnectFaces = new List<Vector3I>();
        private bool ConnectionAllowed(ref Vector3I otherBlockPos, ref Vector3I faceNormal, MySlimBlock other)
        {
            if (MyStructuralIntegrity.Enabled && CubeGrid.StructuralIntegrity != null)
            {
                if (!CubeGrid.StructuralIntegrity.IsConnectionFine(this, other))
                    return false;
            }
            if (DisconnectFaces.Count > 0 && DisconnectFaces.Contains(faceNormal))
                return false;
            return FatBlock == null || !FatBlock.CheckConnectionAllowed || FatBlock.ConnectionAllowed(ref otherBlockPos, ref faceNormal, other.BlockDefinition);
        }

        public void RemoveNeighbours()
        {
            bool allExists = true;
            foreach (var n in Neighbours)
            {
                allExists &= n.Neighbours.Remove(this);
            }
            Debug.Assert(allExists, "Inconsistent neighbours");
            Neighbours.Clear();

            if (FatBlock != null)
                FatBlock.OnRemovedNeighbours();
        }

        void UpdateShowParts()
        {
            if (BlockDefinition.BlockTopology != MyBlockTopology.Cube)
            {
                ShowParts = false;
                return;
            }

            var buildPercent = BuildLevelRatio;
            if (BlockDefinition.BuildProgressModels != null && BlockDefinition.BuildProgressModels.Length > 0)
            {
                var lastModel = BlockDefinition.BuildProgressModels[BlockDefinition.BuildProgressModels.Length - 1];

                ShowParts = buildPercent > lastModel.BuildRatioUpperBound;
                return;
            }
            ShowParts = true;
        }


        public void UpdateMaxDeformation()
        {
            m_cachedMaxDeformation = CubeGrid.Skeleton.MaxDeformation(Position, CubeGrid);
        }

        public int CalculateCurrentModelID()
        {
            var buildPercent = BuildLevelRatio;
            if ((buildPercent < 1.0f) && (BlockDefinition.BuildProgressModels != null) && (BlockDefinition.BuildProgressModels.Length > 0))
            {
                for (int i = 0; i < BlockDefinition.BuildProgressModels.Length; i++)
                {
                    var model = BlockDefinition.BuildProgressModels[i];
                    if (model.BuildRatioUpperBound >= buildPercent)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public string CalculateCurrentModel(out Matrix orientation)
        {
            var buildPercent = BuildLevelRatio;
            Orientation.GetMatrix(out orientation);

            if (buildPercent < 1.0f && BlockDefinition.BuildProgressModels != null && BlockDefinition.BuildProgressModels.Length > 0)
            {
                for (int i = 0; i < BlockDefinition.BuildProgressModels.Length; i++)
                {
                    var model = BlockDefinition.BuildProgressModels[i];
                    if (model.BuildRatioUpperBound >= buildPercent)
                    {
                        if (BlockDefinition.BuildProgressModels[i].RandomOrientation)
                        {
                            orientation = MyCubeGridDefinitions.AllPossible90rotations[Math.Abs(Position.GetHashCode()) % MyCubeGridDefinitions.AllPossible90rotations.Length].GetFloatMatrix();
                        }

                        return BlockDefinition.BuildProgressModels[i].File;
                    }
                }
            }

            return FatBlock != null ? FatBlock.CalculateCurrentModel(out orientation) : BlockDefinition.Model;
        }

        public static Vector3I ComputePositionInGrid(MatrixI localMatrix, MyCubeBlockDefinition blockDefinition, Vector3I min)
        {
            var center = blockDefinition.Center;
            var sizeMinusOne = blockDefinition.Size - 1;
            Vector3I rotatedBlockSize;
            Vector3I rotatedCenter;
            Vector3I.TransformNormal(ref sizeMinusOne, ref localMatrix, out rotatedBlockSize);
            Vector3I.TransformNormal(ref center, ref localMatrix, out rotatedCenter);
            var trueSize = Vector3I.Abs(rotatedBlockSize);
            var offsetCenter = rotatedCenter + min;

            if (rotatedBlockSize.X != trueSize.X) offsetCenter.X += trueSize.X;
            if (rotatedBlockSize.Y != trueSize.Y) offsetCenter.Y += trueSize.Y;
            if (rotatedBlockSize.Z != trueSize.Z) offsetCenter.Z += trueSize.Z;

            //Debug.Assert(Position == offsetCenter);
            return offsetCenter;
            //return Position;
        }

        public void SpawnFirstItemInConstructionStockpile()
        {
            Debug.Assert(!MySession.Static.CreativeMode, "SpawnFirstItemInConstructionStockpile should only be called in survival mode");
            if (!MySession.Static.CreativeMode)
            {
                EnsureConstructionStockpileExists();

                MyComponentStack.GroupInfo info = ComponentStack.GetGroupInfo(0);
                m_stockpile.ClearSyncList();
                m_stockpile.AddItems(1, info.Component.Id);
                CubeGrid.SyncObject.SendStockpileChanged(this, m_stockpile.GetSyncList());
                m_stockpile.ClearSyncList();
            }
        }

        public void FillConstructionStockpile()
        {
            if (!MySession.Static.CreativeMode)
            {
                EnsureConstructionStockpileExists();
                bool stockpileChanged = false;

                for (int i = 0; i < ComponentStack.GroupCount; i++)
                {
                    var groupInfo = ComponentStack.GetGroupInfo(i);

                    var addAmount = groupInfo.TotalCount - groupInfo.MountedCount;

                    if (addAmount > 0)
                    {
                        m_stockpile.AddItems(addAmount, groupInfo.Component.Id);
                        stockpileChanged = true;
                    }
                }
                if (stockpileChanged)
                    CubeGrid.SyncObject.SendStockpileChanged(this, m_stockpile.GetSyncList());
            }
        }

        public void MoveItemsToConstructionStockpile(MyInventoryBase fromInventory)
        {
            if (MySession.Static.CreativeMode || MySession.Static.SimpleSurvival)
                return;

            m_tmpComponents.Clear();
            GetMissingComponents(m_tmpComponents);

            if (m_tmpComponents.Count() != 0)
            {
                EnsureConstructionStockpileExists();

                m_stockpile.ClearSyncList();
                foreach (var kv in m_tmpComponents)
                {
                    var id = new MyDefinitionId(typeof(MyObjectBuilder_Component), kv.Key);
                    int amountAvailable = (int)MyCubeBuilder.BuildComponent.GetItemAmountCombined(fromInventory, id);
                    int moveAmount = Math.Min(kv.Value, amountAvailable);
                    if (moveAmount > 0)
                    {
                        MyCubeBuilder.BuildComponent.RemoveItemsCombined(fromInventory, moveAmount, id);
                        m_stockpile.AddItems((int)moveAmount, new MyDefinitionId(typeof(MyObjectBuilder_Component), kv.Key));
                    }
                }
                CubeGrid.SyncObject.SendStockpileChanged(this, m_stockpile.GetSyncList());
                m_stockpile.ClearSyncList();
            }
        }

        /// <summary>
        /// Moves items with the given flags from the construction inventory to the character.
        /// If the flags are None, all items are moved.
        /// </summary>
        public void MoveItemsFromConstructionStockpile(MyInventoryBase toInventory, MyItemFlags flags = MyItemFlags.None)
        {
            if (m_stockpile == null) return;

            Debug.Assert(toInventory != null);
            if (toInventory == null) return;

            m_tmpItemList.Clear();
            foreach (var item in m_stockpile.GetItems())
            {
                if (flags == MyItemFlags.None || (item.Content.Flags & flags) != 0)
                    m_tmpItemList.Add(item);
            }
            m_stockpile.ClearSyncList();
            foreach (var item in m_tmpItemList)
            {
                // If the item is just some component that is represented by another components, use the first
                // ME Example: ScrapWoodComponent has representation as ScrapWood or ScrapWoodBranches
                MyComponentSubstitutionDefinition substitution;
                if (MyDefinitionManager.Static.TryGetComponentSubstitutionDefinition(item.Content.GetId(), out substitution))
                {
                    Debug.Assert(substitution.ProvidingComponents.Count > 0, "Invalid component substitution definition for: " + item.Content.GetId().ToString());
                    MyDefinitionId componentId = item.Content.GetId();
                    int componentAmount = (int)item.Amount;
                    MyObjectBuilder_Base itemBuilder = item.Content;
                    if (substitution.ProvidingComponents.Count > 0)
                    {
                        componentId = substitution.ProvidingComponents.First().Key;
                        componentAmount = componentAmount * substitution.ProvidingComponents.First().Value;
                        itemBuilder = MyObjectBuilderSerializer.CreateNewObject(componentId);
                    }
                    var amount = (int)toInventory.ComputeAmountThatFits(componentId);
                    amount = Math.Min(amount, componentAmount);                   
                    toInventory.AddItems(amount, itemBuilder);
                    var removedAmount = amount;
                    if (substitution.ProvidingComponents.Count > 0)
                    {
                        removedAmount = removedAmount / substitution.ProvidingComponents.First().Value;
                    }
                    m_stockpile.RemoveItems(amount, item.Content);
                }
                else
                {
                    var amount = (int)toInventory.ComputeAmountThatFits(item.Content.GetId());
                    amount = Math.Min(amount, item.Amount);
                    toInventory.AddItems(amount, item.Content);
                    m_stockpile.RemoveItems(amount, item.Content);
                }
            }
            CubeGrid.SyncObject.SendStockpileChanged(this, m_stockpile.GetSyncList());
            m_stockpile.ClearSyncList();
        }

        public void MoveUnneededItemsFromConstructionStockpile(MyInventoryBase toInventory)
        {
            if (m_stockpile == null) return;

            Debug.Assert(toInventory != null);
            if (toInventory == null) return;

            m_tmpItemList.Clear();
            AcquireUnneededStockpileItems(m_tmpItemList);
            m_stockpile.ClearSyncList();

            foreach (var item in m_tmpItemList)
            {
                var amount = (int)toInventory.ComputeAmountThatFits(item.Content.GetId());
                amount = Math.Min(amount, item.Amount);
                toInventory.AddItems(amount, item.Content);
                m_stockpile.RemoveItems(amount, item.Content);
            }
            CubeGrid.SyncObject.SendStockpileChanged(this, m_stockpile.GetSyncList());
            m_stockpile.ClearSyncList();
        }

        public void ClearConstructionStockpile(MyInventoryBase outputInventory)
        {
            if (!StockpileEmpty)
            {
                IMyInventoryOwner inventoryOwner = null;
                if (outputInventory != null && outputInventory.Container != null)
                    inventoryOwner = outputInventory.Container.Entity as IMyInventoryOwner;
                if (inventoryOwner != null && inventoryOwner.InventoryOwnerType == MyInventoryOwnerTypeEnum.Character)
                {
                    MoveItemsFromConstructionStockpile(outputInventory);
                }
                else
                {
                    m_stockpile.ClearSyncList();
                    m_tmpItemList.Clear();
                    foreach (var item in m_stockpile.GetItems())
                    {
                        m_tmpItemList.Add(item);
                    }
                    foreach (var item in m_tmpItemList)
                    {
                        RemoveFromConstructionStockpile(item);
                    }
                    CubeGrid.SyncObject.SendStockpileChanged(this, m_stockpile.GetSyncList());
                    m_stockpile.ClearSyncList();
                }
            }
            ReleaseConstructionStockpile();
        }

        private void RemoveFromConstructionStockpile(MyStockpileItem item)
        {
            bool removed = m_stockpile.RemoveItems(item.Amount, item.Content.GetId(), item.Content.Flags);
            Debug.Assert(removed, "Not removed?");
            return;
        }

        private void AcquireUnneededStockpileItems(List<MyStockpileItem> outputList)
        {
            if (m_stockpile == null) return;

            var items = m_stockpile.GetItems();
            foreach (var item in items)
            {
                bool found = false;
                foreach (var component in BlockDefinition.Components)
                {
                    if (component.Definition.Id.SubtypeId == item.Content.SubtypeId)
                        found = true;
                }
                if (!found)
                {
                    outputList.Add(item);
                }
            }
        }

        private void ReleaseUnneededStockpileItems()
        {
            if (m_stockpile == null) return;

            if (!Sync.IsServer) return;

            m_tmpItemList.Clear();
            AcquireUnneededStockpileItems(m_tmpItemList);
            m_stockpile.ClearSyncList();

            BoundingBoxD boundingBox = new BoundingBoxD(CubeGrid.GridIntegerToWorld(Min), CubeGrid.GridIntegerToWorld(Max));
            foreach (var item in m_tmpItemList)
            {
                var spawnedEntity = MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(item.Amount, item.Content), boundingBox, CubeGrid.Physics);
                if (spawnedEntity != null)
                {
                    spawnedEntity.Physics.ApplyImpulse(
                        MyUtils.GetRandomVector3Normalized() * spawnedEntity.Physics.Mass / 5.0f,
                        spawnedEntity.PositionComp.GetPosition());
                }
                m_stockpile.RemoveItems(item.Amount, item.Content);
            }
            CubeGrid.SyncObject.SendStockpileChanged(this, m_stockpile.GetSyncList());
            m_stockpile.ClearSyncList();
        }

        public int GetConstructionStockpileItemAmount(MyDefinitionId id)
        {
            if (m_stockpile == null) return 0;
            else return m_stockpile.GetItemAmount(id);
        }

        // TODO: Temporary method until Martin rewrites saving components
        public void SetToConstructionSite()
        {
            m_componentStack.DestroyCompletely();
        }

        public void GetMissingComponents(Dictionary<string, int> addToDictionary)
        {
            m_componentStack.GetMissingComponents(addToDictionary, m_stockpile);
        }

        private void ReleaseConstructionStockpile()
        {
            if (m_stockpile != null)
            {
                Debug.Assert(m_stockpile.IsEmpty(), "Construction stockpile was not empty during removal from MySlimBlock");
                m_stockpile = null;
            }
        }

        private void EnsureConstructionStockpileExists()
        {
            if (m_stockpile == null)
                m_stockpile = new MyConstructionStockpile();
        }

        public void SpawnConstructionStockpile()
        {
            if (m_stockpile == null) return;

            Matrix worldMat = CubeGrid.WorldMatrix;

            int dist = (Max).RectangularDistance(Min) + 3;
            Vector3 a = Min;
            Vector3 b = Max;
            a *= CubeGrid.GridSize;
            b *= CubeGrid.GridSize;
            a = Vector3.Transform(a, worldMat);
            b = Vector3.Transform(b, worldMat);
            Vector3 avgPos = (a + b) / 2;

            Vector3 gravity = MyGravityProviderSystem.CalculateTotalGravityInPoint(avgPos);
            if (gravity.Length() != 0.0f)
            {
                gravity.Normalize();

                Vector3I? intersected = CubeGrid.RayCastBlocks(avgPos, avgPos + gravity * dist * CubeGrid.GridSize);
                if (!intersected.HasValue)
                {
                    a = avgPos;
                }
                else
                {
                    a = intersected.Value;
                    a *= CubeGrid.GridSize;
                    a = Vector3.Transform(a, worldMat);
                    a -= gravity * CubeGrid.GridSize * 0.1f;
                }
            }

            var items = m_stockpile.GetItems();
            foreach (var item in items)
            {
                var inventoryItem = new MyPhysicalInventoryItem(item.Amount, item.Content);
                MyFloatingObjects.Spawn(inventoryItem, a, worldMat.Forward, worldMat.Up, CubeGrid.Physics);
            }
        }

        public bool CanContinueBuild(MyInventory sourceInventory)
        {
            if (IsFullIntegrity) return false;

            return m_componentStack.CanContinueBuild(sourceInventory, m_stockpile);
        }

        public void FixBones(float oldDamage, float maxAllowedBoneMovement)
        {
            float boneMoveFactor = CurrentDamage / oldDamage;
            if (oldDamage == 0.0f)
            {
                boneMoveFactor = 0.0f;
            }

            // Limit bone movement factor so that the bones don't move too quickly
            float maxBoneMovement = (1.0f - boneMoveFactor) * MaxDeformation;
            if (MaxDeformation != 0 && maxBoneMovement > maxAllowedBoneMovement)
            {
                boneMoveFactor = 1.0f - maxAllowedBoneMovement / MaxDeformation;
            }

            if (boneMoveFactor == 0.0f)
            {
                CubeGrid.ResetBlockSkeleton(this, true);
            }

            if (boneMoveFactor > 0.0f)
            {
                CubeGrid.MultiplyBlockSkeleton(this, boneMoveFactor, true);
            }
        }

        void IMyDestroyableObject.DoDamage(float damage, MyStringHash damageType, bool sync, MyHitInfo? hitInfo, long attackerId)
        {
            if (sync)
            {
                Debug.Assert(Sync.IsServer);
                if (Sync.IsServer)
                    MySyncHelper.DoDamageSynced(this, damage, damageType, hitInfo, attackerId);
            }
            else
                this.DoDamage(damage, damageType, hitInfo: hitInfo, attackerId: attackerId);
            return;
        }

        public void DoDamage(float damage, MyStringHash damageType, bool addDirtyParts = true, MyHitInfo? hitInfo = null, bool createDecal = true, long attackerId = 0)
        {
            if (!CubeGrid.BlocksDestructionEnabled)
                return;

            if (FatBlock is MyCompoundCubeBlock) //jn: TODO think of something better
            {
                (FatBlock as MyCompoundCubeBlock).DoDamage(damage, damageType, hitInfo, attackerId);
                return;
            }

            damage *= DamageRatio; // Low-integrity blocks get more damage
            if (MyPerGameSettings.Destruction)
            {
                damage *= DeformationRatio;
            }

            ProfilerShort.Begin("FatBlock.DoDamage");
            try
            {
                if (FatBlock != null && CubeGrid.Physics != null && CubeGrid.Physics.Enabled)  //Fatblock dont have physics
                {
                    var destroyable = FatBlock as IMyDestroyableObject;
                    if (destroyable != null)
                        destroyable.DoDamage(damage, damageType, false, attackerId: attackerId);
                }
            }
            finally { ProfilerShort.End(); }

            MyDamageInformation damageInfo = new MyDamageInformation(false, damage, damageType, attackerId);
            if (UseDamageSystem)
                MyDamageSystem.Static.RaiseBeforeDamageApplied(this, ref damageInfo);

            MySession.Static.NegativeIntegrityTotal += damageInfo.Amount;
            Debug.Assert(damageInfo.Amount > 0);
            AccumulatedDamage += damageInfo.Amount;

            if (m_componentStack.Integrity - AccumulatedDamage <= MyComponentStack.MOUNT_THRESHOLD)
            {
                if (MyPerGameSettings.Destruction && hitInfo.HasValue)
                {
                    AccumulatedDamage = 0;

                    var gridPhysics = CubeGrid.Physics;
                    float maxDestructionRadius = CubeGrid.GridSizeEnum == MyCubeSize.Small ? 0.5f : 3;
                    if (Sync.IsServer)
                        Sandbox.Engine.Physics.MyDestructionHelper.TriggerDestruction(damageInfo.Amount - m_componentStack.Integrity, gridPhysics, hitInfo.Value.Position, hitInfo.Value.Normal, maxDestructionRadius);
                }
                else
                {
                    ApplyAccumulatedDamage(addDirtyParts);
                }
                CubeGrid.RemoveFromDamageApplication(this);
            }
            else
            {
                if (MyFakes.SHOW_DAMAGE_EFFECTS && FatBlock != null && BlockDefinition.RatioEnoughForDamageEffect((Integrity - damage) / MaxIntegrity))
                    FatBlock.SetDamageEffect(true);

                if (hitInfo.HasValue && createDecal)
                    CubeGrid.RenderData.AddDecal(Position, Vector3D.Transform(hitInfo.Value.Position, CubeGrid.PositionComp.WorldMatrixInvScaled),
                        Vector3D.TransformNormal(hitInfo.Value.Normal, CubeGrid.PositionComp.WorldMatrixInvScaled), BlockDefinition.PhysicalMaterial.DamageDecal);
            }

            if (UseDamageSystem)
                MyDamageSystem.Static.RaiseAfterDamageApplied(this, damageInfo);

            m_lastDamage = damage;
            m_lastAttackerId = attackerId;
            m_lastDamageType = damageType;

            return;
        }

        /// <summary>
        /// Destruction does not apply any damage to block (instead triggers destruction) so it is applied through this method 
        /// when fracture component is created or when any of internal fratures is removed from component.
        /// </summary>
        public void ApplyDestructionDamage(float multiplier = 1f)
        {
            if (MyFakes.ENABLE_FRACTURE_COMPONENT && Sync.IsServer && MyPerGameSettings.Destruction)
            {
                ((IMyDestroyableObject)this).DoDamage(multiplier * MyDefinitionManager.Static.DestructionDefinition.DestructionDamage, MyDamageType.Destruction, true);
            }
        }

        /// <summary>
        /// Do not call explicitly. Will be done automatically by the grid.
        /// </summary>
        public void ApplyAccumulatedDamage(bool addDirtyParts = true)
        {
            Debug.Assert(AccumulatedDamage > 0f, "No damage done that could be applied to the block.");
            ProfilerShort.Begin("MySlimBlock.ApplyAccumulatedDamage");

            if (MySession.Static.SurvivalMode)
            {
                EnsureConstructionStockpileExists();
            }
            if (m_stockpile != null)
            {
                m_stockpile.ClearSyncList();
                m_componentStack.ApplyDamage(AccumulatedDamage, m_stockpile);

                if (Sync.IsServer)
                    CubeGrid.SyncObject.SendStockpileChanged(this, m_stockpile.GetSyncList());

                m_stockpile.ClearSyncList();
            }
            else
            {
                m_componentStack.ApplyDamage(AccumulatedDamage, null);
            }

            if (BlockDefinition.RatioEnoughForOwnership(Integrity / MaxIntegrity) && !BlockDefinition.RatioEnoughForOwnership((Integrity - AccumulatedDamage) / BlockDefinition.MaxIntegrity))
            {
                if (FatBlock != null)
                {
                    FatBlock.OnIntegrityChanged(BuildIntegrity, Integrity, false, MySession.LocalPlayerId);
                }
            }

            AccumulatedDamage = 0.0f;
            //CubeGrid.SyncObject.SendIntegrityChanged(this, MyCubeGrid.MyIntegrityChangeEnum.Damage, 0);

            if (m_componentStack.IsDestroyed)
            {
                if (MyFakes.SHOW_DAMAGE_EFFECTS && FatBlock != null)
                    FatBlock.SetDamageEffect(false);
                CubeGrid.RemoveDestroyedBlock(this);
                if (addDirtyParts)
                {
                    CubeGrid.Physics.AddDirtyBlock(this);
                }

                if (UseDamageSystem)
                    MyDamageSystem.Static.RaiseDestroyed(this, new MyDamageInformation(false, m_lastDamage, m_lastDamageType, m_lastAttackerId));
            }

            ProfilerShort.End();
        }

        public void UpdateVisual()
        {
            UpdateShowParts();

            if (!ShowParts)
            {
                if (FatBlock == null)
                {
                    FatBlock = new MyCubeBlock();
                    FatBlock.SlimBlock = this;
                    FatBlock.Init();
                    CubeGrid.Hierarchy.AddChild(FatBlock);
                    m_soundEmitter.Entity = FatBlock;
                    m_soundEmitter.SetPosition(null);
                }
                else
                {
                    FatBlock.UpdateVisual();
                }
            }
            else if (FatBlock != null)
            {
                var pos = FatBlock.WorldMatrix.Translation;
                CubeGrid.Hierarchy.RemoveChild(FatBlock);
                FatBlock.Close();
                FatBlock = null;
                m_soundEmitter.Entity = null;
            }
            CubeGrid.SetBlockDirty(this);
            if (CubeGrid.Physics != null)
            {
                CubeGrid.Physics.AddDirtyArea(Min, Max);
            }
        }

        public void IncreaseMountLevel(float welderMountAmount, long welderOwnerPlayerId, MyInventoryBase outputInventory = null, float maxAllowedBoneMovement = 0.0f, bool isHelping = false, MyOwnershipShareModeEnum sharing = MyOwnershipShareModeEnum.Faction)
        {
            ProfilerShort.Begin("MySlimBlock.IncreaseMountLevel");
            welderMountAmount *= BlockDefinition.IntegrityPointsPerSec;
            MySession.Static.PositiveIntegrityTotal += welderMountAmount;

            if (MySession.Static.CreativeMode)
            {
                ClearConstructionStockpile(outputInventory);
            }
            else
            {
                IMyInventoryOwner inventoryOwner = null;
                if (outputInventory != null && outputInventory.Container != null)
                    inventoryOwner = outputInventory.Container.Entity as IMyInventoryOwner;
                if (inventoryOwner != null && inventoryOwner.InventoryOwnerType == MyInventoryOwnerTypeEnum.Character)
                {
                    MoveItemsFromConstructionStockpile(outputInventory, MyItemFlags.Damaged);
                }
            }

            float oldPercentage = m_componentStack.BuildRatio;
            float oldDamage = CurrentDamage;

            if (!BlockDefinition.RatioEnoughForOwnership(BuildLevelRatio) && BlockDefinition.RatioEnoughForOwnership((BuildIntegrity + welderMountAmount) / BlockDefinition.MaxIntegrity))
            {
                if (FatBlock != null && outputInventory != null && !isHelping)
                {
                    FatBlock.OnIntegrityChanged(BuildIntegrity, Integrity, true, welderOwnerPlayerId, sharing);
                }
            }

            if (MyFakes.SHOW_DAMAGE_EFFECTS && FatBlock != null && !BlockDefinition.RatioEnoughForDamageEffect((Integrity + welderMountAmount) / MaxIntegrity))
            {//stop effect
                FatBlock.SetDamageEffect(false);
            }

            bool removeDecals = false;

            if (m_stockpile != null)
            {
                m_stockpile.ClearSyncList();
                m_componentStack.IncreaseMountLevel(welderMountAmount, m_stockpile);
                CubeGrid.SyncObject.SendStockpileChanged(this, m_stockpile.GetSyncList());
                m_stockpile.ClearSyncList();
            }
            else
            {
                m_componentStack.IncreaseMountLevel(welderMountAmount, null);
            }

            if (m_componentStack.IsFullIntegrity)
            {
                ReleaseConstructionStockpile();
                removeDecals = true;
            }

            ProfilerShort.Begin("ModelChange");
            MyCubeGrid.MyIntegrityChangeEnum integrityChangeType = MyCubeGrid.MyIntegrityChangeEnum.Damage;
            if (BlockDefinition.ModelChangeIsNeeded(oldPercentage, m_componentStack.BuildRatio) || BlockDefinition.ModelChangeIsNeeded(m_componentStack.BuildRatio, oldPercentage))
            {
                removeDecals = true;
                if (FatBlock != null)
                {
                    // this needs to be detected here because for cubes the following call to UpdateVisual() set FatBlock to null when the construction is complete
                    if (m_componentStack.IsFunctional)
                    {
                        integrityChangeType = MyCubeGrid.MyIntegrityChangeEnum.ConstructionEnd;
                    }
                }

                UpdateVisual();
                if (FatBlock != null)
                {
                    int buildProgressID = CalculateCurrentModelID();
                    if (buildProgressID == 0)
                    {
                        integrityChangeType = MyCubeGrid.MyIntegrityChangeEnum.ConstructionBegin;
                    }
                    else if (!m_componentStack.IsFunctional)
                    {
                        integrityChangeType = MyCubeGrid.MyIntegrityChangeEnum.ConstructionProcess;
                    }
                }

                PlayConstructionSound(integrityChangeType);
                CreateConstructionSmokes();

                if (CubeGrid.GridSystems.GasSystem != null)
                {
                    CubeGrid.GridSystems.GasSystem.Pressurize();
                }
            }
            ProfilerShort.End();

            if (HasDeformation)
                CubeGrid.SetBlockDirty(this);

            if (removeDecals)
                CubeGrid.RenderData.RemoveDecals(Position);

            CubeGrid.SyncObject.SendIntegrityChanged(this, integrityChangeType, 0);
            CubeGrid.OnIntegrityChanged(this);

            if (maxAllowedBoneMovement != 0.0f)
                FixBones(oldDamage, maxAllowedBoneMovement);

            if (MyFakes.ENABLE_GENERATED_BLOCKS && !BlockDefinition.IsGeneratedBlock && BlockDefinition.GeneratedBlockDefinitions != null && BlockDefinition.GeneratedBlockDefinitions.Length > 0)
            {
                UpdateProgressGeneratedBlocks(oldPercentage);
            }

            ProfilerShort.End();
        }

        public void DecreaseMountLevel(float grinderAmount, MyInventoryBase outputInventory)
        {
            if (FatBlock != null)
                grinderAmount /= FatBlock.DisassembleRatio;
            else
                grinderAmount /= BlockDefinition.DisassembleRatio;

            grinderAmount = grinderAmount * BlockDefinition.IntegrityPointsPerSec;
            if (MySession.Static.CreativeMode)
            {
                ClearConstructionStockpile(outputInventory);
            }
            else
            {
                EnsureConstructionStockpileExists();
            }

            float oldBuildRatio = m_componentStack.BuildRatio;
            float newBuildRatio = (BuildIntegrity - grinderAmount) / BlockDefinition.MaxIntegrity;

            if (BlockDefinition.RatioEnoughForOwnership(BuildLevelRatio) && !BlockDefinition.RatioEnoughForOwnership(newBuildRatio))
            {
                if (FatBlock != null)
                {
                    FatBlock.OnIntegrityChanged(BuildIntegrity, Integrity, false, MySession.LocalPlayerId);
                }
            }

            long toolOwner = 0;
            if (outputInventory != null && outputInventory.Entity != null)
            {
                var inventoryOwner = outputInventory.Entity;
                var moduleOwner = inventoryOwner as IMyComponentOwner<MyIDModule>;
                var character = inventoryOwner as MyCharacter;
                if (moduleOwner == null)
                {
                    if (character != null)
                    {
                        Debug.Assert(character.ControllerInfo.Controller != null, "Controller was null on the character in DecreaseMountLevel!");
                        if (character.ControllerInfo.Controller == null)
                            toolOwner = character.ControllerInfo.ControllingIdentityId;
                    }
                }
                else
                {
                    MyIDModule module;
                    if (moduleOwner.GetComponent(out module))
                        toolOwner = module.Owner;
                }
            }

            UpdateHackingIndicator(newBuildRatio, oldBuildRatio, toolOwner);

            if (m_stockpile != null)
            {
                m_stockpile.ClearSyncList();
                m_componentStack.DecreaseMountLevel(grinderAmount, m_stockpile);
                CubeGrid.SyncObject.SendStockpileChanged(this, m_stockpile.GetSyncList());
                m_stockpile.ClearSyncList();
            }
            else
            {
                m_componentStack.DecreaseMountLevel(grinderAmount, null);
            }

            bool modelChangeNeeded = BlockDefinition.ModelChangeIsNeeded(m_componentStack.BuildRatio, oldBuildRatio);

            MyCubeGrid.MyIntegrityChangeEnum integrityChangeType = MyCubeGrid.MyIntegrityChangeEnum.Damage;
            if (modelChangeNeeded)
            {
                UpdateVisual();

                if (FatBlock != null)
                {
                    int buildProgressID = CalculateCurrentModelID();
                    if ((buildProgressID == -1) || (BuildLevelRatio == 0f))
                    {
                        integrityChangeType = MyCubeGrid.MyIntegrityChangeEnum.ConstructionEnd;
                    }
                    else if (buildProgressID == BlockDefinition.BuildProgressModels.Length - 1)
                    {
                        integrityChangeType = MyCubeGrid.MyIntegrityChangeEnum.ConstructionBegin;
                    }
                    else
                    {
                        integrityChangeType = MyCubeGrid.MyIntegrityChangeEnum.ConstructionProcess;
                    }
                }

                PlayConstructionSound(integrityChangeType, true);
                CreateConstructionSmokes();
            }

            if (CubeGrid.GridSystems.GasSystem != null)
            {
                CubeGrid.GridSystems.GasSystem.Pressurize();
            }

            if (MyFakes.ENABLE_GENERATED_BLOCKS && !BlockDefinition.IsGeneratedBlock && BlockDefinition.GeneratedBlockDefinitions != null && BlockDefinition.GeneratedBlockDefinitions.Length > 0)
            {
                UpdateProgressGeneratedBlocks(oldBuildRatio);
            }

            CubeGrid.SyncObject.SendIntegrityChanged(this, integrityChangeType, toolOwner);
            CubeGrid.OnIntegrityChanged(this);
        }

        /// <summary>
        /// Completely deconstruct this block
        /// Intended for server-side use
        /// </summary>
        public void FullyDismount(MyInventory outputInventory)
        {
            if (!Sync.IsServer)
                return;

            float oldBuildRatio = m_componentStack.BuildRatio;

            if (MySession.Static.CreativeMode)
                ClearConstructionStockpile(outputInventory);
            else
                EnsureConstructionStockpileExists();

            if (m_stockpile != null)
            {
                m_stockpile.ClearSyncList();
                m_componentStack.DecreaseMountLevel(BuildIntegrity, m_stockpile);
                CubeGrid.SyncObject.SendStockpileChanged(this, m_stockpile.GetSyncList());
                m_stockpile.ClearSyncList();
            }
            else
                m_componentStack.DecreaseMountLevel(BuildIntegrity, null);

            bool modelChangeNeeded = BlockDefinition.ModelChangeIsNeeded(m_componentStack.BuildRatio, oldBuildRatio);
            if (modelChangeNeeded)
            {
                UpdateVisual();
                PlayConstructionSound(MyCubeGrid.MyIntegrityChangeEnum.ConstructionEnd, true);
                CreateConstructionSmokes();
            }

            if (CubeGrid.GridSystems.GasSystem != null)
                CubeGrid.GridSystems.GasSystem.Pressurize();
        }

        private void CreateConstructionSmokes()
        {
            Vector3 halfSize = new Vector3(CubeGrid.GridSize) / 2;
            BoundingBox blockBox = new BoundingBox(Min * CubeGrid.GridSize - halfSize, Max * CubeGrid.GridSize + halfSize);
            if (FatBlock != null && FatBlock.Model != null)
            {
                BoundingBox fatBlockBoxLocal = new BoundingBox(FatBlock.Model.BoundingBox.Min, FatBlock.Model.BoundingBox.Max);
                Matrix m;
                FatBlock.Orientation.GetMatrix(out m);
                BoundingBox fatBlockBoxOriented = BoundingBox.CreateInvalid();

                foreach (var corner in fatBlockBoxLocal.GetCorners())
                {
                    fatBlockBoxOriented = fatBlockBoxOriented.Include(Vector3.Transform(corner, m));
                }

                blockBox = new BoundingBox(fatBlockBoxOriented.Min + blockBox.Center, fatBlockBoxOriented.Max + blockBox.Center);
            }


            blockBox.Inflate(-0.3f);

            Vector3[] corners = blockBox.GetCorners();

            float particleStep = 0.25f;

            for (int e = 0; e < MyOrientedBoundingBox.StartVertices.Length; e++)
            {
                Vector3 offset = corners[MyOrientedBoundingBox.StartVertices[e]];
                float offsetLength = 0;
                float length = Vector3.Distance(offset, corners[MyOrientedBoundingBox.EndVertices[e]]);
                Vector3 offsetStep = particleStep * Vector3.Normalize(corners[MyOrientedBoundingBox.EndVertices[e]] - corners[MyOrientedBoundingBox.StartVertices[e]]);

                while (offsetLength < length)
                {
                    Vector3D tr = Vector3D.Transform(offset, CubeGrid.WorldMatrix);

                    MyParticleEffect smokeEffect;
                    if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_Construction, out smokeEffect))
                    {
                        smokeEffect.AutoDelete = true;
                        smokeEffect.WorldMatrix = MatrixD.CreateTranslation(tr);
                        smokeEffect.UserScale = 0.7f;
                    }

                    offsetLength += particleStep;
                    offset += offsetStep;
                }
            }
        }

        public override string ToString()
        {
            return FatBlock != null ? FatBlock.ToString() : BlockDefinition.DisplayNameText.ToString();
        }

        /// <summary>
        /// Called when block is destroyed before being removed from grid
        /// </summary>
        //public void OnDestroy()
        //{
        //    if (FatBlock != null)
        //    {
        //        Profiler.Begin("MySlimBlock.OnDestroy");
        //        FatBlock.OnDestroy();
        //        Profiler.End();
        //    }
        //}

        public static void ComputeMax(MyCubeBlockDefinition definition, MyBlockOrientation orientation, ref Vector3I min, out Vector3I max)
        {
            Vector3I size = definition.Size - 1;
            MatrixI localMatrix = new MatrixI(orientation);
            Vector3I.TransformNormal(ref size, ref localMatrix, out size);
            Vector3I.Abs(ref size, out size);
            max = min + size;
        }

        public void SetIntegrity(float buildIntegrity, float integrity, MyCubeGrid.MyIntegrityChangeEnum integrityChangeType, long grinderOwner)
        {
            float oldRatio = m_componentStack.BuildRatio;
            m_componentStack.SetIntegrity(buildIntegrity, integrity);

            if (FatBlock != null && !BlockDefinition.RatioEnoughForOwnership(oldRatio) && BlockDefinition.RatioEnoughForOwnership(m_componentStack.BuildRatio))
                FatBlock.OnIntegrityChanged(buildIntegrity, integrity, true, MySession.LocalPlayerId);

            UpdateHackingIndicator(m_componentStack.BuildRatio, oldRatio, grinderOwner);

            if (MyFakes.SHOW_DAMAGE_EFFECTS && FatBlock != null)
            {
                if (!BlockDefinition.RatioEnoughForDamageEffect(Integrity / MaxIntegrity))
                    FatBlock.SetDamageEffect(false);
            }

            bool removeDecals = IsFullIntegrity;

            if (ModelChangeIsNeeded(m_componentStack.BuildRatio, oldRatio))
            {
                removeDecals = true;
                UpdateVisual();

                if (integrityChangeType != MyCubeGrid.MyIntegrityChangeEnum.Damage)
                    CreateConstructionSmokes();

                PlayConstructionSound(integrityChangeType);

                if (CubeGrid.GridSystems.GasSystem != null)
                {
                    CubeGrid.GridSystems.GasSystem.Pressurize();
                }
            }

            if (removeDecals)
                CubeGrid.RenderData.RemoveDecals(Position);

            if (MyFakes.ENABLE_GENERATED_BLOCKS && !BlockDefinition.IsGeneratedBlock && BlockDefinition.GeneratedBlockDefinitions != null && BlockDefinition.GeneratedBlockDefinitions.Length > 0)
            {
                UpdateProgressGeneratedBlocks(oldRatio);
            }
        }

        void UpdateHackingIndicator(float newRatio, float oldRatio, long grinderOwner)
        {
            if (newRatio < oldRatio && FatBlock != null && FatBlock.IDModule != null)
            {
                var relation = FatBlock.IDModule.GetUserRelationToOwner(grinderOwner);

                if (relation == Common.MyRelationsBetweenPlayerAndBlock.Enemies || relation == Common.MyRelationsBetweenPlayerAndBlock.Neutral)
                    FatBlock.HackAttemptTime = MySandboxGame.TotalTimeInMilliseconds;
            }
        }


        public void PlayConstructionSound(MyCubeGrid.MyIntegrityChangeEnum integrityChangeType, bool deconstruction = false)
        {
            //if (m_soundEmitter.Entity == null)
            m_soundEmitter.SetPosition(CubeGrid.PositionComp.GetPosition() + (Position - 1) * CubeGrid.GridSize);
            switch (integrityChangeType)
            {
                case MyCubeGrid.MyIntegrityChangeEnum.ConstructionBegin:
                    if (deconstruction)
                        m_soundEmitter.PlaySound(DECONSTRUCTION_START, true);
                    else
                        m_soundEmitter.PlaySound(CONSTRUCTION_START, true);
                    break;

                case MyCubeGrid.MyIntegrityChangeEnum.ConstructionEnd:
                    if (deconstruction)
                        m_soundEmitter.PlaySound(DECONSTRUCTION_END, true);
                    else
                        m_soundEmitter.PlaySound(CONSTRUCTION_END, true);
                    break;

                case MyCubeGrid.MyIntegrityChangeEnum.ConstructionProcess:
                    if (deconstruction)
                        m_soundEmitter.PlaySound(DECONSTRUCTION_PROG, true);
                    else
                        m_soundEmitter.PlaySound(CONSTRUCTION_PROG, true);
                    break;
            }
        }

        private bool ModelChangeIsNeeded(float a, float b)
        {
            if (a > b)
            {
                return BlockDefinition.ModelChangeIsNeeded(b, a);
            }
            else
            {
                return BlockDefinition.ModelChangeIsNeeded(a, b);
            }
        }

        /// <summary>
        /// Forced change of build progress so that next model is shown.
        /// </summary>
        public void UpgradeBuildLevel()
        {
            float currentPercentage = m_componentStack.BuildRatio;
            float nextThreshold = 1f;

            foreach (var progressModel in BlockDefinition.BuildProgressModels)
            {
                if (progressModel.BuildRatioUpperBound > currentPercentage &&
                    progressModel.BuildRatioUpperBound <= nextThreshold)
                    nextThreshold = progressModel.BuildRatioUpperBound;
            }
            float nextPercentage = MathHelper.Clamp(nextThreshold * 1.001f, 0f, 1f);

            m_componentStack.SetIntegrity(nextPercentage * BlockDefinition.MaxIntegrity, nextPercentage * BlockDefinition.MaxIntegrity);
        }

        /// <summary>
        /// Forced change of build progress to a random value.
        /// </summary>
        public void RandomizeBuildLevel()
        {
            float rndIntegrity = MyUtils.GetRandomFloat(0f, 1f) * BlockDefinition.MaxIntegrity;
            m_componentStack.SetIntegrity(rndIntegrity, rndIntegrity);
        }

        internal void ChangeStockpile(List<MyStockpileItem> items)
        {
            EnsureConstructionStockpileExists();
            m_stockpile.Change(items);
            if (m_stockpile.IsEmpty())
                ReleaseConstructionStockpile();
        }

        internal void GetConstructionStockpileItems(List<MyStockpileItem> m_cacheStockpileItems)
        {
            if (m_stockpile != null)
            {
                foreach (var item in m_stockpile.GetItems())
                {
                    m_cacheStockpileItems.Add(item);
                }
            }
        }

        internal void RequestFillStockpile(MyInventory SourceInventory)
        {
            m_tmpComponents.Clear();
            GetMissingComponents(m_tmpComponents);

            foreach (var component in m_tmpComponents)
            {
                MyDefinitionId componentDefinition = new MyDefinitionId(typeof(MyObjectBuilder_Component), component.Key);
                if (SourceInventory.ContainItems(1, componentDefinition))
                {
                    CubeGrid.RequestFillStockpile(Position, SourceInventory);
                    return;
                }
            }
        }

        public void ComputeWorldCenter(out Vector3D worldCenter)
        {
            ComputeScaledCenter(out worldCenter);
            var worldMat = CubeGrid.WorldMatrix;
            Vector3D.Transform(ref worldCenter, ref worldMat, out worldCenter);
        }

        public void ComputeScaledCenter(out Vector3D scaledCenter)
        {
            var min = (Vector3)Min;
            var max = (Vector3)Max;
            min -= 0.5f;
            max += 0.5f;
            scaledCenter = (max + min) * 0.5f;
            scaledCenter *= CubeGrid.GridSize;
        }

        public void ComputeScaledHalfExtents(out Vector3 scaledHalfExtents)
        {
            var min = (Vector3)Min * CubeGrid.GridSize;
            var max = (Vector3)(Max + 1) * CubeGrid.GridSize;
            scaledHalfExtents = (max - min) * 0.5f;
        }

        public float GetMass()
        {
            if (FatBlock != null)
                return FatBlock.GetMass();
            Matrix m;
            if (MyDestructionData.Static != null)
                return MyDestructionData.Static.GetBlockMass(CalculateCurrentModel(out m), BlockDefinition);
            return BlockDefinition.Mass;
        }

        void IMyDestroyableObject.OnDestroy()
        {
            m_componentStack.DestroyCompletely();
            ReleaseUnneededStockpileItems();
            if (FatBlock != null)
            {
                ProfilerShort.Begin("MySlimBlock.OnDestroy");
                FatBlock.OnDestroy();
                ProfilerShort.End();
            }
            CubeGrid.RemoveFromDamageApplication(this);
            AccumulatedDamage = 0;

        }

        float IMyDestroyableObject.Integrity
        {
            get { return Integrity; }
        }

        internal void Transform(ref MatrixI transform)
        {
            Vector3I tMin;
            Vector3I tMax;
            Vector3I tPos;
            Vector3I.Transform(ref this.Min, ref transform, out tMin);
            Vector3I.Transform(ref this.Max, ref transform, out tMax);
            Vector3I.Transform(ref this.Position, ref transform, out tPos);
            Vector3I forward = Base6Directions.GetIntVector(transform.GetDirection(this.Orientation.Forward));
            Vector3I up = Base6Directions.GetIntVector(transform.GetDirection(this.Orientation.Up));

            Debug.Assert(Vector3I.Dot(ref forward, ref up) == 0);

            this.InitOrientation(ref forward, ref up);

            this.Min = Vector3I.Min(tMin, tMax);
            this.Max = Vector3I.Max(tMin, tMax);
            this.Position = tPos;

            if (FatBlock != null)
                FatBlock.OnTransformed(ref transform);
        }

        /// <summary>
        /// Returns world AABB of the block (geometry AABB). If useAABBFromBlockCubes = true then AABB from block cubes is returned.
        /// </summary>
        public void GetWorldBoundingBox(out BoundingBoxD aabb, bool useAABBFromBlockCubes = false)
        {
            if (FatBlock != null && !useAABBFromBlockCubes)
            {
                aabb = FatBlock.PositionComp.WorldAABB;
            }
            else
            {
                float gridSize = CubeGrid.GridSize;
                aabb = new BoundingBoxD(Min * gridSize - gridSize / 2, Max * gridSize + gridSize / 2);
                aabb = aabb.Transform(CubeGrid.WorldMatrix);
            }
        }

        public static void SetBlockComponents(MyHudBlockInfo hudInfo, MySlimBlock block, MyInventoryBase availableInventory = null)
        {
            hudInfo.Components.Clear();
            for (int i = 0; i < block.ComponentStack.GroupCount; i++)
            {
                var groupInfo = block.ComponentStack.GetGroupInfo(i);
                var componentInfo = new MyHudBlockInfo.ComponentInfo();
                componentInfo.DefinitionId = groupInfo.Component.Id;
                componentInfo.ComponentName = groupInfo.Component.DisplayNameText;
                componentInfo.Icon = groupInfo.Component.Icon;
                componentInfo.TotalCount = groupInfo.TotalCount;
                componentInfo.MountedCount = groupInfo.MountedCount;
                if (availableInventory != null)
                    componentInfo.AvailableAmount = (int)MyCubeBuilder.BuildComponent.GetItemAmountCombined(availableInventory, groupInfo.Component.Id);

                hudInfo.Components.Add(componentInfo);
            }

            if (!block.StockpileEmpty)
            {
                // For each component
                foreach (var comp in block.BlockDefinition.Components)
                {
                    // Get amount in stockpile
                    int amount = block.GetConstructionStockpileItemAmount(comp.Definition.Id);

                    for (int i = 0; amount > 0 && i < hudInfo.Components.Count; i++)
                    {
                        if (block.ComponentStack.GetGroupInfo(i).Component == comp.Definition)
                        {
                            if (block.ComponentStack.IsFullyDismounted)
                            {
                                return;
                            }
                            // Distribute amount in stockpile from bottom to top
                            var info = hudInfo.Components[i];
                            int space = info.TotalCount - info.MountedCount;
                            int movedItems = Math.Min(space, amount);
                            info.StockpileCount = movedItems;
                            amount -= movedItems;
                            hudInfo.Components[i] = info;
                        }
                    }
                }
            }
        }

        private void UpdateProgressGeneratedBlocks(float oldBuildRatio)
        {
            float currentBuildRatio = ComponentStack.BuildRatio;
            if (oldBuildRatio == currentBuildRatio)
                return;

            ProfilerShort.Begin("MySlimBlock.UpdateProgressGeneratedBlocks");
            if (oldBuildRatio < currentBuildRatio)
            {
                ProfilerShort.Begin("Adding generated blocks");
                // Check adding of generated blocks
                if (oldBuildRatio < BlockDefinition.BuildProgressToPlaceGeneratedBlocks && currentBuildRatio >= BlockDefinition.BuildProgressToPlaceGeneratedBlocks)
                {
                    CubeGrid.AdditionalModelGenerators.ForEach(g => g.GenerateBlocks(this));
                }
                ProfilerShort.End();
            }
            else
            {
                ProfilerShort.Begin("Removing generated blocks");
                // Check removing of generated blocks
                if (oldBuildRatio >= BlockDefinition.BuildProgressToPlaceGeneratedBlocks && currentBuildRatio < BlockDefinition.BuildProgressToPlaceGeneratedBlocks)
                {
                    m_tmpBlocks.Clear();

                    CubeGrid.GetGeneratedBlocks(this, m_tmpBlocks);
                    CubeGrid.RazeGeneratedBlocks(m_tmpBlocks);

                    m_tmpBlocks.Clear();
                }
                ProfilerShort.End();
            }
            ProfilerShort.End();
        }

        public MyFractureComponentCubeBlock GetFractureComponent()
        {
            return FatBlock != null ? FatBlock.GetFractureComponent() : null;
        }

        public void RepairFracturedBlockWithFullIntegrity(long toolOwnerId)
        {
            Debug.Assert(Sync.IsServer);

            if (!IsFullIntegrity)
                return;

            var fractureComponent = GetFractureComponent();
            Debug.Assert(fractureComponent != null);
            if (fractureComponent == null)
                return;

            float integrityAfterRepair = Integrity;
            float buildIntegrity = BuildIntegrity;

            RepairFracturedBlock(toolOwnerId);

            CubeGrid.AdditionalModelGenerators.ForEach(g => g.GenerateBlocks(this));
        }

        internal void RepairFracturedBlock(long toolOwnerId)
        {
            Debug.Assert(FatBlock != null);
            if (FatBlock == null)
                return;

            Debug.Assert(FatBlock.Components.Has<MyFractureComponentBase>());

            RepairFracturedBlockInternal();

            List<MySlimBlock> generatedBlocks = new List<MySlimBlock>();
            CubeGrid.GetGeneratedBlocks(this, generatedBlocks);
            foreach (var genBlock in generatedBlocks)
                genBlock.RepairFracturedBlockInternal();

            generatedBlocks.Clear();

            if (Sync.IsServer)
            {
                var aabb = FatBlock.PositionComp.WorldAABB;
                if (BlockDefinition.CubeSize == MyCubeSize.Large)
                    aabb.Inflate(-0.16);
                else
                    aabb.Inflate(-0.04);
                MyFracturedPiecesManager.Static.RemoveFracturesInBox(ref aabb, 0f);

                CubeGrid.SyncObject.SendFractureComponentRepaired(this, toolOwnerId);
            }
        }

        private void RepairFracturedBlockInternal()
        {
            if (FatBlock.Components.Has<MyFractureComponentBase>())
            {
                FatBlock.Components.Remove<MyFractureComponentBase>();
                FatBlock.Render.UpdateRenderObject(false);
                FatBlock.CreateRenderer(FatBlock.Render.PersistentFlags, FatBlock.Render.ColorMaskHsv, FatBlock.Render.ModelStorage);
                UpdateVisual();
                FatBlock.Render.UpdateRenderObject(true);

                var existingBlock = CubeGrid.GetCubeBlock(Position);
                Debug.Assert(existingBlock != null);
                if (existingBlock != null)
                    existingBlock.CubeGrid.UpdateBlockNeighbours(existingBlock);
            }
        }
    }
}
