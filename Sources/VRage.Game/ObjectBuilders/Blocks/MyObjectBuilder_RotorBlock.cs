using ProtoBuf;
using VRage.ObjectBuilders;
using Sandbox.Common.ObjectBuilders;
using System.Xml.Serialization;

namespace Medieval.ObjectBuilders.Blocks 
{

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RotorBlock : MyObjectBuilder_CogWheelsBlock 
    {
        [ProtoMember]
        public bool RotationEnabled;
    }
}
