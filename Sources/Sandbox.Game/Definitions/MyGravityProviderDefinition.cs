using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GravityProviderDefinition))]
    public class MyGravityProviderDefinition : MyCubeBlockDefinition
    {
        public MyBounds Gravity;
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obGenerator = builder as MyObjectBuilder_GravityProviderDefinition;
            MyDebug.AssertDebug(obGenerator != null, "Initializing gravity provider definition using wrong object builder.");
            Gravity = obGenerator.Gravity;
        }
    }
}
