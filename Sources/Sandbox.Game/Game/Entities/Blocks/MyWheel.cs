using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents.Renders;
using Sandbox.Game.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Wheel))]
    class MyWheel : MyMotorRotor
    {
        private MyStringHash m_wheelStringHash = MyStringHash.GetOrCompute("Wheel");
        public float Friction { get; set; }

		public new MyRenderComponentWheel Render
		{
			get { return base.Render as MyRenderComponentWheel; }
			set { base.Render = value; }
		}

        public MyWheel()
        {
            Friction = 1.5f;
            IsWorkingChanged += MyWheel_IsWorkingChanged;
			Render = new MyRenderComponentWheel();
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

            if (value.CollidingEntity is MyVoxelBase && MyFakes.ENABLE_DRIVING_PARTICLES)
            {
                MyVoxelBase voxel = value.CollidingEntity as MyVoxelBase;
                Vector3D contactPosition = value.ContactPosition;
                MyStringHash material = MyStringHash.GetOrCompute(voxel.GetMaterialAt(ref contactPosition).MaterialTypeName);
                MyTuple<int, ContactPropertyParticleProperties> particle = MyMaterialPropertiesHelper.Static.GetCollisionEffectAndProperties(MyMaterialPropertiesHelper.CollisionType.Start, m_wheelStringHash, material);
                
                if (Render != null && particle.Item1 > 0)
                    Render.TrySpawnParticle(value.ContactPosition, particle);
            }
        }
    }
}
