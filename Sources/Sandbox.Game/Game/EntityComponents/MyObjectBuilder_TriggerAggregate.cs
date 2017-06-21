using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TriggerAggregate : MyObjectBuilder_ComponentBase
    {
        [ProtoMember, DefaultValue(null)]
        [DynamicObjectBuilderItem]
        [Serialize(MyObjectFlags.Nullable)]
        [XmlElement("AreaTriggers", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_TriggerBase>))]
        public List<MyObjectBuilder_TriggerBase> AreaTriggers = null;
    }
}
