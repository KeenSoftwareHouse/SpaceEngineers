using Sandbox.Game.Multiplayer;
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
		public float AmountLeftOverDuration { get { return m_amount * (float)TicksLeft + PartialEndAmount; } }
		public int TicksLeft { get { return CalculateTicksBetweenTimes(m_lastRegenTime, DeathTime); } }

		private float PartialEndAmount { get { var ratio = m_duration / m_interval; return (ratio - (float)Math.Truncate(ratio)) * m_amount; } }

		protected float m_interval;
		public float Interval { get { return m_interval; } set { m_interval = value; } }

		protected float m_maxRegenRatio;
		protected float m_minRegenRatio;

		protected float m_duration;
		public float Duration { get { return m_duration; } }
		
		protected int m_lastRegenTime;
		public int LastRegenTime { get { return m_lastRegenTime; } }

		readonly int m_birthTime;
		public int BirthTime { get { return m_birthTime; } }
		public int DeathTime { get { return (Duration >= 0 ? m_birthTime + (int)(m_duration * 1000f) : int.MaxValue); } }
		public int AliveTime { get { return MySandboxGame.TotalGamePlayTimeInMilliseconds - BirthTime; } }

        private bool m_enabled;
        public bool Enabled 
        { 
            get { return m_enabled; }
            set { m_enabled = value; }
        }

		MyEntityStat m_parentStat;

		public MyEntityStatRegenEffect()
		{
			m_lastRegenTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
			m_birthTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            Enabled = true;
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
			m_duration = builder.Duration - (builder.AliveTime / 1000);

            ResetRegenTime();
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
			if (!Sync.IsServer)
				return;

            IncreaseByRemainingValue();
		}

		public virtual void Update(float regenAmountMultiplier = 1.0f)
		{
			if (m_interval <= 0)
				return;

			bool durationFlag = m_duration == 0;
			while(MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastRegenTime >= 0 || durationFlag)
			{
				if (m_amount > 0 && m_parentStat.Value < m_parentStat.MaxValue * m_maxRegenRatio)
					m_parentStat.Value = MathHelper.Clamp(m_parentStat.Value + m_amount * regenAmountMultiplier, m_parentStat.Value, m_parentStat.MaxValue * m_maxRegenRatio);
				else if (m_amount < 0 && m_parentStat.Value > Math.Max(m_parentStat.MinValue, m_parentStat.MaxValue * m_minRegenRatio))
					m_parentStat.Value = MathHelper.Clamp(m_parentStat.Value + m_amount, Math.Max(m_parentStat.MaxValue * m_minRegenRatio, m_parentStat.MinValue), m_parentStat.Value);
				m_lastRegenTime += (int)Math.Round(m_interval * 1000.0f);
				durationFlag = false;
			}
		}

		public int CalculateTicksBetweenTimes(int startTime, int endTime)
		{
			if (startTime < m_birthTime || startTime >= endTime)
				return 0;

			startTime = Math.Max(startTime, m_lastRegenTime);
			endTime = Math.Min(endTime, DeathTime);

			var duration = endTime - startTime;
			var ticksLeft = (int)(duration/Math.Round(m_interval * 1000f));

			return Math.Max(ticksLeft, 0);
		}

        public void SetAmountAndInterval(float amount, float interval, bool increaseByRemaining)
        {
            if (amount == Amount && interval == Interval)
                return;
            if (increaseByRemaining)
                IncreaseByRemainingValue();
            Amount = amount;
            Interval = interval;
            ResetRegenTime();
        }

        public void ResetRegenTime()
        {
            m_lastRegenTime = MySandboxGame.TotalGamePlayTimeInMilliseconds + (int)Math.Round(m_interval * 1000.0f);
        }

        private void IncreaseByRemainingValue()
        {
            if (m_interval <= 0)
                return;

            if (!Enabled)
                return;

            var amountMultiplier = 1 - (m_lastRegenTime - MySandboxGame.TotalGamePlayTimeInMilliseconds) / (m_interval * 1000.0f);
            if (amountMultiplier <= 0.0f)
                return;

            if (m_amount > 0 && m_parentStat.Value < m_parentStat.MaxValue)
                m_parentStat.Value = MathHelper.Clamp(m_parentStat.Value + m_amount * amountMultiplier, m_parentStat.MinValue, Math.Max(m_parentStat.MaxValue * m_maxRegenRatio, m_parentStat.MaxValue));
            else if (m_amount < 0 && m_parentStat.Value > m_parentStat.MinValue)
                m_parentStat.Value = MathHelper.Clamp(m_parentStat.Value + m_amount * amountMultiplier, Math.Max(m_parentStat.MaxValue * m_minRegenRatio, m_parentStat.MinValue), m_parentStat.MaxValue);
        }

		public override string ToString()
		{
			return m_parentStat.ToString() + ": (" + m_amount + "/" + m_interval + "/" + m_duration + ")";
		}
	};
}
