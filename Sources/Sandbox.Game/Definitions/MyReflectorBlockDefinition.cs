using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ReflectorBlockDefinition))]
    public class MyReflectorBlockDefinition : MyLightingBlockDefinition
    {
        public string ReflectorTexture;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_ReflectorBlockDefinition)builder;
            ReflectorTexture = ob.ReflectorTexture;
        }
    }
}
