#region Using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;

using VRageMath;
using Sandbox.Game.World;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using System.Diagnostics;
using Sandbox.Engine.Physics;
using System.Linq;
using VRage.Import;
using Sandbox.Common;
using Sandbox.Game.Multiplayer;
using VRage;
using Sandbox.Game.Components;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game.Entity.UseObject;
using Sandbox.Game.EntityComponents;
using Sandbox.Engine.Multiplayer;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.Models;

using VRage.Game.ModAPI;
using Sandbox.Game.ParticleEffects;
using VRage.Game.Entity.EntityComponents;
using VRageRender.Import;

#endregion

namespace Sandbox.Game.Entities
{
    public partial class MyCubeBlock : MyEntity, IMyComponentOwner<MyIDModule>
    {
        protected static readonly string DUMMY_SUBBLOCK_ID = "subblock_";

        private static List<MyCubeBlockDefinition.MountPoint> m_tmpMountPoints = new List<MyCubeBlockDefinition.MountPoint>();
        private static List<MyCubeBlockDefinition.MountPoint> m_tmpBlockMountPoints = new List<MyCubeBlockDefinition.MountPoint>();
        private static List<MyCubeBlockDefinition.MountPoint> m_tmpOtherBlockMountPoints = new List<MyCubeBlockDefinition.MountPoint>();

        private class MethodDataIsConnectedTo
        {
            public List<MyCubeBlockDefinition.MountPoint> MyMountPoints = new List<MyCubeBlockDefinition.MountPoint>();
            public List<MyCubeBlockDefinition.MountPoint> OtherMountPoints = new List<MyCubeBlockDefinition.MountPoint>();

            public void Clear()
            {
                MyMountPoints.Clear();
                OtherMountPoints.Clear();
            }
        }

        public new MyPhysicsBody Physics
        {
            get { return base.Physics as MyPhysicsBody; }
            set { base.Physics = value; }
        }

        public long OwnerId
        {
            get
            {
                return IDModule == null ? 0 : IDModule.Owner;
            }
        }

        public long BuiltBy
        {
            get { return SlimBlock == null ? 0 : SlimBlock.BuiltBy; }
        }

        private MyResourceSinkComponent m_sinkComp;

        public MyResourceSinkComponent ResourceSink
        {
            get { return m_sinkComp; }
            protected set { if (Components.Contains(typeof(MyResourceSinkComponent))) Components.Remove<MyResourceSinkComponent>(); Components.Add<MyResourceSinkComponent>(value); m_sinkComp = value; }
        }

        public string GetOwnerFactionTag()
        {
            if (IDModule == null)
                return "";

            if (IDModule.Owner == 0)
                return "";

            IMyFaction faction = MySession.Static.Factions.TryGetPlayerFaction(IDModule.Owner);
            if (faction == null)
                return "";

            return faction.Tag;
        }

        public bool IsBeingRemoved = false;


        public VRage.Game.MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(long identityId)
        {
            if (!MyFakes.SHOW_FACTIONS_GUI)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.NoOwnership;

            if (IDModule == null)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.NoOwnership;
            
            return IDModule.GetUserRelationToOwner(identityId);
        }

        public VRage.Game.MyRelationsBetweenPlayerAndBlock GetPlayerRelationToOwner()
        {
            if (!MyFakes.SHOW_FACTIONS_GUI)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.NoOwnership;

            if (IDModule == null)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.NoOwnership;

            System.Diagnostics.Debug.Assert(MySession.Static.LocalHumanPlayer != null);
            if (MySession.Static.LocalHumanPlayer != null)
                return IDModule.GetUserRelationToOwner(MySession.Static.LocalHumanPlayer.Identity.IdentityId);

            return VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral;
        }

        /// <summary>
        /// Whether the two blocks are friendly. This relation is base on their owners and is symmetrical
        /// </summary>
        public bool FriendlyWithBlock(MyCubeBlock block)
        {
            if (GetUserRelationToOwner(block.OwnerId) == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies) return false;
            if (block.GetUserRelationToOwner(OwnerId) == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies) return false;
            return true;
        }

        public bool MarkedToExplode = false;
        public int HackAttemptTime = 0;
        public bool IsBeingHacked
        {
            get { return MySandboxGame.TotalTimeInMilliseconds - HackAttemptTime < MyGridConstants.HACKING_ATTEMPT_TIME_MS; }
        }

        public MyCubeBlockDefinition BlockDefinition { get { return SlimBlock.BlockDefinition; } }
        public Vector3I Min { get { return SlimBlock.Min; } }
        public Vector3I Max { get { return SlimBlock.Max; } }
        public MyBlockOrientation Orientation { get { return SlimBlock.Orientation; } }
        public Vector3I Position { get { return SlimBlock.Position; } }
        public MyCubeGrid CubeGrid { get { return SlimBlock.CubeGrid; } }
        protected List<MyCubeBlockEffect> m_activeEffects = null;

        public MyUseObjectsComponentBase UseObjectsComponent { get { return Components.Get<MyUseObjectsComponentBase>(); } }
        
        // Whether the grid should call the ConnectionAllowed method for this block
        public bool CheckConnectionAllowed { get; set; }

        private int m_numberInGrid;
        public int NumberInGrid {
            get
            {
                return m_numberInGrid;
            }
            set
            {
                m_numberInGrid = value;
                if(m_numberInGrid > 1)
                    DisplayNameText = BlockDefinition.DisplayNameText + " " + m_numberInGrid;
                else
                    DisplayNameText = BlockDefinition.DisplayNameText;
            }
        }


        public MySlimBlock SlimBlock;

        /// <summary>
        /// Shortcut to component stack property.
        /// </summary>
        public bool IsFunctional
        {
            get
            {
                return SlimBlock.ComponentStack.IsFunctional;
            }
        }

        public virtual float DisassembleRatio
        {
            get
            {
                return BlockDefinition.DisassembleRatio;
            }
        }

        public bool IsWorking
        {
            get;
            private set;
        }
        public void UpdateIsWorking()
        {
            bool isWorking = CheckIsWorking();
            bool isWorkingChanged = isWorking != IsWorking;
            IsWorking = isWorking;
            if (isWorkingChanged && IsWorkingChanged != null)
                IsWorkingChanged(this);
        }
        public bool IsSilenced = false;
        public bool SilenceInChange = false;
        public bool UsedUpdateEveryFrame = false;

        protected virtual bool CheckIsWorking()
        {
            return IsFunctional;
        }

        public event Action<MyCubeBlock> IsWorkingChanged;

        public event Func<bool> CanContinueBuildCheck;
        public bool CanContinueBuild()
        {
            if (CanContinueBuildCheck == null) return true;

            bool retval = true;
            foreach (var func in CanContinueBuildCheck.GetInvocationList())
            {
                var boolFunc = func as Func<bool>;
                retval = retval & boolFunc();
            }
            return retval;
        }

        /// <summary>
        /// Detectors contains inverted matrices
        /// </summary>

        private MyIDModule m_IDModule;
        public MyIDModule IDModule
        {
            get
            {
                return m_IDModule;
            }
        }

        /// <summary>
        /// Map from dummy name to subblock (subgrid, note that after grid split the subblock instance will be the same)
        /// </summary>
        protected Dictionary<string, MySlimBlock> SubBlocks;

        /// <summary>
        /// Loaded subblocks from object builder. Cached for getting grid entity when loaded.
        /// </summary>
        private List<MyObjectBuilder_CubeBlock.MySubBlockId> m_loadedSubBlocks;

        public bool IsSubBlock { get { return SubBlockName != null; } }
        /// <summary>
        /// Name of subblock (key in the owner's subblocks map).
        /// </summary>
        public string SubBlockName {
            get;
            internal set;
        }
        /// <summary>
        /// If the block is subblock then OwnerBlock is set to block which owns (spawns) subblocks (subgrids)
        /// </summary>
        public MySlimBlock OwnerBlock {
            get;
            internal set;
        }


        public IMyUseObject GetInteractiveObject(uint shapeKey)
        {
            if (!IsFunctional)
            {
                return null;
            }
            return UseObjectsComponent.GetInteractiveObject(shapeKey);
        }

        public void ReleaseInventory(MyInventory inventory, bool damageContent = false)
        {
            // Spawning of floating objects and inventory modifications should be only done on the server. They are synced correctly already
            if (Sync.IsServer)
            {
                MyEntityInventorySpawnComponent component = null;

                if (Components.TryGet<MyEntityInventorySpawnComponent>(out component))
                {
                    component.SpawnInventoryContainer();
                    MyInventory newInventory = new MyInventory(inventory.MaxVolume, inventory.MaxMass, Vector3.One, inventory.GetFlags());
                    Components.Add<MyInventoryBase>(newInventory);
                }
                else
                {
                    var items = inventory.GetItems();
                    foreach (var item in items)
                    {
                        var spawnItem = item;
                        if (damageContent && item.Content.TypeId == typeof(MyObjectBuilder_Component))
                        {
                            spawnItem.Amount *= (MyFixedPoint)MyDefinitionManager.Static.GetComponentDefinition(item.Content.GetId()).DropProbability;
                            spawnItem.Amount = MyFixedPoint.Floor(spawnItem.Amount);
                            if (spawnItem.Amount == 0)
                                continue;
                        }

                        MyFloatingObjects.EnqueueInventoryItemSpawn(spawnItem, this.PositionComp.WorldAABB, (CubeGrid.Physics != null ? CubeGrid.Physics.GetVelocityAtPoint(PositionComp.GetPosition()) : Vector3.Zero));
                    }
                    inventory.Clear();
                }
            }
        }

        /// <summary>
        /// Called by constraint owner
        /// </summary>
        protected virtual void OnConstraintAdded(GridLinkTypeEnum type, IMyEntity attachedEntity)
        {
            var attachedGrid = attachedEntity as MyCubeGrid;
            if(attachedGrid != null)
            {
                // This crashes when connector (or anything else) connects to two things at the same time
                if (!MyCubeGridGroups.Static.GetGroups(type).LinkExists(EntityId, CubeGrid, attachedGrid))
                    MyCubeGridGroups.Static.CreateLink(type, EntityId, CubeGrid, attachedGrid);
                else
                    Debug.Fail("Adding same link twice!!!");
            }
        }

        /// <summary>
        /// Called by constraint owner
        /// </summary>
        protected virtual void OnConstraintRemoved(GridLinkTypeEnum type, IMyEntity detachedEntity)
        {
            var detachedGrid = detachedEntity as MyCubeGrid;
            if (detachedGrid != null)
            {
                MyCubeGridGroups.Static.BreakLink(type, EntityId, CubeGrid, detachedGrid);
            }
        }

        private static MethodDataIsConnectedTo m_methodDataIsConnectedTo;               

        public virtual void GetTerminalName(StringBuilder result)
        {
            //if (DisplayNameText == String.Empty)
            //{
            //    result.Append(DefinitionDisplayNameText);
            //}
            //else
            {
                result.Append(DisplayNameText);
            }
        }


        public String DefinitionDisplayNameText
        {
            get { return BlockDefinition.DisplayNameText; }
        }

        // Flag if block is destructible even if grid is not.
        protected bool m_forceBlockDestructible;
        public bool ForceBlockDestructible { get { return MyFakes.ENABLE_VR_FORCE_BLOCK_DESTRUCTIBLE && m_forceBlockDestructible; } }


        static MyCubeBlock()
        {
            m_methodDataIsConnectedTo = new MethodDataIsConnectedTo();
        }

        public MyCubeBlock()
        {
           // NumberInGrid = 1;
            (this as MyEntity).PositionComp = new MyBlockPosComponent();
            Render = new Components.MyRenderComponentCubeBlock();
            Render.ShadowBoxLod = true;
        }

        public void Init()
        {
            PositionComp.LocalAABB = new BoundingBox(new Vector3(-SlimBlock.CubeGrid.GridSize / 2), new Vector3(SlimBlock.CubeGrid.GridSize / 2));
            //if (CubeGrid.GridScale < 1.0f)
            //    PositionComp.Scale = CubeGrid.GridScale;
            Components.Add<MyUseObjectsComponentBase>(new MyUseObjectsComponent());

            Matrix localMatrix;
            string currModel;

            if (BlockDefinition.CubeDefinition != null)
            {
                //Ensure we have always only one distinct orientation use
                SlimBlock.Orientation = MyCubeGridDefinitions.GetTopologyUniqueOrientation(BlockDefinition.CubeDefinition.CubeTopology, Orientation);
            }

            CalcLocalMatrix(out localMatrix, out currModel);
            
            if (!string.IsNullOrEmpty(currModel))
            {
                Init(null, currModel, null, null, null);

                OnModelChange();
            }

            Render.EnableColorMaskHsv = true;

            // Can't skip
            Render.SkipIfTooSmall = false;

            CheckConnectionAllowed = false;

            PositionComp.SetLocalMatrix(localMatrix, CubeGrid);//SetWorldMatrix(localMatrix, CubeGrid, true);
            Save = false;

            if(CubeGrid.CreatePhysics)
                UseObjectsComponent.LoadDetectorsFromModel();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }

        public void GetLocalMatrix(out Matrix localMatrix)
        {
            SlimBlock.GetLocalMatrix(out localMatrix);
        }

        public void CalcLocalMatrix(out Matrix localMatrix, out string currModel)
        {
            GetLocalMatrix(out localMatrix);

            Matrix orientation;
            currModel = SlimBlock.CalculateCurrentModel(out orientation);

            orientation.Translation = localMatrix.Translation;
            localMatrix = orientation;
        }
        public virtual void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            //objectBuilder.PersistentFlags |= MyPersistentEntityFlags2.CastShadows;
            // Ensure that if we went from not serializing to serializing, we have a valid entity id.
            if (builder.EntityId == 0)
                EntityId = MyEntityIdentifier.AllocateId();
            else if (builder.EntityId != 0)
                EntityId = builder.EntityId;

            Name = builder.Name;

            NumberInGrid = cubeGrid.BlockCounter.GetNextNumber(builder.GetId());
            Render.ColorMaskHsv = builder.ColorMaskHSV;

            if (MyFakes.ENABLE_SUBBLOCKS)
            {
                bool hasSubblocks = BlockDefinition.SubBlockDefinitions != null && BlockDefinition.SubBlockDefinitions.Count > 0;
                if (hasSubblocks)
                {
                    if (builder.SubBlocks != null && builder.SubBlocks.Length != 0)
                    {
                        // Loading - remember subblock ids.
                        m_loadedSubBlocks = new List<MyObjectBuilder_CubeBlock.MySubBlockId>();

                        foreach (var subblockInfo in builder.SubBlocks)
                            m_loadedSubBlocks.Add(subblockInfo);
                        // Set update flag for loading subblocks, before next frame is required for newly created map - UpdateOnceBeforeFrame is called before world is saved.
                        NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    }
                    else if (Sync.IsServer)
                    {
                        // Empty list used as flag for UpdateOnceBeforeFrame that the block was created with cube builder.
                        m_loadedSubBlocks = new List<MyObjectBuilder_CubeBlock.MySubBlockId>();
                        // Server is creating block with subblocks.
                        SpawnSubBlocks();
                        // Set update flag for InitSubBlocks
                        NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    }
                }
            }


            Components.InitComponents(builder.TypeId, builder.SubtypeId, builder.ComponentContainer);

            base.Init(null);
            base.Render.PersistentFlags |= MyPersistentEntityFlags2.CastShadows;
            Init();
            AddDebugRenderComponent(new MyDebugRenderComponentCubeBlock(this));

            InitOwnership(builder);
        }

        private void InitOwnership(MyObjectBuilder_CubeBlock builder)
        {
            var ownerComp = Components.Get<MyEntityOwnershipComponent>();
            bool canHaveOwnership = BlockDefinition.ContainsComputer();
            if (UseObjectsComponent != null)
            {
                canHaveOwnership = canHaveOwnership || UseObjectsComponent.GetDetectors("ownership").Count > 0;
            }

            if (canHaveOwnership)
            {
                m_IDModule = new MyIDModule();

                bool resetOwnership = MySession.Static.Settings.ResetOwnership && Sync.IsServer;

                if (resetOwnership)
                {
                    m_IDModule.Owner = 0;
                    m_IDModule.ShareMode = MyOwnershipShareModeEnum.None;
                }
                else
                {
                    if ((int)builder.ShareMode == -1)
                        builder.ShareMode = MyOwnershipShareModeEnum.None;

                    var ownerType = MyEntityIdentifier.GetIdObjectType(builder.Owner);
                    if (builder.Owner != 0 && ownerType != MyEntityIdentifier.ID_OBJECT_TYPE.NPC && ownerType != MyEntityIdentifier.ID_OBJECT_TYPE.SPAWN_GROUP)
                    {
                        System.Diagnostics.Debug.Assert(ownerType == MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, "Old save detected, reseting owner to Nobody, please resave.");

                        if (!Sync.Players.HasIdentity(builder.Owner))
                            builder.Owner = 0; //reset, it was old version
                    }

                    m_IDModule.Owner = builder.Owner;
                    m_IDModule.ShareMode = builder.ShareMode;
                }
            }

            if (ownerComp != null && builder.Owner != 0)
            {
                ownerComp.OwnerId = builder.Owner;
                ownerComp.ShareMode = MyOwnershipShareModeEnum.None;
            }
        }

        public sealed override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            Debug.Fail("We should not be creating entity object builders for cube blocks!");
            return base.GetObjectBuilder(copy);
        }

        public virtual MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = MyCubeBlockFactory.CreateObjectBuilder(this);
            builder.ColorMaskHSV = Render.ColorMaskHsv;
            builder.EntityId = EntityId;
            builder.Min = Min;
            builder.Owner = 0;
            builder.ShareMode = MyOwnershipShareModeEnum.None;
            builder.Name = Name;
            if (m_IDModule != null)
            {
                builder.Owner = m_IDModule.Owner;
                builder.ShareMode = m_IDModule.ShareMode;
            }

            if (MyFakes.ENABLE_SUBBLOCKS)
            {
                if (SubBlocks != null && SubBlocks.Count != 0)
                {
                    builder.SubBlocks = new MyObjectBuilder_CubeBlock.MySubBlockId[SubBlocks.Count];
                    int counter = 0;
                    foreach (var pair in SubBlocks)
                    {
                        builder.SubBlocks[counter].SubGridId = pair.Value.CubeGrid.EntityId;
                        builder.SubBlocks[counter].SubGridName = pair.Key;
                        builder.SubBlocks[counter].SubBlockPosition = pair.Value.Min;
                        ++counter;
                    }
                }
            }

            builder.ComponentContainer = Components.Serialize(copy);
            if(copy)
                builder.Name = null;

            return builder;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateIsWorking();
        }

        public override void OnRemovedFromScene(object source)
        {
            StopDamageEffect();
            base.OnRemovedFromScene(source);
        }

        /// <summary>
        /// Returns true if this block can connect to another block (of the given type) in the given position.
        /// This is called only if CheckConnectionAllowed == true.
        /// If this method would return true for any position, set CheckConnectionAllowed to false to avoid
        /// unnecessary overhead. It is the block's responsibility to call CubeGrid.UpdateBlockNeighbors every time the
        /// conditions that are checked by this method change.
        /// </summary>
        public virtual bool ConnectionAllowed(ref Vector3I otherBlockPos, ref Vector3I faceNormal, MyCubeBlockDefinition def)
        {
            if (MyFakes.ENABLE_FRACTURE_COMPONENT && Components.Has<MyFractureComponentBase>())
            {
                MyFractureComponentCubeBlock fractureComponent = GetFractureComponent();

                if (fractureComponent == null || fractureComponent.MountPoints == null)
                    return true;

                m_tmpBlockMountPoints.Clear();
                MyCubeGrid.TransformMountPoints(m_tmpBlockMountPoints, BlockDefinition, fractureComponent.MountPoints.GetInternalArray(), ref SlimBlock.Orientation);

                var other = CubeGrid.GetCubeBlock(otherBlockPos);
                if (other == null)
                    return true;

                var position = Position;
                Debug.Assert(m_tmpMountPoints.Count == 0);
                m_tmpMountPoints.Clear();

                if (other.FatBlock is MyCompoundCubeBlock)
                {
                    foreach (var b in (other.FatBlock as MyCompoundCubeBlock).GetBlocks())
                    {
                        MyFractureComponentCubeBlock blockInCompoundFractureComponent = b.GetFractureComponent();
                        MyCubeBlockDefinition.MountPoint[] mountPoints = null;
                        if (blockInCompoundFractureComponent != null)
                            mountPoints = blockInCompoundFractureComponent.MountPoints.GetInternalArray();
                        else
                            mountPoints = b.BlockDefinition.GetBuildProgressModelMountPoints(b.BuildLevelRatio);

                        m_tmpOtherBlockMountPoints.Clear();
                        MyCubeGrid.TransformMountPoints(m_tmpOtherBlockMountPoints, b.BlockDefinition, mountPoints, ref b.Orientation);
                        m_tmpMountPoints.AddRange(m_tmpOtherBlockMountPoints);
                    }
                }
                else
                {
                    MyCubeBlockDefinition.MountPoint[] mountPoints = null;
                    var otherFractureComponent = other.GetFractureComponent();
                    if (otherFractureComponent != null)
                        mountPoints = otherFractureComponent.MountPoints.GetInternalArray();
                    else
                        mountPoints = def.GetBuildProgressModelMountPoints(other.BuildLevelRatio);

                    MyCubeGrid.TransformMountPoints(m_tmpMountPoints, def, mountPoints, ref other.Orientation);
                }

                bool result = MyCubeGrid.CheckMountPointsForSide(m_tmpBlockMountPoints, ref SlimBlock.Orientation, ref position, BlockDefinition.Id, ref faceNormal, m_tmpMountPoints,
                    ref other.Orientation, ref otherBlockPos, def.Id);

                m_tmpMountPoints.Clear();
                m_tmpBlockMountPoints.Clear();
                m_tmpOtherBlockMountPoints.Clear();

                return result;
            }

            return true;
        }

        /// <summary>
        /// Whether connection is allowed to any of the positions between otherBlockMinPos and otherBlockMaxPos (both inclusive).
        /// Default implementation calls ConnectionAllowed(ref Vector3I otherBlockPos, ref Vector3I faceNormal) in a for loop.
        /// Override this in a subclass if this is not needed (for example, because all calls would return the same value for the same face)
        /// </summary>
        public virtual bool ConnectionAllowed(ref Vector3I otherBlockMinPos, ref Vector3I otherBlockMaxPos, ref Vector3I faceNormal, MyCubeBlockDefinition def)
        {
            Vector3I pos = otherBlockMinPos;
            for (Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref otherBlockMinPos, ref otherBlockMaxPos); it.IsValid(); it.GetNext(out pos))
            {
                if (ConnectionAllowed(ref pos, ref faceNormal, def)) return true;
            }

            return false;
        }

        protected virtual void WorldPositionChanged(object source)
        {
            // NOTE: This is now handled by the UseObjectsComponent itself
			//if (this.UseObjectsComponent.DetectorPhysics != null && this.UseObjectsComponent.DetectorPhysics.Enabled && this.UseObjectsComponent.DetectorPhysics != source)
            //{
            //    UseObjectsComponent.DetectorPhysics.OnWorldPositionChanged(source);
            //}
        }

        protected override void Closing()
        {
            if (UseObjectsComponent.DetectorPhysics != null)
            {
                UseObjectsComponent.ClearPhysics();
            }

            if (MyFakes.ENABLE_SUBBLOCKS && SubBlocks != null)
            {
                foreach (var pair in SubBlocks)
                {
                    MySlimBlock subBlock = pair.Value;
                    if (subBlock.FatBlock != null)
                    {
                        subBlock.FatBlock.OwnerBlock = null;
                        subBlock.FatBlock.SubBlockName = null;
                        subBlock.FatBlock.OnClosing -= SubBlock_OnClosing;
                    }
                }
            }
            SetDamageEffect(false);
            //Moved to RemoveBlockInternal
            //CubeGrid.ChangeOwner(this, OwnerId, 0);

            SlimBlock.ComponentStack.IsFunctionalChanged -= ComponentStack_IsFunctionalChanged;

            base.Closing();
        }


        protected static void UpdateEmissiveParts(uint renderObjectId, float emissivity, Color emissivePartColor, Color displayPartColor)
        {
            if (renderObjectId != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                UpdateNamedEmissiveParts(renderObjectId, "Emissive", emissivePartColor, emissivity);
                UpdateNamedEmissiveParts(renderObjectId, "Display", displayPartColor, emissivity);
            }
        }

        public virtual void UpdateVisual()
        {
            Matrix orientation;
            var currModel = SlimBlock.CalculateCurrentModel(out orientation);
            bool modelChanged = Model != null && Model.AssetName != currModel;

            if (modelChanged || Render.ColorMaskHsv != SlimBlock.ColorMaskHSV || Render.Transparency != SlimBlock.Dithering)
            {
                Render.ColorMaskHsv = SlimBlock.ColorMaskHSV;
                Render.Transparency = SlimBlock.Dithering;

                Vector3D position = WorldMatrix.Translation;
                MatrixD newWorldMatrix = orientation * CubeGrid.WorldMatrix;
                newWorldMatrix.Translation = position;

                PositionComp.SetWorldMatrix(newWorldMatrix, null, true);

                RefreshModels(currModel, null);

                Render.RemoveRenderObjects();
                Render.AddRenderObjects();
                if (CubeGrid.CreatePhysics && modelChanged)
                    UseObjectsComponent.LoadDetectorsFromModel();
                OnModelChange();
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (MyFakes.ENABLE_SUBBLOCKS && m_loadedSubBlocks != null)
            {
                InitSubBlocks();
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            //VRageRender.MyRenderProxy.DebugDrawAABB(PositionComp.WorldAABB, Color.AliceBlue, 1f, 1f, false);
            if (MyFakes.ENABLE_SUBBLOCKS && m_loadedSubBlocks != null)
                InitSubBlocks();
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (m_activeEffects != null && MyPerGameSettings.UseNewDamageEffects)
            {
                for (int i = 0; i < m_activeEffects.Count; i++)
                {
                    if (m_activeEffects[i].CanBeDeleted)
                    {
                        m_activeEffects[i].Stop();
                        m_activeEffects.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        m_activeEffects[i].Update();
                    }
                }
            }
        }

        /// <summary>
        /// Method called when a block has been built (after adding to the grid).
        /// This is called right after placing the block and it doesn't matter whether
        /// it is fully built (creative mode) or is only construction site.
        /// Note that it is not called for blocks which do not create FatBlock at that moment.
        /// </summary>
        public virtual void OnBuildSuccess(long builtBy) { }

        /// <summary>
        /// Method called when user removes a cube block from grid. Useful when block
        /// has to remove some other attached block (like motors).
        /// </summary>
        public virtual void OnRemovedByCubeBuilder() 
        {
            if (MyFakes.ENABLE_SUBBLOCKS && SubBlocks != null)
            {
                // Remove subblock from subgrids (subgrids are not removed).
                foreach (var pair in SubBlocks)
                {
                    MySlimBlock subBlock = pair.Value;
                    subBlock.CubeGrid.RemoveBlock(subBlock, true);
                }
            }
            SetDamageEffect(false);
        }

        /// <summary>
        /// Called at the end of registration from grid systems (after block has been registered).
        /// </summary>
        public virtual void OnRegisteredToGridSystems() 
        {
            if (m_upgradeComponent != null)
            {
                m_upgradeComponent.Refresh(this);
            }
        }

        /// <summary>
        /// Called at the end of unregistration from grid systems (after block has been unregistered).
        /// </summary>
        public virtual void OnUnregisteredFromGridSystems() { }

        /// <summary>
        /// Return true when contact is valid
        /// </summary>
        public virtual void ContactPointCallback(ref MyGridContactInfo value) { }

        /// <summary>
        /// Called when block is destroyed before being removed from grid
        /// </summary>
        public virtual void OnDestroy() 
        {
            SetDamageEffect(false);
        }

        /// <summary>
        /// Called when the model referred by the block is changed
        /// </summary>
        public virtual void OnModelChange()
        {
        }

        public virtual string CalculateCurrentModel(out Matrix orientation)
        {
            Orientation.GetMatrix(out orientation);
            return BlockDefinition.Model;
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            UpdateIsWorking();
            CubeGrid.UpdateOwnership(OwnerId, IsFunctional);
        }

        internal virtual void OnIntegrityChanged(float buildIntegrity, float integrity, bool setOwnership, long owner, MyOwnershipShareModeEnum sharing = MyOwnershipShareModeEnum.Faction)
        {
            if (BlockDefinition.ContainsComputer())
            {
                var ownerComp = Components.Get<MyEntityOwnershipComponent>();
                if (setOwnership)
                {
                    if (m_IDModule.Owner == 0)
                    {
                        if (Sync.IsServer)
                        {
                            CubeGrid.ChangeOwnerRequest(CubeGrid, this, owner, sharing);
                        }
                    }

                    if (ownerComp != null && ownerComp.OwnerId == 0)
                    {
                        if (Sync.IsServer)
                            CubeGrid.ChangeOwnerRequest(CubeGrid, this, owner, sharing);
                }
                }
                else
                {
                    if (m_IDModule.Owner != 0 && Sync.IsServer)
                    {
                        sharing = MyOwnershipShareModeEnum.None;
                        CubeGrid.ChangeOwnerRequest(CubeGrid, this, 0, sharing);
                    }

                    if (ownerComp != null && ownerComp.OwnerId != 0 && Sync.IsServer)
                    {
                        sharing = MyOwnershipShareModeEnum.None;
                        CubeGrid.ChangeOwnerRequest(CubeGrid, this, 0, sharing);
                }
            }
        }
        }

        public void ChangeBlockOwnerRequest(long playerId, MyOwnershipShareModeEnum shareMode)
        {
            CubeGrid.ChangeOwnerRequest(CubeGrid, this, playerId, shareMode);
        }

        public bool SetEffect(string effectName, bool stopPrevious = false)
        {
            return SetEffect(effectName, 0f, stopPrevious, true);
        }

        public bool SetEffect(string effectName, float parameter, bool stopPrevious = false, bool ignoreParameter = false, bool removeSameNameEffects = false){
            if (BlockDefinition == null || BlockDefinition.Effects == null)
                return false;//block does not have any effects

            int i;
            int effectId = -1;
            
            //find effect in block definitions that corresponds to name and parameter
            for (i = 0; i < BlockDefinition.Effects.Length; i++)
            {
                if (effectName.Equals(BlockDefinition.Effects[i].Name) && (ignoreParameter || (parameter >= BlockDefinition.Effects[i].ParameterMin && parameter <= BlockDefinition.Effects[i].ParameterMax)))
                {
                    effectId = i;
                    break;
                }
            }
            if (effectId == -1)
                return false;//effect not in block definition

            if (m_activeEffects == null)
                m_activeEffects = new List<MyCubeBlockEffect>();

            //check active effects for this new effect
            for (i = 0; i < m_activeEffects.Count; i++)
            {
                if (m_activeEffects[i].EffectId == effectId)
                {
                    if (stopPrevious)
                    {
                        m_activeEffects[i].Stop();
                        m_activeEffects.RemoveAt(i);
                        break;
                    }
                    else
                        return false;//effect is already running
                }
            }

            if (removeSameNameEffects)
                RemoveEffect(effectName, effectId);

            if (m_activeEffects.Count == 0)
            {
                m_wasUpdatedEachFrame = (NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) != 0;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            }

            m_activeEffects.Add(new MyCubeBlockEffect(effectId, BlockDefinition.Effects[effectId], this));
            return true;
        }

        public int RemoveEffect(string effectName, int exception = -1)
        {
            if (BlockDefinition == null || BlockDefinition.Effects == null || m_activeEffects == null)
                return 0;//block does not have any effects

            int ret = 0;
            int i, j;
            for (i = 0; i < BlockDefinition.Effects.Length; i++)
            {
                if (effectName.Equals(BlockDefinition.Effects[i].Name))
                {
                    for (j = 0; j < m_activeEffects.Count; j++)
                    {
                        if (m_activeEffects[j].EffectId == i && i != exception)
                        {
                            m_activeEffects[j].Stop();
                            m_activeEffects.RemoveAt(j);
                            ret++;
                        }
                    }
                }
            }
            if (m_activeEffects.Count == 0 && m_wasUpdatedEachFrame == false)
            {
                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
            }
            return ret;
        }

        private MyParticleEffect m_damageEffect;// = new MyParticleEffect();
        private bool m_wasUpdatedEachFrame=false;
        public virtual void SetDamageEffect(bool show)
        {
            if (MyPerGameSettings.UseNewDamageEffects && show)
                SetEffect("Damage", (SlimBlock.Integrity / SlimBlock.MaxIntegrity), removeSameNameEffects: true);

            bool effectCreated = (m_activeEffects != null && MyPerGameSettings.UseNewDamageEffects && m_activeEffects.Count > 0);

            if (MyPerGameSettings.UseNewDamageEffects && show == false)
                RemoveEffect("Damage");

            if (MyFakes.SHOW_DAMAGE_EFFECTS && BlockDefinition.DamageEffectID != null&& MySandboxGame.Static.EnableDamageEffects)
            {
                if (!show && m_damageEffect != null)
                {//stop
                    m_damageEffect.Stop();
                    m_damageEffect = null;
                    if (!m_wasUpdatedEachFrame)
                        NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
                }
                if (show && m_damageEffect == null && effectCreated == false)
                {//start
                    if (MyParticlesManager.TryCreateParticleEffect((int)BlockDefinition.DamageEffectID, out m_damageEffect))
                    {
                        m_damageEffect.UserScale = Model.BoundingBox.Perimeter * 0.018f;//scale to size of item
                        setDamageWorldMatrix();
                        m_damageEffect.OnDelete += damageEffect_OnDelete;
                    }
                    m_wasUpdatedEachFrame = (NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) != 0;
                    //Debug.Assert(!m_wasUpdatedEachFrame, "may not t NeedUpdate correctly!");
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                }
            }
        }
        public virtual void StopDamageEffect()
        {
            if (MyPerGameSettings.UseNewDamageEffects)
                RemoveEffect("Damage");

            if (MyFakes.SHOW_DAMAGE_EFFECTS && BlockDefinition.DamageEffectID != null)
            {
                if (m_damageEffect != null)
                {//stop
                    m_damageEffect.Stop();
                    m_damageEffect = null;
                    if (!m_wasUpdatedEachFrame)
                        NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
                } 
            }
        }
        MatrixD m_LocalOffset=new MatrixD();
        private void setDamageWorldMatrix()
        {
            m_LocalOffset = MatrixD.CreateTranslation(0.85f * this.PositionComp.LocalVolume.Center);//<1 because 100% of offset would bury the effect inside wall
            m_damageEffect.WorldMatrix = m_LocalOffset * WorldMatrix;
        }

        void damageEffect_OnDelete(object sender, EventArgs e)
        {
            SetDamageEffect(false);
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (m_damageEffect != null)
            {
                setDamageWorldMatrix();
            }
        }


        public void ChangeOwner(long owner, MyOwnershipShareModeEnum shareMode)
        {
            var ownerComp = Components.Get<MyEntityOwnershipComponent>();
            if (ownerComp != null)
            {
                bool changed = owner != ownerComp.OwnerId || shareMode != ownerComp.ShareMode;
                if (changed)
                {
                    var oldOwner = ownerComp.OwnerId;
                    ownerComp.OwnerId = owner;
                    ownerComp.ShareMode = shareMode;

                    if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
                        CubeGrid.ChangeOwner(this, oldOwner, owner);

                    OnOwnershipChanged();
                }
            }
            else if (IDModule != null)
            {
                bool changed = owner != m_IDModule.Owner || shareMode != m_IDModule.ShareMode;
                if (changed)
                {
                    var oldOwner = m_IDModule.Owner;
                    m_IDModule.Owner = owner;
                    m_IDModule.ShareMode = shareMode;

                    if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
                        CubeGrid.ChangeOwner(this, oldOwner, owner);

                    OnOwnershipChanged();
                }
            }
        }

        protected virtual void OnOwnershipChanged()
        {
            
        }

        bool IMyComponentOwner<MyIDModule>.GetComponent(out MyIDModule component)
        {
            component = m_IDModule;
            return m_IDModule != null && IsFunctional;
        }

        /// <summary>
        /// Notifies about grid change with old grid in parameter (new grid is available in property).
        /// </summary>
        public virtual void OnCubeGridChanged(MyCubeGrid oldGrid)
        {
            if (MyFakes.ENABLE_FRACTURE_COMPONENT && Components.Has<MyFractureComponentBase>())
            {
                var fractureComponent = GetFractureComponent();
                if (fractureComponent != null)
                    fractureComponent.OnCubeGridChanged();
            }
        }

        internal virtual void OnAddedNeighbours()
        {
            
        }

        internal virtual void OnRemovedNeighbours()
        {

        }

        internal virtual void OnTransformed(ref MatrixI transform)
        {
            
        }

        internal virtual void UpdateWorldMatrix()
        {
            Matrix local;
            GetLocalMatrix(out local);
            PositionComp.SetWorldMatrix(local, null, true);
        }

        public class MyBlockPosComponent : MyPositionComponent
        {
            protected override void OnWorldPositionChanged(object source, bool updateChildren)
            {
                base.OnWorldPositionChanged(source, updateChildren);
                (Container.Entity as MyCubeBlock).WorldPositionChanged(source);
            }
        }

        private void InitSubBlocks()
        {
            if (MyFakes.ENABLE_SUBBLOCKS && m_loadedSubBlocks != null)
            {
                bool wereAllSubBlocksInitialized = AllSubBlocksInitialized();
                bool spawned = m_loadedSubBlocks.Count == 0 && Sync.IsServer && wereAllSubBlocksInitialized;

                if (!wereAllSubBlocksInitialized) 
                {
                    for (int i = m_loadedSubBlocks.Count - 1; i >= 0; --i)
                    {
                        var subBlockId = m_loadedSubBlocks[i];

                        MyEntity entity;
                        if (MyEntities.TryGetEntityById(subBlockId.SubGridId, out entity))
                        {
                            var subgrid = entity as MyCubeGrid;
                            if (subgrid != null)
                            {
                                var subblock = subgrid.GetCubeBlock(subBlockId.SubBlockPosition);
                                // subbblock can be null when parts of grid are destroyed (removed) on server
                                if (subblock != null)
                                    AddSubBlock(subBlockId.SubGridName, subblock);
                            }
                            else
                            {
                                Debug.Assert(false, "Loaded entity is not grid!");
                            }

                            m_loadedSubBlocks.RemoveAt(i);
                        }
                    }
                }

                bool allSubBlocksInitialized = AllSubBlocksInitialized();
                if (allSubBlocksInitialized)
                {
                    m_loadedSubBlocks = null;

                    if ((spawned || !wereAllSubBlocksInitialized))
                        SubBlocksInitialized(spawned);
                }
            }
        }

        protected bool AllSubBlocksInitialized()
        {
            bool hasDefinedSubBlocks = BlockDefinition.SubBlockDefinitions != null && BlockDefinition.SubBlockDefinitions.Count != 0;
            if (!hasDefinedSubBlocks)
                return false;

            return SubBlocks != null && SubBlocks.Count != 0 && (SubBlocks.Count == BlockDefinition.SubBlockDefinitions.Count || m_loadedSubBlocks == null 
                || m_loadedSubBlocks.Count == 0);
        }

        protected void AddSubBlock(string dummyName, MySlimBlock subblock)
        {
            if (SubBlocks == null)
                SubBlocks = new Dictionary<string, MySlimBlock>();

            // Subblock can already be added - happen for client's blocks which sync subblocks by property change internally.
            MySlimBlock existingSubBlock;
            if (SubBlocks.TryGetValue(dummyName, out existingSubBlock))
            {
                if (subblock == existingSubBlock)
                    return;

                Debug.Fail("Subblock already exists.");
                RemoveSubBlock(dummyName, removeFromGrid: false);
            }

            SubBlocks.Add(dummyName, subblock);
            Debug.Assert(subblock.FatBlock != null);
            subblock.FatBlock.SubBlockName = dummyName;
            Debug.Assert(SlimBlock != null);
            subblock.FatBlock.OwnerBlock = SlimBlock;
            subblock.FatBlock.OnClosing += SubBlock_OnClosing;
        }

        private void SpawnSubBlocks()
        {
            if (!MyFakes.ENABLE_SUBBLOCKS)
                return;

            Debug.Assert(Sync.IsServer);

            if (!CubeGrid.CreatePhysics)
                return;

            MyCubeBlockDefinition subBlockDefinition;
            MatrixD subBlockMatrix;
            Vector3 dummyPosition;

            var finalModel = VRage.Game.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
            foreach (var dummy in finalModel.Dummies)
            {
                if (!MyCubeBlock.GetSubBlockDataFromDummy(BlockDefinition, dummy.Key, dummy.Value, true, out subBlockDefinition, out subBlockMatrix, out dummyPosition))
                    continue;

                string dummyName = dummy.Key.Substring(DUMMY_SUBBLOCK_ID.Length);

                Debug.Assert(!subBlockMatrix.IsMirrored());
                Matrix localMatrix;
                GetLocalMatrix(out localMatrix);
                Matrix subGridWorldMatrix = subBlockMatrix * localMatrix * CubeGrid.WorldMatrix;

                MySlimBlock subblock = null;
                MyCubeGrid subgrid = MyCubeBuilder.SpawnDynamicGrid(subBlockDefinition, null, subGridWorldMatrix, new Vector3(0, -1f, 0));
                if (subgrid != null)
                {
                    subblock = subgrid.GetCubeBlock(Vector3I.Zero);
                }
                else
                {
                    Debug.Assert(false, "SubGrid has not been set!");
                    continue;
                }

                if (subblock == null || subblock.FatBlock == null)
                {
                    Debug.Assert(false, "Fatblock cannot be null for subblocks!");
                    continue;
                }

                AddSubBlock(dummyName, subblock);
            }
        }

        /// <summary>
        /// Function called when all subblocks have been initialized.
        /// </summary>
        /// <param name="spawned">true if subblocks have been just spawned on server, otherwise false (load)</param>
        protected virtual void SubBlocksInitialized(bool spawned)
        {
        }

        protected virtual void OnSubBlockClosing(MySlimBlock subBlock)
        {
            subBlock.FatBlock.OnClosing -= SubBlock_OnClosing;
            Debug.Assert(SubBlocks != null);
            if (SubBlocks != null)
                SubBlocks.Remove(subBlock.FatBlock.SubBlockName);
        }

        private void SubBlock_OnClosing(MyEntity obj)
        {
            MyCubeBlock subblock = obj as MyCubeBlock;
            if (subblock != null)
            {
                var pair = SubBlocks.FirstOrDefault(p => p.Value == subblock.SlimBlock);
                if (pair.Value != null)
                    OnSubBlockClosing(pair.Value);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        /// <summary>
        /// Removes subblock with the given name from the block. 
        /// </summary>
        protected bool RemoveSubBlock(string subBlockName, bool removeFromGrid = true)
        {
            Debug.Assert(SubBlocks != null);
            if (SubBlocks == null)
                return false;

            MySlimBlock subBlock;
            if (SubBlocks.TryGetValue(subBlockName, out subBlock))
            {
                if (removeFromGrid)
                    subBlock.CubeGrid.RemoveBlock(subBlock, true);

                if (SubBlocks.Remove(subBlockName))
                {
                    if (subBlock.FatBlock != null)
                    {
                        subBlock.FatBlock.OwnerBlock = null;
                        subBlock.FatBlock.SubBlockName = null;
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns block offset in spawned grid.
        /// </summary>
        public static Vector3 GetBlockGridOffset(MyCubeBlockDefinition blockDefinition)
        {
            float cubeSize = MyDefinitionManager.Static.GetCubeSize(blockDefinition.CubeSize);

            Vector3 offset = Vector3.Zero;
            if ((blockDefinition.Size.X % 2) == 0)
                offset.X = cubeSize / 2f;
            if ((blockDefinition.Size.Y % 2) == 0)
                offset.Y = cubeSize / 2f;
            if ((blockDefinition.Size.Z % 2) == 0)
                offset.Z = cubeSize / 2f;

            return offset;
        }

        /// <summary>
        /// Returns subblock data from dummy, subblock matrix can be offset (according to useOffset parameter) so the dummy position output is also provided.
        /// </summary>
        /// <returns>true when dummy is subblock otherwise false</returns>
        public static bool GetSubBlockDataFromDummy(MyCubeBlockDefinition ownerBlockDefinition, string dummyName, MyModelDummy dummy, bool useOffset, out MyCubeBlockDefinition subBlockDefinition,
            out MatrixD subBlockMatrix, out Vector3 dummyPosition) 
        {
            subBlockDefinition = null;
            subBlockMatrix = MatrixD.Identity;
            dummyPosition = Vector3.Zero;

            if (!dummyName.ToLower().StartsWith(MyCubeBlock.DUMMY_SUBBLOCK_ID))
                return false;

            if (ownerBlockDefinition.SubBlockDefinitions == null)
                return false;

            string dummyNameShort = dummyName.Substring(MyCubeBlock.DUMMY_SUBBLOCK_ID.Length);

            MyDefinitionId definitiondId;
            if (!ownerBlockDefinition.SubBlockDefinitions.TryGetValue(dummyNameShort, out definitiondId))
            {
                Debug.Assert(false, "SubBlock definition not found!");
                return false;
            }

            MyDefinitionManager.Static.TryGetCubeBlockDefinition(definitiondId, out subBlockDefinition);
            if (subBlockDefinition == null)
            {
                Debug.Assert(false, "SubBlock definition not found!");
                return false;
            }

            const double dotEpsilon = 0.00000001;
            subBlockMatrix = MatrixD.Normalize(dummy.Matrix);
            Vector3I forward = Base6Directions.GetIntVector(Base6Directions.GetClosestDirection(subBlockMatrix.Forward));
            double forwardDot = Vector3D.Dot(subBlockMatrix.Forward, (Vector3D)forward);
            if (Math.Abs(1 - forwardDot) <= dotEpsilon)
                subBlockMatrix.Forward = forward;

            Vector3I right = Base6Directions.GetIntVector(Base6Directions.GetClosestDirection(subBlockMatrix.Right));
            double rightDot = Vector3D.Dot(subBlockMatrix.Right, (Vector3D)right);
            if (Math.Abs(1 - rightDot) <= dotEpsilon)
                subBlockMatrix.Right = right;

            Vector3I up = Base6Directions.GetIntVector(Base6Directions.GetClosestDirection(subBlockMatrix.Up));
            double upDot = Vector3D.Dot(subBlockMatrix.Up, (Vector3D)up);
            if (Math.Abs(1 - upDot) <= dotEpsilon)
                subBlockMatrix.Up = up;

            dummyPosition = subBlockMatrix.Translation;
            if (useOffset)
            {
                Vector3 offset = MyCubeBlock.GetBlockGridOffset(subBlockDefinition);
                subBlockMatrix.Translation -= Vector3D.TransformNormal(offset, subBlockMatrix);
            }

            return true;
        }

        public virtual float GetMass()
        {
            Matrix m;
            if (MyDestructionData.Static != null)
                return MyDestructionData.Static.GetBlockMass(SlimBlock.CalculateCurrentModel(out m), BlockDefinition);
            return BlockDefinition.Mass;
        }

        public virtual BoundingBox GetGeometryLocalBox()
        {
            if (Model != null)
                return Model.BoundingBox; //TODO pm: BB is centered on model center not block center

            return new BoundingBox(new Vector3(-CubeGrid.GridSize / 2), new Vector3(CubeGrid.GridSize / 2));
        }

        public DictionaryReader<string, MySlimBlock> GetSubBlocks()
        {
            return new DictionaryReader<string, MySlimBlock>(SubBlocks);
        }

        public bool TryGetSubBlock(string name, out MySlimBlock block)
        {
            if (SubBlocks == null)
            {
                block = null;
                return false;
            }

            return SubBlocks.TryGetValue(name, out block);
        }

        private MyUpgradableBlockComponent m_upgradeComponent;
        public MyUpgradableBlockComponent GetComponent()
        {
            if (m_upgradeComponent == null)
            {
                m_upgradeComponent = new MyUpgradableBlockComponent(this);
            }
            return m_upgradeComponent;
        }

        private Dictionary<string, float> m_upgradeValues;
        public Dictionary<string, float> UpgradeValues
        {
            get
            {
                if (m_upgradeValues == null)
                {
                    m_upgradeValues = new Dictionary<string, float>();
                }

                return m_upgradeValues;
            }
        }
        public void AddUpgradeValue(string name, float defaultValue)
        {
            float previousDefault;
            if (UpgradeValues.TryGetValue(name, out previousDefault))
            {
                if (previousDefault != defaultValue)
                {
                    VRage.Utils.MyLog.Default.WriteLine("ERROR while adding upgraded block " + DisplayNameText.ToString() + ". Duplicate with different default value found!");
                }
            }
            else
            {
                UpgradeValues.Add(name, defaultValue);
            }
        }

        public event Action OnUpgradeValuesChanged;
        public void CommitUpgradeValues()
        {
            var handler = OnUpgradeValuesChanged;
            if (handler != null)
            {
                handler();
            }
        }

        public virtual void CreateRenderer(MyPersistentEntityFlags2 persistentFlags, Vector3 colorMaskHsv, object modelStorage)
        {
            Render = new Components.MyRenderComponentCubeBlock();
            Render.ColorMaskHsv = colorMaskHsv;
            Render.ShadowBoxLod = true;
            Render.EnableColorMaskHsv = true;
            Render.SkipIfTooSmall = false;
            Render.PersistentFlags |= persistentFlags | MyPersistentEntityFlags2.CastShadows;
            Render.ModelStorage = modelStorage;
        }

        public MyFractureComponentCubeBlock GetFractureComponent()
        {
            MyFractureComponentCubeBlock fractureComponent = null;
            if (MyFakes.ENABLE_FRACTURE_COMPONENT)
                fractureComponent = Components.Get<MyFractureComponentBase>() as MyFractureComponentCubeBlock;
            return fractureComponent;
        }

        public override void RefreshModels(string modelPath, string modelCollisionPath)
        {
            MyModel model = MyModels.GetModelOnlyData(modelPath);
            if (model != null)
                model.Rescale(CubeGrid.GridScale);

            if (modelCollisionPath != null)
            {
                model = MyModels.GetModelOnlyData(modelCollisionPath);
                if (model != null)
                    model.Rescale(CubeGrid.GridScale);
            }

            // Must be after model rescale
            base.RefreshModels(modelPath, modelCollisionPath);
        }
    }
}

