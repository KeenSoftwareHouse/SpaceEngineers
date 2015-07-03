using System.Collections.Generic;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Entities
{
	[MyFactoryTag(typeof(MyObjectBuilder_EntityStat))]
	public class MyEntityStat
	{
		protected float m_currentValue;
		public float Value { get { return m_currentValue; } set { m_currentValue = MathHelper.Clamp(value, m_minValue, m_maxValue); } }
		public float CurrentRatio { get { return Value / (MaxValue - MinValue); } }

		protected float m_minValue;
		public float MinValue { get { return m_minValue; } }

		protected float m_maxValue;
		public float MaxValue { get { return m_maxValue; } }

		private Dictionary<int, MyEntityStatRegenEffect> m_effects = new Dictionary<int, MyEntityStatRegenEffect>();
		private static List<int> m_tmpRemoveEffects = new List<int>();

		public virtual void Init(MyObjectBuilder_Base objectBuilder)
		{
			var builder = objectBuilder as MyObjectBuilder_EntityStat;

			if (builder == null)
				return;

			m_minValue = builder.MinValue;
			m_maxValue = builder.MaxValue;
			m_currentValue = MathHelper.Clamp(builder.Value, m_minValue, m_maxValue);
		}

		public int AddEffect(MyObjectBuilder_EntityStatRegenEffect objectBuilder)
		{
			var effect = MyEntityStatEffectFactory.CreateInstance(objectBuilder);
			effect.Init(objectBuilder, this);

			int nextId = 0;

			for (; nextId < m_effects.Count; ++nextId )
			{
				if(!m_effects.ContainsKey(nextId))
				{
					break;
				}
			}
			m_effects.Add(nextId, effect);

			return nextId;
		}

		public bool RemoveEffect(int id)
		{
			m_effects.Remove(id);

			return false;
		}

		public void ClearEffects()
		{
			m_effects.Clear();
		}

		public bool TryGetEffect(int id, out MyEntityStatRegenEffect outEffect)
		{
			return m_effects.TryGetValue(id, out outEffect);
		}

		public virtual void Update()
		{
			foreach(var effectPair in m_effects)
			{
				var effect = effectPair.Value;

				if (effect.Duration >= 0 && effect.AliveTime > effect.Duration)
				{
					m_tmpRemoveEffects.Add(effectPair.Key);
					continue;
				}
				effect.Update();
			}
			foreach (var key in m_tmpRemoveEffects)
				RemoveEffect(key);

			m_tmpRemoveEffects.Clear();
		}

		public void Increase(float amount) { m_currentValue = MathHelper.Clamp(m_currentValue + amount, m_minValue, m_maxValue); }
		public void Decrease(float amount) { m_currentValue = MathHelper.Clamp(m_currentValue - amount, m_minValue, m_maxValue); }
	};
}
