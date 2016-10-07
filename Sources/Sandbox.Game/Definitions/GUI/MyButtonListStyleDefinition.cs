using System.Diagnostics;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions.GUI;
using VRageMath;

namespace Sandbox.Definitions.GUI
{
    [MyDefinitionType(typeof(MyObjectBuilder_ButtonListStyleDefinition))]
    public class MyButtonListStyleDefinition : MyDefinitionBase
    {
        public Vector2 ButtonSize;
        public Vector2 ButtonMargin;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var buttonListStyleOb = builder as MyObjectBuilder_ButtonListStyleDefinition;
            Debug.Assert(buttonListStyleOb != null,"Wrong object builder.");
        }
    }
}
