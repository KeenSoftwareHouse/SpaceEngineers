using Sandbox;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry;
using System;
using System.Collections.Generic;
using VRage.Game.ObjectBuilders;
using VRage.Library.Utils;
using VRageMath;

namespace SpaceEngineers.Game.World.Environment
{
	[MyEnvironmentalParticleLogicType(typeof(MyObjectBuilder_EnvironmentalParticleLogicSpace))]
	class MyEnvironmentalParticleLogicSpace : MyEnvironmentalParticleLogic
	{
		int m_lastParticleSpawn = 0;

		public MyEntity ControlledEntity { get { return MySession.ControlledEntity as MyEntity; } }
		public Vector3 ControlledVelocity { get { return ControlledEntity is MyCockpit || ControlledEntity is MyRemoteControl ? ControlledEntity.GetTopMostParent().Physics.LinearVelocity : ControlledEntity.Physics.LinearVelocity; } }

		public bool ShouldDrawParticles { get { return (HasControlledNonZeroVelocity() && !IsInGridAABB() && !IsNearPlanet()); } }

		public override void Init(MyObjectBuilder_EnvironmentalParticleLogic builder)
		{
			base.Init(builder);

			var objectBuilder = builder as MyObjectBuilder_EnvironmentalParticleLogicSpace;
			if (objectBuilder == null)
				return;
		}

		public override void UpdateBeforeSimulation()
		{
			base.UpdateBeforeSimulation();

			if (!ShouldDrawParticles)
				return;

			var distance = ParticleSpawnDistance;
			var angle = Math.PI/2.0f;
			var tanFovSq = Math.Tan(angle / 2.0f);
			var velocityVector = ControlledVelocity;
			var sweepArea = 4 * distance * distance * tanFovSq;
			var particlesLeftToSpawn = (0.25f + MyRandom.Instance.NextFloat() * 1.25f) * velocityVector.Length() * sweepArea * ParticleDensity * MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS; // particles/update
			while (particlesLeftToSpawn-- > 1.0f)
			{
				var minAngle = angle / 2.0f;
				var maxAngle = minAngle + angle;
				var phi = minAngle + MyRandom.Instance.NextFloat() * (maxAngle - minAngle);
				var theta = minAngle + MyRandom.Instance.NextFloat() * (maxAngle - minAngle);
				float degree = (float)Math.PI/180.0f;
				if (Math.Abs(phi - Math.PI / 2.0) < 2.0f * degree && Math.Abs(theta - Math.PI / 2.0) < 2.0f * degree)
				{
					phi += (Math.Sign(MyRandom.Instance.NextFloat()) * 2.0f * degree);
					theta += (Math.Sign(MyRandom.Instance.NextFloat()) * 2.0f * degree);
				}
				var sinTheta = (float)Math.Sin(theta);
				var cosTheta = (float)Math.Cos(theta);
				var sinPhi = (float)Math.Sin(phi);
				var cosPhi = (float)Math.Cos(phi);

				var upVector = MySector.MainCamera.UpVector;
				var forwardVector = Vector3.Normalize(velocityVector);
				var leftVector = Vector3.Cross(forwardVector, -upVector);
				Vector3 particlePosition = MySector.MainCamera.Position
					+ distance * (upVector * cosTheta
					+ leftVector * sinTheta * cosPhi
					+ forwardVector * sinTheta * sinPhi);

				Spawn(particlePosition);
				m_lastParticleSpawn = MySandboxGame.TotalGamePlayTimeInMilliseconds;
			}
		}

		public override void UpdateAfterSimulation()
		{
			if (!ShouldDrawParticles)
				DeactivateAll();

			base.UpdateAfterSimulation();
		}

		public override void Draw()
		{
			base.Draw();

			if (!ShouldDrawParticles)
				return;

			var direction = -Vector3.Normalize(ControlledVelocity);
			float thickness = 0.025f;
			float speed = ControlledVelocity.Length();

			float length = (float)MathHelper.Clamp(speed / 100.0f, 0.0, 1.0);

			if (length < 0.1f)
				return;

			foreach (var particle in m_activeParticles)
			{
				if (!particle.Active)
					continue;

				MyTransparentGeometry.AddLineBillboard(particle.Material, particle.Color, particle.Position, direction, length, thickness);
			}
		}

		private bool IsInGridAABB()
		{
			var sphere = new BoundingSphereD(MySector.MainCamera.Position, 0.1f);
			List<MyEntity> entityList = null;
			try
			{
				entityList = MyEntities.GetEntitiesInSphere(ref sphere);

				foreach (var entity in entityList)
				{
					var grid = entity as MyCubeGrid;

					if (grid == null || grid.GridSizeEnum == MyCubeSize.Small)
						continue;

					return true;
				}
			}
			finally
			{
				if (entityList != null)
					entityList.Clear();
			}

			return false;
		}

		private bool HasControlledNonZeroVelocity()
		{
			var entity = ControlledEntity;
			if (entity == null || MySession.IsCameraUserControlledSpectator())
				return false;

			var remoteControl = entity as MyRemoteControl;
			if (remoteControl != null)
				entity = remoteControl.GetTopMostParent();

			var cockpit = entity as MyCockpit;
			if (cockpit != null)
				entity = cockpit.GetTopMostParent();

			if (entity != null && entity.Physics != null && entity.Physics.LinearVelocity != Vector3.Zero)
				return true;

			return false;
		}

		private bool IsNearPlanet()
		{
			if (ControlledEntity == null)
				return false;

			return !Vector3.IsZero(MyGravityProviderSystem.CalculateGravityInPointForGrid(ControlledEntity.PositionComp.GetPosition()));
		}
	}
}
