using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_DestructionDefinition))]
    public class MyDestructionDefinition : MyDefinitionBase
    {
        public float DestructionDamage;
        public string Icon;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_DestructionDefinition;

            DestructionDamage = ob.DestructionDamage;
            Icon = ob.Icon;
        }

        public void Merge(MyDestructionDefinition src)
        {
            DestructionDamage = src.DestructionDamage;
            Icon = src.Icon;
        }
    }
}
