using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components.Session;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_SessionComponentResearchDefinition))]
    public class MySessionComponentResearchDefinition: MySessionComponentDefinition
    {
        public bool WhitelistMode;
        public List<MyDefinitionId> Researches = new List<MyDefinitionId>();

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_SessionComponentResearchDefinition;

            WhitelistMode = ob.WhitelistMode;
            if (ob.Researches != null)
                foreach (var research in ob.Researches)
                    Researches.Add(research);
        }
    }
}
