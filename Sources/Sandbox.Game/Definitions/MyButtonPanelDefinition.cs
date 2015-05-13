using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ButtonPanelDefinition))]
    public class MyButtonPanelDefinition : MyCubeBlockDefinition
    {
        public int ButtonCount;
        public string[] ButtonSymbols;
        public Vector4[] ButtonColors;
        public Vector4 UnassignedButtonColor;

        protected override void Init(Common.ObjectBuilders.Definitions.MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_ButtonPanelDefinition;
            ButtonCount = ob.ButtonCount;
            ButtonSymbols = ob.ButtonSymbols;
            ButtonColors = ob.ButtonColors;
            UnassignedButtonColor = ob.UnassignedButtonColor;
        }
    }
}
