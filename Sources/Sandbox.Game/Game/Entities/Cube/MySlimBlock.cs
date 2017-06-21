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
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRageRender;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Sandbox.Engine.Models;
using VRage.Game.Entity;
using VRage.Game;
using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Network;
using VRage.Game.Models;
using VRage.Profiler;

namespace Sandbox.Game.Entities.Cube
{
    [StaticEventOwner]
    [MyCubeBlockType(typeof(MyObjectBuilder_CubeBlock))]
    public partial class MySlimBlock : IMyDestroyableObject, IMyDecalProxy
    {
        static List<VertexArealBoneIndexWeight> m_boneIndexWeightTmp;

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
        [ThreadStatic]
        static List<MySlimBlock> m_tmpMultiBlocksPerThread;

        static Dictionary<string, int> m_tmpComponents { get { return MyUtils.Init(ref m_tmpComponentsPerThread); } }
        static List<MyStockpileItem> m_tmpItemList { get { return MyUtils.Init(ref m_tmpItemListPerThread); } }
        static List<Vector3I> m_tmpCubeNeighbours { get { return MyUtils.Init(ref m_tmpCubeNeighboursPerThread); } }
        static List<MySlimBlock> m_tmpBlocks { get { return MyUtils.Init(ref m_tmpBlocksPerThread); } }
        static List<MySlimBlock> m_tmpMultiBlocks { get { return MyUtils.Init(ref m_tmpMultiBlocksPerThread); } }

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
        public float BlockGeneralDamageModifier = 1f;
		
		public Vector3D WorldPosition
        {
            get { return CubeGrid.GridIntegerToWorld(Position); }
        }

        public BoundingBoxD WorldAABB
        {
            get 
			{ 
				return new BoundingBoxD((Min * CubeGrid.GridSize) - CubeGrid.GridSizeHalfVector, 
										(Max * CubeGrid.GridSize) + CubeGrid.GridSizeHalfVector).TransformFast(CubeGrid.PositionComp.WorldMatrix); 
			}
        }
		
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

        private MyComponentStack m_componentStack;
        private MyObjectBuilder_CubeBlock m_objectBuilder;

        private MyConstructionStockpile m_stockpile = null;
        private float m_cachedMaxDeformation;

        private long m_builtByID;

        /// <summary>
        /// Neighbours which are connected by mount points
        /// </summary>
        public List<MySlimBlock> Neighbours = new List<MySlimBlock>();

        public bool IsFullIntegrity
        {
            get 
            {
                if (m_componentStack != null)
                {
                    return m_componentStack.IsFullIntegrity;
                }
                else
                {
                    return true;
                }
            }
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
                if (CubeGrid != null)
                {
                    return CubeGrid.Skeleton.IsDeformed(Position, 0.0f, CubeGrid, true);
                }
                else
                {
                    return false;
                }
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

        public long BuiltBy
        {
            get { return m_builtByID; }
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
        /// Multiblock unique identifier (all blocks in a multiblock have the same identifier). 0 means single block (default). Unique in one grid.
        /// </summary>
        public int MultiBlockId;
        /// <summary>
        /// Index of block in multiblock definition.
        /// </summary>
        public int MultiBlockIndex = -1;

        public bool IsMultiBlockPart
        {
            get { return MyFakes.ENABLE_MULTIBLOCK_PART_IDS && MultiBlockId != 0 && MultiBlockDefinition != null && MultiBlockIndex != -1; }
        }

        /// <summary>
        /// Cached count of all breakable shapes per model.
        /// </summary>
        private static readonly Dictionary<string, int> m_modelTotalFracturesCount = new Dictionary<string, int>();

        public bool ForceBlockDestructible { get { return FatBlock != null ? FatBlock.ForceBlockDestructible : false; } }

        public long OwnerId
        {
            get
            {
                if (this.FatBlock != null && FatBlock.OwnerId != 0) return FatBlock.OwnerId;
                MyGridOwnershipComponentBase ownershipComponent;
                CubeGrid.Components.TryGet(out ownershipComponent);
                if (ownershipComponent != null) return ownershipComponent.GetBlockOwnerId(this);
                else return 0;
            }
        }

        public MySlimBlock()
        {
            UniqueId = MyRandom.Instance.Next();
            UseDamageSystem = true;
        }

        public void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid, MyCubeBlock fatBlock)
        {
            ProfilerShort.Begin("SlimBlock.Init(objectBuilder, ...)");
            Debug.Assert(cubeGrid != null);
            FatBlock = fatBlock;

            if (objectBuilder is MyObjectBuilder_CompoundCubeBlock)
                BlockDefinition = MyCompoundCubeBlock.GetCompoundCubeBlockDefinition();
            else if (!MyDefinitionManager.Static.TryGetCubeBlockDefinition(objectBuilder.GetId(), out BlockDefinition))
                {
                    //BlockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "StoneCube"));
                    ProfilerShort.End();
                    return;
                }

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

            if (objectBuilder.MultiBlockId != 0 && objectBuilder.MultiBlockDefinition != null && objectBuilder.MultiBlockIndex != -1)
            {
                MultiBlockDefinition = MyDefinitionManager.Static.TryGetMultiBlockDefinition(objectBuilder.MultiBlockDefinition.Value);
                if (MultiBlockDefinition != null)
                {
                    MultiBlockId = objectBuilder.MultiBlockId;
                    MultiBlockIndex = objectBuilder.MultiBlockIndex;
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

            if (MyFakes.SHOW_DAMAGE_EFFECTS && FatBlock != null && BlockDefinition.RatioEnoughForDamageEffect(BuildIntegrity / MaxIntegrity) == false && BlockDefinition.RatioEnoughForDamageEffect(Integrity / MaxIntegrity))
            {//start effect
                if (CurrentDamage > 0.01f)//fix for weird simple blocks having FatBlock - old save?
                {
                    FatBlock.SetDamageEffect(true);
                }

            }

            UpdateMaxDeformation();

            m_builtByID = objectBuilder.BuiltBy;
            BlockGeneralDamageModifier = objectBuilder.BlockGeneralDamageModifier;

            ProfilerShort.End();
        }
        public void ResumeDamageEffect()
        {

            if (MyFakes.SHOW_DAMAGE_EFFECTS && FatBlock != null && BlockDefinition.RatioEnoughForDamageEffect(BuildIntegrity / MaxIntegrity) == false && BlockDefinition.RatioEnoughForDamageEffect(Integrity / MaxIntegrity))
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
            Debug.Assert(m_componentStack.MaxIntegrity != 0, "Invalid block maximum integrity");
            builder.IntegrityPercent = m_componentStack.Integrity / m_componentStack.MaxIntegrity;
            builder.BuildPercent = m_componentStack.BuildRatio;
            builder.ColorMaskHSV = ColorMaskHSV;
            builder.BuiltBy = m_builtByID;

            if (m_stockpile == null || m_stockpile.GetItems().Count == 0)
                builder.ConstructionStockpile = null;
            else
                builder.ConstructionStockpile = m_stockpile.GetObjectBuilder();

            if (IsMultiBlockPart)
            {
                builder.MultiBlockDefinition = MultiBlockDefinition.Id;
                builder.MultiBlockId = MultiBlockId;
                builder.MultiBlockIndex = MultiBlockIndex;
            }
            builder.BlockGeneralDamageModifier = BlockGeneralDamageModifier;

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
                CubeGrid.SendStockpileChanged(this, m_stockpile.GetSyncList());
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
                    CubeGrid.SendStockpileChanged(this, m_stockpile.GetSyncList());
            }
        }

        public void MoveItemsToConstructionStockpile(MyInventoryBase fromInventory)
        {
            if (MySession.Static.CreativeMode)
                return;

            m_tmpComponents.Clear();
            GetMissingComponents(m_tmpComponents);

            if (m_tmpComponents.Count != 0)
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
                CubeGrid.SendStockpileChanged(this, m_stockpile.GetSyncList());
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
                var amount = toInventory.ComputeAmountThatFits(item.Content.GetId()).ToIntSafe();
                amount = Math.Min(amount, item.Amount);
                toInventory.AddItems(amount, item.Content);
                m_stockpile.RemoveItems(amount, item.Content);
            }
            CubeGrid.SendStockpileChanged(this, m_stockpile.GetSyncList());
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
                var amount = toInventory.ComputeAmountThatFits(item.Content.GetId()).ToIntSafe();
                amount = Math.Min(amount, item.Amount);
                toInventory.AddItems(amount, item.Content);
                m_stockpile.RemoveItems(amount, item.Content);
            }
            CubeGrid.SendStockpileChanged(this, m_stockpile.GetSyncList());
            m_stockpile.ClearSyncList();
        }

        public void ClearConstructionStockpile(MyInventoryBase outputInventory)
        {
            if (!StockpileEmpty)
            {
                MyEntity inventoryOwner = null;
                if (outputInventory != null && outputInventory.Container != null)
                    inventoryOwner = outputInventory.Container.Entity as MyEntity;
                if (inventoryOwner != null && inventoryOwner.InventoryOwnerType() == MyInventoryOwnerTypeEnum.Character)
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
                    CubeGrid.SendStockpileChanged(this, m_stockpile.GetSyncList());
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
                if(item.Amount < 0.01f)
                {
                    continue;
                }

                var spawnedEntity = MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(item.Amount, item.Content), boundingBox, CubeGrid.Physics);
                if (spawnedEntity != null)
                {
                    spawnedEntity.Physics.ApplyImpulse(
                        MyUtils.GetRandomVector3Normalized() * spawnedEntity.Physics.Mass / 5.0f,
                        spawnedEntity.PositionComp.GetPosition());
                }
                m_stockpile.RemoveItems(item.Amount, item.Content);
            }
            CubeGrid.SendStockpileChanged(this, m_stockpile.GetSyncList());
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
                if (!MyFakes.ENABLE_GENERATED_BLOCKS || !BlockDefinition.IsGeneratedBlock)
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

        public bool CanContinueBuild(MyInventoryBase sourceInventory)
        {
            if (IsFullIntegrity || (sourceInventory == null && !MySession.Static.CreativeMode)) return false;

            if (FatBlock != null && !FatBlock.CanContinueBuild()) return false;

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

        public bool DoDamage(float damage, MyStringHash damageType, bool sync, MyHitInfo? hitInfo, long attackerId)
        {
            damage = damage * BlockGeneralDamageModifier * CubeGrid.GridGeneralDamageModifier;
            if (damage <= 0)
                return false;
            if (sync)
            {
                Debug.Assert(Sync.IsServer);
                if (Sync.IsServer)
                    DoDamageSynced(this, damage, damageType, hitInfo, attackerId);
            }
            else
                this.DoDamage(damage, damageType, hitInfo: hitInfo, attackerId: attackerId);
            return true;
        }

        public void DoDamage(float damage, MyStringHash damageType, MyHitInfo? hitInfo = null, bool addDirtyParts = true, long attackerId = 0)
        {
            if (!CubeGrid.BlocksDestructionEnabled && !ForceBlockDestructible)
                return;

            var compoundBlock = FatBlock as MyCompoundCubeBlock;
            if (compoundBlock != null) //jn: TODO think of something better
            {
                compoundBlock.DoDamage(damage, damageType, hitInfo, attackerId);
                return;
            }

            if (IsMultiBlockPart)
            {
                var multiBlockInfo = CubeGrid.GetMultiBlockInfo(MultiBlockId);
                Debug.Assert(multiBlockInfo != null);
                if (multiBlockInfo != null)
                {
                    Debug.Assert(m_tmpMultiBlocks.Count == 0);
                    m_tmpMultiBlocks.AddRange(multiBlockInfo.Blocks);

                    float totalMaxIntegrity = multiBlockInfo.GetTotalMaxIntegrity();
                    foreach (var multiBlockPart in m_tmpMultiBlocks)
                        multiBlockPart.DoDamageInternal(damage * (multiBlockPart.MaxIntegrity / totalMaxIntegrity), damageType, addDirtyParts: addDirtyParts, hitInfo: hitInfo, attackerId: attackerId);

                    m_tmpMultiBlocks.Clear();
                }
            }
            else
            {
                DoDamageInternal(damage, damageType, addDirtyParts: addDirtyParts, hitInfo: hitInfo, attackerId: attackerId);
            }
        }

        public void DoDamageInternal(float damage, MyStringHash damageType, bool addDirtyParts = true, MyHitInfo? hitInfo = null, long attackerId = 0)
        {
            if (!CubeGrid.BlocksDestructionEnabled && !ForceBlockDestructible)
                return;

            damage *= DamageRatio; // Low-integrity blocks get more damage
            if (MyPerGameSettings.Destruction || MyFakes.ENABLE_VR_BLOCK_DEFORMATION_RATIO)
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
                    ApplyAccumulatedDamage(addDirtyParts, attackerId: attackerId);
                }
                CubeGrid.RemoveFromDamageApplication(this);
            }
            else
            {
                if (MyFakes.SHOW_DAMAGE_EFFECTS && FatBlock != null && BlockDefinition.RatioEnoughForDamageEffect(BuildIntegrity / MaxIntegrity) == false && BlockDefinition.RatioEnoughForDamageEffect((Integrity - damage) / MaxIntegrity))
                    FatBlock.SetDamageEffect(true);
            }

            if (UseDamageSystem)
                MyDamageSystem.Static.RaiseAfterDamageApplied(this, damageInfo);

            m_lastDamage = damage;
            m_lastAttackerId = attackerId;
            m_lastDamageType = damageType;
        }

        void IMyDecalProxy.AddDecals(MyHitInfo hitInfo, MyStringHash source, object customdata, IMyDecalHandler decalHandler, MyStringHash material)
        {
            MyDecalRenderInfo renderable = new MyDecalRenderInfo();
            MyCubeGridHitInfo gridHitInfo = customdata as MyCubeGridHitInfo;
            if (gridHitInfo == null)
            {
                Debug.Fail("MyCubeGridHitInfo must not be null");
                return;
            }

            if (FatBlock == null)
            {
                renderable.Position = Vector3D.Transform(hitInfo.Position, CubeGrid.PositionComp.WorldMatrixInvScaled);
                renderable.Normal = Vector3D.TransformNormal(hitInfo.Normal, CubeGrid.PositionComp.WorldMatrixInvScaled);
                renderable.RenderObjectId = CubeGrid.Render.GetRenderObjectID();
            }
            else
            {
                renderable.Position = Vector3D.Transform(hitInfo.Position, FatBlock.PositionComp.WorldMatrixInvScaled);
                renderable.Normal = Vector3D.TransformNormal(hitInfo.Normal, FatBlock.PositionComp.WorldMatrixInvScaled);
                renderable.RenderObjectId = FatBlock.Render.GetRenderObjectID();
            }

            if (material.GetHashCode() == 0)
                renderable.Material = MyStringHash.GetOrCompute(BlockDefinition.PhysicalMaterial.Id.SubtypeName);
            else
                renderable.Material = material;

            VertexBoneIndicesWeights? boneIndicesWeights = gridHitInfo.Triangle.GetAffectingBoneIndicesWeights(ref m_boneIndexWeightTmp);
            if (boneIndicesWeights.HasValue)
            {
                renderable.BoneIndices = boneIndicesWeights.Value.Indices;
                renderable.BoneWeights = boneIndicesWeights.Value.Weights;
            }

            var decalId = decalHandler.AddDecal(ref renderable);
            if (decalId != null)
                CubeGrid.RenderData.AddDecal(Position, gridHitInfo, decalId.Value);
        }

        /// <summary>
        /// Destruction does not apply any damage to block (instead triggers destruction) so it is applied through this method 
        /// when fracture component is created or when any of internal fratures is removed from component.
        /// </summary>
        public void ApplyDestructionDamage(float integrityRatioFromFracturedPieces)
        {
            if (MyFakes.ENABLE_FRACTURE_COMPONENT && Sync.IsServer && MyPerGameSettings.Destruction)
            {
                float damage = (ComponentStack.IntegrityRatio - integrityRatioFromFracturedPieces) * BlockDefinition.MaxIntegrity;
                if (CanApplyDestructionDamage(damage))
                    ((IMyDestroyableObject)this).DoDamage(damage, MyDamageType.Destruction, true);
                else if (CanApplyDestructionDamage(MyDefinitionManager.Static.DestructionDefinition.DestructionDamage))
                    ((IMyDestroyableObject)this).DoDamage(MyDefinitionManager.Static.DestructionDefinition.DestructionDamage, MyDamageType.Destruction, true);
            }
        }

        private bool CanApplyDestructionDamage(float damage)
        {
            Debug.Assert(MyPerGameSettings.Destruction);

            if (damage <= 0f)
                return false;

            if (IsMultiBlockPart)
            {
                var multiBlockInfo = CubeGrid.GetMultiBlockInfo(MultiBlockId);
                Debug.Assert(multiBlockInfo != null);
                if (multiBlockInfo != null)
                {
                    // Check damage
                    float totalMaxIntegrity = multiBlockInfo.GetTotalMaxIntegrity();
                    foreach (var multiBlockPart in multiBlockInfo.Blocks)
                    {
                        float defaultDamage = damage * multiBlockPart.MaxIntegrity / totalMaxIntegrity;
                        defaultDamage *= multiBlockPart.DamageRatio;
                        defaultDamage *= multiBlockPart.DeformationRatio;
                        defaultDamage += multiBlockPart.AccumulatedDamage; // Also include accumulated damage
                        if (multiBlockPart.Integrity - defaultDamage <= MyComponentStack.MOUNT_THRESHOLD)
                            return false;
                    }

                    return true;
                }

                return false;
            }
            else
            {
                damage *= DamageRatio;
                damage *= DeformationRatio;
                damage += AccumulatedDamage; // Also include accumulated damage
                return (Integrity - damage > MyComponentStack.MOUNT_THRESHOLD);
            }
        }

        internal int GetTotalBreakableShapeChildrenCount()
        {
            if (FatBlock == null)
                return 0;

            var model = FatBlock.Model.AssetName;

            int totalFracturesCountForModel = 0;
            if (m_modelTotalFracturesCount.TryGetValue(model, out totalFracturesCountForModel))
            {
                return totalFracturesCountForModel;
            }
            else
            {
                var modelData = VRage.Game.Models.MyModels.GetModelOnlyData(model);
                if (modelData.HavokBreakableShapes == null)
                    MyDestructionData.Static.LoadModelDestruction(model, BlockDefinition, Vector3.One);

                var shape = modelData.HavokBreakableShapes[0];
                int count = shape.GetTotalChildrenCount();
                m_modelTotalFracturesCount.Add(model, count);
                return count;
            }
        }

        /// <summary>
        /// Do not call explicitly. Will be done automatically by the grid.
        /// </summary>
        public void ApplyAccumulatedDamage(bool addDirtyParts = true, long attackerId = 0)
        {
            Debug.Assert(AccumulatedDamage > 0f, "No damage done that could be applied to the block.");
            ProfilerShort.Begin("MySlimBlock.ApplyAccumulatedDamage");

            if (MySession.Static.SurvivalMode)
            {
                EnsureConstructionStockpileExists();
            }

            float predmgIntegrity = Integrity;
            if (m_stockpile != null)
            {
                m_stockpile.ClearSyncList();
                m_componentStack.ApplyDamage(AccumulatedDamage, m_stockpile);

                if (Sync.IsServer)
                    CubeGrid.SendStockpileChanged(this, m_stockpile.GetSyncList());

                m_stockpile.ClearSyncList();
            }
            else
            {
                m_componentStack.ApplyDamage(AccumulatedDamage, null);
            }

            //by Gregory: BuildRatio is not updated for this!!! For now check this way TODO
            // AB: we need to call it only when red line is crossed and only once
            if (!BlockDefinition.RatioEnoughForDamageEffect(predmgIntegrity / MaxIntegrity) &&
                BlockDefinition.RatioEnoughForDamageEffect((Integrity) / MaxIntegrity))
            {
                if (FatBlock != null)
                {
                    FatBlock.OnIntegrityChanged(BuildIntegrity, Integrity, false, MySession.Static.LocalPlayerId);
                }
            }

            AccumulatedDamage = 0.0f;
            //CubeGrid.SyncObject.SendIntegrityChanged(this, MyCubeGrid.MyIntegrityChangeEnum.Damage, 0);

            if (m_componentStack.IsDestroyed)
            {
                if (MyFakes.SHOW_DAMAGE_EFFECTS && FatBlock != null)
                    FatBlock.SetDamageEffect(false);
                CubeGrid.RemoveDestroyedBlock(this, attackerId: attackerId);
                if (addDirtyParts)
                {
                    CubeGrid.Physics.AddDirtyBlock(this);
                }

                if (UseDamageSystem)
                    MyDamageSystem.Static.RaiseDestroyed(this, new MyDamageInformation(false, m_lastDamage, m_lastDamageType, m_lastAttackerId));
            }

            ProfilerShort.End();
        }

        public void UpdateVisual(bool updatePhysics = true)
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
            }
            CubeGrid.SetBlockDirty(this);
            if (updatePhysics && CubeGrid.Physics != null)
            {
                CubeGrid.Physics.AddDirtyArea(Min, Max);
            }
        }

        public void IncreaseMountLevelToDesiredRatio(float desiredIntegrityRatio, long welderOwnerPlayerId, MyInventoryBase outputInventory = null, float maxAllowedBoneMovement = 0.0f, bool isHelping = false, MyOwnershipShareModeEnum sharing = MyOwnershipShareModeEnum.Faction)
        {
            float desiredIntegrity = desiredIntegrityRatio * MaxIntegrity;
            float welderAmount = desiredIntegrity - Integrity;
            Debug.Assert(welderAmount >= 0f);
            if (welderAmount <= 0f)
                return;

            IncreaseMountLevel(welderAmount / BlockDefinition.IntegrityPointsPerSec, welderOwnerPlayerId, outputInventory: outputInventory, maxAllowedBoneMovement: maxAllowedBoneMovement,
                isHelping: isHelping, sharing: sharing);
        }

        public void DecreaseMountLevelToDesiredRatio(float desiredIntegrityRatio, MyInventoryBase outputInventory)
        {
            float desiredIntegrity = desiredIntegrityRatio * MaxIntegrity;
            float grinderAmount = Integrity - desiredIntegrity;
            Debug.Assert(grinderAmount >= 0f);
            if (grinderAmount <= 0f)
                return;

            if (FatBlock != null)
                grinderAmount *= FatBlock.DisassembleRatio;
            else
                grinderAmount *= BlockDefinition.DisassembleRatio;

            DecreaseMountLevel(grinderAmount / BlockDefinition.IntegrityPointsPerSec, outputInventory, useDefaultDeconstructEfficiency: true);
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
                MyEntity inventoryOwner = null;
                if (outputInventory != null && outputInventory.Container != null)
                    inventoryOwner = outputInventory.Container.Entity as MyEntity;
                if (inventoryOwner != null && inventoryOwner.InventoryOwnerType() == MyInventoryOwnerTypeEnum.Character)
                {
                    MoveItemsFromConstructionStockpile(outputInventory, MyItemFlags.Damaged);
                }
            }

            float oldPercentage = m_componentStack.BuildRatio;
            float oldDamage = CurrentDamage;


            //Add ownership check in order for the IntegrityChanged not to be called many times
            if (BlockDefinition.RatioEnoughForOwnership(BuildLevelRatio))
            {
                if (FatBlock != null && FatBlock.OwnerId == 0 && outputInventory != null && !isHelping)
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
                CubeGrid.SendStockpileChanged(this, m_stockpile.GetSyncList());
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
            MyIntegrityChangeEnum integrityChangeType = MyIntegrityChangeEnum.Damage;
            if (BlockDefinition.ModelChangeIsNeeded(oldPercentage, m_componentStack.BuildRatio) || BlockDefinition.ModelChangeIsNeeded(m_componentStack.BuildRatio, oldPercentage))
            {
                removeDecals = true;
                if (FatBlock != null)
                {
                    // this needs to be detected here because for cubes the following call to UpdateVisual() set FatBlock to null when the construction is complete
                    if (m_componentStack.IsFunctional)
                    {
                        integrityChangeType = MyIntegrityChangeEnum.ConstructionEnd;
                    }
                }

                UpdateVisual();
                if (FatBlock != null)
                {
                    int buildProgressID = CalculateCurrentModelID();
                    if (buildProgressID == 0)
                    {
                        integrityChangeType = MyIntegrityChangeEnum.ConstructionBegin;
                    }
                    else if (!m_componentStack.IsFunctional)
                    {
                        integrityChangeType = MyIntegrityChangeEnum.ConstructionProcess;
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

            CubeGrid.SendIntegrityChanged(this, integrityChangeType, 0);
            CubeGrid.OnIntegrityChanged(this);

            if (maxAllowedBoneMovement != 0.0f)
                FixBones(oldDamage, maxAllowedBoneMovement);

            if (MyFakes.ENABLE_GENERATED_BLOCKS && !BlockDefinition.IsGeneratedBlock && BlockDefinition.GeneratedBlockDefinitions != null && BlockDefinition.GeneratedBlockDefinitions.Length > 0)
            {
                UpdateProgressGeneratedBlocks(oldPercentage);
            }

            ProfilerShort.End();
        }

        public void DecreaseMountLevel(float grinderAmount, MyInventoryBase outputInventory, bool useDefaultDeconstructEfficiency = false)
        {
            Debug.Assert(Sync.IsServer, "This method is only meant to be called on the server!");
            if (!Sync.IsServer||m_componentStack.IsFullyDismounted)
                return;

            if (FatBlock != null)
                grinderAmount /= FatBlock.DisassembleRatio;
            else
                grinderAmount /= BlockDefinition.DisassembleRatio;

            grinderAmount = grinderAmount * BlockDefinition.IntegrityPointsPerSec;
            float oldBuildRatio = m_componentStack.BuildRatio;
            DeconstructStockpile(grinderAmount, outputInventory, useDefaultDeconstructEfficiency: useDefaultDeconstructEfficiency);

            float newBuildRatio = (BuildIntegrity - grinderAmount) / BlockDefinition.MaxIntegrity;

            //Call Integrity Changed if owner is nobody or is not local player
            if (BlockDefinition.RatioEnoughForDamageEffect(BuildLevelRatio))
            {
                if (FatBlock != null && FatBlock.OwnerId != 0 && FatBlock.OwnerId != MySession.Static.LocalPlayerId)
                {
                    FatBlock.OnIntegrityChanged(BuildIntegrity, Integrity, false, MySession.Static.LocalPlayerId);
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

            bool modelChangeNeeded = BlockDefinition.ModelChangeIsNeeded(m_componentStack.BuildRatio, oldBuildRatio);

            MyIntegrityChangeEnum integrityChangeType = MyIntegrityChangeEnum.Damage;
            if (modelChangeNeeded)
            {
                UpdateVisual();

                if (FatBlock != null)
                {
                    int buildProgressID = CalculateCurrentModelID();
                    if ((buildProgressID == -1) || (BuildLevelRatio == 0f))
                    {
                        integrityChangeType = MyIntegrityChangeEnum.ConstructionEnd;
                    }
                    else if (buildProgressID == BlockDefinition.BuildProgressModels.Length - 1)
                    {
                        integrityChangeType = MyIntegrityChangeEnum.ConstructionBegin;
                    }
                    else
                    {
                        integrityChangeType = MyIntegrityChangeEnum.ConstructionProcess;
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

            CubeGrid.SendIntegrityChanged(this, integrityChangeType, toolOwner);
            CubeGrid.OnIntegrityChanged(this);
        }

        /// <summary>
        /// Completely deconstruct this block
        /// Intended for server-side use
        /// </summary>
        public void FullyDismount(MyInventory outputInventory)
        {
            Debug.Assert(Sync.IsServer, "This method is only meant to be called on the server!");
            if (!Sync.IsServer)
                return;

            DeconstructStockpile(BuildIntegrity, outputInventory);

            float oldBuildRatio = m_componentStack.BuildRatio;
            bool modelChangeNeeded = BlockDefinition.ModelChangeIsNeeded(m_componentStack.BuildRatio, oldBuildRatio);
            if (modelChangeNeeded)
            {
                UpdateVisual();
                PlayConstructionSound(MyIntegrityChangeEnum.ConstructionEnd, true);
                CreateConstructionSmokes();
            }

            if (CubeGrid.GridSystems.GasSystem != null)
                CubeGrid.GridSystems.GasSystem.Pressurize();
        }

        private void DeconstructStockpile(float deconstructAmount, MyInventoryBase outputInventory, bool useDefaultDeconstructEfficiency = false)
        {
            Debug.Assert(Sync.IsServer, "This method is only meant to be called on the server!");
            if (MySession.Static.CreativeMode)
            {
                ClearConstructionStockpile(outputInventory);
            }
            else
            {
                EnsureConstructionStockpileExists();
            }

            if (m_stockpile != null)
            {
                m_stockpile.ClearSyncList();
                m_componentStack.DecreaseMountLevel(deconstructAmount, m_stockpile, useDefaultDeconstructEfficiency: useDefaultDeconstructEfficiency);
                CubeGrid.SendStockpileChanged(this, m_stockpile.GetSyncList());
                m_stockpile.ClearSyncList();
            }
            else
            {
                m_componentStack.DecreaseMountLevel(deconstructAmount, null, useDefaultDeconstructEfficiency: useDefaultDeconstructEfficiency);
            }
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
                        smokeEffect.WorldMatrix = MatrixD.CreateTranslation(tr);
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

        public void SetIntegrity(float buildIntegrity, float integrity, MyIntegrityChangeEnum integrityChangeType, long grinderOwner)
        {
            float oldRatio = m_componentStack.BuildRatio;
            m_componentStack.SetIntegrity(buildIntegrity, integrity);

            if (FatBlock != null && !BlockDefinition.RatioEnoughForOwnership(oldRatio) && BlockDefinition.RatioEnoughForOwnership(m_componentStack.BuildRatio))
                FatBlock.OnIntegrityChanged(buildIntegrity, integrity, true, MySession.Static.LocalPlayerId);

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

                if (integrityChangeType != MyIntegrityChangeEnum.Damage)
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

                if (relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies || relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral)
                    FatBlock.HackAttemptTime = MySandboxGame.TotalTimeInMilliseconds;
            }
        }


        public void PlayConstructionSound(MyIntegrityChangeEnum integrityChangeType, bool deconstruction = false)
        {
            MyEntity3DSoundEmitter emitter = MyAudioComponent.TryGetSoundEmitter();
            if (emitter == null)
                return;
            if(FatBlock != null)
                emitter.SetPosition(FatBlock.PositionComp.GetPosition());
            else
                emitter.SetPosition(CubeGrid.PositionComp.GetPosition() + (Position - 1) * CubeGrid.GridSize);
            switch (integrityChangeType)
            {
                case MyIntegrityChangeEnum.ConstructionBegin:
                    if (deconstruction)
                        emitter.PlaySound(DECONSTRUCTION_START, true, alwaysHearOnRealistic: true);
                    else
                        emitter.PlaySound(CONSTRUCTION_START, true, alwaysHearOnRealistic: true);
                    break;

                case MyIntegrityChangeEnum.ConstructionEnd:
                    if (deconstruction)
                        emitter.PlaySound(DECONSTRUCTION_END, true, alwaysHearOnRealistic: true);
                    else
                        emitter.PlaySound(CONSTRUCTION_END, true, alwaysHearOnRealistic: true);
                    break;

                case MyIntegrityChangeEnum.ConstructionProcess:
                    if (deconstruction)
                        emitter.PlaySound(DECONSTRUCTION_PROG, true, alwaysHearOnRealistic: true);
                    else
                        emitter.PlaySound(CONSTRUCTION_PROG, true, alwaysHearOnRealistic: true);
                    break;

                default:
                    emitter.PlaySound(MySoundPair.Empty);
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
            scaledCenter = (Max + Min) * CubeGrid.GridSizeHalf;
        }

        public void ComputeScaledHalfExtents(out Vector3 scaledHalfExtents)
        {
            scaledHalfExtents = ((Max + 1)- Min) * CubeGrid.GridSizeHalf;
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
            if (FatBlock != null)
            {
                ProfilerShort.Begin("MySlimBlock.OnDestroy");
                FatBlock.OnDestroy();
                ProfilerShort.End();
            }
            m_componentStack.DestroyCompletely();
            ReleaseUnneededStockpileItems();
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
                aabb = aabb.TransformFast(CubeGrid.WorldMatrix);
            }
        }

        // CH: TODO: Put these into the MyHudBlockInfo, when refactoring it
        public static void SetBlockComponents(MyHudBlockInfo hudInfo, MySlimBlock block, MyInventoryBase availableInventory = null)
        {
            SetBlockComponentsInternal(hudInfo, block.BlockDefinition, block, availableInventory);
        }

        public static void SetBlockComponents(MyHudBlockInfo hudInfo, MyCubeBlockDefinition blockDefinition, MyInventoryBase availableInventory = null)
        {
            SetBlockComponentsInternal(hudInfo, blockDefinition, null, availableInventory);
        }

        // CH: TODO: This method actually doesn't have a bad internal structure, but it should be refactored BIG TIME (and put to MyHudBlockInfo)!
        private static void SetBlockComponentsInternal(MyHudBlockInfo hudInfo, MyCubeBlockDefinition blockDefinition, MySlimBlock block, MyInventoryBase availableInventory)
        {
            hudInfo.Components.Clear();

            if (block != null)
            {
                Debug.Assert(block.BlockDefinition == blockDefinition, "The definition given to SetBlockComponnentsInternal was not a definition of the block");
            }

            hudInfo.InitBlockInfo(blockDefinition);
            hudInfo.ShowAvailable = MyPerGameSettings.AlwaysShowAvailableBlocksOnHud;

            if (!MyFakes.ENABLE_SMALL_GRID_BLOCK_COMPONENT_INFO && blockDefinition.CubeSize == MyCubeSize.Small) return;

            if (block != null)
            {
                hudInfo.BlockIntegrity = block.Integrity / block.MaxIntegrity;
            }

            // CH: TODO: Multiblocks
            if (block != null && block.IsMultiBlockPart)
            {
                var multiBlockInfo = block.CubeGrid.GetMultiBlockInfo(block.MultiBlockId);
                Debug.Assert(multiBlockInfo != null);
                if (multiBlockInfo != null)
                {
                    // Load all block definition components
                    foreach (var blockDefId in multiBlockInfo.MultiBlockDefinition.BlockDefinitions) 
                    {
                        MyCubeBlockDefinition blockDef;
                        if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(blockDefId.Id, out blockDef))
                        {
                            hudInfo.AddComponentsForBlock(blockDef);
                        }
                    }

                    // Merge components from all blocks
                    hudInfo.MergeSameComponents();

                    // Add mounted counts to components
                    foreach (var multiBlockPart in multiBlockInfo.Blocks)
                    {
                        for (int j = 0; j < multiBlockPart.BlockDefinition.Components.Length; ++j)
                        {
                            var comp = multiBlockPart.BlockDefinition.Components[j];
                            var groupInfo = multiBlockPart.ComponentStack.GetGroupInfo(j);

                            for (int i = 0; i < hudInfo.Components.Count; i++)
                            {
                                if (hudInfo.Components[i].DefinitionId == comp.Definition.Id)
                                {
                                    var c = hudInfo.Components[i];
                                    c.MountedCount += groupInfo.MountedCount;
                                    hudInfo.Components[i] = c;
                                    break;
                                }
                            }
                        }
                    }

                    // Inventory counts
                    for (int i = 0; i < hudInfo.Components.Count; i++)
                    {
                        if (availableInventory != null)
                        {
                            var c = hudInfo.Components[i];
                            c.AvailableAmount = (int)MyCubeBuilder.BuildComponent.GetItemAmountCombined(availableInventory, c.DefinitionId);
                            hudInfo.Components[i] = c;
                        }

                        // Get amount in stockpile
                        int amount = 0;
                        foreach (var multiBlockPart in multiBlockInfo.Blocks)
                        {
                            if (!multiBlockPart.StockpileEmpty)
                                amount += multiBlockPart.GetConstructionStockpileItemAmount(hudInfo.Components[i].DefinitionId);
                        }

                        if (amount > 0)
                        {
                            //RKTODO: ??? see below code for non multiblocks
                            /*amount =*/ SetHudInfoComponentAmount(hudInfo, amount, i);
                        }
                    }
                }
            }
            else if (block == null && blockDefinition.MultiBlock != null)
            {
                MyDefinitionId defId = new MyDefinitionId(typeof(MyObjectBuilder_MultiBlockDefinition), blockDefinition.MultiBlock);
                var mbDefinition = MyDefinitionManager.Static.TryGetMultiBlockDefinition(defId);
                if (mbDefinition != null)
                {
                    foreach (var blockDefId in mbDefinition.BlockDefinitions)
                    {
                        MyCubeBlockDefinition blockDef;
                        if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(blockDefId.Id, out blockDef))
                        {
                            hudInfo.AddComponentsForBlock(blockDef);
                        }
                    }

                    // Merge components from all blocks
                    hudInfo.MergeSameComponents();

                    for (int i = 0; i < hudInfo.Components.Count; ++i)
                    {
                        var component = hudInfo.Components[i];
                        component.AvailableAmount = (int)MyCubeBuilder.BuildComponent.GetItemAmountCombined(availableInventory, component.DefinitionId);
                        hudInfo.Components[i] = component;
                    }
                }
            }
            else
            {
                for (int i = 0; i < blockDefinition.Components.Length; i++)
                {
                    MyComponentStack.GroupInfo groupInfo = new MyComponentStack.GroupInfo();
                    if (block != null)
                    {
                        groupInfo = block.ComponentStack.GetGroupInfo(i);
                    }
                    else
                    {
                        var component = blockDefinition.Components[i];
                        groupInfo.Component = component.Definition;
                        groupInfo.TotalCount = component.Count;
                        groupInfo.MountedCount = 0;
                        groupInfo.AvailableCount = 0;
                        groupInfo.Integrity = 0.0f;
                        groupInfo.MaxIntegrity = component.Count * component.Definition.MaxIntegrity;
                    }
                    AddBlockComponent(hudInfo, groupInfo, availableInventory);
                }

                if (block != null && !block.StockpileEmpty)
                {
                    // For each component
                    foreach (var comp in block.BlockDefinition.Components)
                    {
                        // Get amount in stockpile
                        int amount = block.GetConstructionStockpileItemAmount(comp.Definition.Id);

                        if (amount > 0)
                        {
                            for (int i = 0; i < hudInfo.Components.Count; i++)
                            {
                                if (block.ComponentStack.GetGroupInfo(i).Component == comp.Definition)
                                {
                                    if (block.ComponentStack.IsFullyDismounted)
                                    {
                                        return;
                                    }

                                    amount = SetHudInfoComponentAmount(hudInfo, amount, i);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static int SetHudInfoComponentAmount(MyHudBlockInfo hudInfo, int amount, int i)
        {
            // Distribute amount in stockpile from bottom to top
            var info = hudInfo.Components[i];
            int space = info.TotalCount - info.MountedCount;
            int movedItems = Math.Min(space, amount);
            info.StockpileCount = movedItems;
            amount -= movedItems;
            hudInfo.Components[i] = info;
            return amount;
        }

        private static void AddBlockComponent(MyHudBlockInfo hudInfo, MyComponentStack.GroupInfo groupInfo, MyInventoryBase availableInventory)
        {
            var componentInfo = new MyHudBlockInfo.ComponentInfo();
            componentInfo.DefinitionId = groupInfo.Component.Id;
            componentInfo.ComponentName = groupInfo.Component.DisplayNameText;
            componentInfo.Icons = groupInfo.Component.Icons;
            componentInfo.TotalCount = groupInfo.TotalCount;
            componentInfo.MountedCount = groupInfo.MountedCount;
            if (availableInventory != null)
                componentInfo.AvailableAmount = (int)MyCubeBuilder.BuildComponent.GetItemAmountCombined(availableInventory, groupInfo.Component.Id);

            hudInfo.Components.Add(componentInfo);
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
                    Debug.Assert(m_tmpBlocks.Count == 0);
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

        /// <summary>
        /// Repairs multiblock - adds missing blocks and repairs existing ones.
        /// </summary>
        /// <param name="toolOwnerId"></param>
        /// <param name="repairExistingBlocks"></param>
        private void RepairMultiBlock(long toolOwnerId)
        {
            Debug.Assert(Sync.IsServer);

            var multiBlockInfo = CubeGrid.GetMultiBlockInfo(MultiBlockId);
            Debug.Assert(multiBlockInfo != null);
            if (multiBlockInfo == null)
                return;

            bool isFractured = multiBlockInfo.IsFractured();
            if (!isFractured)
                return;

            // Repair
            Debug.Assert(m_tmpMultiBlocks.Count == 0);
            m_tmpMultiBlocks.AddRange(multiBlockInfo.Blocks);

            foreach (var multiBlockPart in m_tmpMultiBlocks)
            {
                if (multiBlockPart.GetFractureComponent() == null)
                    continue;

                multiBlockPart.RepairFracturedBlock(toolOwnerId);
            }

            m_tmpMultiBlocks.Clear();
        }

        public void RepairFracturedBlockWithFullHealth(long toolOwnerId)
        {
            Debug.Assert(Sync.IsServer);

            if (BlockDefinition.IsGeneratedBlock)
                return;

            if (MyFakes.ENABLE_MULTIBLOCK_CONSTRUCTION && IsMultiBlockPart)
            {
                RepairMultiBlock(toolOwnerId);
                if (!MySession.Static.SurvivalMode)
                    CubeGrid.AddMissingBlocksInMultiBlock(MultiBlockId, toolOwnerId);
                return;
            }

            var fractureComponent = GetFractureComponent();
            Debug.Assert(fractureComponent != null);
            if (fractureComponent == null)
                return;

            RepairFracturedBlock(toolOwnerId);
        }

        internal void RepairFracturedBlock(long toolOwnerId)
        {
            Debug.Assert(FatBlock != null);
            if (FatBlock == null)
                return;

            Debug.Assert(FatBlock.Components.Has<MyFractureComponentBase>());

            RemoveFractureComponent();

            Debug.Assert(m_tmpBlocks.Count == 0);
            CubeGrid.GetGeneratedBlocks(this, m_tmpBlocks);
            foreach (var genBlock in m_tmpBlocks)
            {
                genBlock.RemoveFractureComponent();
                genBlock.SetGeneratedBlockIntegrity(this);
            }

            m_tmpBlocks.Clear();

            // Re-create destroyed generated blocks
            UpdateProgressGeneratedBlocks(0f);

            if (Sync.IsServer)
            {
                var aabb = FatBlock.PositionComp.WorldAABB;
                if (BlockDefinition.CubeSize == MyCubeSize.Large)
                    aabb.Inflate(-0.16);
                else
                    aabb.Inflate(-0.04);
                MyFracturedPiecesManager.Static.RemoveFracturesInBox(ref aabb, 0f);

                CubeGrid.SendFractureComponentRepaired(this, toolOwnerId);
            }
        }

        internal void RemoveFractureComponent()
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

        public void SetGeneratedBlockIntegrity(MySlimBlock generatingBlock)
        {
            Debug.Assert(BlockDefinition.IsGeneratedBlock);
            if (!BlockDefinition.IsGeneratedBlock)
                return;

            float oldRatio = ComponentStack.BuildRatio;
            ComponentStack.SetIntegrity(generatingBlock.BuildLevelRatio * MaxIntegrity,
                generatingBlock.ComponentStack.IntegrityRatio * MaxIntegrity);

            if (ModelChangeIsNeeded(ComponentStack.BuildRatio, oldRatio))
                UpdateVisual();
        }

        public void GetLocalMatrix(out Matrix localMatrix)
        {
            Orientation.GetMatrix(out localMatrix);
            localMatrix.Translation = ((Min + Max) * 0.5f) * CubeGrid.GridSize;

            Vector3 modelOffset;
            Vector3.TransformNormal(ref BlockDefinition.ModelOffset, ref localMatrix, out modelOffset);
            localMatrix.Translation += modelOffset;
        }

        static void DoDamageSynced(MySlimBlock block, float damage, MyStringHash damageType, MyHitInfo? hitInfo, long attackerId)
        {
            var msg = new DoDamageSlimBlockMsg();
            msg.GridEntityId = block.CubeGrid.EntityId;
            msg.Position = block.Position;
            msg.Damage = damage;
            msg.HitInfo = hitInfo;
            msg.AttackerEntityId = attackerId;
            msg.CompoundBlockId = 0xFFFFFFFF;

            // Get compound block id
            var blockOnPosition = block.CubeGrid.GetCubeBlock(block.Position);
            if (blockOnPosition != null && block != blockOnPosition && blockOnPosition.FatBlock is MyCompoundCubeBlock)
            {
                MyCompoundCubeBlock compound = blockOnPosition.FatBlock as MyCompoundCubeBlock;
                ushort? compoundBlockId = compound.GetBlockId(block);
                if (compoundBlockId != null)
                    msg.CompoundBlockId = compoundBlockId.Value;
            }

            block.DoDamage(damage, damageType, hitInfo: hitInfo, attackerId: attackerId);
#if !XB1_NOMULTIPLAYER
            MyMultiplayer.RaiseStaticEvent(s => MySlimBlock.DoDamageSlimBlock, msg);
#endif // !XB1_NOMULTIPLAYER
        }

        [Event, Reliable, Broadcast]
        static void DoDamageSlimBlock(DoDamageSlimBlockMsg msg)
        {
            Debug.Assert(!Sync.IsServer);

            MyCubeGrid grid;
            if (!MyEntities.TryGetEntityById<MyCubeGrid>(msg.GridEntityId, out grid))
                return;

            var block = grid.GetCubeBlock(msg.Position);
            if (block == null)
                return;

            if (msg.CompoundBlockId != 0xFFFFFFFF && block.FatBlock is MyCompoundCubeBlock)
            {
                var compound = block.FatBlock as MyCompoundCubeBlock;
                var blockInCompound = compound.GetBlock((ushort)msg.CompoundBlockId);
                if (blockInCompound != null)
                    block = blockInCompound;
            }

            block.DoDamage(msg.Damage, msg.Type, hitInfo: msg.HitInfo, attackerId: msg.AttackerEntityId);
        }

        /// <summary>
        /// Makes sure this block no longer counts towards the block limit of the player who built it
        /// </summary>
        public void RemoveAuthorship()
        {
            var identity = MySession.Static.Players.TryGetIdentity(m_builtByID);
            if (identity != null)
                identity.DecreaseBlocksBuilt(BlockDefinition.BlockPairName, CubeGrid);
        }

        /// <summary>
        /// Makes the block count towards the block limit of the player who built it
        /// </summary>
        public void AddAuthorship()
        {
            var identity = MySession.Static.Players.TryGetIdentity(m_builtByID);
            if (identity != null)
                identity.IncreaseBlocksBuilt(BlockDefinition.BlockPairName, CubeGrid);
        }

        /// <summary>
        /// Transfers the block to count towards other player's limit
        /// </summary>
        public void TransferAuthorship(long newOwner)
        {
            var oldIdentity = MySession.Static.Players.TryGetIdentity(m_builtByID);
            var newIdentity = MySession.Static.Players.TryGetIdentity(newOwner);
            if (oldIdentity != null && newIdentity != null)
            {
                oldIdentity.DecreaseBlocksBuilt(BlockDefinition.BlockPairName, CubeGrid);
                m_builtByID = newOwner;
                newIdentity.IncreaseBlocksBuilt(BlockDefinition.BlockPairName, CubeGrid);
            }
        }

        [ProtoContract]
        struct DoDamageSlimBlockMsg
        {
            [ProtoMember]
            public long GridEntityId;

            [ProtoMember]
            public Vector3I Position;

            [ProtoMember]
            public float Damage;

            [ProtoMember]
            public MyStringHash Type;

            [ProtoMember]
            public MyHitInfo? HitInfo;

            [ProtoMember]
            public long AttackerEntityId;

            [ProtoMember]
            public uint CompoundBlockId;
        }
    }
}
