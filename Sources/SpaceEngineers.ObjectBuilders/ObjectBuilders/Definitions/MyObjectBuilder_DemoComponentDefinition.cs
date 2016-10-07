using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;

namespace Sandbox.ObjectBuilders.Definitions
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DemoComponentDefinition : MyObjectBuilder_SessionComponentDefinition
    {
        public float Float;
        public int Int;
        public string String;
    }
}
