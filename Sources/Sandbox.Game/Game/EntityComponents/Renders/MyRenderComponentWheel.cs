using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.EntityComponents.Renders
{
	public class MyRenderComponentWheel : MyRenderComponentCubeBlock
	{
		private int m_lastEffectCreationTime = 0;
		private int m_effectCreationInterval = 125;
		private static int m_lastGlobalEffectCreationTime = 0;

		public override void Draw()
		{
			base.Draw();
		}

		public override void OnAddedToContainer()
		{
			base.OnAddedToContainer();
			m_lastEffectCreationTime = MySandboxGame.TotalGamePlayTimeInMilliseconds + 2*m_effectCreationInterval;
		}

        public bool TrySpawnParticle(Vector3 position, Vector3 normal, string particleName)
		{
			if (!MyFakes.ENABLE_DRIVING_PARTICLES)
				return false;

			MyWheel wheel = Entity as MyWheel;
			if(wheel == null)
				return false;

            if (MyUtils.GetRandomInt(10) < 5)//spawn only about 20% of particles
                return false;
            var speedMultiplier = wheel.GetTopMostParent().Physics.LinearVelocity.Length() / MyGridPhysics.ShipMaxLinearVelocity();
			var currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (currentTime - m_lastEffectCreationTime < 5)
                return false;

			MyParticleEffect drivingEffect = null;
            if (!MyParticlesManager.TryCreateParticleEffect(particleName, out drivingEffect))
				return false;

			m_lastEffectCreationTime = currentTime;
			m_lastGlobalEffectCreationTime = m_lastEffectCreationTime;

            drivingEffect.WorldMatrix = MatrixD.CreateWorld(position, normal, Vector3.CalculatePerpendicularVector(normal));
            var speedScaleMultiplier = 1.0f + speedMultiplier * 3.0f;
            drivingEffect.UserScale = speedScaleMultiplier;

			return true;
		}
	}
}
