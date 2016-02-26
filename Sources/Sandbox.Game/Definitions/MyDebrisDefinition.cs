using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_DebrisDefinition))]
    public class MyDebrisDefinition : MyDefinitionBase
    {
        public string Model;
        public MyDebrisType Type;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_DebrisDefinition;
            MyDebug.AssertDebug(ob != null);

            this.Model = ob.Model;
            this.Type = ob.Type;
        }
    }
}
