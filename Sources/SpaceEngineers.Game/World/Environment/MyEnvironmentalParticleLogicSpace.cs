using Sandbox;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders;
using VRage.Library.Utils;
using VRage.Profiler;
using VRageMath;

namespace SpaceEngineers.Game.World.Environment
{
	[MyEnvironmentalParticleLogicType(typeof(MyObjectBuilder_EnvironmentalParticleLogicSpace))]
	class MyEnvironmentalParticleLogicSpace : MyEnvironmentalParticleLogic
	{
		int m_lastParticleSpawn = 0;
		float m_particlesLeftToSpawn = 0.0f;

		public MyEntity ControlledEntity { get { return MySession.Static.ControlledEntity as MyEntity; } }
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

			ProfilerShort.Begin("SpaceParticles.UpdateBeforeSimulation");
			try
			{
				if (!ShouldDrawParticles)
					return;

				if (ControlledVelocity.Length() < 10.0f)
					return;

				var distance = ParticleSpawnDistance;
				var angle = Math.PI / 2.0f;
				var tanFovSq = 1.0f;// Math.Tan(angle / 2.0f);
				var velocityVector = ControlledVelocity - 8.5f * Vector3.Normalize(ControlledVelocity);
				var sweepArea = 4 * distance * distance * tanFovSq;
				m_particlesLeftToSpawn += (float)((0.25f + MyRandom.Instance.NextFloat() * 1.25f) * velocityVector.Length() * sweepArea * ParticleDensity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS); // particles/update
				if (m_particlesLeftToSpawn < 1.0f)
					return;

				var minAngle = angle / 2.0f;
				var maxAngle = minAngle + angle;
				var phi = minAngle + MyRandom.Instance.NextFloat() * (maxAngle - minAngle);
				var theta = minAngle + MyRandom.Instance.NextFloat() * (maxAngle - minAngle);
				var restrictedAngleAmount = 6.0f;

				while (m_particlesLeftToSpawn-- >= 1.0f)
				{
					float degree = (float)Math.PI / 180.0f;
					if (Math.Abs(phi - Math.PI / 2.0) < restrictedAngleAmount * degree && Math.Abs(theta - Math.PI / 2.0) < restrictedAngleAmount * degree)
					{
						phi += (Math.Sign(MyRandom.Instance.NextFloat()) * restrictedAngleAmount * degree);
						theta += (Math.Sign(MyRandom.Instance.NextFloat()) * restrictedAngleAmount * degree);
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
			finally
			{
				ProfilerShort.End();
			}
		}

		public override void UpdateAfterSimulation()
		{
			ProfilerShort.Begin("SpaceParticles.UpdateAfterSimulation");
			if (!ShouldDrawParticles)
			{
				DeactivateAll();
				m_particlesLeftToSpawn = 0;
			}
			ProfilerShort.End();
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

			float length = (float)MathHelper.Clamp(speed / 50.0f, 0.0, 1.0);

			foreach (var particle in m_activeParticles)
			{
				if (!particle.Active)
					continue;

				MyTransparentGeometry.AddLineBillboard(particle.Material, particle.Color, particle.Position, direction, length, thickness);
			}
		}

		private bool IsInGridAABB()
		{
			ProfilerShort.Begin("SpaceParticles.IsInGridAABB");
			var isInGrid = false;
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

					isInGrid = true;
					break;
				}
			}
			finally
			{
				if (entityList != null)
					entityList.Clear();
			}

			ProfilerShort.End();
			return isInGrid;
		}

		private bool HasControlledNonZeroVelocity()
		{
			var entity = ControlledEntity;
			if (entity == null || MySession.Static.IsCameraUserControlledSpectator())
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

			return !Vector3.IsZero(MyGravityProviderSystem.CalculateNaturalGravityInPoint(ControlledEntity.PositionComp.GetPosition()));
		}
	}
}
