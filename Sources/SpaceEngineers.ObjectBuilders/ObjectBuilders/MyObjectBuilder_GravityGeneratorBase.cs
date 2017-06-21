using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;
using VRage;
using System.ComponentModel;
using VRage.Game;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_GravityGeneratorBase : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember, DefaultValue(9.81f)]
        public float GravityAcceleration = 9.81f;
    }
}
