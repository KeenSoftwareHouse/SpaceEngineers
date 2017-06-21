using Sandbox.Game;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders;
using VRage.Utils;

namespace SpaceStatEffect
{
	public struct MyEffectConstants
	{
		public static float HealthTick = 100f/240f;
		public static float HealthInterval = 1;

		public static float MedRoomHeal = -0.075f;
	}

	[MyStatLogicDescriptor("SpaceStatEffect")]
	public class MySpaceStatEffect : MyStatLogic
	{
		private static MyStringHash HealthId = MyStringHash.GetOrCompute("Health");

		private MyEntityStat Health { get { MyEntityStat health; if (m_stats.TryGetValue(HealthId, out health)) return health; return null; } }

		private int m_healthEffectId;

		public override void Init(IMyCharacter character, Dictionary<MyStringHash, MyEntityStat> stats, string scriptName)
		{
			base.Init(character, stats, scriptName);

			InitPermanentEffects();
			InitActions();

			var health = Health;
			if (health != null)
				health.OnStatChanged += OnHealthChanged;
		}

		public override void Close()
		{
			var health = Health;
			if (health != null)
				health.OnStatChanged -= OnHealthChanged;

			ClearPermanentEffects();

			base.Close();
		}

		private void OnHealthChanged(float newValue, float oldValue, object statChangeData)
		{
			var health = Health;
			if (health != null
				&& health.Value - health.MinValue < 0.001f
				&& Character != null)
			{
				Character.Kill(statChangeData);
			}
		}

		private void InitPermanentEffects()
		{
			if (!EnableAutoHealing)
				return;

			var effectBuilder = new MyObjectBuilder_EntityStatRegenEffect();

			var health = Health;
			if (health != null)
			{
				effectBuilder.TickAmount = SpaceStatEffect.MyEffectConstants.HealthTick;
				effectBuilder.Interval = SpaceStatEffect.MyEffectConstants.HealthInterval;
				effectBuilder.MaxRegenRatio = 0.7f;
				effectBuilder.MinRegenRatio = 0;
				m_healthEffectId = health.AddEffect(effectBuilder);
			}
		}

		private void ClearPermanentEffects()
		{
			if (!EnableAutoHealing)
				return;

			var health = Health;
			if (health != null)
				health.RemoveEffect(m_healthEffectId);
		}

		private void InitActions()
		{
			MyStatAction action = new MyStatAction();

			string actionId = "MedRoomHeal";
			action.StatId = HealthId;
			action.Cost = MyEffectConstants.MedRoomHeal;
			AddAction(actionId, action);
		}
	}
}