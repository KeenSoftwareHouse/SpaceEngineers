using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_EdgesDefinition))]
    public class MyEdgesDefinition : MyDefinitionBase
    {
        public MyEdgesModelSet Large;
        public MyEdgesModelSet Small;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_EdgesDefinition;
            MyDebug.AssertDebug(ob != null);

            this.Large = ob.Large;
            this.Small = ob.Small;
        }
    }
}
