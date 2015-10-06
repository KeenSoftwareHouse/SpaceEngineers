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
                m_worldVolumeDirty = true;
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
        public override void SetWorldMatrix(MatrixD worldMatrix, object source = null, bool forceUpdate = false)
        {
            if (Entity.Parent != null && source != Entity.Parent)
                return;
            MyUtils.AssertIsValid(worldMatrix);

            if (Scale != null)
            {
                //ProfilerShort.Begin("Scale");
                MyUtils.Normalize(ref worldMatrix, out worldMatrix);
                worldMatrix = MatrixD.CreateScale(Scale.Value) * worldMatrix;
                //ProfilerShort.End();
            }
            if (m_worldMatrix.EqualsFast(ref worldMatrix) && !forceUpdate)
                return;

            //MatrixD localMatrix;


            //m_previousWorldMatrix = this.m_worldMatrix;

            if (this.Container.Entity.Parent == null)
            {
                m_localMatrix = worldMatrix;
            }
            else
            {
               //MatrixD matParentInv = MatrixD.Invert();
               m_localMatrix = (Matrix)(worldMatrix * this.Container.Entity.Parent.WorldMatrixInvScaled);
            }
            this.m_worldMatrix = worldMatrix;

            //ProfilerShort.Begin("EqualsFast");
            //if (/*!m_localMatrix.EqualsFast(ref localMatrix) ||*/ !m_previousWorldMatrix.EqualsFast(ref worldMatrix))
            {
                //ProfilerShort.BeginNextBlock("UpdateWM");
                //m_localMatrixChanged = true;
                //this.m_localMatrix = localMatrix;
                OnWorldPositionChanged(source);
                //ProfilerShort.BeginNextBlock("sync");
                if (MyPerGameSettings.MultiplayerEnabled)
                {
                    if (this.Container.Entity.InScene && source != m_syncObject && ShouldSync && this.Container.Entity.Parent == null)
                    {
                        m_syncObject.MarkPhysicsDirty();
                    }
                }
            }
            //ProfilerShort.End();
        }

        /// <summary>
        /// Updates the world matrix (change caused by this entity)
        /// </summary>
        //protected override void UpdateWorldMatrix(object source = null)
        //{

        //    //if (this.Container.Entity.Parent != null)
        //    //{
        //    //    //ProfilerShort.Begin("Parent!=null");
        //    //    MatrixD parentWorldMatrix = this.Container.Entity.Parent.WorldMatrix;
        //    //    UpdateWorldMatrix(ref parentWorldMatrix, source);
        //    //    //ProfilerShort.End();
        //    //    return;
        //    //}

        //    if (Entity.Parent == null)
        //    {
        //        MyGamePruningStructure.Move(Container.Entity as MyEntity);
        //        OnWorldPositionChanged(source);
        //    }
        //    else
        //    {
        //        var parentWM = Entity.Parent.PositionComp.WorldMatrix;
        //        UpdateWorldMatrix(ref parentWM, source);
        //    }

        //    //Container.Entity.Render.InvalidateRenderObjects();

        //    //UpdateChildren(source); //update children WMs
        //    //SetDirty(); //we dont update world volumes and stuff

        //    //UpdateWorldVolume();
        //    //ProfilerShort.Begin("OnChanged");
        //    //OnWorldPositionChanged(source);

        //    //ProfilerShort.BeginNextBlock("Physics.Onchanged");

        //    //ProfilerShort.End();
        //    //m_normalizedInvMatrixDirty = true;
        //    //m_invScaledMatrixDirty = true;
        //    // NotifyEntityChange(source);

        //    //if (this.Container.Entity.Physics != null && this.m_physics.Enabled && this.m_physics != source)
        //    //{
        //    //    this.m_physics.OnWorldPositionChanged(source);
        //    //}

        //    //ProfilerShort.Begin("Raise");
        //    //RaiseOnPositionChanged(this);

        //    ////ProfilerShort.BeginNextBlock("Action");
        //    //if (WorldPositionChanged != null)
        //    //{
        //    //    WorldPositionChanged(source);
        //    //}
        //    //ProfilerShort.End();
        //}

        /// <summary>
        /// Updates the childs of this entity.
        /// </summary>
        protected virtual void UpdateChildren(object source)
        {
            //ProfilerShort.Begin("Children");
            if (m_hierarchy == null)
            {
                //ProfilerShort.End();
                return;
            }
            foreach (var child in m_hierarchy.Children)
            {
                //child.Container.Entity.PositionComp.SetDirty();
                child.Container.Entity.PositionComp.UpdateWorldMatrix(ref this.m_worldMatrix, source);
            }
            //ProfilerShort.End();
        }

        /// <summary>
        /// Called when [world position changed].
        /// </summary>
        /// <param name="source">The source object that caused this event.</param>
        protected override void OnWorldPositionChanged(object source)
        {
            //ProfilerShort.Begin("OnWorldPositionChanged");
            Debug.Assert(source != this && (Container.Entity == null || source != Container.Entity), "Recursion detected!");

            if (Entity.Parent == null)
            {
                Container.Entity.UpdateGamePruningStructure();
            }

            if (Container.Entity.Render != null)
                Container.Entity.Render.InvalidateRenderObjects();

            UpdateChildren(source); //update children WMs
            m_worldVolumeDirty = true;
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
            ProfilerShort.End();
            //ProfilerShort.End();
        }
        #endregion

    }
}
