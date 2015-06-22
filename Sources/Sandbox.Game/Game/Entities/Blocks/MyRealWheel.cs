using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_RealWheel))]
    class MyRealWheel : MyMotorRotor
    {
        public override void ContactPointCallback(ref MyGridContactInfo value)
        {
            //return;
            var prop = value.Event.ContactProperties;
            prop.Friction = 0.85f;
            prop.Restitution = 0.2f;
            value.EnableParticles = false;
            value.RubberDeformation = true;
        }
    }
}
