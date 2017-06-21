using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Components;
using System;
using System.Collections.Generic;
using VRage.Network;

using VRage.Game.Components;
using VRage.Game.Entity;

namespace Sandbox.Game.Replication
{
    public class MyCraftingComponentReplicable : MyExternalReplicableEvent<MyCraftingComponentBase>
    {
        public MyCraftingComponentBase CraftingComponent { get { return Instance; } }

        Action<MyEntity> m_raiseDestroyedHandler; 

        public MyCraftingComponentReplicable()
        {
            m_raiseDestroyedHandler = (entity) => RaiseDestroyed();
        }

        protected override void OnHook()
        {
            base.OnHook();
            if (CraftingComponent != null)
            {
                ((MyEntity)CraftingComponent.Entity).OnClose += m_raiseDestroyedHandler;
                CraftingComponent.BeforeRemovedFromContainer += (component) => OnComponentRemovedFromContainer();
            }
        }

        protected override void OnLoad(VRage.Library.Collections.BitStream stream, Action<MyCraftingComponentBase> loadingDoneHandler)
        {
            long entityId;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out entityId);            

            MyEntities.CallAsync(() => LoadAsync(entityId, loadingDoneHandler));
        }

        protected override void OnLoadBegin(VRage.Library.Collections.BitStream stream, Action<MyCraftingComponentBase> loadingDoneHandler)
        {
            OnLoad(stream, loadingDoneHandler);
        }

        private void LoadAsync(long entityId, Action<MyCraftingComponentBase> loadingDoneHandler)
        {
            MyEntity entity;
            MyCraftingComponentBase craftComp = null;
            MyGameLogicComponent gameLogic = null;
            if (MyEntities.TryGetEntityById(entityId, out entity) && 
                (entity.Components.TryGet<MyCraftingComponentBase>(out craftComp) ||
                (entity.Components.TryGet<MyGameLogicComponent>(out gameLogic) && gameLogic is MyCraftingComponentBase)))
            {
                if (craftComp == null)
                {
                    craftComp = gameLogic as MyCraftingComponentBase;
                }
                // TODO: If something needs to be initialized etc. it should probably go here..    
            }
            else
            {
                System.Diagnostics.Debug.Fail("MyCraftingComponentReplicable - trying to init crafting component on entity, but either entity or component wasn't found!");
            }
            
            loadingDoneHandler(craftComp); 
        }

        public override IMyReplicable GetParent()
        {
            System.Diagnostics.Debug.Assert(!((MyEntity)CraftingComponent.Entity).Closed, "Sending inventory of closed entity");
            if (CraftingComponent.Entity is MyCharacter)
            {
                return MyExternalReplicable.FindByObject(CraftingComponent.Entity);
            }

            if (CraftingComponent.Entity is MyCubeBlock)
            {
                return MyExternalReplicable.FindByObject((CraftingComponent.Entity as MyCubeBlock).CubeGrid);
            }

            return null;
        }

        public override float GetPriority(MyClientInfo client,bool cached)
        {
            // TODO: This can be adjusted, but for now, make sure it is always created on clients
            return 1.0f;
        }

        public override bool OnSave(VRage.Library.Collections.BitStream stream)
        {
            long ownerId = CraftingComponent.Entity.EntityId;
            VRage.Serialization.MySerializer.Write(stream, ref ownerId);
            return true;
        }

        public override void OnDestroy()
        {
            if (CraftingComponent != null && CraftingComponent.Entity != null)
            {
                ((MyEntity)CraftingComponent.Entity).OnClose -= m_raiseDestroyedHandler;
            }
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
           // TODO: Return state groups 
        }

        public void OnComponentRemovedFromContainer()
        {
            if (CraftingComponent != null && CraftingComponent.Entity != null)
            {
                ((MyEntity)CraftingComponent.Entity).OnClose -= m_raiseDestroyedHandler;
                RaiseDestroyed();
            }
        }

        public override bool HasToBeChild
        {
            get { return true; }
        }

        public override VRageMath.BoundingBoxD GetAABB()
        {
            System.Diagnostics.Debug.Fail("GetAABB can be called only on root replicables");
            return VRageMath.BoundingBoxD.CreateInvalid();
        }
    }
}
