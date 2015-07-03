using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ObjectBuilders;
using VRage.Utils;

namespace SpaceStatEffect
{
	public struct MyEffectConstants
	{
		public static float HealthTick = 100f/240f;
		public static float HealthInterval = 1;
	}

	[MyStatLogicDescriptor("SpaceStatEffect")]
	public class MySpaceStatEffect : MyStatLogic
	{
		private static MyStringHash HealthId = MyStringHash.GetOrCompute("Health");

		private MyEntityStat Health { get { MyEntityStat health; if (m_stats.TryGetValue(HealthId, out health)) return health; return null; } }

		private int m_healthEffectId;

		public override void Init(IMyCharacter character, Dictionary<MyStringHash, MyEntityStat> stats)
		{
			base.Init(character, stats);

			InitPermanentEffects();
		}

		private void InitPermanentEffects()
		{
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
	}
}