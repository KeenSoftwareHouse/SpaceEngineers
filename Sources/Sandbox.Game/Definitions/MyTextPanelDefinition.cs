using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_TextPanelDefinition))]
    class MyTextPanelDefinition : MyCubeBlockDefinition
    {
        public float RequiredPowerInput;
        public int TextureResolution;
        public int TextureAspectRadio;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_TextPanelDefinition)builder;
                 
            RequiredPowerInput = ob.RequiredPowerInput;
            TextureResolution = ob.TextureResolution;
            TextureAspectRadio = ob.TextureAspectRadio;
        }
    }
}