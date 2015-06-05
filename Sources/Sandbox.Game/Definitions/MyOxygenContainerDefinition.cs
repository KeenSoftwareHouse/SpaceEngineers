using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_OxygenContainerDefinition))]
    public class MyOxygenContainerDefinition : MyPhysicalItemDefinition, IMyOxygenBottle
    {
        public float Capacity;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_OxygenContainerDefinition;

            Capacity = ob.Capacity;
        }

        float Sandbox.ModAPI.Ingame.IMyOxygenBottle.OxygenLevel
        {
            get
            {
                return Capacity;
            }
        }
    }
}
