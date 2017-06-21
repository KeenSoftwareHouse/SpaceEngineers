using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ObjectBuilders;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Components
{
    public class MyEntityGameLogic : MyGameLogicComponent
    {
        /// <summary>
        /// This event may not be invoked at all, when calling MyEntities.CloseAll, marking is bypassed
        /// </summary>
        public event Action<MyEntity> OnMarkForClose;
        public event Action<MyEntity> OnClose;
        public event Action<MyEntity> OnClosing;

        protected MyEntity m_entity;
        public MyGameLogicComponent GameLogic { get; set; }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_entity = Container.Entity as MyEntity;
        }

        public MyEntityGameLogic()
        {
            GameLogic = new MyNullGameLogicComponent();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            ProfilerShort.Begin("MyEntity.Init(objectBuilder)");
            if (objectBuilder != null)
            {
                if (objectBuilder.PositionAndOrientation.HasValue)
                {
                    var posAndOrient = objectBuilder.PositionAndOrientation.Value;
                    MatrixD matrix = MatrixD.CreateWorld(posAndOrient.Position, posAndOrient.Forward, posAndOrient.Up);
                    MyUtils.AssertIsValid(matrix);

                    Container.Entity.PositionComp.SetWorldMatrix(matrix);
                }
                // Do not copy EntityID if it gets overwritten later. It might
                // belong to some existing entity that we're making copy of.
                if (objectBuilder.EntityId != 0)
                    Container.Entity.EntityId = objectBuilder.EntityId;
                Container.Entity.Name = objectBuilder.Name;
                Container.Entity.Render.PersistentFlags = objectBuilder.PersistentFlags;
            }

            AllocateEntityID();

            Container.Entity.InScene = false;

            MyEntities.SetEntityName(m_entity, false);

            if (m_entity.SyncFlag)
            {
                m_entity.CreateSync();
            }
            GameLogic.Init(objectBuilder);
            ProfilerShort.End();
        }

        //  This is real initialization of this class!!! Instead of constructor.
        public void Init(StringBuilder displayName,
                         string model,
                         MyEntity parentObject,
                         float? scale,
                         string modelCollision = null)
        {
            ProfilerShort.Begin("MyEntity.Init(...models...)");
            Container.Entity.DisplayName = displayName != null ? displayName.ToString() : null;

            m_entity.RefreshModels(model, modelCollision);

            if (parentObject != null)
            {
                parentObject.Hierarchy.AddChild(Container.Entity, false, false);
            }

            Container.Entity.PositionComp.Scale = scale;

            AllocateEntityID();
            ProfilerShort.End();
        }


        private void AllocateEntityID()
        {
            if (Container.Entity.EntityId == 0 && MyEntityIdentifier.AllocationSuspended == false)
            {
                Container.Entity.EntityId = MyEntityIdentifier.AllocateId();
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            var objBuilder = MyEntityFactory.CreateObjectBuilder(Container.Entity as MyEntity);

            objBuilder.PositionAndOrientation = new MyPositionAndOrientation()
            {
                Position = Container.Entity.PositionComp.GetPosition(),
                Up = (Vector3)Container.Entity.WorldMatrix.Up,
                Forward = (Vector3)Container.Entity.WorldMatrix.Forward
            };

            objBuilder.EntityId = Container.Entity.EntityId;
            Debug.Assert(objBuilder.EntityId != 0);

            objBuilder.Name = Container.Entity.Name;
            objBuilder.PersistentFlags = Container.Entity.Render.PersistentFlags;

            return objBuilder;
        }

        #region Update
        public override void UpdateOnceBeforeFrame()
        {
            GameLogic.UpdateOnceBeforeFrame();
            Debug.Assert(!m_entity.Closed, "Cannot update entity, entity is closed");
        }

        public override void UpdateBeforeSimulation()
        {
            GameLogic.UpdateBeforeSimulation();
            Debug.Assert(!m_entity.Closed, "Cannot update entity, entity is closed");
        }
        public override void UpdateAfterSimulation()
        {
            GameLogic.UpdateAfterSimulation();
            Debug.Assert(!m_entity.Closed, "Cannot update entity, entity is closed");
            //if(m_syncObject != null) m_syncObject.Update();
        }

        public override void UpdatingStopped()
        {
            Debug.Assert(!m_entity.Closed, "Cannot update entity, entity is closed");
            //if(m_syncObject != null) m_syncObject.Update();
        }

        /// <summary>
        /// Called each 10th frame if registered for update10
        /// </summary>
        public override void UpdateBeforeSimulation10()
        {
            GameLogic.UpdateBeforeSimulation10();
            Debug.Assert(!m_entity.Closed, "Cannot update entity, entity is closed");
        }
        public override void UpdateAfterSimulation10()
        {
            GameLogic.UpdateAfterSimulation10();
            Debug.Assert(!m_entity.Closed, "Cannot update entity, entity is closed");
            //if (m_syncObject != null) m_syncObject.Update10();
        }


        /// <summary>
        /// Called each 100th frame if registered for update100
        /// </summary>
        public override void UpdateBeforeSimulation100()
        {
            GameLogic.UpdateBeforeSimulation100();
            Debug.Assert(!m_entity.Closed, "Cannot update entity, entity is closed");
        }
        public override void UpdateAfterSimulation100()
        {
            GameLogic.UpdateAfterSimulation100();
            Debug.Assert(!m_entity.Closed, "Cannot update entity, entity is closed");
            //if (m_syncObject != null) m_syncObject.Update100();
        }
        #endregion

        /// <summary>
        /// This method marks this entity for close which means, that Close
        /// will be called after all entities are updated
        /// </summary>
        public override void MarkForClose()
        {
            // TODO: Make synchronized
            //if (!MarkedForClose)
            {
                // Needs update = false, added, because entities was updated once before closed
                //NeedsUpdate = MyEntityUpdateEnum.NONE;

                MarkedForClose = true;
                MyEntities.Close(m_entity);
                GameLogic.MarkForClose();
                ProfilerShort.Begin("MarkForCloseHandler");
                var handler = OnMarkForClose;
                if (handler != null) handler(m_entity);
                ProfilerShort.End();
            }
        }

        public override void Close()
        {
            GameLogic.Close();
            //doesnt work in parallel update
            //Debug.Assert(MySandboxGame.IsMainThread(), "Entity.Close() called not from Main Thread!");
            Debug.Assert(MyEntities.UpdateInProgress == false, "Do not close entities directly in Update*, use MarkForClose() instead");
            Debug.Assert(MyEntities.CloseAllowed == true, "Use MarkForClose()");
            //Debug.Assert(!Closed, "Close() called twice!");

            //Children has to be cleared after close notification is send
            while (m_entity.Hierarchy.Children.Count > 0)
            {
                MyHierarchyComponentBase compToRemove = m_entity.Hierarchy.Children[m_entity.Hierarchy.Children.Count - 1];
                Debug.Assert(compToRemove.Parent != null, "Entity has no parent but is part of children collection");

                compToRemove.Container.Entity.Close();

                m_entity.Hierarchy.Children.Remove(compToRemove);
            }

            //OnPositionChanged = null;

            CallAndClearOnClosing();

            MyEntities.RemoveName(m_entity);
            MyEntities.RemoveFromClosedEntities(m_entity);

            if (m_entity.Physics != null)
            {
                m_entity.Physics.Close();
                m_entity.Physics = null;

                m_entity.RaisePhysicsChanged();
            }

            MyEntities.UnregisterForUpdate(m_entity, true);


            if (m_entity.Hierarchy.Parent == null) //only root objects are in entities list
                MyEntities.Remove(m_entity);
            else
            {
                m_entity.Parent.Hierarchy.Children.Remove(m_entity.Hierarchy);

                //remove children first
                if (m_entity.Parent.InScene)
                    m_entity.OnRemovedFromScene(m_entity);

                MyEntities.RaiseEntityRemove(m_entity);
            }

            if (m_entity.EntityId != 0)
            {
                MyEntityIdentifier.RemoveEntity(m_entity.EntityId);
            }

            //this.EntityId = 0;
            Debug.Assert(m_entity.Hierarchy.Children.Count == 0);

            CallAndClearOnClose();

            Closed = true;
        }

        protected void CallAndClearOnClose()
        {
            if (OnClose != null)
                OnClose(m_entity);

            OnClose = null;
        }

        protected void CallAndClearOnClosing()
        {
            if (OnClosing != null)
                OnClosing(m_entity);

            OnClosing = null;
        }
    }
}
