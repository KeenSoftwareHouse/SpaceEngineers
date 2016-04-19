using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_UsableItemDefinition))]
    public class MyUsableItemDefinition : MyPhysicalItemDefinition
    {
        public string UseSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_UsableItemDefinition;

            UseSound = ob.UseSound;
        }
    }
}
