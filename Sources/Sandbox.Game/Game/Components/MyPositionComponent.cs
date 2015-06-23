using Sandbox.Common.Components;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Common;
using VRage;
using ProfilerShort = VRage.ProfilerShort;
using VRage.Utils;
using VRage.Components;

namespace Sandbox.Game.Components
{
    public class MyPositionComponent : MyPositionComponentBase
    {
        #region Properties
        public Action<object> WorldPositionChanged; //Temporary, faster than event, if you want to link, crate lambda calling your and previous function

        /// <summary>
        /// Sets the local aabb.
        /// </summary>
        /// <value>
        /// The local aabb.
        /// </value>
        public override BoundingBox LocalAABB
        {
            get
            {
                return m_localAABB;
            }
            set
            {
                m_localAABB = value;
                m_localVolume = BoundingSphere.CreateFromBoundingBox(m_localAABB);
                UpdateWorldVolume();
                Container.Entity.UpdateGamePruningStructure();
            }
        }

        #endregion

        MySyncComponentBase m_syncObject;
        MyPhysicsComponentBase m_physics;
        MyHierarchyComponentBase m_hierarchy;

        public static bool SynchronizationEnabled = true;

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_syncObject = Container.Get<MySyncComponentBase>();
            m_physics = Container.Get<MyPhysicsComponentBase>();
            m_hierarchy = Container.Get<MyHierarchyComponentBase>();
            Container.ComponentAdded += container_ComponentAdded;
            Container.ComponentRemoved += container_ComponentRemoved;
        }

        public override void OnRemovedFromContainer()
        {
            base.OnRemovedFromContainer();
            Container.ComponentAdded -= container_ComponentAdded;
            Container.ComponentRemoved -= container_ComponentRemoved;
        }

        void container_ComponentAdded(Type type, MyEntityComponentBase comp)
        {
            if (type == typeof(MySyncComponentBase))
                m_syncObject = comp as MySyncComponentBase;
            else if (type == typeof(MyPhysicsComponentBase))
                m_physics = comp as MyPhysicsComponentBase;
            else if (type == typeof(MyHierarchyComponentBase))
                m_hierarchy = comp as MyHierarchyComponentBase;
        }

        void container_ComponentRemoved(Type type, MyEntityComponentBase comp)
        {
            if (type == typeof(MySyncComponentBase))
                m_syncObject = null;
            else if (type == typeof(MyPhysicsComponentBase))
                m_physics = null;
            else if (type == typeof(MyHierarchyComponentBase))
                m_hierarchy = null;
        }

        #region Position And Movement Methods

        protected override bool ShouldSync
        {
            get
            {
                return SynchronizationEnabled && Container.Get<MySyncComponentBase>() != null && m_syncObject != null;
            }
        }

        /// <summary>
        /// Sets the world matrix.
        /// </summary>
        /// <param name="worldMatrix">The world matrix.</param>
        /// <param name="source">The source object that caused this change or null when not important.</param>
        public override void SetWorldMatrix(MatrixD worldMatrix, object source = null)
        {
            MyUtils.AssertIsValid(worldMatrix);

            ProfilerShort.Begin("Scale");
            if (Scale != null)
            {
                MyUtils.Normalize(ref worldMatrix, out worldMatrix);
                worldMatrix = MatrixD.CreateScale(Scale.Value) * worldMatrix;
            }
            ProfilerShort.End();
            MatrixD localMatrix;

            if (this.Container.Entity.Parent == null)
            {
                m_previousParentWorldMatrix = this.m_worldMatrix;
                this.m_worldMatrix = worldMatrix;
                localMatrix = worldMatrix;
            }
            else
            {
               MatrixD matParentInv = MatrixD.Invert(this.Container.Entity.Parent.WorldMatrix);
               localMatrix = (Matrix)(worldMatrix * matParentInv);
            }

            ProfilerShort.Begin("EqualsFast");
            if (!m_localMatrix.EqualsFast(ref localMatrix) || !m_previousParentWorldMatrix.EqualsFast(ref worldMatrix))
            {
                ProfilerShort.BeginNextBlock("UpdateWM");
                m_localMatrixChanged = true;
                this.m_localMatrix = localMatrix;
                UpdateWorldMatrix(source);
                ProfilerShort.BeginNextBlock("sync");
                if (MyPerGameSettings.MultiplayerEnabled)
                {
                    if (this.Container.Entity.InScene && source != m_syncObject && ShouldSync && this.Container.Entity.Parent == null)
                    {
                        m_syncObject.UpdatePosition();
                    }
                }
            }
            ProfilerShort.End();
        }

        /// <summary>
        /// Updates the world matrix (change caused by this entity)
        /// </summary>
        public override void UpdateWorldMatrix(object source = null)
        {
            if (this.Container.Entity.Parent != null)
            {
                MatrixD parentWorldMatrix = this.Container.Entity.Parent.WorldMatrix;
                UpdateWorldMatrix(ref parentWorldMatrix, source);
                return;
            }

            //UpdateWorldVolume();
            ProfilerShort.Begin("OnChanged");
            OnWorldPositionChanged(source);

            ProfilerShort.BeginNextBlock("Physics.Onchanged");
            if (this.Container.Entity.Physics != null && this.m_physics.Enabled && this.m_physics != source)
            {
                this.m_physics.OnWorldPositionChanged(source);
            }
            ProfilerShort.End();
            m_normalizedInvMatrixDirty = true;
            m_invScaledMatrixDirty = true;
            // NotifyEntityChange(source);
        }

        /// <summary>
        /// Updates the world matrix (change caused by parent)
        /// </summary>
        public override void UpdateWorldMatrix(ref MatrixD parentWorldMatrix, object source = null)
        {
            if (!m_previousParentWorldMatrix.EqualsFast(ref parentWorldMatrix) || m_localMatrixChanged)
            {
                m_localMatrixChanged = false;
                MatrixD.Multiply(ref this.m_localMatrix, ref parentWorldMatrix, out this.m_worldMatrix);
                m_previousParentWorldMatrix = parentWorldMatrix;
                OnWorldPositionChanged(source);

                if (this.m_physics != null && this.m_physics.Enabled && this.m_physics != source)
                {
                    this.m_physics.OnWorldPositionChanged(source);
                }

                m_normalizedInvMatrixDirty = true;
                m_invScaledMatrixDirty = true;
           }

            //NotifyEntityChange(source);
        }

        /// <summary>
        /// Updates the childs of this entity.
        /// </summary>
        protected virtual void UpdateChildren(object source)
        {
            if (m_hierarchy == null)
            {
                return;
            }
            for (int i = 0; i < m_hierarchy.Children.Count; i++)
            {
                m_hierarchy.Children[i].Container.Entity.PositionComp.UpdateWorldMatrix(ref this.m_worldMatrix, source);
            }
        }

        /// <summary>
        /// Updates the volume of this entity.
        /// </summary>
        public override void UpdateWorldVolume()
        {
            m_worldAABB = m_localAABB.Transform(ref this.m_worldMatrix);

            m_worldVolume.Center = Vector3D.Transform(m_localVolume.Center, ref m_worldMatrix);
            m_worldVolume.Radius = m_localVolume.Radius;
            //bad, breaks rotating objects
            //if (oldWorldAABB.Contains(m_worldAABB) != ContainmentType.Contains)
            {   //New world AABB is not same as previous world AABB
                Container.Entity.Render.InvalidateRenderObjects();
            }
        }

        /// <summary>
        /// Called when [world position changed].
        /// </summary>
        /// <param name="source">The source object that caused this event.</param>
        public override void OnWorldPositionChanged(object source)
        {
            Debug.Assert(source != this && (Container.Entity == null || source != Container.Entity), "Recursion detected!");
            ProfilerShort.Begin("Volume");
            UpdateWorldVolume();
            ProfilerShort.BeginNextBlock("Prunning.Move");
            MyGamePruningStructure.Move(Container.Entity as MyEntity);

            ProfilerShort.BeginNextBlock("Children");
            UpdateChildren(source);

            ProfilerShort.BeginNextBlock("Raise");
            RaiseOnPositionChanged(this);

            ProfilerShort.BeginNextBlock("Action");
            if (WorldPositionChanged != null)
            {
                WorldPositionChanged(source);
            }
            ProfilerShort.End();
        }
        #endregion

    }
}
