using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.VRageData;

namespace Medieval.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MechanicalTransferBlockDefinition : MyObjectBuilder_MechanicalSourceBlockDefinition
    {
        [ProtoContract]
        public class MyOBMechanicalSwitch
        {
            [XmlAttribute]
            [ProtoMember]
            public string SwitchName;

            [XmlAttribute]
            [ProtoMember]
            public string[] SubBlocks;
        }

        [ProtoContract]
        public class MyOBMechanicalLock
        {
            [XmlAttribute]
            [ProtoMember]
            public string LockName;

            [XmlAttribute]
            [ProtoMember]
            public bool LockForwardDirection;

            [XmlAttribute]
            [ProtoMember]
            public string[] SubBlocks;
        }

        [XmlArrayItem("Switch")]
        [ProtoMember, DefaultValue(null)]
        public MyOBMechanicalSwitch[] Switches = null;

        [XmlArrayItem("Lock")]
        [ProtoMember, DefaultValue(null)]
        public MyOBMechanicalLock[] Locks = null;
    }
}
