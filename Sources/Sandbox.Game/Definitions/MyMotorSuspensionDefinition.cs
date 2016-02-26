using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_MotorSuspensionDefinition))]
    public class MyMotorSuspensionDefinition : MyMotorStatorDefinition
    {
        public float MaxSteer;
        public float SteeringSpeed;
        public float PropulsionForce;
        public float MinHeight;
        public float MaxHeight;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_MotorSuspensionDefinition)builder;
            MaxSteer = ob.MaxSteer;
            SteeringSpeed = ob.SteeringSpeed;
            PropulsionForce = ob.PropulsionForce;
            MinHeight = ob.MinHeight;
            MaxHeight = ob.MaxHeight;
        }
    }
}
