using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.TransparentGeometry.Particles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.EntityComponents.Renders
{
	class MyRenderComponentWheel : MyRenderComponentCubeBlock
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

		public bool TrySpawnParticle(Vector3D worldPosition)
		{
			if (!MyFakes.ENABLE_DRIVING_PARTICLES)
				return false;

			MyWheel wheel = Entity as MyWheel;
			if(wheel == null)
				return false;

			var speedMultiplier = wheel.GetTopMostParent().Physics.LinearVelocity.Length() / MyGridPhysics.ShipMaxLinearVelocity();
			var currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
			if (currentTime - m_lastGlobalEffectCreationTime < 5
				|| currentTime - m_lastEffectCreationTime < m_effectCreationInterval * MyUtils.GetRandomFloat(0.9f * (1.5f - speedMultiplier), 1.1f / (0.25f + speedMultiplier)))
				return false;

			MyParticleEffect drivingEffect = null;
			if (!MyParticlesManager.TryCreateParticleEffect(51, out drivingEffect))
				return false;

			m_lastEffectCreationTime = currentTime;
			m_lastGlobalEffectCreationTime = m_lastEffectCreationTime;

			drivingEffect.WorldMatrix = MatrixD.CreateTranslation(worldPosition);
			drivingEffect.Preload = 1.0f;
			var speedScaleMultiplier = 1.0f + speedMultiplier*6.0f;
			drivingEffect.UserScale = 0.25f * speedScaleMultiplier;

			return true;
		}
	}
}
