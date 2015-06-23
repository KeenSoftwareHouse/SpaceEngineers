using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Wheel))]
    class MyWheel : MyMotorRotor
    {
        public float Friction { get; set; }

        public MyWheel()
        {
            Friction = 1.5f;
            IsWorkingChanged += MyWheel_IsWorkingChanged;
        }

        void MyWheel_IsWorkingChanged(MyCubeBlock obj)
        {
            if(Stator != null)
                Stator.UpdateIsWorking();
        }

        public override void ContactPointCallback(ref MyGridContactInfo value)
        {
            //return;
            var prop = value.Event.ContactProperties;
            prop.Friction = Friction;
            prop.Restitution = 0.5f;
            value.EnableParticles = false;
            value.RubberDeformation = true;
        }
    }
}
