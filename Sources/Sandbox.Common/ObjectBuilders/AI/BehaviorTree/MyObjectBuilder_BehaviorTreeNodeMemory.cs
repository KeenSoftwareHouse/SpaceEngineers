using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.AI
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorTreeNodeMemory : MyObjectBuilder_Base
    {
        [XmlAttribute]
        [ProtoMember, DefaultValue(false)]
        public bool InitCalled = false;

    }
}
