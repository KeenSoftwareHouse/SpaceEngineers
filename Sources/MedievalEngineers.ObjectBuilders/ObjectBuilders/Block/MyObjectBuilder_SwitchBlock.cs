using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using System.Xml.Serialization;
using VRage.Game;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_SwitchBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoMember]
        public int State;
    }
}
