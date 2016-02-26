using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using System.ComponentModel;
using VRage.Game;
using VRage.ObjectBuilders;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_GravityGeneratorSphere : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float Radius = 150f;

        [ProtoMember, DefaultValue(9.81f)]
        public float GravityAcceleration = 9.81f;
    }
}
