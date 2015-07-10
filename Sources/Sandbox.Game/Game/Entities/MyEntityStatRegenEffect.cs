using System;
using System.Diagnostics;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Entities
{
	[MyEntityStatEffectTypeAttribute(typeof(MyObjectBuilder_EntityStatRegenEffect))]
	public class MyEntityStatRegenEffect
	{
		protected float m_amount;
		public float Amount { get { return m_amount; } set { m_amount = value; } }

		protected float m_interval;
		public float Interval { get { return m_interval; } set { m_interval = value; } }

		protected float m_maxRegenRatio;
		protected float m_minRegenRatio;

		protected float m_duration;
		public float Duration { get { return m_duration; } }
		
		protected float m_lastRegenTime;

		readonly float m_birthTime;
		public float AliveTime { get { return MySandboxGame.TotalGamePlayTimeInMilliseconds - m_birthTime; } }

		MyEntityStat m_parentStat;

		public MyEntityStatRegenEffect()
		{
			m_lastRegenTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
			m_birthTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
		}

		public virtual void Init(MyObjectBuilder_Base objectBuilder, MyEntityStat parentStat)
		{
			m_parentStat = parentStat;

			var builder = objectBuilder as MyObjectBuilder_EntityStatRegenEffect;

			if (builder == null)
				return;

			Debug.Assert(builder.Interval > 0f);
			if (builder.Interval <= 0f)
				return;

			m_amount = builder.TickAmount;
			m_interval = builder.Interval;
			m_maxRegenRatio = builder.MaxRegenRatio;
			m_minRegenRatio = builder.MinRegenRatio;
			m_duration = builder.Duration - builder.AliveTime;
		}

		public virtual MyObjectBuilder_EntityStatRegenEffect GetObjectBuilder()
		{
			var builder = new MyObjectBuilder_EntityStatRegenEffect();

			builder.TickAmount = m_amount;
			builder.Interval = m_interval;
			builder.MaxRegenRatio = m_maxRegenRatio;
			builder.MinRegenRatio = m_minRegenRatio;
			builder.Duration = m_duration;
			builder.AliveTime = AliveTime;

			return builder;
		}

		public virtual void Closing()
		{
			if (m_interval == 0.0f)
				return;

			var amountMultiplier = Math.Max((m_interval * 1000.0f - (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastRegenTime)), 0.0f) / (m_interval * 1000.0f);
			if (amountMultiplier <= 0.0f)
				return;

			if (m_amount > 0 && m_parentStat.Value < m_parentStat.MaxValue)
				m_parentStat.Value = MathHelper.Clamp(m_parentStat.Value + m_amount*amountMultiplier, m_parentStat.MinValue, Math.Max(m_parentStat.MaxValue * m_maxRegenRatio, m_parentStat.MaxValue));
			else if (m_amount < 0 && m_parentStat.Value > m_parentStat.MinValue)
				m_parentStat.Value = MathHelper.Clamp(m_parentStat.Value + m_amount*amountMultiplier, Math.Max(m_parentStat.MaxValue * m_minRegenRatio, m_parentStat.MinValue), m_parentStat.MaxValue);
		}

		public virtual void Update()
		{
			if (m_interval <= 0)
				return;

			bool durationFlag = m_duration == 0;
			while(MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastRegenTime > m_interval * 1000f || durationFlag)
			{
				if (m_amount > 0 && m_parentStat.Value < m_parentStat.MaxValue * m_maxRegenRatio)
					m_parentStat.Value = MathHelper.Clamp(m_parentStat.Value + m_amount, m_parentStat.Value, m_parentStat.MaxValue * m_maxRegenRatio);
				else if (m_amount < 0 && m_parentStat.Value > Math.Max(m_parentStat.MinValue, m_parentStat.MaxValue * m_minRegenRatio))
					m_parentStat.Value = MathHelper.Clamp(m_parentStat.Value + m_amount, Math.Max(m_parentStat.MaxValue * m_minRegenRatio, m_parentStat.MinValue), m_parentStat.Value);
				m_lastRegenTime = Math.Min(m_lastRegenTime + m_interval * 1000.0f, MySandboxGame.TotalGamePlayTimeInMilliseconds);
				durationFlag = false;
			}
		}
	};
}
