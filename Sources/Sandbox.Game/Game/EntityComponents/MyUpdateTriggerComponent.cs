using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.World;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace Sandbox.Game.Components
{
    [MyComponentBuilder(typeof(MyObjectBuilder_UpdateTrigger))]
    public class MyUpdateTriggerComponent : MyTriggerComponent
    {
        private int m_size = 100;
        public int Size
        {
            get 
            { 
                return m_size; 
            }
            set 
            { 
                m_size = value;
                if (Entity != null)
                    m_AABB.Inflate(value / 2); 
            }
        }
        private Dictionary<MyEntity, VRage.ModAPI.MyEntityUpdateEnum> m_needsUpdate = new Dictionary<MyEntity,VRage.ModAPI.MyEntityUpdateEnum>();

        public MyUpdateTriggerComponent() { }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = base.Serialize(copy) as MyObjectBuilder_UpdateTrigger;
            ob.Size = m_size;
            return ob;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);
            var ob = builder as MyObjectBuilder_UpdateTrigger;
            m_size = ob.Size;
        }

        void grid_OnBlockOwnershipChanged(MyCubeGrid obj)
        {
            bool playerOwner = false;
            foreach(var owner in obj.BigOwners)
            {
                var faction = MySession.Static.Factions.GetPlayerFaction(owner);
                if(faction != null && !faction.IsEveryoneNpc())
                {
                    playerOwner = true;
                    break;
                }
            }
            foreach(var owner in obj.SmallOwners)
            {
                var faction = MySession.Static.Factions.GetPlayerFaction(owner);
                if (faction != null && !faction.IsEveryoneNpc())
                {
                    playerOwner = true;
                    break;
                }
            }
            if (playerOwner)
            {
                obj.Components.Remove<MyUpdateTriggerComponent>();
                obj.OnBlockOwnershipChanged -= grid_OnBlockOwnershipChanged;
            }
        }

        public MyUpdateTriggerComponent(int triggerSize)
        {
            m_size = triggerSize;
        }

        protected override void UpdateInternal()
        {
            if (Entity.Physics == null)
                return;

            m_AABB = Entity.PositionComp.WorldAABB.Inflate(m_size / 2);

            bool wasDisabled = m_needsUpdate.Count != 0;
            for (int i = QueryResult.Count - 1; i >= 0; i--)
            {
                var e = QueryResult[i];
                if (!e.Closed && e.PositionComp.WorldAABB.Intersects(m_AABB) && (e is MyMeteor) == false)
                {
                    break;
                }
                QueryResult.RemoveAtFast(i);
            }

            DoQuery = QueryResult.Count == 0;
            base.UpdateInternal();

            if (QueryResult.Count == 0)
            {
                if (!wasDisabled)
                    DisableRecursively((MyEntity)Entity);
            }
            else
            {
                if (wasDisabled)
                {
                    EnableRecursively((MyEntity)Entity);
                    m_needsUpdate.Clear();
                }
            }
        }

        protected override bool QueryEvaluator(MyEntity entity)
        {
            if (entity.Physics == null || entity.Physics.IsStatic)
                return false;

            if (entity is MyFloatingObject || entity is MyDebrisBase)
                return false;

            if (entity == Entity.GetTopMostParent())
                return false;

            return true;
        }

        private void DisableRecursively(MyEntity entity)
        {
            Enabled = false;
            m_needsUpdate[entity] = entity.NeedsUpdate;
            entity.NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.NONE;
            entity.Render.Visible = false;
            if (entity.Hierarchy == null) 
                return;

            foreach(var c in entity.Hierarchy.Children)
            {
                DisableRecursively((MyEntity)c.Entity);
            }
        }

        private void EnableRecursively(MyEntity entity)
        {
            Enabled = true;

            if (m_needsUpdate.ContainsKey(entity))
            {
                entity.NeedsUpdate = m_needsUpdate[entity];
            }
            else
            {
                System.Diagnostics.Debug.Fail("Entity was not disabled!");
            }
            entity.Render.Visible = true;
            if (entity.Hierarchy == null)
                return;

            foreach (var c in entity.Hierarchy.Children)
            {
                EnableRecursively((MyEntity)c.Entity);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (Entity != null && !Entity.MarkedForClose)
                if (QueryResult.Count != 0)
                {
                    EnableRecursively((MyEntity)Entity);
                    m_needsUpdate.Clear();
                }

            m_needsUpdate.Clear();
        }

        public override string ComponentTypeDebugString
        {
            get { return "Pirate update trigger"; }
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            var grid = Entity as MyCubeGrid;
            if (grid != null)
                grid.OnBlockOwnershipChanged += grid_OnBlockOwnershipChanged;
        }
    }
}
