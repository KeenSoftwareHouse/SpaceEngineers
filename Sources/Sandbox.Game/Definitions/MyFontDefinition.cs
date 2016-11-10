using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_FontDefinition))]
    public class MyFontDefinition : MyDefinitionBase
    {
	    public string Path;
        public bool Default;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var font = builder as MyObjectBuilder_FontDefinition;
            MyDebug.AssertDebug(font != null, "Initializing font definition using wrong object builder.!");
            
            Path = font.Path;
            Default = font.Default;
        }
    }
}
