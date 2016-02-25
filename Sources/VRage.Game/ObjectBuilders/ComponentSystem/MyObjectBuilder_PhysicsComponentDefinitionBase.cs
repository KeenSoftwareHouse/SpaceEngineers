using System;
using System.ComponentModel;
using ProtoBuf;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicsComponentDefinitionBase : MyObjectBuilder_ComponentDefinitionBase
    {
        public enum MyMassPropertiesComputationType
        {
            None,
            Box,
            Sphere,
            Capsule,
            Cylinder
        }

        [Flags]
        public enum MyUpdateFlags
        {
            Gravity = 1 << 0,
        }

        [ProtoMember, DefaultValue(MyMassPropertiesComputationType.None)]
        public MyMassPropertiesComputationType MassPropertiesComputation = MyMassPropertiesComputationType.None;

        [ProtoMember, DefaultValue(RigidBodyFlag.RBF_DEFAULT)]
        public RigidBodyFlag RigidBodyFlags = RigidBodyFlag.RBF_DEFAULT;

        [ProtoMember, DefaultValue(null)]
        public string CollisionLayer = null;

        [ProtoMember, DefaultValue(null)]
        public float? LinearDamping = null;

        [ProtoMember, DefaultValue(null)]
        public float? AngularDamping = null;

        [ProtoMember]
        public bool ForceActivate;

        [ProtoMember]
        public MyUpdateFlags UpdateFlags;

        [ProtoMember]
        public bool Serialize;

    }
}
