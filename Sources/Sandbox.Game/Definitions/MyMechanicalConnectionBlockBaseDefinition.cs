using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{

    [MyDefinitionType(typeof(MyObjectBuilder_MechanicalConnectionBlockBaseDefinition))]
    public class MyMechanicalConnectionBlockBaseDefinition : MyCubeBlockDefinition
    {
        public string TopPart;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_MechanicalConnectionBlockBaseDefinition;
            TopPart = ob.TopPart ?? ob.RotorPart;
        }
    }
}
