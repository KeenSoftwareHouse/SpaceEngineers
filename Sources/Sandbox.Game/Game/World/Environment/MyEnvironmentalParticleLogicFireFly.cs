using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders;
using VRage.Library.Utils;
using VRageMath;
using VRageRender;
using VRage.Game;
using VRage.Profiler;

namespace Sandbox.Game.World
{
	[MyEnvironmentalParticleLogicType(typeof(MyObjectBuilder_EnvironmentalParticleLogicFireFly))]
	public class MyEnvironmentalParticleLogicFireFly : MyEnvironmentalParticleLogic
	{
		struct PathData
		{
			public const int PathPointCount = 16;
			public Vector3D[] PathPoints;
		}

		int m_particleSpawnInterval = 60;
		float m_particleSpawnIntervalRandomness = 0.5f;
		int m_particleSpawnCounter = 0;
		static int m_updateCounter = 0;
		const int m_killDeadParticlesInterval = 60;

        List<HkBodyCollision> m_bodyCollisions = new List<HkBodyCollision>();
		List<MyEnvironmentItems.ItemInfo> m_tmpItemInfos = new List<MyEnvironmentItems.ItemInfo>();

		public override void UpdateBeforeSimulation()
		{
			base.UpdateBeforeSimulation();

            if (!MyFakes.ENABLE_PLANET_FIREFLIES)
                return;

			if (m_particleSpawnCounter-- > 0)
				return;

            m_particleSpawnCounter = 0;

			var entity = MySession.Static.ControlledEntity as MyEntity;
			if (entity == null)
				return;

			var controlledEntity = entity.GetTopMostParent();
			if (controlledEntity == null)
				return;

			try
			{
				ProfilerShort.Begin("Fireflies.Spawn");

				m_particleSpawnCounter = (int)Math.Round(m_particleSpawnCounter + m_particleSpawnCounter*m_particleSpawnIntervalRandomness * (MyRandom.Instance.NextFloat() * 2.0f - 1.0f));
                float normalVar = (MyRandom.Instance.FloatNormal() + 3.0f) / 6.0f;
				if (normalVar > m_particleDensity)
					return;

				var naturalGravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(MySector.MainCamera.Position);
				if (naturalGravity.Dot(MySector.DirectionToSunNormalized) <= 0) // Only spawn at night and on planets
					return;

				var velocity = (MySession.Static.GetCameraControllerEnum() != MyCameraControllerEnum.Entity
								&& MySession.Static.GetCameraControllerEnum() != MyCameraControllerEnum.ThirdPersonSpectator
								? Vector3.Zero : controlledEntity.Physics.LinearVelocity);
				var speed = velocity.Length();

                var currentCharacter = controlledEntity as MyCharacter;

                float characterFlyingMaxSpeed = MyGridPhysics.ShipMaxLinearVelocity();

                if (currentCharacter != null && currentCharacter.Physics != null && currentCharacter.Physics.CharacterProxy != null)
                {
                    characterFlyingMaxSpeed = currentCharacter.Physics.CharacterProxy.CharacterFlyingMaxLinearVelocity();
                }

                Vector3 halfExtents = Vector3.One * m_particleSpawnDistance;
                if (speed / characterFlyingMaxSpeed > 1.0f)
                    halfExtents += 10.0f * velocity / characterFlyingMaxSpeed;

                var entityTranslation = MySector.MainCamera.Position;
                var searchPosition = entityTranslation + velocity;

                MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(MySector.MainCamera.Position);

                if (planet == null)
                    return;

                // TODO: Re-do this using new system.
			    Vector3D cameraPosition = MySector.MainCamera.Position;
                //var foundEnvironmentItems = planet.GetEnvironmentItemsAtPosition(ref cameraPosition);

                /*foreach (var environmentItems in foundEnvironmentItems)
                    environmentItems.GetAllItemsInRadius(searchPosition, m_particleSpawnDistance, m_tmpItemInfos);*/

                var spawnPosition = default(Vector3D);
                bool spawnPositionFound = m_tmpItemInfos.Count != 0;
                if (spawnPositionFound)
                {
                    int selectedTreeIndex = MyRandom.Instance.Next(0, m_tmpItemInfos.Count - 1);
                    spawnPosition = m_tmpItemInfos[selectedTreeIndex].Transform.Position;
                    spawnPositionFound = true;
                }
                else
                    return;

                var spawnedParticle = Spawn(spawnPosition);

                if (spawnedParticle == null)
                    return;

                InitializePath(spawnedParticle);
            }
            finally
            {
                m_bodyCollisions.Clear();
                m_tmpItemInfos.Clear();

				ProfilerShort.End();
			}
		}

		public override void Simulate()
		{
			base.Simulate();

			ProfilerShort.Begin("Fireflies.Simulate");
			foreach(var particle in m_activeParticles)
			{
				var oldPosition = particle.Position;
				Vector3D newPosition = GetInterpolatedPosition(particle);
				particle.Position = newPosition;
			}
			ProfilerShort.End();
		}

		public override void UpdateAfterSimulation()
		{
			ProfilerShort.Begin("Fireflies.UpdateAfterSimulation");
			if (m_updateCounter++ >= m_killDeadParticlesInterval)
			{
				foreach (var particle in m_activeParticles)
				{
					if (IsInGridAABB(particle.Position))
						particle.Deactivate();
				}
				m_updateCounter = 0;
			}
			ProfilerShort.End();

			base.UpdateAfterSimulation();
		}

		public override void Draw()
		{
			base.Draw();

			ProfilerShort.Begin("Fireflies.Draw");
			var scale = 0.075f;
			float width = scale/1.66f;
			float height = scale;

			foreach (var particle in m_activeParticles)
			{
				if (!particle.Active)
					continue;

				Vector4 drawColor = particle.Color;
				var lifeRatio = (float)(MySandboxGame.TotalGamePlayTimeInMilliseconds - particle.BirthTime) / (float)particle.LifeTime;
				if (lifeRatio < 0.1f)
					drawColor = particle.Color * lifeRatio;
				else if (lifeRatio > 0.9f)
					drawColor = particle.Color * (1.0f - lifeRatio);

				var directionVector = Vector3D.CalculatePerpendicularVector(-Vector3D.Normalize(particle.Position - MySector.MainCamera.Position));

				MyTransparentGeometry.AddLineBillboard(particle.Material, drawColor, particle.Position, directionVector, height, width);
			}
			ProfilerShort.End();
		}

		private void InitializePath(MyEnvironmentalParticle particle)
		{
			var pathData = new PathData();
			if(pathData.PathPoints == null)
				pathData.PathPoints = new Vector3D[PathData.PathPointCount+2];

			var gravityDirection = Vector3D.Normalize(MyGravityProviderSystem.CalculateNaturalGravityInPoint(particle.Position));
			pathData.PathPoints[1] = particle.Position - Vector3D.Normalize(MyGravityProviderSystem.CalculateNaturalGravityInPoint(particle.Position)) * MyRandom.Instance.NextFloat()*2.5f;

			for (int index = 2; index < PathData.PathPointCount+1; ++index )
			{
				var pathLength = 5.0f;
				Vector3D randomNormal = Vector3D.Normalize(new Vector3D(MyRandom.Instance.NextFloat(), MyRandom.Instance.NextFloat(), MyRandom.Instance.NextFloat()) * 2.0f - Vector3D.One);
				pathData.PathPoints[index] = pathData.PathPoints[index-1] + randomNormal * (MyRandom.Instance.NextFloat() + 1.0f) * pathLength - gravityDirection / (float)index * pathLength;
			}

			pathData.PathPoints[0] = pathData.PathPoints[1] - gravityDirection;
			pathData.PathPoints[PathData.PathPointCount + 1] = pathData.PathPoints[PathData.PathPointCount] + Vector3D.Normalize(pathData.PathPoints[PathData.PathPointCount] - pathData.PathPoints[PathData.PathPointCount - 1]);

			particle.UserData = pathData;   // TODO: Boxing
		}

		private Vector3D GetInterpolatedPosition(MyEnvironmentalParticle particle)
		{
			Vector3D newPosition = particle.Position;
			Debug.Assert(particle.UserData != null);
			if (particle.UserData == null)
				return newPosition;

			double globalRatio = MathHelper.Clamp((double)(MySandboxGame.TotalGamePlayTimeInMilliseconds - particle.BirthTime) / (double)particle.LifeTime, 0.0, 1.0);

			var pointCount = PathData.PathPointCount - 2;
			int pathIndex = 1 + (int)(globalRatio * pointCount);
			float localRatio = (float)(globalRatio * pointCount - Math.Truncate(globalRatio * pointCount));
			PathData pathData = (particle.UserData as PathData?).Value;
			newPosition = Vector3D.CatmullRom(pathData.PathPoints[pathIndex - 1], pathData.PathPoints[pathIndex], pathData.PathPoints[pathIndex + 1], pathData.PathPoints[pathIndex + 2], localRatio);

			if (!newPosition.IsValid())
				newPosition = particle.Position;

			return newPosition;
		}

		private bool IsInGridAABB(Vector3D worldPosition)
		{
			var sphere = new BoundingSphereD(worldPosition, 0.1f);
			List<MyEntity> entityList = null;
			try
			{
				entityList = MyEntities.GetEntitiesInSphere(ref sphere);

				foreach (var entity in entityList)
				{
					var grid = entity as MyCubeGrid;

					if (grid == null)
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
	}
}
