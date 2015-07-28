using System.Collections.Generic;
using VRage.Game.ObjectBuilders;
using VRageMath;
using EnvironmentalParticleSettings = Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_EnvironmentDefinition.EnvironmentalParticleSettings;

namespace Sandbox.Game.World
{
	[MyEnvironmentalParticleLogicType(typeof(MyObjectBuilder_EnvironmentalParticleLogic))]
	public class MyEnvironmentalParticleLogic
	{
		public class MyEnvironmentalParticle
		{
			private Vector3 m_position;
			public Vector3 Position { get { return m_position; } }

			private string m_material;
			public string Material { get { return m_material; } }

			private Vector4 m_color;
			public Vector4 Color { get { return m_color; } }

			private int m_birthTime;
			public int BirthTime { get { return m_birthTime; } }

			private int m_lifeTime;
			public int LifeTime { get { return m_lifeTime; } }

			private bool m_active;
			public bool Active { get { return m_active; } }

			public MyEnvironmentalParticle(string material, Vector4 color)
			{
				m_birthTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
				if (material == null)
					m_material = "ErrorMaterial";
				else
					m_material = material;
				m_color = color;
				m_position = new Vector3();
				m_lifeTime = 1500;
				Deactivate();
			}

			public void Activate(Vector3 position)
			{
				m_birthTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
				m_position = position;
				m_active = true;
			}

			public void Deactivate()
			{
				m_active = false;
			}
		}

		private float m_particleDensity;
		private float m_particleSpawnDistance;
		private float m_particleDespawnDistance;

		protected float ParticleDensity { get { return m_particleDensity; } }
		protected float ParticleSpawnDistance { get { return m_particleSpawnDistance; } }
		protected float ParticleDespawnDistance { get { return m_particleDespawnDistance; } }

		private const int m_maxParticles = 128;
		protected List<MyEnvironmentalParticle> m_nonActiveParticles = new List<MyEnvironmentalParticle>(m_maxParticles);
		protected List<MyEnvironmentalParticle> m_activeParticles = new List<MyEnvironmentalParticle>(m_maxParticles);
		protected List<int> m_particlesToRemove = new List<int>();

		public virtual void Init(MyObjectBuilder_EnvironmentalParticleLogic builder)
		{
			m_particleDensity = builder.Density;
			m_particleSpawnDistance = builder.MaxSpawnDistance;
			m_particleDespawnDistance = builder.DespawnDistance;

			for(int index = 0; index < m_maxParticles; ++index)
			{
				m_nonActiveParticles.Add(new MyEnvironmentalParticle(builder.Material, builder.ParticleColor));
			}
		}

		public virtual void UpdateBeforeSimulation() { }

		public virtual void UpdateAfterSimulation()
		{
			for (int index = 0; index < m_activeParticles.Count; ++index)
			{
				var particle = m_activeParticles[index];
				if (MySandboxGame.TotalGamePlayTimeInMilliseconds - particle.BirthTime >= particle.LifeTime)
				{
					m_particlesToRemove.Add(index);
				}
				else if ((particle.Position - MySector.MainCamera.Position).Length() > m_particleDespawnDistance)
					m_particlesToRemove.Add(index);
			}

			for (int index = m_particlesToRemove.Count - 1; index >= 0; --index)
			{
				var particleIndex = m_particlesToRemove[index];
				m_nonActiveParticles.Add(m_activeParticles[particleIndex]);
				m_activeParticles[particleIndex].Deactivate();
				m_activeParticles.RemoveAt(particleIndex);
			}
			m_particlesToRemove.Clear();
		}

		public virtual void Draw() { }

		protected void Spawn(Vector3 position)
		{
			var nonActiveCount = m_nonActiveParticles.Count;
			if (nonActiveCount <= 0)
				return;

			var particle = m_nonActiveParticles[nonActiveCount - 1];
			m_activeParticles.Add(particle);
			m_nonActiveParticles.RemoveAtFast(nonActiveCount - 1);
			particle.Activate(position);
		}

		protected void DeactivateAll()
		{
			foreach(var particle in m_activeParticles)
			{
				m_nonActiveParticles.Add(particle);
				particle.Deactivate();
			}
			m_activeParticles.Clear();
		}
	}
}
