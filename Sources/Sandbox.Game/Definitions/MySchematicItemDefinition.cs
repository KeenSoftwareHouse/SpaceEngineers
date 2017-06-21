using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_SchematicItemDefinition))]
    public class MySchematicItemDefinition : MyUsableItemDefinition
    {
        public MyDefinitionId Research;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_SchematicItemDefinition;

            MyDebug.AssertDebug(ob.Research.HasValue, String.Format("No research specified for {0}", ob.Id));
            Research = ob.Research.Value;
        }
    }
}
