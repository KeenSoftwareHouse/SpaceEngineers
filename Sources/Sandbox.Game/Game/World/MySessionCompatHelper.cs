using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Utils;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRageMath;
using VRage.Game.ObjectBuilders;

namespace Sandbox.Game.World
{
    public class MySessionCompatHelper
    {
        public virtual void FixSessionComponentObjectBuilders(MyObjectBuilder_Checkpoint checkpoint, MyObjectBuilder_Sector sector)
        {
            if (checkpoint.ScriptManagerData == null)
                checkpoint.ScriptManagerData = (MyObjectBuilder_ScriptManager)checkpoint.SessionComponents.FirstOrDefault(x => x is MyObjectBuilder_ScriptManager);
        }

        public virtual void FixSessionObjectBuilders(MyObjectBuilder_Checkpoint checkpoint, MyObjectBuilder_Sector sector)
        {
        }

        public virtual void AfterEntitiesLoad(int saveVersion)
        { }

        public virtual void CheckAndFixPrefab(MyObjectBuilder_Definitions prefab)
        { }

        /// <summary>
        /// Converts the given builder to be of type EntityBase only with components. Prefix is added to sub type name for the created EntityBase and also for component builders.
        /// Should be used when an entity was transformed to components and do not need specific entity implementation at all. 
        /// </summary>
        protected MyObjectBuilder_EntityBase ConvertBuilderToEntityBase(MyObjectBuilder_EntityBase origEntity, string subTypeNamePrefix)
        {
            var origSubTypeName = !string.IsNullOrEmpty(origEntity.SubtypeName)
                ? origEntity.SubtypeName : (origEntity.EntityDefinitionId != null ? origEntity.EntityDefinitionId.Value.SubtypeName : null);
            System.Diagnostics.Debug.Assert(origSubTypeName != null);
            if (origSubTypeName == null)
                return null;

            var newSubTypeName = subTypeNamePrefix != null ? subTypeNamePrefix : "" + origSubTypeName;

            MyObjectBuilder_EntityBase newBuilder = MyObjectBuilderSerializer.CreateNewObject(typeof(MyObjectBuilder_EntityBase), newSubTypeName) as MyObjectBuilder_EntityBase;
            newBuilder.EntityId = origEntity.EntityId;
            newBuilder.PersistentFlags = origEntity.PersistentFlags;
            newBuilder.Name = origEntity.Name;
            newBuilder.PositionAndOrientation = origEntity.PositionAndOrientation;
            newBuilder.ComponentContainer = origEntity.ComponentContainer;

            if (newBuilder.ComponentContainer != null && newBuilder.ComponentContainer.Components.Count > 0) 
            {
                foreach (var componentBuidler in newBuilder.ComponentContainer.Components)
                {
                    if (!string.IsNullOrEmpty(componentBuidler.Component.SubtypeName) && componentBuidler.Component.SubtypeName == origSubTypeName)
                        componentBuidler.Component.SubtypeName = newSubTypeName;
                }
            }

            return newBuilder;
        }

        private MyObjectBuilder_EntityBase ConvertInventoryBagToEntityBase(MyObjectBuilder_EntityBase oldBagBuilder, Vector3 linearVelocity, Vector3 angularVelocity)
        {
            MyObjectBuilder_EntityBase newBuilder = ConvertBuilderToEntityBase(oldBagBuilder, null);
            if (newBuilder == null)
                return null;

            if (newBuilder.ComponentContainer == null)
                newBuilder.ComponentContainer = MyObjectBuilderSerializer.CreateNewObject(typeof(MyObjectBuilder_ComponentContainer), newBuilder.SubtypeName) as MyObjectBuilder_ComponentContainer;

            foreach (var componentBuidler in newBuilder.ComponentContainer.Components)
            {
                if (componentBuidler.Component is MyObjectBuilder_PhysicsComponentBase)
                {
                    // Data already written.
                    return newBuilder;
                }
            }

            MyObjectBuilder_PhysicsComponentBase physicsComponent = MyObjectBuilderSerializer.CreateNewObject(typeof(MyObjectBuilder_PhysicsBodyComponent),
                newBuilder.SubtypeName) as MyObjectBuilder_PhysicsComponentBase;
            newBuilder.ComponentContainer.Components.Add(new MyObjectBuilder_ComponentContainer.ComponentData() 
                { Component = physicsComponent, TypeId = typeof(MyPhysicsComponentBase).Name });

            physicsComponent.LinearVelocity = linearVelocity;
            physicsComponent.AngularVelocity = angularVelocity;

            return newBuilder;
        }

        protected MyObjectBuilder_EntityBase ConvertInventoryBagToEntityBase(MyObjectBuilder_EntityBase oldBuilder)
        {
            var replicableBuilder = oldBuilder as MyObjectBuilder_ReplicableEntity;
            if (replicableBuilder != null)
            {
                var newBuilder = ConvertInventoryBagToEntityBase(replicableBuilder, replicableBuilder.LinearVelocity, replicableBuilder.AngularVelocity);
                return newBuilder;
            }
            else
            {
                var bagBuilder = oldBuilder as MyObjectBuilder_InventoryBagEntity;
                if (bagBuilder != null)
                {
                    var newBuilder = ConvertInventoryBagToEntityBase(bagBuilder, bagBuilder.LinearVelocity, bagBuilder.AngularVelocity);
                    return newBuilder;
                }
            }

            return null;
        }
    }
}
