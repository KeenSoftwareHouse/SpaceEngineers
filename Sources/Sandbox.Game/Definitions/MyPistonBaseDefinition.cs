using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PistonBaseDefinition))]
    public class MyPistonBaseDefinition : MyCubeBlockDefinition
    {
        public float Minimum;
        public float Maximum;
        public string TopPart;
        public float MaxVelocity;
        public float RequiredPowerInput;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_PistonBaseDefinition)builder;
            Minimum = ob.Minimum;
            Maximum = ob.Maximum;
            TopPart = ob.TopPart;
            MaxVelocity = ob.MaxVelocity;
            RequiredPowerInput = ob.RequiredPowerInput;
        }
    }
}
