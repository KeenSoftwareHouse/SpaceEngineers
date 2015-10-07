using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_MotorStatorDefinition))]
    public class MyMotorStatorDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float RequiredPowerInput;
        public float MaxForceMagnitude;
        public string RotorPart;
        public float RotorDisplacementMin;
        public float RotorDisplacementMax;
        public float RotorDisplacementInModel;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_MotorStatorDefinition)builder;
	        ResourceSinkGroup = MyStringHash.GetOrCompute(ob.ResourceSinkGroup);
            RequiredPowerInput = ob.RequiredPowerInput;
            MaxForceMagnitude = ob.MaxForceMagnitude;
            RotorPart = ob.RotorPart;        
            RotorDisplacementMin = ob.RotorDisplacementMin;
            RotorDisplacementMax = ob.RotorDisplacementMax;
            RotorDisplacementInModel = ob.RotorDisplacementInModel;
        }
    }
}
