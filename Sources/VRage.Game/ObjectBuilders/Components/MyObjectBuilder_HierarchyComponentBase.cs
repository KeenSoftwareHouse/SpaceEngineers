using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game.ObjectBuilders.Components
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_HierarchyComponentBase : MyObjectBuilder_ComponentBase
    {
        [DynamicItem(typeof(MyObjectBuilderDynamicSerializer), true)]
        [XmlArrayItem("MyObjectBuilder_EntityBase", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EntityBase>))]
        public List<MyObjectBuilder_EntityBase> Children = new List<MyObjectBuilder_EntityBase>(); 
    }
}
