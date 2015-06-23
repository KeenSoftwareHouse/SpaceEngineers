using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Components;
using VRage.Game.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Components
{
	public class MyCharacterStatComponent : MyEntityComponentBase
	{
		public const float STAMINA_WEAPON_SWING = -2f;
		public const float FOOD_TO_STAMINA = 0.5f;
		public static MyStringHash HealthId = MyStringHash.GetOrCompute("Health");
		public static MyStringHash StaminaId = MyStringHash.GetOrCompute("Stamina");
		public static MyStringHash FoodId = MyStringHash.GetOrCompute("Food");

		public MyEntityStat Health { get { MyEntityStat health; if (m_stats.TryGetValue(HealthId, out health)) return health; return null; } }
		public MyEntityStat Stamina { get { MyEntityStat stamina; if (m_stats.TryGetValue(StaminaId, out stamina)) return stamina; return null; } }
		public MyEntityStat Food { get { MyEntityStat food; if (m_stats.TryGetValue(FoodId, out food)) return food; return null; } }

		public static readonly float LOW_HEALTH_RATIO = 0.2f;

		private Dictionary<MyStringHash, MyEntityStat> m_stats;
		private List<MyStatLogic> m_scripts;

		private int m_updateCounter = 0;

		public MyCharacterStatComponent()
		{
			m_stats = new Dictionary<MyStringHash, MyEntityStat>(MyStringHash.Comparer);
			m_scripts = new List<MyStatLogic>();
		}

		public void InitStats(MyStatsDefinition definition)
		{
			if (definition == null)
				return;
			
			foreach(var statId in definition.Stats)
			{
				MyEntityStatDefinition statDefinition = null;
				if (!MyDefinitionManager.Static.TryGetDefinition(statId, out statDefinition))
					continue;

			if (!statDefinition.Enabled || (!statDefinition.EnabledInCreative && MySession.Static.CreativeMode))
					continue;

				var builder = new MyObjectBuilder_EntityStat();
				builder.MaxValue = statDefinition.MaxValue;
				builder.MinValue = statDefinition.MinValue;
				builder.Value = statDefinition.MaxValue;
				AddStat(statId.SubtypeId, builder);
			}

			Type scriptType;
			foreach (var scriptName in definition.Scripts)
			{
				if (MyScriptManager.Static.StatScripts.TryGetValue(scriptName, out scriptType))
				{
					var script = (MyStatLogic)Activator.CreateInstance(scriptType);
					script.Init(Entity as MyCharacter, m_stats);
					m_scripts.Add(script);
				}
			}
		}

		public virtual void Update()
		{
			var character = Container.Entity as MyCharacter;
			if (character == null || character.IsDead)
				return;

			int debugCounter = 0;
			foreach (var stat in m_stats.Values)
			{
				stat.Update();
			}

			foreach (var script in m_scripts)
			{
				script.Update();
			}

			if(m_updateCounter++%10 == 0)
			{
				foreach( var script in m_scripts)
				{
					script.Update10();
				}
			}
		}

		public void Consume(MyFixedPoint amount, MyConsumableItemDefinition definition)
		{
			MyEntityStat stat;
			var regenEffect = new MyObjectBuilder_EntityStatRegenEffect();
			regenEffect.Interval = 1;
			regenEffect.MaxRegenRatio = 1.0f;
			regenEffect.MinRegenRatio = 0.0f;

			foreach (var statValue in definition.Stats)
			{
				if (m_stats.TryGetValue(MyStringHash.GetOrCompute(statValue.Name), out stat))
				{
					regenEffect.TickAmount = statValue.Value;
					regenEffect.Duration = statValue.Time;
					stat.AddEffect(regenEffect);
				}
			}

            // MW:TODO change/remove when there is syncing of consuming items
            if (Entity is MyCharacter)
                (Entity as MyCharacter).StartSecondarySound(definition.EatingSound, true);
		}

		public bool TryGetStat(MyStringHash statId, out MyEntityStat outStat)
		{
			return m_stats.TryGetValue(statId, out outStat);
		}

		public MyEntityStat AddStat(MyStringHash statId, MyObjectBuilder_EntityStat objectBuilder)
		{
			MyEntityStat stat = null;

			if (!m_stats.TryGetValue(statId, out stat))
			{
				stat = new MyEntityStat();
				stat.Init(objectBuilder);

				m_stats.Add(statId, stat);
			}
			else
				stat.ClearEffects();

			return stat;
		}

		public bool RemoveStat(MyStringHash statId)
		{
			return m_stats.Remove(statId);
		}

		public override void OnRemovedFromContainer()
		{
			foreach(var script in m_scripts)
			{
				script.Close();
			}

			base.OnRemovedFromContainer();
		}

		public bool AddHealth(float amount)
		{
			MyEntityStat health;
			if (m_stats.TryGetValue(HealthId, out health))
			{
				health.Increase(amount);
				return true;
			}
			return false;
		}

		public bool AddStamina(float amount)
		{
			MyEntityStat stamina;
			if (m_stats.TryGetValue(StaminaId, out stamina))
			{
				stamina.Increase(amount);
				return true;
			}
			return false;
		}

		public bool AddFood(float amount)
		{
			MyEntityStat food;
			if (m_stats.TryGetValue(FoodId, out food))
			{
				if (amount > 0)
					AddStamina(amount * FOOD_TO_STAMINA);

				food.Increase(amount);
				return true;
			}
			return false;
		}
	}
}
