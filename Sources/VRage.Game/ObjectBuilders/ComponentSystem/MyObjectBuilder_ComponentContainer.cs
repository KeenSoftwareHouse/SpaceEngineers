using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ComponentContainer : MyObjectBuilder_Base
    {
        [ProtoContract]
        public class ComponentData
        {
            [ProtoMember]
            public string TypeId;

            [ProtoMember]
            [DynamicObjectBuilder]
            [XmlElement(Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_ComponentBase>))]
            public MyObjectBuilder_ComponentBase Component;
        }

        [ProtoMember]
        public List<ComponentData> Components = new List<ComponentData>();
    }
}
