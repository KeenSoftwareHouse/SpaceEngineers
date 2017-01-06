using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Profiler;
using VRage.Sync;
using VRage.Utils;
using VRageMath;
using VRage.Profiler;
#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
using System.Reflection;
using VRage.Reflection;
#endif // XB1

namespace Sandbox.Game.Entities.Blocks
{
    public abstract class MyMechanicalConnectionBlockBase : MyFunctionalBlock
    {
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        protected struct State
#else // XB1
        protected struct State : IMySetGetMemberDataHelper
#endif // XB1
        {
            /// <summary>
            /// <para>No value - detached </para>
            /// <para>0 - try to attach </para>
            /// </summary>
            public long? TopBlockId;
            public MyDeltaTransform? MasterToSlave;
            public bool Welded;

#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
            public object GetMemberData(MemberInfo m)
            {
                if (m.Name == "TopBlockId")
                    return TopBlockId;
                if (m.Name == "MasterToSlave")
                    return MasterToSlave;
                if (m.Name == "Welded")
                    return Welded;

                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
                return null;
            }
#endif // XB1
        }

        protected readonly Sync<State> m_connectionState;

        private Sync<bool> m_forceWeld;

        private Sync<float> m_weldSpeed;
        private float m_weldSpeedSq; //squared
        private float m_unweldSpeedSq; //squared


        //this cannot be TopBlock.CbueGrid because its used to break links on grid split
        private MyCubeGrid m_topGrid; 

        protected MyCubeGrid TopGrid
        {
            get { return m_topGrid; }
        }

        private MyAttachableTopBlockBase m_topBlock;

        protected MyAttachableTopBlockBase TopBlock
        {
            get { return m_topBlock; }
        }

        protected bool m_isWelding { get; private set; }
        protected bool m_welded { get; private set; }
        protected bool m_isAttached { get; private set; }

        protected static List<HkBodyCollision> m_penetrations = new List<HkBodyCollision>();
        protected static HashSet<MySlimBlock> m_tmpSet = new HashSet<MySlimBlock>();

        protected HkConstraint m_constraint;
        private bool m_needReattach;
        private bool m_updateAttach;

        protected event Action<MyMechanicalConnectionBlockBase> AttachedEntityChanged;

        public MyMechanicalConnectionBlockBase()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_connectionState = SyncType.CreateAndAddProp<State>();
            m_topAndBottomSamePhysicsBody = SyncType.CreateAndAddProp<bool>();
            m_forceWeld = SyncType.CreateAndAddProp<bool>();
            m_speedWeld = SyncType.CreateAndAddProp<bool>();
            m_weldSpeedSq = SyncType.CreateAndAddProp<float>();
#endif // XB1

            m_forceWeld.ValueChanged += (o) => OnForceWeldChanged();
            m_connectionState.ValueChanged += (o) => OnAttachTargetChanged();
            m_connectionState.Validate = ValidateTopBlockId;
            m_weldSpeed.ValueChanged += WeldSpeed_ValueChanged;
            CreateTerminalControls();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            m_updateAttach = true;
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyMechanicalConnectionBlockBase>())
                return;
            base.CreateTerminalControls();
            var weldSpeed = new MyTerminalControlSlider<MyMechanicalConnectionBlockBase>("Weld speed", MySpaceTexts.BlockPropertyTitle_WeldSpeed, MySpaceTexts.Blank);
            weldSpeed.SetLimits((block) => 0f, (block) => MyGridPhysics.SmallShipMaxLinearVelocity());
            weldSpeed.DefaultValueGetter = (block) => MyGridPhysics.LargeShipMaxLinearVelocity() - 5f;
            weldSpeed.Getter = (x) => x.m_weldSpeed;
            weldSpeed.Setter = (x, v) => x.m_weldSpeed.Value = v;
            weldSpeed.Writer = (x, res) => res.AppendDecimal((float)Math.Sqrt(x.m_weldSpeedSq), 1).Append("m/s");
            weldSpeed.EnableActions();
            MyTerminalControlFactory.AddControl(weldSpeed);

            var weldForce = new MyTerminalControlCheckbox<MyMechanicalConnectionBlockBase>("Force weld", MySpaceTexts.BlockPropertyTitle_WeldForce, MySpaceTexts.Blank);
            weldForce.Getter = (x) => x.m_forceWeld;
            weldForce.Setter = (x, v) => x.m_forceWeld.Value = v;
            weldForce.EnableAction();
            MyTerminalControlFactory.AddControl(weldForce);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            var ob = objectBuilder as MyObjectBuilder_MechanicalConnectionBlock;
            m_weldSpeed.Value = ob.WeldSpeed;
            m_forceWeld.Value = ob.ForceWeld;

            if (ob.TopBlockId.HasValue && ob.TopBlockId.Value != 0)
            {
                MyDeltaTransform? deltaTransform = ob.MasterToSlaveTransform.HasValue ? ob.MasterToSlaveTransform.Value : (MyDeltaTransform?)null;
                m_connectionState.Value = new State() { TopBlockId = ob.TopBlockId, MasterToSlave = deltaTransform, Welded = ob.IsWelded || ob.ForceWeld };
            }
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_MechanicalConnectionBlock;

            ob.WeldSpeed = m_weldSpeed;
            ob.ForceWeld = m_forceWeld;
            if(!Sync.IsServer) //server has the correct positions
                ob.MasterToSlaveTransform = m_connectionState.Value.MasterToSlave.HasValue ? m_connectionState.Value.MasterToSlave.Value : (MyPositionAndOrientation?)null;

            ob.TopBlockId = m_connectionState.Value.TopBlockId;
            ob.IsWelded = m_connectionState.Value.Welded;

            return ob;
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            Debug.Assert(m_isAttached == (TopBlock != null), "Inconsistency. critical!");
            ProfilerShort.Begin("CheckVelocities()");
            CheckVelocities();
            ProfilerShort.End();
            if (m_updateAttach)
            {
                UpdateAttachState();
            }
            if (m_needReattach)
            {
                Reattach();
            }
        }

        private bool ValidateTopBlockId(State newState)
        {
            if (newState.TopBlockId == null) // Detach allowed always
                return true;
            else if (newState.TopBlockId == 0) // Try attach only valid when detached
                return m_connectionState.Value.TopBlockId == null;
            else // Attach directly now allowed by client
                return false;
        }

        void WeldSpeed_ValueChanged(SyncBase obj)
        {
            m_weldSpeedSq = m_weldSpeed * m_weldSpeed;
            const float safeUnweldSpeedDif = 2;
            m_unweldSpeedSq = Math.Max(m_weldSpeed - safeUnweldSpeedDif, 0);
            m_unweldSpeedSq *= m_unweldSpeedSq;
        }

        private void OnForceWeldChanged()
        {
            if(!m_isAttached)
                return;
            if (Sync.IsServer)
            {
                if (m_forceWeld)
                {
                    if (!m_welded)
                    {
                        WeldGroup(true);
                        UpdateText();
                    }
                }
                else
                {
                    CheckVelocities(); //unwelds if under velocity threshold
                }
            }
            else
            {
                //client does not care - state is synced to server and server will invoke welding
            }
        }

        private void OnAttachTargetChanged()
        {
            m_updateAttach = true;
        }

        /// <summary>
        /// Checks grids linear velocity and welds connection if over threshold
        /// </summary>
        private void CheckVelocities()
        {
            if (m_forceWeld)
                return;
            if (!MyFakes.WELD_ROTORS || Sync.IsServer == false || CubeGrid == null || CubeGrid.Physics == null)
                return;

            var velSq = CubeGrid.Physics.LinearVelocity.LengthSquared();

            if (velSq <= m_unweldSpeedSq && m_welded)
            {
                UnweldGroup();
                CreateConstraint(TopBlock);
            }

            if (m_welded)
            {
                return;
            }

            if (velSq > m_weldSpeedSq && TopGrid == null)
            {
                //TryGetTop();
            }

            if (TopBlock == null || TopGrid == null || TopGrid.Physics == null) //cannot weld
                return;

            if (velSq > m_weldSpeedSq)
            {
                if (m_welded)
                {
                    return;
                }

                if (TopBlock != null && TopGrid != null)
                {
                    WeldGroup(false);
                    return;
                }
                return;
            }
            return;
        }

        /// <summary>
        /// Welds connection, always ends with m_welded == true
        /// </summary>
        /// <param name="force"></param>
        private void WeldGroup(bool force)
        {
            if (!MyFakes.WELD_ROTORS)
                return;
            Debug.Assert(!m_welded, "Welding twice!");
            Debug.Assert(m_isAttached, "Missing attached grid, nothing to weld!");
            Debug.Assert(!m_isWelding, "Already welding!");
            Debug.Assert(MyCubeGridGroups.Static.Physical.LinkExists(EntityId, CubeGrid, TopGrid), "Links must already exist!");
            Debug.Assert(MyCubeGridGroups.Static.Logical.LinkExists(EntityId, CubeGrid, TopGrid), "Links must already exist!");
            
            m_isWelding = true;

            DisposeConstraint();

            MyWeldingGroups.Static.CreateLink(EntityId, CubeGrid, TopGrid);


            if (Sync.IsServer)
            {
                MatrixD masterToSlave = TopBlock.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                m_connectionState.Value = new State() { TopBlockId = TopBlock.EntityId, MasterToSlave = masterToSlave, Welded = true };
            }

            m_welded = true;

            m_isWelding = false;
            RaisePropertiesChanged();
        }

        private void UnweldGroup()
        {
            if (m_welded)
            {
                m_isWelding = true;

                var removed = MyWeldingGroups.Static.BreakLink(EntityId, CubeGrid, m_topGrid);
                Debug.Assert(removed);

                if(Sync.IsServer)
                    m_connectionState.Value = new State() { TopBlockId = TopBlock.EntityId, Welded = false};

                m_welded = false;
                m_isWelding = false;
                RaisePropertiesChanged();
            }
        }

        private void cubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            Debug.Assert(m_isAttached, "Event must not be registered for detached block!");
            cubeGrid_OnPhysicsChanged();

            if (TopGrid == null)
                return;

            if (CubeGrid == null)
                return;

            if (TopGrid.Physics == null)
                return;

            if (CubeGrid.Physics == null)
                return;




            if (MyCubeGridGroups.Static.Logical.GetGroup(TopBlock.CubeGrid) !=
                MyCubeGridGroups.Static.Logical.GetGroup(CubeGrid))
            {
                m_needReattach = true;
                return;
            }

            if (m_constraint != null)
            {
                bool matchingBodies = ((m_constraint.RigidBodyA == CubeGrid.Physics.RigidBody &&
                                        m_constraint.RigidBodyB == TopGrid.Physics.RigidBody) ||
                                       (m_constraint.RigidBodyA == TopGrid.Physics.RigidBody &&
                                        m_constraint.RigidBodyB == CubeGrid.Physics.RigidBody));
                if (!matchingBodies)
                {
                    m_needReattach = true;
                    return;
                }
            }
            
            if (!m_welded)
            {
                m_needReattach = TopGrid.Physics.RigidBody != CubeGrid.Physics.RigidBody;
                return;
            }
        }

        protected virtual void cubeGrid_OnPhysicsChanged() { }

        private void CubeGrid_OnGridSplit(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            Debug.Assert(m_isAttached, "Event must not be registered for detached block!");
            if (grid1 == TopGrid && grid2 == TopBlock.CubeGrid)
            {
                m_needReattach = true;
            }
        }

        void TopBlock_OnClosing(MyEntity obj)
        {
            Detach();
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();

            if (Sync.IsServer)
            {
                if (m_isAttached)
                {
                    m_needReattach = false;
                    var oldState = m_connectionState.Value;
                    Detach();
                    m_connectionState.Value = oldState;
                }
            }
        }

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            m_updateAttach = true;
        }

        private void RaiseAttachedEntityChanged()
        {
            if (AttachedEntityChanged != null)
            {
                AttachedEntityChanged(this);
            }
        }

        //Only overriden, not called from elsewhere
        protected virtual void Detach(bool updateGroups = true)
        {
            Debug.Assert(m_isAttached && TopBlock != null, "Inconsistency! critical!");
            if (m_welded)
            {
                UnweldGroup();
            }

            if (updateGroups)
            {
                m_needReattach = false; //i.e. block could be destroyed after the flag was rised
                
                BreakLinks(TopGrid, TopBlock);

                if (Sync.IsServer)
                {
                    m_connectionState.Value = new State() { TopBlockId = null, Welded = false };
                }
            }

            DisposeConstraint();
     
            if (TopBlock != null)
            {
                TopBlock.Detach(false);
            }


            m_topGrid = null;
            m_topBlock = null;
            m_isAttached = false;

            UpdateText();

            if (updateGroups)
                RaiseAttachedEntityChanged();
        }

        protected abstract void DisposeConstraint();

        protected virtual bool CreateConstraint(MyAttachableTopBlockBase top)
        {
            if (CanAttach(top))
            {
                return !m_welded && CubeGrid.Physics.RigidBody != top.CubeGrid.Physics.RigidBody;
            }
            else
                return false;
        }

        //Only overriden, not called from elsewhere
        protected virtual bool Attach(MyAttachableTopBlockBase topBlock, bool updateGroup = true)
        {
            Debug.Assert(TopBlock == null, "Inconsistency");
            Debug.Assert(!m_welded, "Inconsistency");
            if (topBlock.CubeGrid.Physics == null)
                return false;

            if (CubeGrid.Physics == null || !CubeGrid.Physics.Enabled)
                return false;

            m_topBlock = topBlock;
            m_topGrid = TopBlock.CubeGrid;
            TopBlock.Attach(this);

            if (updateGroup)
            {
                CubeGrid.OnPhysicsChanged += cubeGrid_OnPhysicsChanged;

                TopGrid.OnGridSplit += CubeGrid_OnGridSplit;
                TopGrid.OnPhysicsChanged += cubeGrid_OnPhysicsChanged;
                TopBlock.OnClosing += TopBlock_OnClosing;

                OnConstraintAdded(GridLinkTypeEnum.Physical, TopGrid);
                OnConstraintAdded(GridLinkTypeEnum.Logical, TopGrid);

                RaiseAttachedEntityChanged(); //top is already set we can call here
            }

            if (Sync.IsServer)
            {
                MatrixD masterToSlave = TopBlock.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                m_connectionState.Value = new State() { TopBlockId = TopBlock.EntityId, MasterToSlave = masterToSlave, Welded = m_welded };
            }
            else if (m_connectionState.Value.MasterToSlave.HasValue)
            {
                TopBlock.CubeGrid.WorldMatrix = MatrixD.Multiply(m_connectionState.Value.MasterToSlave.Value, this.WorldMatrix);
            }

            m_isAttached = true; //Welding only allowed when attached

            if (m_forceWeld)
                WeldGroup(true);
            CheckVelocities();

            return true;
        }

        private void FindAndAttachTopServer()
        {
            Debug.Assert(Sync.IsServer, "Server only method");
            MyAttachableTopBlockBase top = FindMatchingTop();
            if (top != null)
            {
                TryAttach(top);
            }
        }

        private void UpdateAttachState()
        {
            m_updateAttach = false;
            m_needReattach = false;
            Debug.Assert(m_isAttached == (TopBlock != null), "Inconsistent state!! Fix critical");

            if (m_connectionState.Value.TopBlockId.HasValue == false) // Detached
            {
                if (m_isAttached)
                {
                    Detach();
                }
            }
            else if (m_connectionState.Value.TopBlockId == 0) // Find anything to attach (only on server)
            {
                if (m_isAttached)
                {
                    Detach();
                }
                //nothing to do for client in this case
                if (Sync.IsServer)
                {
                    FindAndAttachTopServer();
                }
               
            }
            else if (TopBlock == null || TopBlock.EntityId != m_connectionState.Value.TopBlockId)
            {
                long topBlockId = m_connectionState.Value.TopBlockId.Value;
                if (TopBlock != null)
                {
                    Detach();
                }

                MyAttachableTopBlockBase top;
                if (!MyEntities.TryGetEntityById(topBlockId, out top) ||
                    !TryAttach(top))
                {
                    //Top is not replicated or in scene yet
                    if(Sync.IsServer && (top == null || top.MarkedForClose))
                    { //removed from GS or scene and top was destroyed in meantime
                        m_connectionState.Value = new State() { TopBlockId = null, MasterToSlave = null };
                    }
                    else
                        m_updateAttach = true;
                }
            }
            else if (m_welded != m_connectionState.Value.Welded)
            {
                if (m_connectionState.Value.Welded)
                    WeldGroup(true);
                else
                    UnweldGroup();
            }
            RefreshConstraint();
        }

        private bool TryAttach(MyAttachableTopBlockBase top, bool updateGroup = true)
        {
            return (CanAttach(top) && Attach(top, updateGroup));
        }

        private bool CanAttach(MyAttachableTopBlockBase top)
        {
            bool closed = MarkedForClose || CubeGrid.MarkedForClose;
            if(closed)
            {
                return false;
            }

            bool topClosed = top.MarkedForClose || top.CubeGrid.MarkedForClose;

            if(topClosed)
            {
                return false;
            }

            bool notInWorld = CubeGrid.Physics == null ||
                CubeGrid.Physics.RigidBody == null || CubeGrid.Physics.RigidBody.InWorld == false;

            if(notInWorld)
            {
                return false;
            }

            bool topNotInWorld = top.CubeGrid.Physics == null || top.CubeGrid.Physics.RigidBody == null ||
                top.CubeGrid.Physics.RigidBody.InWorld == false;

            if(topNotInWorld)
            {
                return false;
            }

            if(top.CubeGrid.Physics.HavokWorld != CubeGrid.Physics.HavokWorld)
            {
                return false;
            }
            return true;
        }

        private MyAttachableTopBlockBase FindMatchingTop()
        {
            Debug.Assert(CubeGrid != null, "\"Impossible\"");
            Debug.Assert(m_penetrations != null, "\"Impossible\"");
            Debug.Assert(CubeGrid.Physics != null, "\"Impossible\"");

            Quaternion orientation;
            Vector3D pos;
            Vector3 halfExtents;
            ComputeTopQueryBox(out pos, out halfExtents, out orientation);
            try
            {
                MyPhysics.GetPenetrationsBox(ref halfExtents, ref pos, ref orientation, m_penetrations, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                foreach (var obj in m_penetrations)
                {
                    var entity = obj.GetCollisionEntity();
                    if (entity == null || entity == CubeGrid)
                        continue;

                    MyAttachableTopBlockBase top = FindTopInGrid(entity, pos);

                    if (top != null)
                    {
                        return top;
                    }

                    MyPhysicsBody body = entity.Physics as MyPhysicsBody;
                    if (body != null)
                    {
                        foreach (var child in body.WeldInfo.Children)
                        {
                            top = FindTopInGrid(child.Entity, pos);
                            if (top != null)
                            {
                                return top;
                            }
                        }
                    }
                }          
            }
            catch(Exception ex)
            {

            }
            finally
            {
                m_penetrations.Clear();
            }
            return null;
        }

        MyAttachableTopBlockBase FindTopInGrid(IMyEntity entity, Vector3D pos)
        {
            var grid = entity as MyCubeGrid;
            if (grid != null)
            {
                var pos2 = TransformPosition(ref pos);
                var blockPos = grid.RayCastBlocks(pos2, pos2 + WorldMatrix.Up);
                if (blockPos.HasValue)
                {
                    var slimBlock = grid.GetCubeBlock(blockPos.Value);
                    if (slimBlock != null && slimBlock.FatBlock != null)
                    {
                        return slimBlock.FatBlock as MyAttachableTopBlockBase;
                    }
                }
            }

            return null;
        }

        protected virtual Vector3D TransformPosition(ref Vector3D position)
        {
            return position;
        }

        public abstract void ComputeTopQueryBox(out Vector3D pos, out Vector3 halfExtents, out Quaternion orientation);

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);
            if (m_isAttached)
            {
                m_needReattach = false;
                var oldState = m_connectionState.Value;
                Detach();
                m_connectionState.Value = oldState;
            }
        }

        public override void OnBuildSuccess(long builtBy)
        {
            base.OnBuildSuccess(builtBy);

            if (Sync.IsServer)
            {
                CreateTopPartAndAttach(builtBy);
            }
        }

        protected void RecreateTop(long? builderId = null, bool smallToLarge = false) //If smallToLarge is set will add small Top Part Grid to Large CubeGrid. Used in MotorStator
        {
            if (m_isAttached)
            {
                long builder = builderId.HasValue ? builderId.Value : MySession.Static.LocalPlayerId;
                if (builder == MySession.Static.LocalPlayerId)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.HeadAlreadyExists);
                }
                return;
            }

            if (builderId.HasValue)
            {
                MyMultiplayer.RaiseEvent(this, x => x.DoRecreateTop, builderId.Value, smallToLarge);
            }
            else
            {
                MyMultiplayer.RaiseEvent(this, x => x.DoRecreateTop, MySession.Static.LocalPlayerId, smallToLarge);
            }
        }

        [Event, Reliable, Server]
        private void DoRecreateTop(long builderId, bool smallToLarge)
        {
            if (TopBlock != null) return;
            CreateTopPartAndAttach(builderId, smallToLarge);
        }

        private void Reattach()
        {
            m_needReattach = false;
            if (TopBlock == null)
            {
                MyLog.Default.WriteLine("ToBlock null in MechanicalConnection.Reatach");
                m_updateAttach = true;
                return;
            }
            Debug.Assert(m_isAttached);

            //Either bottom or top grid got split/merged into different grid, need to break links and create new ones
            var updateGroup = MyCubeGridGroups.Static.Logical.GetGroup(TopBlock.CubeGrid) != MyCubeGridGroups.Static.Logical.GetGroup(CubeGrid);
            var top = TopBlock;
            var topGrid = TopGrid;
            Detach(updateGroup);

            if (TryAttach(top, updateGroup))
            {
                if (top.CubeGrid.Physics != null)
                {
                    top.CubeGrid.Physics.ForceActivate();
                }
            }
            else
             {
                 if (!updateGroup) //attach failed, need to break links
                 {
                     BreakLinks(topGrid, top);
                     RaiseAttachedEntityChanged();
                 }

                 if (Sync.IsServer)
                 {
                     m_connectionState.Value = new State() {TopBlockId = 0, MasterToSlave = null};
                 }
             }
            Debug.Assert(m_isAttached || !MyCubeGridGroups.Static.Physical.LinkExists(EntityId,CubeGrid,topGrid));
        }

        /// <summary>
        /// Breaks links and unregisters all events
        /// </summary>
        private void BreakLinks(MyCubeGrid topGrid, MyAttachableTopBlockBase topBlock)
        {
            OnConstraintRemoved(GridLinkTypeEnum.Physical, topGrid);
            OnConstraintRemoved(GridLinkTypeEnum.Logical, topGrid);

            if (CubeGrid != null)
                CubeGrid.OnPhysicsChanged -= cubeGrid_OnPhysicsChanged;

            if (topGrid != null)
            {
                topGrid.OnGridSplit -= CubeGrid_OnGridSplit;
                topGrid.OnPhysicsChanged -= cubeGrid_OnPhysicsChanged;
            }

            if (topBlock != null)
            {
                topBlock.OnClosing -= TopBlock_OnClosing;
            }
        }

        private void CreateTopPartAndAttach(long builtBy, bool smallToLarge = false)
        {
            Debug.Assert(Sync.IsServer, "Server only method.");
            Sandbox.Engine.Multiplayer.MyMultiplayer.ReplicateImmediatelly(this, this.CubeGrid);
            MyAttachableTopBlockBase topBlock;
            CreateTopPart(out topBlock, builtBy, MyDefinitionManager.Static.TryGetDefinitionGroup(((MyMechanicalConnectionBlockBaseDefinition)BlockDefinition).TopPart), smallToLarge);
            if (topBlock != null) // No place for top part
            {
                Attach(topBlock);
            }
        }

        protected virtual bool CanPlaceRotor(MyAttachableTopBlockBase rotorBlock, long builtBy)
        {
            return true;
        }

        private void RefreshConstraint()
        {
            if (m_welded)
            {
                if(m_constraint != null)
                {
                    Debug.Fail("Constraint still present on welded block");
                    DisposeConstraint();
                }
                return;
            }
            bool createconstraint = m_constraint == null;
                
            if (m_constraint != null && !m_constraint.InWorld)
            {
                DisposeConstraint();
                createconstraint = true;
            }

            if (createconstraint && TopBlock != null)
            {
                CreateConstraint(TopBlock);
            }
        }

        private void CreateTopPart(out MyAttachableTopBlockBase topBlock, long builtBy, MyCubeBlockDefinitionGroup topGroup, bool smallToLarge)
        {
            Debug.Assert(Sync.IsServer, "Server only method.");
            if (topGroup == null)
            {
                topBlock = null;
                return;
            }

            var gridSize = CubeGrid.GridSizeEnum;
            if (smallToLarge && gridSize == MyCubeSize.Large)   //If we have pressed the Attach Small Rotor Head button on large grid take the small grid definition from pair
            {
                gridSize = MyCubeSize.Small;
            }
            var matrix = GetTopGridMatrix();

            var definition = topGroup[gridSize];
            Debug.Assert(definition != null);

            var block = MyCubeGrid.CreateBlockObjectBuilder(definition, Vector3I.Zero, MyBlockOrientation.Identity, MyEntityIdentifier.AllocateId(), OwnerId, fullyBuilt: MySession.Static.CreativeMode);
            matrix.Translation = Vector3D.Transform(-definition.Center * CubeGrid.GridSize, matrix);

            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridBuilder.GridSizeEnum = gridSize;
            gridBuilder.IsStatic = false;
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(matrix);
            gridBuilder.CubeBlocks.Add(block);

            var grid = MyEntityFactory.CreateEntity<MyCubeGrid>(gridBuilder);
            grid.Init(gridBuilder);

            topBlock = (MyAttachableTopBlockBase)grid.GetCubeBlock(Vector3I.Zero).FatBlock;

            if (!CanPlaceTop(topBlock, builtBy))
            {
                topBlock = null;
                grid.Close();
                return;
            }
            grid.PositionComp.SetPosition(grid.WorldMatrix.Translation - (Vector3D.Transform(topBlock.DummyPosLoc, grid.WorldMatrix) - grid.WorldMatrix.Translation));

            MyEntities.Add(grid);
            if (MyFakes.ENABLE_SENT_GROUP_AT_ONCE)
            {
                MyMultiplayer.ReplicateImmediatelly(MyExternalReplicable.FindByObject(grid), MyExternalReplicable.FindByObject(CubeGrid));
            }

            MatrixD masterToSlave = topBlock.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
            m_connectionState.Value = new State() { TopBlockId = topBlock.EntityId, MasterToSlave = masterToSlave};
        }

        protected abstract MatrixD GetTopGridMatrix();

        protected virtual bool CanPlaceTop(MyAttachableTopBlockBase topBlock, long builtBy)
        {
            return true;
        }

        protected virtual void UpdateText()
        {

        }

        public MyStringId GetAttachState()
        {
            if (m_welded || m_isWelding)
            {
                return MySpaceTexts.BlockPropertiesText_MotorLocked;
            }
            else if (m_connectionState.Value.TopBlockId == null)
                return MySpaceTexts.BlockPropertiesText_MotorDetached;
            else if (m_connectionState.Value.TopBlockId.Value == 0)
                return MySpaceTexts.BlockPropertiesText_MotorAttachingAny;
            else if (m_isAttached)
                return MySpaceTexts.BlockPropertiesText_MotorAttached;
            else
                return MySpaceTexts.BlockPropertiesText_MotorAttachingSpecific;
        }

        protected HkConstraint SafeConstraint
        {
            get { RefreshConstraint(); return m_constraint; }
        }
    }
}
