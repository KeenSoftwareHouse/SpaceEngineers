using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;
using VRageMath.PackedVector;
using VRage.Game.ObjectBuilders.ComponentSystem;
using System.Diagnostics;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// This class creates replicable object for MyReplicableEntity : MyEntity
    /// </summary>    
    public class MyInventoryBagReplicable : MyEntityReplicableBase<MyInventoryBagEntity>
    {
        public override bool OnSave(BitStream stream)
        {
            MyObjectBuilder_InventoryBagEntity builder = (MyObjectBuilder_InventoryBagEntity)Instance.GetObjectBuilder();
            // Some old bags might be saved without subtype name.
            if (string.IsNullOrEmpty(builder.SubtypeName))
                return false;

            var physicsComponent = MyInventoryBagEntity.GetPhysicsComponentBuilder(builder);
            Debug.Assert(physicsComponent != null);
            if (physicsComponent == null)
                return false;

            VRage.Serialization.MySerializer.Write(stream, ref builder, MyObjectBuilderSerializer.Dynamic);

            return true;
        }

        protected override void OnLoad(BitStream stream, Action<MyInventoryBagEntity> loadingDoneHandler)
        {
            MyObjectBuilder_InventoryBagEntity builder = (MyObjectBuilder_InventoryBagEntity)VRage.Serialization.MySerializer.CreateAndRead<MyObjectBuilder_EntityBase>(stream, MyObjectBuilderSerializer.Dynamic);
            var physicsComponent = MyInventoryBagEntity.GetPhysicsComponentBuilder(builder);
            Debug.Assert(physicsComponent != null);
            if (physicsComponent == null)
                return;

            MyInventoryBagEntity entity = (MyInventoryBagEntity)MyEntities.CreateFromObjectBuilderAndAdd(builder);
            entity.DebugCreatedBy = DebugCreatedBy.FromServer;     
            loadingDoneHandler(entity);        
        }

        protected override IMyStateGroup CreatePhysicsGroup()
        {
            return new StateGroups.MyEntityPhysicsStateGroup(Instance, this);
        }
    }
}
