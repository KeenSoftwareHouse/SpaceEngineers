using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GravityGeneratorDefinition))]
    public class MyGravityGeneratorDefinition : MyGravityGeneratorBaseDefinition
    {
        public float RequiredPowerInput;
        public Vector3 MinFieldSize;
        public Vector3 MaxFieldSize;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obGenerator = builder as MyObjectBuilder_GravityGeneratorDefinition;
            MyDebug.AssertDebug(obGenerator != null, "Initializing definition using wrong object builder.");
            RequiredPowerInput = obGenerator.RequiredPowerInput;
            MinFieldSize = obGenerator.MinFieldSize;
            MaxFieldSize = obGenerator.MaxFieldSize;
        }
    }
}
