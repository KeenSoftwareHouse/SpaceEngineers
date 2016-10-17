using System.Collections.Generic;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Components
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_HierarchyComponentBase : MyObjectBuilder_ComponentBase
    {
        public List<MyObjectBuilder_EntityBase> Children = new List<MyObjectBuilder_EntityBase>(); 
    }
}
