using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_MechanicalConnectionBlockBaseDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public string RotorPart = null;

        [ProtoMember]
        public string TopPart = null;
    }
}
