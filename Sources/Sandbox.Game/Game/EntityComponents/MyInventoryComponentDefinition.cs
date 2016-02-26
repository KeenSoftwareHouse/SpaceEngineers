using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    [MyDefinitionType(typeof(MyObjectBuilder_InventoryComponentDefinition))]
    public class MyInventoryComponentDefinition : MyComponentDefinitionBase
    {
        public float Volume;
        public float Mass;
        public bool RemoveEntityOnEmpty;
        public bool MultiplierEnabled;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_InventoryComponentDefinition;
            Volume = ob.Volume;
            if (ob.Size != null) 
            {
                Vector3 size = ob.Size.Value;
                Volume = size.Volume;
            }
            Mass = ob.Mass;
            RemoveEntityOnEmpty = ob.RemoveEntityOnEmpty;
            MultiplierEnabled = ob.MultiplierEnabled;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_InventoryComponentDefinition;

            ob.Volume = Volume;
            ob.Mass = Mass;
            ob.RemoveEntityOnEmpty = RemoveEntityOnEmpty;
            ob.MultiplierEnabled = MultiplierEnabled;

            return ob;
        }
    }
}
