using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Medieval.ObjectBuilders.Definitions
{
    /// <summary>
    /// Definition for one small grid cog wheel (used inside large block as mechanical subblock).
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_CogWheelBlockDefinition : MyObjectBuilder_MechanicalSubBlockDefinition
    {
        [ProtoMember]
        public int TeethCount;

        [ProtoMember]
        public float MaxFrictionTorque;
    }
}
