using System;
using System.Collections.Generic;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    [MyComponentBuilder(typeof(MyObjectBuilder_AreaTrigger))]
    public class MyAreaTriggerComponent : MyTriggerComponent
    {
        private readonly HashSet<MyEntity>  m_prevQuery = new HashSet<MyEntity>();
        private readonly List<MyEntity>     m_resultsToRemove = new List<MyEntity>(); 

        public string Name      { get; set; }
        public double Radius    { get { return m_boundingSphere.Radius;} set { m_boundingSphere.Radius = value; }}

        public Vector3D Center
        {
            get
            {
                return m_boundingSphere.Center;
            }
            set
            {
                m_boundingSphere.Center = value;
                if(Entity != null)
                    DefaultTranslation = m_boundingSphere.Center - Entity.PositionComp.GetPosition();
            }
        }

        public MyAreaTriggerComponent() : this(String.Empty)
        { }

        public MyAreaTriggerComponent(string name) : base(TriggerType.Sphere, 20)
        {
            Name = name;
        }

        protected override void UpdateInternal()
        {
            base.UpdateInternal();
            // Trigger left for all entities that currently left the trigger area.
            foreach (var entity in m_prevQuery)
                if(!QueryResult.Contains(entity))
                {
                    if (MyVisualScriptLogicProvider.AreaTrigger_Left != null)
                    {
                        MyPlayer.PlayerId playerId; 
                        if(MySession.Static.Players.ControlledEntities.TryGetValue(entity.EntityId, out playerId))
                        {
                            var identity = MySession.Static.Players.TryGetPlayerIdentity(playerId);
                            MyVisualScriptLogicProvider.AreaTrigger_Left(Name, identity.IdentityId);
                        }
                    }

                    m_resultsToRemove.Add(entity);
                }

            // Remove all entities that left.
            foreach (var entity in m_resultsToRemove)
                m_prevQuery.Remove(entity);

            m_resultsToRemove.Clear();

            // Add and trigger for all entities that entered area.
            foreach (var entity in QueryResult)
                if (m_prevQuery.Add(entity) && MyVisualScriptLogicProvider.AreaTrigger_Entered != null)
                {
                    MyPlayer.PlayerId playerId;
                    if (MySession.Static.Players.ControlledEntities.TryGetValue(entity.EntityId, out playerId))
                    {
                        var identity = MySession.Static.Players.TryGetPlayerIdentity(playerId);
                        MyVisualScriptLogicProvider.AreaTrigger_Entered(Name, identity.IdentityId);
                    }
                }

        }

        protected override bool QueryEvaluator(MyEntity entity)
        {
            if (entity is MyCharacter && ((MyCharacter)entity).IsBot == false)
                return true;

            if (entity is MyCubeGrid)
            {
                if(MySession.Static.Players.ControlledEntities.ContainsKey(entity.EntityId))
                    return true;
            }

            return false;
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy)
        {
            var ob = base.Serialize(copy) as MyObjectBuilder_AreaTrigger;
            ob.Name = Name;

            return ob;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);
            var ob = (MyObjectBuilder_AreaTrigger) builder;

            Name = ob.Name;
        }

        public override bool IsSerialized()
        {
            return true;
        }
    }
}
