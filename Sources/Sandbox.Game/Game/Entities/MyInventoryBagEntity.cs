using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.Game.Components;
using VRageMath;
using VRage.Game.Components;
using Sandbox.Game.Entities.Character;
using Sandbox.Common;
using Sandbox.Engine.Models;
using Havok;
using Sandbox.Game.World;
using System.Diagnostics;
using VRage;
using VRage.Game.Models;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Inventory;
using VRage.Game.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.ModAPI;
using Sandbox.Game.Multiplayer;

namespace Sandbox.Game.Entities
{

    /// <summary>
    /// Inventory bag spawned when character died, container breaks, or when entity from other inventory cannot be spawned then bag spawned with the item in its inventory.
    /// </summary>
    [MyEntityType(typeof(MyObjectBuilder_ReplicableEntity), mainBuilder: false)] // Backward compatibility
    [MyEntityType(typeof(MyObjectBuilder_InventoryBagEntity), mainBuilder: true)]
    public class MyInventoryBagEntity : MyEntity, IMyInventoryBag
    {
        private const string INVENTORY_USE_DUMMY_NAME = "inventory";
        Vector3 m_gravity = Vector3.Zero;
        MyDefinitionId m_definitionId;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            // Fix old EntityDefinitionId with MyObjectBuilder_EntityBase.
            if (objectBuilder.EntityDefinitionId != null && objectBuilder.EntityDefinitionId.Value.TypeId != typeof(MyObjectBuilder_InventoryBagEntity))
            {
                objectBuilder.EntityDefinitionId = new SerializableDefinitionId(typeof(MyObjectBuilder_InventoryBagEntity), objectBuilder.EntityDefinitionId.Value.SubtypeName);
            }

            base.Init(objectBuilder);

            if (objectBuilder is MyObjectBuilder_InventoryBagEntity)
            {
                var builderIBE = (MyObjectBuilder_InventoryBagEntity)objectBuilder;
                var physicsComponentBuilder = GetPhysicsComponentBuilder(builderIBE);
                if (physicsComponentBuilder == null)
                {
                    Physics.LinearVelocity = builderIBE.LinearVelocity;
                    Physics.AngularVelocity = builderIBE.AngularVelocity;
                }
            }
            else if (objectBuilder is MyObjectBuilder_ReplicableEntity)
            {
                // Backward compatibility
                var builderRE = (MyObjectBuilder_ReplicableEntity)objectBuilder;
                Physics.LinearVelocity = builderRE.LinearVelocity;
                Physics.AngularVelocity = builderRE.AngularVelocity;
            }
            else
            {
                Debug.Fail("Unknown object builder for inventory bag");
            }
        }

        internal static MyObjectBuilder_PhysicsComponentBase GetPhysicsComponentBuilder(MyObjectBuilder_InventoryBagEntity builder)
        {
            if (builder.ComponentContainer != null && builder.ComponentContainer.Components.Count > 0)
            {
                foreach (var componentData in builder.ComponentContainer.Components)
                {
                    if (componentData.Component is MyObjectBuilder_PhysicsComponentBase)
                    {
                        return componentData.Component as MyObjectBuilder_PhysicsComponentBase;
                    }
                }
            }

            return null;
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (Physics != null)
            {
                Physics.RigidBody.Gravity = m_gravity;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            m_gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(PositionComp.GetPosition());
        }

    }
}
