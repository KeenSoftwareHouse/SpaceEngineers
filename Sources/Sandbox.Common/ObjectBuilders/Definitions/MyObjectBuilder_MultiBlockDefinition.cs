using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MultiBlockDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class MyOBMultiBlockPartDefinition
        {
            [ProtoMember(1)]
            public SerializableDefinitionId Id;

            [ProtoMember(2)]
            public SerializableVector3I Position;

            [ProtoMember(3)]
            public SerializableBlockOrientation Orientation;
        }

        [XmlArrayItem("BlockDefinition")]
        [ProtoMember(1), DefaultValue(null)]
        public MyOBMultiBlockPartDefinition[] BlockDefinitions = null;
    }
}
