using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_EnvironmentItemDefinition))]
    public class MyEnvironmentItemDefinition : MyPhysicalModelDefinition
    {
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_EnvironmentItemDefinition;
            MyDebug.AssertDebug(ob != null);
        }
    }
}
