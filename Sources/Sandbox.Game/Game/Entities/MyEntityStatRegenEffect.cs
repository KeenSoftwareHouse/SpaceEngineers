using System;
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

			m_amount = builder.TickAmount;
			m_interval = builder.Interval;
			m_maxRegenRatio = builder.MaxRegenRatio;
			m_minRegenRatio = builder.MinRegenRatio;
			m_duration = builder.Duration - builder.AliveTime;
		}

		public virtual void Update()
		{
			if (m_parentStat.CurrentRatio >= m_minRegenRatio && m_parentStat.CurrentRatio <= m_maxRegenRatio &&
				m_lastRegenTime + m_interval * 1000f < MySandboxGame.TotalGamePlayTimeInMilliseconds)
			{
				m_parentStat.Value = MathHelper.Clamp(m_parentStat.Value + m_amount, Math.Max(m_parentStat.MaxValue * m_minRegenRatio, m_parentStat.MinValue), m_parentStat.MaxValue * m_maxRegenRatio);
				m_lastRegenTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
			}
		}
	};
}
