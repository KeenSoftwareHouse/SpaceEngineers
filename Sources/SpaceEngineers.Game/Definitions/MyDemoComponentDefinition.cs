using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Components.Session;
using VRage.Game.Definitions;

namespace SpaceEngineers.Game.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_DemoComponentDefinition))]
    public class MyDemoComponentDefinition : MySessionComponentDefinition
    {
        public float Float;
        public int Int;
        public string String;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_DemoComponentDefinition) builder;

            Float = ob.Float;
            Int = ob.Int;
            String = ob.String;
        }
    }
}
