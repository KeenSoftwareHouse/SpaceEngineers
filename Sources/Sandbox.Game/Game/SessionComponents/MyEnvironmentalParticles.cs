using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Sandbox.Game.SessionComponents
{
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.Simulation | MyUpdateOrder.AfterSimulation)]
	class MyEnvironmentalParticles : MySessionComponentBase
	{
		private List<MyEnvironmentalParticleLogic> m_particleHandlers = new List<MyEnvironmentalParticleLogic>();

		public override void LoadData()
		{
			base.LoadData();

			if (MyDefinitionManager.Static.EnvironmentDefinition == null)
				return;

			var typeList = MyDefinitionManager.Static.EnvironmentDefinition.EnvironmentalParticles;
			foreach(var typeDefinition in typeList)
			{
				MyObjectBuilder_EnvironmentalParticleLogic objectBuilder = MyObjectBuilderSerializer.CreateNewObject(typeDefinition.Id) as MyObjectBuilder_EnvironmentalParticleLogic;
				Debug.Assert(objectBuilder != null);
				if (objectBuilder == null)
					continue;

				objectBuilder.Density = typeDefinition.Density;
				objectBuilder.DespawnDistance = typeDefinition.DespawnDistance;
				objectBuilder.ParticleColor = typeDefinition.Color;
				objectBuilder.MaxSpawnDistance = typeDefinition.MaxSpawnDistance;
				objectBuilder.Material = typeDefinition.Material;
				objectBuilder.MaxLifeTime = typeDefinition.MaxLifeTime;
				objectBuilder.MaxParticles = typeDefinition.MaxParticles;

				var logic = MyEnvironmentalParticleLogicFactory.CreateEnvironmentalParticleLogic(objectBuilder);
				logic.Init(objectBuilder);
				m_particleHandlers.Add(logic);
			}
		}

		public override void UpdateBeforeSimulation()
		{
			base.UpdateBeforeSimulation();

			ProfilerShort.Begin("UpdateBeforeSimulation all");
			foreach(var particleLogic in m_particleHandlers)
			{
				particleLogic.UpdateBeforeSimulation();
			}
			ProfilerShort.End();
		}

		public override void Simulate()
		{
			base.Simulate();

			ProfilerShort.Begin("Simulate all");
			foreach(var particleLogic in m_particleHandlers)
			{
				particleLogic.Simulate();
			}
			ProfilerShort.End();
		}

		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();

			ProfilerShort.Begin("UpdateAfterSimulation all");
			foreach(var particleLogic in m_particleHandlers)
			{
				particleLogic.UpdateAfterSimulation();
			}
			ProfilerShort.End();
		}

		public override void Draw()
		{
			base.Draw();

			ProfilerShort.Begin("Draw all");
			foreach(var particleLogic in m_particleHandlers)
			{
				particleLogic.Draw();
			}
			ProfilerShort.End();
		}
	}
}
