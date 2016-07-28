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
using VRage;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
using System.Reflection;
using VRage.Reflection;
#endif // XB1

namespace Sandbox.Game.Entities.Blocks
{
    abstract public class MyMechanicalConnectionBlockBase : MyFunctionalBlock
    {
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        protected struct State
#else // XB1
        protected struct State : IMySetGetMemberDataHelper
#endif // XB1
        {
            public long? TopBlockId;
            public MyDeltaTransform? MasterToSlave;
            public bool Force;
            public bool Welded;

#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
            public object GetMemberData(MemberInfo m)
            {
                if (m.Name == "TopBlockId")
                    return TopBlockId;
                if (m.Name == "MasterToSlave")
                    return MasterToSlave;
                if (m.Name == "Force")
                    return Force;
                if (m.Name == "Welded")
                    return Welded;

                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
                return null;
            }
#endif // XB1
        }

        protected readonly Sync<State> m_connectionState;
        protected readonly Sync<bool> m_topAndBottomSamePhysicsBody;

        protected MyCubeGrid m_topGrid;
        protected MyAttachableTopBlockBase m_topBlock;

        MyAttachableTopBlockBase m_topBlockToReattach;

        protected bool m_isWelding = false;
        protected Sync<bool> m_forceWeld;
        protected Sync<bool> m_speedWeld;

        protected bool m_welded = false;

        protected Sync<float> m_weldSpeedSq; //squared

        protected bool m_isAttached = false;
        bool m_forceApply = false;
        protected static List<HkBodyCollision> m_penetrations = new List<HkBodyCollision>();
        protected static HashSet<MySlimBlock> m_tmpSet = new HashSet<MySlimBlock>();

        protected HkConstraint m_constraint;

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
            m_topAndBottomSamePhysicsBody.ValidateNever();

            m_speedWeld.Value = false;
            CreateTerminalControls();     
        }

        static void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyMechanicalConnectionBlockBase>())
                return;

            var weldSpeed = new MyTerminalControlSlider<MyMechanicalConnectionBlockBase>("Weld speed", MySpaceTexts.BlockPropertyTitle_WeldSpeed, MySpaceTexts.Blank);
            weldSpeed.SetLimits((block) => 0f, (block) => MyGridPhysics.SmallShipMaxLinearVelocity());
            weldSpeed.DefaultValueGetter = (block) => MyGridPhysics.LargeShipMaxLinearVelocity() - 5f;
            weldSpeed.Getter = (x) => (float)Math.Sqrt(x.m_weldSpeedSq);
            weldSpeed.Setter = (x, v) => x.m_weldSpeedSq.Value = v * v;
            weldSpeed.Writer = (x, res) => res.AppendDecimal((float)Math.Sqrt(x.m_weldSpeedSq), 1).Append("m/s");
            weldSpeed.EnableActions();
            MyTerminalControlFactory.AddControl(weldSpeed);

            var weldForce = new MyTerminalControlCheckbox<MyMechanicalConnectionBlockBase>("Force weld", MySpaceTexts.BlockPropertyTitle_WeldForce, MySpaceTexts.Blank);
            weldForce.Getter = (x) => x.m_forceWeld;
            weldForce.Setter = (x, v) => x.m_forceWeld.Value = v;
            weldForce.EnableAction();
            MyTerminalControlFactory.AddControl(weldForce);
        }

        bool ValidateTopBlockId(State newState)
        {
            if (newState.TopBlockId == null) // Detach allowed always
                return true;
            else if (newState.TopBlockId == 0) // Try attach only valid when detached
                return m_connectionState.Value.TopBlockId == null;
            else // Attach directly now allowed by client
                return false;
        }

        void OnForceWeldChanged()
        {
            if(Sync.IsServer == false)
            {
                m_forceApply = m_connectionState.Value.Force;
                return;
            }

            if (m_forceWeld.Value)
            {
                if (m_welded == false && m_topBlock != null)
                {
                    WeldGroup(true);
                    UpdateText();
                }
            }
            else if (m_welded && m_speedWeld == false)
            {
                UnweldGroup();
                m_forceApply = true;
                TryAttach();
                UpdateText();
            }
        }

        void OnAttachTargetChanged()
        {
            UpdateText();
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            if (Sync.IsServer == false)
            {
                m_forceApply = m_connectionState.Value.Force;
            }
        }

        protected bool CheckVelocities()
        {              
            if (!MyFakes.WELD_ROTORS || Sync.IsServer == false || m_forceWeld || CubeGrid == null || CubeGrid.Physics == null)
                return false;

            var velSq = CubeGrid.Physics.LinearVelocity.LengthSquared();

            const float safeUnweldSpeedDif = 2 * 2;
            if (velSq < m_weldSpeedSq - safeUnweldSpeedDif && m_welded)
            {
                m_speedWeld.Value = false;
                UnweldGroup();
                m_forceApply = true;
                TryAttach();
            }

            if (m_welded)
            {
                return true;
            }

            if (velSq > m_weldSpeedSq && m_topGrid == null)
            {
                TryGetTop();
            }

            if (m_topBlock == null || m_topGrid == null || m_topGrid.Physics == null)
                return false;

            if (velSq > m_weldSpeedSq)
            {
                if (m_welded)
                {
                    return true;
                }

                if (m_topBlock != null && m_topGrid != null)
                {
                    m_speedWeld.Value = true;
                    WeldGroup(false);
                    return true;
                }
                return false;
            }
            return false;
        }

        private void TryGetTop()
        {
            if (m_connectionState.Value.TopBlockId.HasValue)
            {
                if (MyEntities.TryGetEntityById<MyAttachableTopBlockBase>(m_connectionState.Value.TopBlockId.Value, out m_topBlock))
                {
                    m_topGrid = m_topBlock.CubeGrid;
                    if (m_topGrid != null)
                    {
                        if (m_connectionState.Value.MasterToSlave.HasValue)
                        {
                            m_topGrid.WorldMatrix = MatrixD.Multiply(m_connectionState.Value.MasterToSlave.Value, this.WorldMatrix);
                        }
                    }
                }
            }
        }

        private void WeldGroup(bool force)
        {
            var topBlock = m_topBlock;
            MyCubeGrid topGrid = m_topBlock.CubeGrid;

            m_isWelding = true;

            if (m_isAttached)
            {
                Detach(false);
            }

            MyWeldingGroups.Static.CreateLink(EntityId, CubeGrid, topGrid);

            if (MyCubeGridGroups.Static.GetGroups(GridLinkTypeEnum.Physical).LinkExists(EntityId, CubeGrid, topGrid) == false)
            {
                OnConstraintAdded(GridLinkTypeEnum.Physical, topGrid);
            }

            if (MyCubeGridGroups.Static.GetGroups(GridLinkTypeEnum.Logical).LinkExists(EntityId, CubeGrid, topGrid) == false)
            {
                OnConstraintAdded(GridLinkTypeEnum.Logical, topGrid);
            }

            if (Sync.IsServer)
            {
                MatrixD masterToSlave = topBlock.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                m_connectionState.Value = new State() { TopBlockId = topBlock.EntityId, MasterToSlave = masterToSlave, Welded = true,Force = force};
               
            }

            topBlock.Attach(this);
            topBlock.CubeGrid.OnPhysicsChanged += cubeGrid_OnPhysicsChanged;

            m_welded = true;
            m_topGrid = topGrid;
            m_topBlock = topBlock;

            m_isWelding = false;
            RaisePropertiesChanged();
        }

        private void UnweldGroup()
        {
            if (m_welded)
            {
                m_isWelding = true;
                var topGrid = m_topBlock.CubeGrid;

                MyWeldingGroups.Static.BreakLink(EntityId, CubeGrid, topGrid);

                if (MyCubeGridGroups.Static.GetGroups(GridLinkTypeEnum.Physical).LinkExists(EntityId, CubeGrid, topGrid))
                {
                    OnConstraintRemoved(GridLinkTypeEnum.Physical, topGrid);
                }

                if (MyCubeGridGroups.Static.GetGroups(GridLinkTypeEnum.Logical).LinkExists(EntityId, CubeGrid, topGrid))
                {
                    OnConstraintRemoved(GridLinkTypeEnum.Logical, topGrid);
                }

                CustomUnweld();
                m_welded = false;
                m_isWelding = false;
                RaisePropertiesChanged();
            }
        }

        protected void CubeGrid_OnGridSplit(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            OnGridSplit();
        }

        public void OnGridSplit()
        {
            m_topAndBottomSamePhysicsBody.Value = false;
            if (m_welded && m_isWelding == false)
            {
                UnweldGroup();
                WeldGroup(true);
            }
            else
            {
                Reattach(true);
            }
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();

            if (Sync.IsServer)
            {
                if (m_topBlock != null)
                {
                    m_topBlockToReattach = m_topBlock;
                    Detach(true);
                }
                CubeGrid.OnGridSplit -= CubeGrid_OnGridSplit;
            }
        }

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            if (Sync.IsServer)
            {
                CubeGrid.OnGridSplit += CubeGrid_OnGridSplit;

                if (CubeGrid.Physics != null)
                {
                    TryAttach();
                }
            }
        }

        protected void TryWeldServer()
        {
            if(m_topBlockToReattach != null && m_forceWeld)
            {
                m_topBlock = m_topBlockToReattach;
                m_topGrid = m_topBlock.CubeGrid;
                m_topBlockToReattach = null;
            }

            if (m_topBlock == null && (m_connectionState.Value.TopBlockId.HasValue))
            {
                TryGetTop();
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                return;
            }

            if (m_topBlock != null)
            {
                if (m_forceWeld || m_speedWeld)
                {
                    if (m_welded == false)
                    {
                        WeldGroup(false);
                        UpdateText();
                    }
                }
                else if (m_welded)
                {
                    UnweldGroup();
                    m_forceApply = true;
                    TryAttach();
                    UpdateText();
                }
            }

        }

        private void TryWeldClient()
        {
            if (m_forceApply && m_welded)
            {
                UnweldGroup();
                UpdateText();
            }

            if (m_connectionState.Value.Welded == false)
            {
                if (m_welded)
                {
                    UnweldGroup();
                    UpdateText();
                }
            }
            else if (m_connectionState.Value.Welded == true && (m_welded == false || m_topBlock == null || m_topBlock.EntityId != m_connectionState.Value.TopBlockId))
            {
                if (m_topBlock == null)
                {
                    TryGetTop();
                    NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    return;
                }

                if (m_topBlock != null)
                {
                    WeldGroup(false);
                    m_forceApply = false;
                    UpdateText();
                }
                else
                {
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                }
            }
        }

        protected void TryWeld()
        {
            if (Sync.IsServer)
            {
                TryWeldServer();
            }
            else
            {
                TryWeldClient();
            }
        }

        public void SyncDetach()
        {
            m_connectionState.Value = new State() { TopBlockId = 0};
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public virtual bool Detach(bool updateGroup = true)
        {
            var tmptopGrid = m_topBlock == null ? null: m_topBlock.CubeGrid;
            if (m_connectionState.Value.Welded || m_welded)
            {
                UnweldGroup();
            }
            else
            {
                if (updateGroup && tmptopGrid != null && (m_connectionState.Value.Welded == false || m_isWelding == false))
                {
                    OnConstraintRemoved(GridLinkTypeEnum.Physical, tmptopGrid);
                    OnConstraintRemoved(GridLinkTypeEnum.Logical, tmptopGrid);
                }
            }

            //Debug.Assert(m_topGrid != null);

            DisposeConstraint();
     
            if (m_topBlock != null)
            {
                m_topBlock.Detach(m_connectionState.Value.Welded || m_isWelding);         
 
                if(updateGroup)
                {
                    tmptopGrid.OnPhysicsChanged -= cubeGrid_OnPhysicsChanged;     
                    if (Sync.IsServer)
                    {                       
                        m_connectionState.Value = new State() { TopBlockId = 0, Welded = false };
                    }
                }
            }

           
            m_topGrid = null;
            m_topBlock = null;
            m_isAttached = false;

            UpdateText();
            return true;
        }

        protected abstract void DisposeConstraint();

        protected virtual bool Attach(MyAttachableTopBlockBase topBlock, bool updateGroup = true)
        {
            if (topBlock.CubeGrid.Physics == null)
                return false;

            if (CubeGrid.Physics != null && CubeGrid.Physics.Enabled)
            {
                m_topBlock = topBlock;
                m_topGrid = m_topBlock.CubeGrid;

                if (updateGroup)
                {
                    OnConstraintAdded(GridLinkTypeEnum.Physical, m_topGrid);
                    OnConstraintAdded(GridLinkTypeEnum.Logical, m_topGrid);
                    m_topGrid.OnPhysicsChanged += cubeGrid_OnPhysicsChanged;
                }

                if (CubeGrid.Physics.RigidBody == m_topGrid.Physics.RigidBody)
                {                
                    if (m_welded)
                    {
                        m_isAttached = true;
                        return true;
                    }
                    else 
                    {
                        m_topAndBottomSamePhysicsBody.Value = true;
                        m_isAttached = false;
                        return false;
                    }
                }
                else
                {
                    m_topAndBottomSamePhysicsBody.Value = false;
                }


                if (m_connectionState.Value.MasterToSlave.HasValue)
                {
                    m_topBlock.CubeGrid.WorldMatrix = MatrixD.Multiply(m_connectionState.Value.MasterToSlave.Value, this.WorldMatrix);
                }

                return true;
            }
            return m_welded;
        }

        private void FindTopServer(MyEntityUpdateEnum updateFlags)
        {
            MyAttachableTopBlockBase top = null;
            if (m_topBlockToReattach != null)
            {
                top = m_topBlockToReattach;
            }
            else
            {
                top = FindMatchingTop();
            }

            if (top != null)
            {
                if (CanDetach())
                {
                    Detach();
                }

                if (TryAttach(top))
                {
                    m_topBlockToReattach = null;
                }
                else
                {
                    updateFlags |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                }

            }
            else
            {
                updateFlags |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        protected void TryAttach()
        {        
            var updateFlags = NeedsUpdate;
            updateFlags &= ~MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            updateFlags &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;

            if(m_welded || m_forceWeld || m_speedWeld)
            {
                if (m_welded)
                {
                    NeedsUpdate = updateFlags;
                }
                return;
            }

            if (m_connectionState.Value.TopBlockId.HasValue == false) // Detached
            {
                if (CanDetach())
                {
                    Detach();
                }
            }
            else if (m_connectionState.Value.TopBlockId == 0) // Find anything to attach (only on server)
            {
                //nothing to do for client in this case
                if (Sync.IsServer)
                {
                    FindTopServer(updateFlags);
                }
               
            }
            else if (m_topAndBottomSamePhysicsBody == false && m_connectionState.Value.TopBlockId.HasValue &&
                    (m_isAttached == false || m_forceApply || m_topBlock == null || m_topBlock.EntityId != m_connectionState.Value.TopBlockId))
            {

                long topBlockId = m_connectionState.Value.TopBlockId.Value;
                if (m_topBlock != null && m_isAttached)
                {
                    Detach();
                }

                bool attached = false;
                MyAttachableTopBlockBase top;
                if (MyEntities.TryGetEntityById<MyAttachableTopBlockBase>(topBlockId, out top))
                {
                    if (TryAttach(top))
                    {
                        attached = true;
                        m_forceApply = false;
                    }
                }

                if (attached == false && m_topAndBottomSamePhysicsBody == false)
                {
                    // top not found by EntityId or not in scene, try again in 10 frames
                    updateFlags |= MyEntityUpdateEnum.EACH_10TH_FRAME;

                    if (m_forceApply)
                    {
                        updateFlags |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    }
                }
            }
            NeedsUpdate = updateFlags;
            RefreshConstraint();
        }

        private bool TryAttach(MyAttachableTopBlockBase top)
        {
            bool attached = false;
            if (CanAttach(top))
            {
                attached = Attach(top);
                if ((attached || m_topAndBottomSamePhysicsBody) && Sync.IsServer)
                {
                    MatrixD masterToSlave = top.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                    m_connectionState.Value = new State() { TopBlockId = top.EntityId, MasterToSlave = masterToSlave, Welded = m_welded};
                }
            }
            return attached;
        }

        bool CanAttach(MyAttachableTopBlockBase top)
        {
            bool closed = MarkedForClose || Closed || CubeGrid.MarkedForClose || CubeGrid.Closed;
            if(closed)
            {
                return false;
            }

            bool topClosed = top.MarkedForClose || top.Closed || top.CubeGrid.MarkedForClose || top.CubeGrid.Closed;

            if(topClosed)
            {
                return false;
            }

            bool notInWorld = CubeGrid.Physics == null ||
                CubeGrid.Physics.RigidBody == null || CubeGrid.Physics.RigidBody.InWorld == false || CubeGrid.Physics.RigidBody.IsAddedToWorld == false;

            if(notInWorld)
            {
                return false;
            }

            bool topNotInWorld = top.CubeGrid.Physics == null || top.CubeGrid.Physics.RigidBody == null ||
                top.CubeGrid.Physics.RigidBody.InWorld == false || top.CubeGrid.Physics.RigidBody.IsAddedToWorld == false;

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

        protected MyAttachableTopBlockBase FindMatchingTop()
        {
            Debug.Assert(CubeGrid != null);
            Debug.Assert(m_penetrations != null);
            Debug.Assert(CubeGrid.Physics != null);
            if (CubeGrid == null)
            {
                MySandboxGame.Log.WriteLine("MyPistonBase.FindMatchingTop(): Cube grid == null!");
                return null;
            }

            if (m_penetrations == null)
            {
                MySandboxGame.Log.WriteLine("MyPistonBase.FindMatchingTop(): penetrations cache == null!");
                return null;
            }

            if (CubeGrid.Physics == null)
            {
                MySandboxGame.Log.WriteLine("MyPistonBase.FindMatchingTop(): Cube grid physics == null!");
                return null;
            }

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
                // Rotor should always be on position [0,0,0];
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
            m_topBlockToReattach = m_topBlock;
            Detach(true);
            CubeGrid.OnPhysicsChanged -= cubeGrid_OnPhysicsChanged;
        }

        protected override void Closing()
        {
            CubeGrid.OnPhysicsChanged -= cubeGrid_OnPhysicsChanged;
            Detach();
            m_topBlockToReattach = null;
            base.Closing();
        }

        protected void cubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            cubeGrid_OnPhysicsChanged();
            if (Sync.IsServer)
            {
                m_topAndBottomSamePhysicsBody.Value = false;
                if (m_welded == false && m_isWelding == false)
                {
                    Reattach();
                }
            }
        }

        protected virtual void cubeGrid_OnPhysicsChanged() { }

        public void Reattach(bool force = false)
        {
            if (m_isWelding || m_welded || m_topBlock == null || m_topBlock.Closed)
            {
                return;
            }

            var top = m_topBlock;
            bool detached = Detach(force);

            if (Sync.IsServer)
            {
                m_connectionState.Value = new State() { TopBlockId = 0, MasterToSlave = null };
            }

            if (CanAttach(top))
            {
                m_topBlock = top;
                m_topGrid = top.CubeGrid;

                bool attached = Attach(top, force);

                if (attached && Sync.IsServer)
                {
                    MatrixD masterToSlave = top.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                    m_connectionState.Value = new State() { TopBlockId = top.EntityId, MasterToSlave = masterToSlave, Force = force };
                }

                //Debug.Assert(detached && attached);
                if (!top.MarkedForClose && top.CubeGrid.Physics != null)
                {
                    top.CubeGrid.Physics.ForceActivate();
                }
            }
            else
            {
                if (MyCubeGridGroups.Static.GetGroups(GridLinkTypeEnum.Physical).LinkExists(EntityId, CubeGrid, top.CubeGrid))
                {
                    OnConstraintRemoved(GridLinkTypeEnum.Physical, top.CubeGrid);
                }

                if (MyCubeGridGroups.Static.GetGroups(GridLinkTypeEnum.Logical).LinkExists(EntityId, CubeGrid, top.CubeGrid))
                {
                    OnConstraintRemoved(GridLinkTypeEnum.Logical, top.CubeGrid);
                }

                if (Sync.IsServer && top != null)
                {
                    top.Attach(this);
                }
            }
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        protected void CreateTopGrid(long builtBy)
        {
            Sandbox.Engine.Multiplayer.MyMultiplayer.ReplicateImmediatelly(this, this.CubeGrid);
            CreateTopGrid(out m_topGrid, out m_topBlock, builtBy);
            if (m_topBlock != null) // No place for top part
            {
                MatrixD masterToSlave = m_topBlock.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                m_connectionState.Value = new State() { TopBlockId = m_topBlock.EntityId, MasterToSlave = masterToSlave };
                Attach(m_topBlock);
           }
        }

        public void ReattachTop(MyAttachableTopBlockBase top)
        {
            if (m_topBlock != null || m_welded)
            {
                Detach();
            }

            m_topBlockToReattach = top;
            // Rotor not found by EntityId or not in scene, try again in 10 frames
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

        }

        protected void RefreshConstraint()
        {
            if (m_constraint != null && !m_constraint.InWorld)
            {
                var oldState = m_connectionState.Value;
                Detach();
                m_connectionState.Value = oldState;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
              
            }
        }

        protected bool CanDetach()
        {
            return m_constraint != null;
        }

        protected abstract void CreateTopGrid(out MyCubeGrid topGrid, out MyAttachableTopBlockBase topBlock, long ownerId);

        protected virtual void UpdateText()
        {

        }

        public MyStringId GetAttachState()
        {
            if ((m_welded || m_isWelding) && SafeConstraint == null)
            {
                return MySpaceTexts.BlockPropertiesText_MotorLocked;
            }
            else if (m_topAndBottomSamePhysicsBody.Value)
            {
                return MySpaceTexts.BlockPropertiesText_MotorAttached;
            }
            else if (m_connectionState.Value.TopBlockId == null)
                return MySpaceTexts.BlockPropertiesText_MotorDetached;
            else if (m_connectionState.Value.TopBlockId.Value == 0)
                return MySpaceTexts.BlockPropertiesText_MotorAttachingAny;
            else if (SafeConstraint != null)
                return MySpaceTexts.BlockPropertiesText_MotorAttached;
            else
                return MySpaceTexts.BlockPropertiesText_MotorAttachingSpecific;
        }

        protected virtual void CustomUnweld()
        { 
        }

        protected HkConstraint SafeConstraint
        {
            get { RefreshConstraint(); return m_constraint; }
        }
    }
}
