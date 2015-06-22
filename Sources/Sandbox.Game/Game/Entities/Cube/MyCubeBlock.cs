#region Using

using System;
using System.Collections.Generic;
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
using Sandbox.Graphics.TransparentGeometry.Particles;
using VRage.Collections;
using Sandbox.Common.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Components;
using VRage.Game.Entity.UseObject;

#endregion

namespace Sandbox.Game.Entities
{
    public partial class MyCubeBlock : MyEntity, IMyComponentOwner<MyIDModule>
    {
        protected static readonly string DUMMY_SUBBLOCK_ID = "subblock_";

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

        public long OwnerId
        {
            get
            {
                return IDModule == null ? 0 : IDModule.Owner;
            }
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


        public MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(long playerId)
        {
            if (!MyFakes.SHOW_FACTIONS_GUI)
                return MyRelationsBetweenPlayerAndBlock.FactionShare;

            if (IDModule == null)
                return MyRelationsBetweenPlayerAndBlock.FactionShare;

            return IDModule.GetUserRelationToOwner(playerId);
        }

        public MyRelationsBetweenPlayerAndBlock GetPlayerRelationToOwner()
        {
            System.Diagnostics.Debug.Assert(MySession.LocalHumanPlayer != null);
            if (MySession.LocalHumanPlayer != null)
                return GetUserRelationToOwner(MySession.LocalHumanPlayer.Identity.IdentityId);

            return MyRelationsBetweenPlayerAndBlock.Neutral;
        }

        /// <summary>
        /// Whether the two blocks are friendly. This relation is base on their owners and is symmetrical
        /// </summary>
        public bool FriendlyWithBlock(MyCubeBlock block)
        {
            if (GetUserRelationToOwner(block.OwnerId) == MyRelationsBetweenPlayerAndBlock.Enemies) return false;
            if (block.GetUserRelationToOwner(OwnerId) == MyRelationsBetweenPlayerAndBlock.Enemies) return false;
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

        protected virtual bool CheckIsWorking()
        {
            return IsFunctional;
        }

        public event Action<MyCubeBlock> IsWorkingChanged;

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
        protected readonly Dictionary<string, MySlimBlock> SubBlocks = new Dictionary<string, MySlimBlock>();
        private bool m_subBlocksInitialized;
        // Flag if subblocks are loaded from object builder or created from definition.
        private bool m_subBlocksLoaded;
        private struct MySubBlockLoadInfo
        {
            public long GridId;
            public Vector3I SubBlockPosition;
        }
        /// <summary>
        /// Loaded subblocks (subgrids) stored as ids from builder. Cached for setting subblocks later (UpdateOnceBeforeFrame) because subgrids have not been initialized yet.
        /// </summary>
        private readonly Dictionary<string, MySubBlockLoadInfo> m_subBlockIds = new Dictionary<string, MySubBlockLoadInfo>();
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


        public IMyUseObject GetInteractiveObject(int shapeKey)
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

                    MyFloatingObjects.EnqueueInventoryItemSpawn(spawnItem, this.PositionComp.WorldAABB);
                }
                inventory.Clear();
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
                MyCubeGridGroups.Static.CreateLink(type, EntityId, CubeGrid, attachedGrid);
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

        protected static void CreateGridGroupLink(GridLinkTypeEnum type, long linkId, MyCubeGrid parent, MyCubeGrid child)
        {
            MyCubeGridGroups.Static.CreateLink(type, linkId, parent, child);
        }

        protected static bool BreakGridGroupLink(GridLinkTypeEnum type, long linkId, MyCubeGrid parent, MyCubeGrid child)
        {
            return MyCubeGridGroups.Static.BreakLink(type, linkId, parent, child);
        }

        private static MethodDataIsConnectedTo m_methodDataIsConnectedTo;

        public virtual String DisplayNameText { get; protected set; }

        public virtual void GetTerminalName(StringBuilder result)
        {
            result.Append(DisplayNameText);
        }


        public String DefinitionDisplayNameText
        {
            get { return BlockDefinition.DisplayNameText; }
        }

        public static Dictionary<int, List<BoundingBox>> ExcludedAreaForCameraIntersections = new Dictionary<int, List<BoundingBox>>();

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

            WorldMatrix = localMatrix;
            Save = false;

            UseObjectsComponent.LoadDetectorsFromModel();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }

        public void CalcLocalMatrix(out Matrix localMatrix, out string currModel)
        {
            SlimBlock.Orientation.GetMatrix(out localMatrix);
            localMatrix.Translation = ((SlimBlock.Min + SlimBlock.Max) * 0.5f) * SlimBlock.CubeGrid.GridSize;

            Vector3 modelOffset;
            Vector3.TransformNormal(ref BlockDefinition.ModelOffset, ref localMatrix, out modelOffset);
            localMatrix.Translation += modelOffset;

            Matrix orientation;
            currModel = SlimBlock.CalculateCurrentModel(out orientation);

            Vector3 position = localMatrix.Translation;
            localMatrix = orientation;
            localMatrix.Translation = position;
        }

        public virtual void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            //objectBuilder.PersistentFlags |= MyPersistentEntityFlags2.CastShadows;
            // Ensure that if we went from not serializing to serializing, we have a valid entity id.
            if (builder.EntityId == 0)
                EntityId = MyEntityIdentifier.AllocateId();
            else if (builder.EntityId != 0)
                EntityId = builder.EntityId;

            NumberInGrid = cubeGrid.BlockCounter.GetNextNumber(builder.GetId());
            Render.ColorMaskHsv = builder.ColorMaskHSV;

            if (BlockDefinition.ContainsComputer() || (MyFakes.ENABLE_BATTLE_SYSTEM && MySession.Static.Battle))
            {
                m_IDModule = new MyIDModule();

                bool resetOwnership = MySession.Static.Settings.ResetOwnership && Sync.IsServer && (!MyFakes.ENABLE_BATTLE_SYSTEM || !MySession.Static.Battle);

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

            if (MyFakes.ENABLE_SUBBLOCKS)
            {
                if (builder.SubBlocks != null)
                {
                    foreach (var subblockInfo in builder.SubBlocks)
                    {
                        m_subBlockIds.Add(subblockInfo.SubGridName, new MySubBlockLoadInfo() { GridId=subblockInfo.SubGridId, SubBlockPosition=subblockInfo.SubBlockPosition });
                    }

                    m_subBlocksLoaded = m_subBlockIds.Count > 0;

                    if (BlockDefinition.SubBlockDefinitions != null && BlockDefinition.SubBlockDefinitions.Count > 0 && m_subBlockIds.Count == 0)
                    {
                        m_subBlocksInitialized = true;
                        m_subBlocksLoaded = true;
                    }

                    // Set update flag for InitSubBlocks
                    NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                }
            }


            base.Init(null);
            base.Render.PersistentFlags |= MyPersistentEntityFlags2.CastShadows;
            Init();
            AddDebugRenderComponent(new MyDebugRenderComponentCubeBlock(this));
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
            if (m_IDModule != null)
            {
                builder.Owner = m_IDModule.Owner;
                builder.ShareMode = m_IDModule.ShareMode;
            }

            if (MyFakes.ENABLE_SUBBLOCKS)
            {
                if (SubBlocks.Count > 0 || (BlockDefinition.SubBlockDefinitions != null && BlockDefinition.SubBlockDefinitions.Count > 0))
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

            return builder;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateIsWorking();
        }

        public override void OnRemovedFromScene(object source)
        {
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
            for (Vector3I.RangeIterator it = new Vector3I.RangeIterator(ref otherBlockMinPos, ref otherBlockMaxPos); it.IsValid(); it.GetNext(out pos))
            {
                if (ConnectionAllowed(ref pos, ref faceNormal, def)) return true;
            }

            return false;
        }

        protected virtual void WorldPositionChanged(object source)
        {
            if (this.UseObjectsComponent.DetectorPhysics != null && this.UseObjectsComponent.DetectorPhysics.Enabled && this.UseObjectsComponent.DetectorPhysics != source)
            {
                UseObjectsComponent.DetectorPhysics.OnWorldPositionChanged(source);
            }
        }

        protected override void Closing()
        {
            if (UseObjectsComponent.DetectorPhysics != null)
            {
                UseObjectsComponent.ClearPhysics();
            }

            if (MyFakes.ENABLE_SUBBLOCKS)
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
                VRageRender.MyRenderProxy.UpdateModelProperties(renderObjectId, 0, null, -1,
                    "Emissive", null, emissivePartColor, null, null, emissivity);
                VRageRender.MyRenderProxy.UpdateModelProperties(renderObjectId, 0, null, -1,
                    "Display", null, displayPartColor, null, null, emissivity);
            }
        }

        public virtual void UpdateVisual()
        {
            Matrix orientation;
            var currModel = SlimBlock.CalculateCurrentModel(out orientation);

            if (Model != null && Model.AssetName != currModel || Render.ColorMaskHsv != SlimBlock.ColorMaskHSV || Render.Transparency != SlimBlock.Dithering)
            {
                Render.ColorMaskHsv = SlimBlock.ColorMaskHSV;
                Render.Transparency = SlimBlock.Dithering;

                Vector3D position = WorldMatrix.Translation;
                MatrixD newWorldMatrix = orientation * CubeGrid.WorldMatrix;
                newWorldMatrix.Translation = position;

                WorldMatrix = newWorldMatrix;

                RefreshModels(currModel, null);

                Render.RemoveRenderObjects();
                Render.AddRenderObjects();
                UseObjectsComponent.LoadDetectorsFromModel();
                OnModelChange();
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            InitSubBlocks();
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
            if (MyFakes.ENABLE_SUBBLOCKS)
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
                if (setOwnership)
                {
                    if (m_IDModule.Owner == 0)
                    {
                        if (Sync.IsServer)
                        {
                            CubeGrid.SyncObject.ChangeOwnerRequest(CubeGrid, this, owner, sharing);
                        }
                    }
                }
                else
                {
                    if (m_IDModule.Owner != 0)
                    {
                        sharing = MyOwnershipShareModeEnum.None;
                        CubeGrid.SyncObject.ChangeOwnerRequest(CubeGrid, this, 0, sharing);
                    }
                }
            }
        }

        public void ChangeBlockOwnerRequest(long playerId, MyOwnershipShareModeEnum shareMode)
        {
            CubeGrid.SyncObject.ChangeOwnerRequest(CubeGrid, this, playerId, shareMode);
        }

        private MyParticleEffect m_damageEffect;// = new MyParticleEffect();
        private bool m_wasUpdatedEachFrame=false;
        internal void SetDamageEffect(bool show)
        {
            if (MyFakes.SHOW_DAMAGE_EFFECTS && BlockDefinition.DamageEffectID != null&& MySandboxGame.Static.EnableDamageEffects)
            {
                if (!show && m_damageEffect != null)
                {//stop
                    m_damageEffect.Stop();
                    m_damageEffect = null;
                    if (!m_wasUpdatedEachFrame)
                        NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
                }
                if (show && m_damageEffect == null)
                {//start
                    if (MyParticlesManager.TryCreateParticleEffect((int)BlockDefinition.DamageEffectID, out m_damageEffect))
                    {
                        m_damageEffect.UserScale = Model.BoundingBox.Perimeter*.01f;//scale to size of item
                        setDamageWorldMatrix();
                        m_damageEffect.OnDelete += damageEffect_OnDelete;
                        m_damageEffect.AutoDelete = false;
                    }
                    m_wasUpdatedEachFrame = (NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) != 0;
                    //Debug.Assert(!m_wasUpdatedEachFrame, "may not t NeedUpdate correctly!");
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                }
            }
        }
        internal void StopDamageEffect()
        {
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
            if (m_IDModule == null)
            {
                if (MyFakes.ENABLE_BATTLE_SYSTEM && MySession.Static.Battle)
                {
                    m_IDModule = new MyIDModule();
                    m_IDModule.Owner = 0;
                    m_IDModule.ShareMode = MyOwnershipShareModeEnum.None;
                }
                else
                {
                    return;
                }
            }

            bool changed = owner != m_IDModule.Owner || shareMode != m_IDModule.ShareMode;
            if (changed)
            {
                var oldOwner = m_IDModule.Owner;
                m_IDModule.Owner = owner;
                m_IDModule.ShareMode = shareMode;

                if(MyFakes.ENABLE_TERMINAL_PROPERTIES)
                    CubeGrid.ChangeOwner(this, oldOwner, owner);

                OnOwnershipChanged();
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
            string modelName;

            CalcLocalMatrix(out local, out modelName);
            WorldMatrix = local;
        }

        public class MyBlockPosComponent : MyPositionComponent
        {
            public override void OnWorldPositionChanged(object source)
            {
                base.OnWorldPositionChanged(source);
                (Container.Entity as MyCubeBlock).WorldPositionChanged(source);
            }
        }

        protected virtual void InitSubBlocks()
        {
            if (!MyFakes.ENABLE_SUBBLOCKS)
                return;

            if (m_subBlocksInitialized)
                return;

          //  if (!Sync.IsServer)
            //    return;

            try
            {
                MyCubeBlockDefinition subBlockDefinition;
                MatrixD subBlockMatrix;
                Vector3 dummyPosition;

                var finalModel = Sandbox.Engine.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
                foreach (var dummy in finalModel.Dummies)
                {
                    if (!MyCubeBlock.GetSubBlockDataFromDummy(BlockDefinition, dummy.Key, dummy.Value, true, out subBlockDefinition, out subBlockMatrix, out dummyPosition))
                        continue;

                    string dummyName = dummy.Key.Substring(DUMMY_SUBBLOCK_ID.Length);

                    MySlimBlock subblock = null;
                    MyCubeGrid subgrid = null;
                    MySubBlockLoadInfo subBlockLoadInfo;
                    if (m_subBlockIds.TryGetValue(dummyName, out subBlockLoadInfo))
                    {
                        MyEntity entity;
                        if (MyEntities.TryGetEntityById(subBlockLoadInfo.GridId, out entity))
                        {
                            subgrid = entity as MyCubeGrid;
                            if (subgrid != null)
                            {
                                subblock = subgrid.GetCubeBlock(subBlockLoadInfo.SubBlockPosition);
                                Debug.Assert(subblock != null, "Cannot find subblock in subgrid!");
                                if (subblock == null)
                                    continue;
                            }
                            else
                            {
                                Debug.Assert(false, "Loaded entity is not grid!");
                                continue;
                            }
                        }
                        else
                        {
                            Debug.Assert(false, "Cannot load subgrid!");
                            continue;
                        }
                    }

                    if (!m_subBlocksLoaded)
                    {
                        if (subgrid == null)
                        {
                            Debug.Assert(!subBlockMatrix.IsMirrored());
                            Matrix subGridWorldMatrix = subBlockMatrix * PositionComp.LocalMatrix * CubeGrid.WorldMatrix;

                            //TODO: Try to find better way how to sync entity ID of subblocks..
                            subgrid = MyCubeBuilder.SpawnDynamicGrid(subBlockDefinition, null, subGridWorldMatrix, EntityId + (SubBlocks.Count * 16) + 1);
                            if (subgrid != null)
                                subblock = subgrid.GetCubeBlock(Vector3I.Zero);
                        }

                        if (subgrid == null)
                        {
                            Debug.Assert(false, "SubGrid has not been set!");
                            continue;
                        }

                        if (subblock == null || subblock.FatBlock == null)
                        {
                            Debug.Assert(false, "Fatblock cannot be null for subblocks!");
                            continue;
                        }
                    }

                    if (subblock != null)
                    {
                        SubBlocks.Add(dummyName, subblock);
                        subblock.FatBlock.SubBlockName = dummyName;
                        subblock.FatBlock.OwnerBlock = SlimBlock;
                        subblock.FatBlock.OnClosing += SubBlock_OnClosing;
                        Debug.Assert(SlimBlock != null);
                    }
                }
            }
            finally
            {
                m_subBlockIds.Clear();

                m_subBlocksInitialized = true;
            }
        }

        protected virtual void OnSubBlockClosing(MySlimBlock subBlock)
        {
            subBlock.FatBlock.OnClosing -= SubBlock_OnClosing;
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

        virtual internal float GetMass()
        {
            Matrix m;
            if (MyDestructionData.Static != null)
                return MyDestructionData.Static.GetBlockMass(SlimBlock.CalculateCurrentModel(out m), BlockDefinition);
            return BlockDefinition.Mass;
        }

        virtual public BoundingBox GetGeometryLocalBox()
        {
            if (Model != null)
                return Model.BoundingBox; //TODO pm: BB is centered on model center not block center

            return new BoundingBox(new Vector3(-CubeGrid.GridSize / 2), new Vector3(CubeGrid.GridSize / 2));
        }

        public DictionaryReader<string, MySlimBlock> GetSubBlocks()
        {
            return new DictionaryReader<string, MySlimBlock>(SubBlocks);
        }

        private MyUpgradableBlockComponent m_upgradeComponent;
        internal MyUpgradableBlockComponent GetComponent()
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
        public event Action OnUpgradeValuesChanged;
        public void CommitUpgradeValues()
        {
            var handler = OnUpgradeValuesChanged;
            if (handler != null)
            {
                handler();
            }
        }
    }
}

