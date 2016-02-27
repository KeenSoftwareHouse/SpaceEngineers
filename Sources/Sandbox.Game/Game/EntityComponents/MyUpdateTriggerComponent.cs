using Havok;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Library.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Components
{
    [MyComponentBuilder(typeof(MyObjectBuilder_UpdateTrigger))]
    public class MyUpdateTriggerComponent : MyEntityComponentBase
    {
        const int UpdateFrequency = 300; //5 seconds
        private int m_size = 100;
        List<MyEntity> m_queryResult = new List<MyEntity>();
        BoundingBoxD m_triggerAABB;
        private int m_updateTick;
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
                    m_triggerAABB = Entity.PositionComp.WorldAABB.Inflate(value / 2); 
            }
        }
        int m_activeCounter = 0;
        private Dictionary<MyEntity, VRage.ModAPI.MyEntityUpdateEnum> m_needsUpdate = new Dictionary<MyEntity,VRage.ModAPI.MyEntityUpdateEnum>();

        bool m_isEnabled = false;

        public MyUpdateTriggerComponent() { }

        public override VRage.Game.ObjectBuilders.ComponentSystem.MyObjectBuilder_ComponentBase Serialize()
        {
            var ob = base.Serialize() as MyObjectBuilder_UpdateTrigger;
            ob.Size = m_size;
            return ob;
        }

        public override void Deserialize(VRage.Game.ObjectBuilders.ComponentSystem.MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);
            var ob = builder as MyObjectBuilder_UpdateTrigger;
            m_size = ob.Size;
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_triggerAABB = Entity.PositionComp.WorldAABB.Inflate(m_size / 2);
            m_updateTick = MyRandom.Instance.Next(UpdateFrequency-1);
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

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
            MySessionComponentEntityTrigger.Static.RemoveTrigger((MyEntity)Entity);
            if (Entity != null && !Entity.MarkedForClose)
                Close();
        }

        public MyUpdateTriggerComponent(int triggerSize)
        {
            m_size = triggerSize;
        }

        private void QueryTrigger()
        {
            m_queryResult.Clear();
            MyGamePruningStructure.GetTopMostEntitiesInBox(ref m_triggerAABB, m_queryResult);
            for (int i = m_queryResult.Count - 1; i >= 0; i--)
            {
                var e = m_queryResult[i];
                if (e.Physics == null || e.Physics.IsStatic)
                {
                    m_queryResult.RemoveAtFast(i);
                    continue;
                }

                if(e is MyFloatingObject || e is MyDebrisBase)
                {
                    m_queryResult.RemoveAtFast(i);
                    continue;
                }

                if (e == Entity.GetTopMostParent())
                {
                    m_queryResult.RemoveAtFast(i);
                    continue;
                }
                //if (e.Physics.RigidBody != null && e.Physics.RigidBody.IsActive)
                //    activeCounter++;
                //e.Physics.RigidBody.Activated += RBActivated;
                //e.Physics.RigidBody.Deactivated += RBDeactivated;
            }
        }

        private void DisableRecursively(MyEntity entity)
        {
            m_isEnabled = false;
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
            m_isEnabled = true;

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

        void RBActivated(HkEntity e) { m_activeCounter++; }
        void RBDeactivated(HkEntity e) { m_activeCounter--; }

        public void Update()
        {
            if (MySession.Static.GameplayFrameCounter % UpdateFrequency != m_updateTick)
                return;

            if (Entity.Physics == null)
                return;

            m_triggerAABB = Entity.PositionComp.WorldAABB.Inflate(m_size / 2);

            bool wasDisabled = m_needsUpdate.Count != 0;
            for (int i = m_queryResult.Count-1; i >= 0; i--)
            {
                var e = m_queryResult[i];
                if (!e.Closed && e.PositionComp.WorldAABB.Intersects(m_triggerAABB)&& (e is MyMeteor) ==false)
                {
                    break;
                }
                m_queryResult.RemoveAtFast(i);
            }
            if(m_queryResult.Count == 0)
            {
                QueryTrigger();
            }

            if (m_queryResult.Count == 0)
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

        public void Close()
        {
            if (m_queryResult.Count != 0)
            {
                EnableRecursively((MyEntity)Entity);
                m_needsUpdate.Clear();
            }
            m_queryResult.Clear();
            m_needsUpdate.Clear();
        }

        public void DebugDraw()
        {
            MyRenderProxy.DebugDrawAABB(m_triggerAABB, m_queryResult.Count == 0 ? Color.Red : Color.Green, 1, 1, false);
            foreach(var e in m_queryResult)
            {
                MyRenderProxy.DebugDrawAABB(e.PositionComp.WorldAABB, Color.Yellow, 1, 1, false);
                MyRenderProxy.DebugDrawLine3D(e.WorldMatrix.Translation, Entity.WorldMatrix.Translation, Color.Yellow, Color.Green, false);
            }
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public override string ComponentTypeDebugString
        {
            get { return "Update trigger"; }
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
            MySessionComponentEntityTrigger.Static.AddTrigger(this);
            //QueryTrigger();
            //if (m_queryResult.Count == 0)
            //{
            //    DisableRecursively((MyEntity)Entity);
            //}
            var grid = Entity as MyCubeGrid;
            if (grid != null)
                grid.OnBlockOwnershipChanged += grid_OnBlockOwnershipChanged;
        }

        public bool IsEnabled()
        {
            return m_isEnabled;
        }
    }
}
