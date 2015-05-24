using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_VirtualMassDefinition))]
    public class MyVirtualMassDefinition : MyCubeBlockDefinition
    {
        public float RequiredPowerInput;
        public float VirtualMass;

        /// <summary>
        ///     Max allowed virtual mass for block
        /// </summary>
        public float MaxVirtualMass = 10000;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obMass = builder as MyObjectBuilder_VirtualMassDefinition;
            MyDebug.AssertDebug(obMass != null, "Initializing virtual mass definition using wrong object builder.");
            RequiredPowerInput = obMass.RequiredPowerInput;
            VirtualMass = obMass.VirtualMass;
        }
    }
}
