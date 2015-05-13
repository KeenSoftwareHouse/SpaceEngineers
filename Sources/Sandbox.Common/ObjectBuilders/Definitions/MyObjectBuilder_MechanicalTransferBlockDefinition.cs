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
            [ProtoMember(1)]
            public string SwitchName;

            [XmlAttribute]
            [ProtoMember(2)]
            public string[] SubBlocks;
        }

        [ProtoContract]
        public class MyOBMechanicalLock
        {
            [XmlAttribute]
            [ProtoMember(1)]
            public string LockName;

            [XmlAttribute]
            [ProtoMember(2)]
            public bool LockForwardDirection;

            [XmlAttribute]
            [ProtoMember(3)]
            public string[] SubBlocks;
        }

        [XmlArrayItem("Switch")]
        [ProtoMember(1), DefaultValue(null)]
        public MyOBMechanicalSwitch[] Switches = null;

        [XmlArrayItem("Lock")]
        [ProtoMember(2), DefaultValue(null)]
        public MyOBMechanicalLock[] Locks = null;
    }
}
