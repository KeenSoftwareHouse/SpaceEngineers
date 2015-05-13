using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipControllerDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public bool EnableFirstPerson;
        [ProtoMember(2)]
        public bool EnableShipControl;
    }
}
