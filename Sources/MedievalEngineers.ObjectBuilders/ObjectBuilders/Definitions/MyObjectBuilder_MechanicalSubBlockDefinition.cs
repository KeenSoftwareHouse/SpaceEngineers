using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage;
using VRage.Game;

namespace Medieval.ObjectBuilders.Definitions
{
    /// <summary>
    /// Definition for common subblock mechanical parts.
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_MechanicalSubBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public SerializableVector3 PivotOffset;

        [ProtoMember, DefaultValue(25000f)]
        public float BreakableThreshold = 25000f;
    }
}
