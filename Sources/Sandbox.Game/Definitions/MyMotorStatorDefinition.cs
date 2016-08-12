using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_MotorStatorDefinition))]
    public class MyMotorStatorDefinition : MyMechanicalConnectionBlockBaseDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float RequiredPowerInput;
        public float MaxForceMagnitude;
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
            RotorDisplacementMin = ob.RotorDisplacementMin;
            RotorDisplacementMax = ob.RotorDisplacementMax;
            RotorDisplacementInModel = ob.RotorDisplacementInModel;
        }
    }
}
