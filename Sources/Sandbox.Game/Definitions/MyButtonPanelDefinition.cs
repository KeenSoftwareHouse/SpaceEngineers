using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ButtonPanelDefinition))]
    public class MyButtonPanelDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public int ButtonCount;
        public string[] ButtonSymbols;
        public Vector4[] ButtonColors;
        public Vector4 UnassignedButtonColor;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_ButtonPanelDefinition;
	        ResourceSinkGroup = MyStringHash.GetOrCompute(ob.ResourceSinkGroup);
            ButtonCount = ob.ButtonCount;
            ButtonSymbols = ob.ButtonSymbols;
            ButtonColors = ob.ButtonColors;
            UnassignedButtonColor = ob.UnassignedButtonColor;
        }
    }
}
