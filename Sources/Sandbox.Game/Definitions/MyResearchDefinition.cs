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
    [MyDefinitionType(typeof(MyObjectBuilder_ResearchDefinition))]
    public class MyResearchDefinition : MyDefinitionBase
    {
        public List<MyDefinitionId> Entries;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_ResearchDefinition;

            Entries = new List<MyDefinitionId>();
            foreach (var definitionId in ob.Entries)
                Entries.Add(definitionId);
        }
    }
}
