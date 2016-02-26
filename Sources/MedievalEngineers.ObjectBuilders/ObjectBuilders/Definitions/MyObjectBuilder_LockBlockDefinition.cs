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
    /// Definition for one small grid lock block (used inside large block as mechanical subblock).
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_LockBlockDefinition : MyObjectBuilder_MechanicalSubBlockDefinition
    {
        [ProtoMember]
        public float LockAngle;

        [ProtoMember, DefaultValue(null)]
        public string SoundLock = null;

        [ProtoMember, DefaultValue(null)]
        public string SoundUnlock = null;
    }
}
