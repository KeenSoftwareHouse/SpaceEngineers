using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_TextPanelDefinition))]
    public class MyTextPanelDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float RequiredPowerInput;
        public int TextureResolution;
        public int TextureAspectRadio;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_TextPanelDefinition)builder;

	        ResourceSinkGroup = MyStringHash.GetOrCompute(ob.ResourceSinkGroup);
            RequiredPowerInput = ob.RequiredPowerInput;
            TextureResolution = ob.TextureResolution;
            TextureAspectRadio = ob.TextureAspectRadio;
        }
    }
}