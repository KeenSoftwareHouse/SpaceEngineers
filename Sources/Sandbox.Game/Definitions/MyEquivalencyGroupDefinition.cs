using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_EquivalencyGroupDefinition))]
    public class MyEquivalencyGroupDefinition: MyDefinitionBase
    {
        public MyDefinitionId MainElement;
        public bool ForceMainElement;
        public List<MyDefinitionId> Equivalents;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_EquivalencyGroupDefinition;

            if (ob != null)
            {
                MainElement = ob.MainId;
                ForceMainElement = ob.ForceMainId;
                Equivalents = new List<MyDefinitionId>();
                foreach (var equivalent in ob.EquivalentId)
                    Equivalents.Add(equivalent);
            }
        }
    }
}
