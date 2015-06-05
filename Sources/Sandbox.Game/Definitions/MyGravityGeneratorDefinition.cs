using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

using Sandbox.Engine.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GravityGeneratorDefinition))]
    public class MyGravityGeneratorDefinition : MyCubeBlockDefinition
    {
        public float RequiredPowerInput;
        public MyBoundedVector3 FieldSize;
        public MyBounds Gravity;
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obGenerator = builder as MyObjectBuilder_GravityGeneratorDefinition;
            MyDebug.AssertDebug(obGenerator != null, "Initializing gravity generator definition using wrong object builder.");
            RequiredPowerInput = obGenerator.RequiredPowerInput;
            FieldSize = obGenerator.FieldSize;
            Gravity = obGenerator.Gravity;
        }
    }
}
