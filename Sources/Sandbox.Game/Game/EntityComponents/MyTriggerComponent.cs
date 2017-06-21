using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Components
{
    [MyComponentBuilder(typeof(MyObjectBuilder_TriggerBase))]
    public class MyTriggerComponent : MyEntityComponentBase
    {
        public enum TriggerType
        {
            AABB, Sphere
        }

        private static      uint                    m_triggerCounter;
        private const       uint                    PRIME = 31;

        private readonly    uint                    m_updateOffset;
        private readonly    List<MyEntity>          m_queryResult = new List<MyEntity>();

        protected           TriggerType             m_triggerType;
        protected           BoundingBoxD            m_AABB;
        protected           BoundingSphereD         m_boundingSphere;
        public              Vector3D                DefaultTranslation = Vector3D.Zero;

        protected           bool                    DoQuery                     { get; set; }
        protected           List<MyEntity>          QueryResult                 { get { return m_queryResult; } }

        public uint                                 UpdateFrequency             { get; set; }
        public virtual      bool                    Enabled                     { get; protected set; }
        public override     string                  ComponentTypeDebugString    { get { return "Trigger"; } }
        public              Color?                  CustomDebugColor            { get; set; }

        /// <summary>
        /// Trigger BB center position.
        /// </summary>
        public Vector3D Center
        {
            get
            {
                switch (m_triggerType)
                {
                    case TriggerType.AABB:
                        return m_AABB.Center;
                        break;
                    case TriggerType.Sphere:
                        return m_boundingSphere.Center;
                        break;
                    default:
                        return Vector3D.Zero;
                }
            }
        }

        public MyTriggerComponent(TriggerType type, uint updateFrequency = 300)
        {
            m_triggerType = type;
            UpdateFrequency = updateFrequency;

            m_updateOffset = m_triggerCounter++*PRIME%UpdateFrequency;
            DoQuery = true;
        }

        public MyTriggerComponent()
        {
            m_triggerType = TriggerType.AABB;
            UpdateFrequency = 300;

            m_updateOffset = m_triggerCounter++*PRIME%UpdateFrequency;
            DoQuery = true;
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = base.Serialize() as MyObjectBuilder_TriggerBase;
            if (ob != null)
            {
                ob.AABB = m_AABB;
                ob.BoundingSphere = m_boundingSphere;
                ob.Type = (int) m_triggerType;
                ob.Offset = DefaultTranslation;
            }

            return ob;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);
            var ob = builder as MyObjectBuilder_TriggerBase;

            if (ob != null)
            {
                m_AABB = ob.AABB;
                m_boundingSphere = ob.BoundingSphere;
                m_triggerType = ob.Type == -1 ? TriggerType.AABB : (TriggerType) ob.Type;
                DefaultTranslation = ob.Offset;
            }
        }

        public override void OnAddedToScene()
        {
            MySessionComponentTriggerSystem.Static.AddTrigger(this);
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
            MySessionComponentTriggerSystem.Static.RemoveTrigger((MyEntity) Entity, this);
            Entity.PositionComp.OnPositionChanged -= OnEntityPositionCompPositionChanged;
            Dispose();
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            Entity.PositionComp.OnPositionChanged += OnEntityPositionCompPositionChanged;

            if(Entity.InScene)
            {
                MySessionComponentTriggerSystem.Static.AddTrigger(this);
            }
        }

        private void OnEntityPositionCompPositionChanged(MyPositionComponentBase myPositionComponentBase)
        {
            // Update BB position to respective entity position.
            // default translation keeps the relative offset to entity.
            switch (m_triggerType)
            {
                case TriggerType.AABB:
                    var translation = Entity.PositionComp.GetPosition() - m_AABB.Matrix.Translation + DefaultTranslation;
                    m_AABB.Translate(translation);
                    break;
                case TriggerType.Sphere:
                    m_boundingSphere.Center = Entity.PositionComp.GetPosition() + DefaultTranslation;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Update()
        {
            if (MySession.Static.GameplayFrameCounter%UpdateFrequency != m_updateOffset)
                return;

            UpdateInternal();
        }

        /// <summary>
        /// Override this function to set custom update behaviour.
        /// Call base at first because it queries objects if DoQuery is set.
        /// </summary>
        protected virtual void UpdateInternal()
        {
            if (DoQuery)
            {
                m_queryResult.Clear();

                switch (m_triggerType)
                {
                    case TriggerType.AABB:
                        MyGamePruningStructure.GetTopMostEntitiesInBox(ref m_AABB, m_queryResult);
                        break;
                    case TriggerType.Sphere:
                        MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref m_boundingSphere, m_queryResult);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                for (int index = 0; index < m_queryResult.Count;)
                {
                    var result = m_queryResult[index];
                    if (!QueryEvaluator(result))
                        m_queryResult.RemoveAtFast(index);
                    else
                    {
                        switch (m_triggerType)
                        {
                            case TriggerType.AABB:
                                if (!m_AABB.Intersects(m_queryResult[index].PositionComp.WorldAABB))
                                    m_queryResult.RemoveAtFast(index);
                                else
                                    index++;
                                break;
                            case TriggerType.Sphere:
                                if (!m_boundingSphere.Intersects(m_queryResult[index].PositionComp.WorldAABB))
                                    m_queryResult.RemoveAtFast(index);
                                else
                                    index++;
                                break;
                            default:
                                index++;
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Override for custom trigger disposal before removing.
        /// </summary>
        public virtual void Dispose()
        {
            m_queryResult.Clear();
        }

        public virtual void DebugDraw()
        {
            var defaultColor = Color.Red;
            if (CustomDebugColor.HasValue)
                defaultColor = CustomDebugColor.Value;

            if (m_triggerType == TriggerType.AABB)
                MyRenderProxy.DebugDrawAABB(m_AABB, m_queryResult.Count == 0 ? defaultColor : Color.Green, 1, 1, false);
            else
                MyRenderProxy.DebugDrawSphere(m_boundingSphere.Center, (float) m_boundingSphere.Radius, m_queryResult.Count == 0 ? defaultColor : Color.Green, 1, false);

            if (Entity.Parent != null)
            {
                MyRenderProxy.DebugDrawLine3D(Center, Entity.Parent.PositionComp.GetPosition(), Color.Yellow, Color.Green, false);
            }

            foreach (var e in m_queryResult)
            {
                MyRenderProxy.DebugDrawAABB(e.PositionComp.WorldAABB, Color.Yellow, 1, 1, false);
                MyRenderProxy.DebugDrawLine3D(e.WorldMatrix.Translation, Entity.WorldMatrix.Translation, Color.Yellow, Color.Green, false);
            }
        }

        /// <summary>
        /// Override to discard query results of your choice.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>True for valid entities.</returns>
        protected virtual bool QueryEvaluator(MyEntity entity)
        {
            return true;
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public bool Contains(Vector3D point)
        {
            switch (m_triggerType)
            {
                case TriggerType.AABB:
                    return m_AABB.Contains(point) == ContainmentType.Contains;
                case TriggerType.Sphere:
                    return m_boundingSphere.Contains(point) == ContainmentType.Contains;
                default:
                    return false;
            }
        }
    }
}
