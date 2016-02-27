using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_MotorSuspensionDefinition : MyObjectBuilder_MotorStatorDefinition
    {
        [ProtoMember]
        public float MaxSteer = 0.8f;

        [ProtoMember]
        public float SteeringSpeed = 0.1f;

        [ProtoMember]
        public float PropulsionForce = 10000;

        [ProtoMember]
        public float MinHeight = -0.32f;

        [ProtoMember]
        public float MaxHeight = 0.26f;
    }
}
