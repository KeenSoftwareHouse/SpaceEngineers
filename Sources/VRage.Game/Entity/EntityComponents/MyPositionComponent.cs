using System;
using System.Diagnostics;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Profiler;
using VRageMath;
using VRage.Utils;

namespace VRage.Game.Components
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
                base.LocalAABB = value;
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

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
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
        public override void SetWorldMatrix(MatrixD worldMatrix, object source = null, bool forceUpdate = false, bool updateChildren = true)
        {
            if (Entity.Parent != null && source != Entity.Parent)
                return;

            worldMatrix.AssertIsValid();

            if (Scale != null)
            {
                MyUtils.Normalize(ref worldMatrix, out worldMatrix);
                worldMatrix = MatrixD.CreateScale(Scale.Value) * worldMatrix;
            }
            if (!forceUpdate && m_worldMatrix.EqualsFast(ref worldMatrix))
                return;

            if (this.Container.Entity.Parent == null)
            {
                m_localMatrix = worldMatrix;
            }
            else
            {
               m_localMatrix = (Matrix)(worldMatrix * this.Container.Entity.Parent.WorldMatrixInvScaled);
            }
            this.m_worldMatrix = worldMatrix;

            OnWorldPositionChanged(source, updateChildren);
        }

        /// <summary>
        /// Updates the childs of this entity.
        /// </summary>
        protected virtual void UpdateChildren(object source)
        {
            if (m_hierarchy == null)
                return;
            foreach (var child in m_hierarchy.Children)
            {
                child.Container.Entity.PositionComp.UpdateWorldMatrix(ref this.m_worldMatrix, source);
            }
        }

        /// <summary>
        /// Called when [world position changed].
        /// </summary>
        /// <param name="source">The source object that caused this event.</param>
        protected override void OnWorldPositionChanged(object source, bool updateChildren = true)
        {
            //ProfilerShort.Begin("OnWorldPositionChanged");
            Debug.Assert(source != this && (Container.Entity == null || source != Container.Entity), "Recursion detected!");

            if (Entity.Parent == null || (Entity.Flags & EntityFlags.IsGamePrunningStructureObject) != 0)
            {
                Container.Entity.UpdateGamePruningStructure();
            }

            if (updateChildren)
                UpdateChildren(source); //update children WMs
            m_worldVolumeDirty = true;
            m_worldAABBDirty = true;
            m_normalizedInvMatrixDirty = true;
            m_invScaledMatrixDirty = true;

            if (this.m_physics != null && this.m_physics.Enabled && this.m_physics != source)
            {
                this.m_physics.OnWorldPositionChanged(source);
            }

            ProfilerShort.Begin("Raise");
            RaiseOnPositionChanged(this);

            //ProfilerShort.BeginNextBlock("Action");
            if (WorldPositionChanged != null)
            {
                WorldPositionChanged(source);
            }

            //Render objects invalidate must be after WorldPositionChanged, because there PositionLeftBottomCorner is updated
            if (Container.Entity.Render != null)
                Container.Entity.Render.InvalidateRenderObjects();

            ProfilerShort.End();
            //ProfilerShort.End();
        }
        #endregion

    }
}
