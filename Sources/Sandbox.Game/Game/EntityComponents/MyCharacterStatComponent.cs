using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using VRage;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders;
using VRage.Utils;
using VRage.Game.ModAPI;

namespace Sandbox.Game.Components
{
	[MyComponentBuilder(typeof(MyObjectBuilder_CharacterStatComponent))]
	public class MyCharacterStatComponent : MyEntityStatComponent
	{
		public static MyStringHash HealthId = MyStringHash.GetOrCompute("Health");

		public MyEntityStat Health
        {
            get
            {
                MyEntityStat health;
                if (Stats.TryGetValue(HealthId, out health)) return health;
                return null;
            }
        }
        public MyDamageInformation LastDamage { get; private set; }

        public float HealthRatio { get { var retVal = 1.0f; var health = Health; if (health != null) retVal = health.Value / health.MaxValue; return retVal; } }

		public static readonly float LOW_HEALTH_RATIO = 0.2f;

		private MyCharacter m_character = null;

		public override void Update()
		{

			if (m_character != null && m_character.IsDead)
			{
				foreach (var stat in Stats)
				{
					stat.ClearEffects();
				}

				m_scripts.Clear();
			}

			base.Update();
		}

		public override void OnAddedToContainer()
		{
			base.OnAddedToContainer();

			m_character = Container.Entity as MyCharacter;
		}

		public override void OnBeforeRemovedFromContainer()
		{
			m_character = null;

			base.OnBeforeRemovedFromContainer();
		}

		public void OnHealthChanged(float newHealth, float oldHealth, object statChangeData)
		{
			if (m_character == null || !m_character.CharacterCanDie) return;

			m_character.ForceUpdateBreath();

			if (newHealth < oldHealth)
				OnDamage(newHealth, oldHealth);
		}

		private void OnDamage(float newHealth, float oldHealth)
		{
			if (m_character != null && !m_character.IsDead)
			{
				m_character.SoundComp.PlayDamageSound(oldHealth);
			}
			else
				return;

			m_character.Render.Damage();
		}

		public void DoDamage(float damage, bool updateSync, object statChangeData = null)
		{
			var health = Health;
			if (health == null)
				return;

			if(m_character != null)
				m_character.CharacterAccumulatedDamage += damage;

            if (statChangeData is MyDamageInformation)
                LastDamage = (MyDamageInformation)statChangeData;
			health.Decrease(damage, statChangeData);

			if (updateSync && !Sync.IsServer)
			{
				RequestStatChange(health);
			}
		}

		public void Consume(MyFixedPoint amount, MyConsumableItemDefinition definition)
		{
			if (definition == null)
				return;

			MyEntityStat stat;
			var regenEffect = new MyObjectBuilder_EntityStatRegenEffect();
			regenEffect.Interval = 1.0f;
			regenEffect.MaxRegenRatio = 1.0f;
			regenEffect.MinRegenRatio = 0.0f;

			foreach (var statValue in definition.Stats)
			{
				if (Stats.TryGetValue(MyStringHash.GetOrCompute(statValue.Name), out stat))
				{
					regenEffect.TickAmount = statValue.Value*(float)amount;
					regenEffect.Duration = statValue.Time;
					stat.AddEffect(regenEffect);
				}
			}
		}
	}
}
