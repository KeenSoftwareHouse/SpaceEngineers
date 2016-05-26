using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
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
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    abstract public class MyMechanicalConnectionBlockBase : MyFunctionalBlock
    {
        protected struct State
        {
            public long? TopBlockId;
            public MyDeltaTransform? MasterToSlave;
            public bool Force;
        }

        protected readonly Sync<long?> m_weldedEntityId;
        protected readonly Sync<State> m_connectionState;

        protected MyCubeGrid m_topGrid;
        protected MyAttachableTopBlockBase m_topBlock;

        protected bool m_welded = false;
        protected long? m_weldedTopBlockId;
        protected bool m_isWelding = false;
        protected Sync<bool> m_forceWeld;

        protected Sync<float> m_weldSpeedSq; //squared

        protected bool m_isAttached = false;

        protected static List<HkBodyCollision> m_penetrations = new List<HkBodyCollision>();
        protected static HashSet<MySlimBlock> m_tmpSet = new HashSet<MySlimBlock>();

        public MyMechanicalConnectionBlockBase()
        {
            m_weldedEntityId.ValidateNever();
            m_weldedEntityId.ValueChanged += (o) => OnWeldedEntityIdChanged();
            m_forceWeld.ValueChanged += (o) => OnWeldedEntityIdChanged();
            m_connectionState.ValueChanged += (o) => OnAttachTargetChanged();
            m_connectionState.Validate = ValidateTopBlockId;
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

        void OnWeldedEntityIdChanged()
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        void OnAttachTargetChanged()
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            UpdateText();
        }

        protected bool CheckVelocities()
        {
            if (m_forceWeld && m_topBlock == null && m_weldedEntityId.Value.HasValue)
            {
                if (MyEntities.TryGetEntityById<MyAttachableTopBlockBase>(m_weldedEntityId.Value.Value, out m_topBlock))
                {
                    m_topGrid = m_topBlock.CubeGrid;
                }
            }

            if (!MyFakes.WELD_ROTORS || Sync.IsServer == false)
                return false;

            if (m_topBlock == null || m_topGrid == null || m_topGrid.Physics == null)
                return false;

            var velSq = CubeGrid.Physics.LinearVelocity.LengthSquared();
            if (m_forceWeld || velSq > m_weldSpeedSq)
            {
                if (m_welded)
                {
                    return true;
                }

                if (m_topBlock != null && m_topGrid != null && MyWeldingGroups.Static.GetGroup(CubeGrid) != MyWeldingGroups.Static.GetGroup(m_topGrid))
                {
                    WeldGroup();
                    return true;
                }
                return false;
            }

            const float safeUnweldSpeedDif = 2 * 2;
            if (!m_forceWeld && velSq < m_weldSpeedSq - safeUnweldSpeedDif && m_welded)
            {
                UnweldGroup();
            }
            if (m_welded)
                return true;
            return false;
        }

        private void WeldGroup()
        {
            var topGrid = m_topGrid;
            var topBlock = m_topBlock;

            m_isWelding = true;

            m_weldedTopBlockId = m_topBlock.EntityId;

            Detach(false);
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

                m_connectionState.Value = new State() { TopBlockId = m_weldedTopBlockId, MasterToSlave = masterToSlave };
                m_weldedEntityId.Value = m_weldedTopBlockId;
            }

            m_topGrid = topGrid;
            m_topBlock = topBlock;
            m_welded = true;

            m_isWelding = false;
            RaisePropertiesChanged();
        }

        private void UnweldGroup()
        {
            if (m_welded)
            {
                m_isWelding = true;

                if (Sync.IsServer)
                {
                    m_weldedEntityId.Value = null;
                }

                m_weldedTopBlockId = null;

                MyWeldingGroups.Static.BreakLink(EntityId, CubeGrid, m_topGrid);


                if (MyCubeGridGroups.Static.GetGroups(GridLinkTypeEnum.Physical).LinkExists(EntityId, CubeGrid, m_topGrid))
                {
                    OnConstraintRemoved(GridLinkTypeEnum.Physical, m_topGrid);
                }

                if (MyCubeGridGroups.Static.GetGroups(GridLinkTypeEnum.Logical).LinkExists(EntityId, CubeGrid, m_topGrid))
                {
                    OnConstraintRemoved(GridLinkTypeEnum.Logical, m_topGrid);
                }

                if (CanAttach(m_topBlock))
                {
                    Attach(m_topBlock, false);
                }

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
            if (m_welded && m_isWelding == false)
            {
                UnweldGroup();
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
                CubeGrid.OnGridSplit -= CubeGrid_OnGridSplit;
            }
            Detach(true);
        }

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            if (Sync.IsServer)
            {
                CubeGrid.OnGridSplit += CubeGrid_OnGridSplit;
            }
            TryAttach();
        }

        protected void TryWeld()
        {
            if (m_weldedEntityId.Value == null)
            {
                if (m_welded)
                {
                    UnweldGroup();
                }
            }
            else if (m_weldedEntityId.Value != m_weldedTopBlockId && m_topBlock != null)
            {
                WeldGroup();
            }
        }

        public void SyncDetach()
        {
            m_connectionState.Value = new State() { TopBlockId = 0, MasterToSlave = null };
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public virtual bool Detach(bool updateGroup = true)
        {
            if (m_isWelding == false && m_welded)
            {
                UnweldGroup();
            }

            var tmptopGrid = m_topGrid;
            if (updateGroup && m_topGrid != null && (m_welded == false || m_isWelding == false))
            {
                OnConstraintRemoved(GridLinkTypeEnum.Physical, tmptopGrid);
                OnConstraintRemoved(GridLinkTypeEnum.Logical, tmptopGrid);
            }

            //Debug.Assert(m_topGrid != null);

            DisposeConstraint();
     
            if (m_topBlock != null)
            {
                m_topBlock.Detach(m_welded || m_isWelding);          
            }

            if (updateGroup && tmptopGrid != null)
            {
                tmptopGrid.OnPhysicsChanged -= cubeGrid_OnPhysicsChanged;
            }

            if (updateGroup && Sync.IsServer)
            {
                m_connectionState.Value = new State() { TopBlockId = 0};
            }

            m_topGrid = null;
            m_topBlock = null;
            m_isAttached = false;

            UpdateText();
            return true;
        }

        protected abstract void DisposeConstraint();

        protected abstract bool Attach(MyAttachableTopBlockBase rotor, bool updateGroup = true);

        protected void TryAttach()
        {
            if (!CubeGrid.InScene || CubeGrid.Physics == null || CubeGrid.Physics.RigidBody == null || CubeGrid.Physics.RigidBody.InWorld == false)
                return;
            var updateFlags = NeedsUpdate;
            updateFlags &= ~MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            updateFlags &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;

            if (m_connectionState.Value.TopBlockId.HasValue == false) // Detached
            {
                if (CanDetach())
                {
                    Detach();
                }
            }
            else if (m_connectionState.Value.TopBlockId == 0) // Find anything to attach (only on server)
            {
                if (Sync.IsServer)
                {
                    var top = FindMatchingTop();
                    if (top != null)
                    {
                        if (CanDetach())
                        {
                            Detach();
                        }

                        if (CanAttach(top))
                        {
                            MatrixD masterToSlave = top.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                            m_connectionState.Value = new State() { TopBlockId = top.EntityId, MasterToSlave = masterToSlave };
                            Attach(top);
                        }
                    }
                    else
                    {
                        updateFlags |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                    }
                }
            }
            else if (m_connectionState.Value.TopBlockId.HasValue && ((false == m_welded && (m_connectionState.Value.Force || m_topBlock == null || m_topBlock.EntityId != m_connectionState.Value.TopBlockId)) ||
                     (m_welded && m_weldedTopBlockId != m_connectionState.Value.TopBlockId))) // Attached to something else or nothing
            {
                if (m_topBlock != null || m_welded)
                {
                    Detach();
                }

                MyAttachableTopBlockBase top;
                bool attached = false;
                if (MyEntities.TryGetEntityById<MyAttachableTopBlockBase>(m_connectionState.Value.TopBlockId.Value, out top) && !top.MarkedForClose && top.CubeGrid.InScene && top.CubeGrid.Physics.IsInWorld)
                {
                    if (Sync.IsServer == false)
                    {
                        if (m_connectionState.Value.MasterToSlave.HasValue)
                        {
                            top.CubeGrid.WorldMatrix = MatrixD.Multiply(m_connectionState.Value.MasterToSlave.Value, this.WorldMatrix);
                        }
                    }
                    else
                    {
                        MatrixD masterToSlave = top.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                        m_connectionState.Value = new State() { TopBlockId = top.EntityId, MasterToSlave = masterToSlave };
                    }
                    if (CanAttach(top))
                    {
                        attached = Attach(top);
                    }
                }

                if (!attached)
                {
                    // Rotor not found by EntityId or not in scene, try again in 10 frames
                    updateFlags |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                }
            }
            NeedsUpdate = updateFlags;
            RefreshConstraint();
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
                CubeGrid.Physics.RigidBody == null || CubeGrid.Physics.RigidBody.InWorld == false;

            if(notInWorld)
            {
                return false;
            }

            bool topNotInWorld = top.CubeGrid.Physics == null || top.CubeGrid.Physics.RigidBody == null ||
                CubeGrid.Physics.RigidBody.InWorld == false;

            if(topNotInWorld)
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
            Detach(true);
            CubeGrid.OnPhysicsChanged -= cubeGrid_OnPhysicsChanged;
        }

        protected override void Closing()
        {
            CubeGrid.OnPhysicsChanged -= cubeGrid_OnPhysicsChanged;
            Detach();
            base.Closing();
        }

        protected void cubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            cubeGrid_OnPhysicsChanged();
            if (Sync.IsServer)
            {
                Reattach();
            }
        }

        protected virtual void cubeGrid_OnPhysicsChanged() { }

        public void Reattach(bool force = false)
        {
            if (m_topBlock == null || m_topBlock.Closed)
            {
                return;
            }

            if (m_isWelding)
            {
                return;
            }

            if (force == false &&m_welded)
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
                if (Sync.IsServer)
                {
                    MatrixD masterToSlave = top.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                    m_connectionState.Value = new State() { TopBlockId = top.EntityId, MasterToSlave = masterToSlave, Force = force };
                }

                bool attached = Attach(top, force);

                //Debug.Assert(detached && attached);
                if (!top.MarkedForClose && top.CubeGrid.Physics != null)
                    top.CubeGrid.Physics.ForceActivate();
            }
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            if (m_welded || m_isWelding)
            {
                UnweldGroup();
            }
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

        protected abstract void CreateTopGrid(out MyCubeGrid topGrid, out MyAttachableTopBlockBase topBlock, long ownerId);

        protected virtual void UpdateText()
        {

        }

        protected abstract bool CanDetach();

        protected abstract void RefreshConstraint();
    }
}
