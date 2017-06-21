using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_LightingBlockDefinition))]
    public class MyLightingBlockDefinition : MyCubeBlockDefinition
    {
        public MyBounds LightRadius;
        public MyBounds LightReflectorRadius;
        public MyBounds LightFalloff;
        public MyBounds LightIntensity;
        public MyBounds BlinkIntervalSeconds;
        public MyBounds BlinkLenght;
        public MyBounds BlinkOffset;
	    public MyStringHash ResourceSinkGroup;
        public float RequiredPowerInput;
        public string LightGlare;
        public bool HasPhysics;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_LightingBlockDefinition)builder;

            BlinkIntervalSeconds = ob.LightBlinkIntervalSeconds;
            BlinkLenght = ob.LightBlinkLenght;
            BlinkOffset = ob.LightBlinkOffset;
            LightRadius = ob.LightRadius;
            LightReflectorRadius = ob.LightReflectorRadius;
            LightFalloff = ob.LightFalloff;
            LightIntensity     = ob.LightIntensity;
	        ResourceSinkGroup = MyStringHash.GetOrCompute(ob.ResourceSinkGroup);
            RequiredPowerInput = ob.RequiredPowerInput;
            LightGlare         = ob.LightGlare;
            HasPhysics = ob.HasPhysics;
        }
    }
}
