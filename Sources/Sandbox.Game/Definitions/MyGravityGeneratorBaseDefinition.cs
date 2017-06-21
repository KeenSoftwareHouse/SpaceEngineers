using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GravityGeneratorBaseDefinition))]
    public class MyGravityGeneratorBaseDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float MinGravityAcceleration;
        public float MaxGravityAcceleration;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obGenerator = builder as MyObjectBuilder_GravityGeneratorBaseDefinition;
            MyDebug.AssertDebug(obGenerator != null, "Initializing definition using wrong object builder.");
	        ResourceSinkGroup = MyStringHash.GetOrCompute(obGenerator.ResourceSinkGroup);
            MinGravityAcceleration = obGenerator.MinGravityAcceleration;
            MaxGravityAcceleration = obGenerator.MaxGravityAcceleration;
        }
    }
}
