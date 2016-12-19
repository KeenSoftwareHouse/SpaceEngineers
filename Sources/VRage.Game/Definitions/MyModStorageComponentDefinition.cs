using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ModStorageComponentDefinition))]
    public class MyModStorageComponentDefinition : MyComponentDefinitionBase
    {
        public Guid[] RegisteredStorageGuids;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_ModStorageComponentDefinition;
            RegisteredStorageGuids = ob.RegisteredStorageGuids;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_ModStorageComponentDefinition;
            ob.RegisteredStorageGuids = RegisteredStorageGuids;
            return ob;
        }
    }
}
