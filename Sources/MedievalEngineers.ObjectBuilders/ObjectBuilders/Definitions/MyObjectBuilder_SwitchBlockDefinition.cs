using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage;

namespace Medieval.ObjectBuilders.Definitions
{
    /// <summary>
    /// Definition for one small grid switch block (used inside large block as mechanical subblock).
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_SwitchBlockDefinition : MyObjectBuilder_MechanicalSubBlockDefinition
    {
        [ProtoMember]
        public SerializableVector3 SwitchOffset;
    }
}
