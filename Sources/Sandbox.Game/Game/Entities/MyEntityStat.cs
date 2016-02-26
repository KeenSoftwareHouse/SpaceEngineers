using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Common;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    [MyFactoryTag(typeof(MyObjectBuilder_EntityStat))]
    public class MyEntityStat
    {
        protected float m_currentValue;
        private float m_lastSyncValue;
        public float Value { get { return m_currentValue; } set { SetValue(value, null); } }
        public float CurrentRatio { get { return Value / (MaxValue - MinValue); } }

        protected float m_minValue;
        public float MinValue { get { return m_minValue; } }

        protected float m_maxValue;
        public float MaxValue { get { return m_maxValue; } }

        protected float m_defaultValue;
        public float DefaultValue { get { return m_defaultValue; } }

        private bool m_syncFlag = false;
        public bool ShouldSync { get { return m_syncFlag; } }

        private Dictionary<int, MyEntityStatRegenEffect> m_effects = new Dictionary<int, MyEntityStatRegenEffect>();
        private static List<int> m_tmpRemoveEffects = new List<int>();
        private int m_updateCounter = 0;

        private float m_statRegenLeft = 0;
        public float StatRegenLeft { get { return m_statRegenLeft; } set { m_statRegenLeft = value; } }

        private float m_regenAmountMultiplier = 1.0f;
        private float m_regenAmountMultiplierDuration = 0;
        private int m_regenAmountMultiplierTimeStart = 0;
        private int m_regenAmountMultiplierTimeAlive = 0;
        private bool m_regenAmountMultiplierActive = false;

        private MyStringHash m_statId;
        public MyStringHash StatId { get { return m_statId; } }

        public delegate void StatChangedDelegate(float newValue, float oldValue, object statChangeData);
        public event StatChangedDelegate OnStatChanged;

        public MyEntityStatDefinition StatDefinition = null;

        public virtual void Init(MyObjectBuilder_Base objectBuilder)
        {
            var builder = (MyObjectBuilder_EntityStat) objectBuilder;

            MyEntityStatDefinition definition;
            MyDefinitionManager.Static.TryGetDefinition<MyEntityStatDefinition>(new MyDefinitionId(builder.TypeId, builder.SubtypeId), out definition);

            Debug.Assert(definition != null);
            StatDefinition = definition;

            System.Diagnostics.Debug.Assert(!float.IsNaN(definition.MaxValue) && !float.IsNaN(definition.MinValue) && !float.IsNaN(definition.DefaultValue), "Invalid values in stat definition!");

            m_maxValue = definition.MaxValue;
            m_minValue = definition.MinValue;
            m_currentValue = builder.Value * m_maxValue;
            m_defaultValue = definition.DefaultValue;
            
            m_lastSyncValue = m_currentValue;
            m_statId = MyStringHash.GetOrCompute(definition.Name);

            m_regenAmountMultiplier = builder.StatRegenAmountMultiplier;
            m_regenAmountMultiplierDuration = builder.StatRegenAmountMultiplierDuration;
            m_regenAmountMultiplierTimeStart = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_regenAmountMultiplierTimeAlive = 0;
            m_regenAmountMultiplierActive = m_regenAmountMultiplierDuration > 0;

            ClearEffects();
            if (builder.Effects != null)
            {
                foreach (var effectBuilder in builder.Effects)
                {
                    AddEffect(effectBuilder);
                }
            }
        }

        public virtual MyObjectBuilder_EntityStat GetObjectBuilder()
        {
            var builder = new MyObjectBuilder_EntityStat();
            MyEntityStatDefinition definition = MyDefinitionManager.Static.GetDefinition(new MyDefinitionId(builder.TypeId, StatDefinition.Id.SubtypeId)) as MyEntityStatDefinition;

            builder.SubtypeName = StatDefinition.Id.SubtypeName;
            Debug.Assert(definition != null);
            if (definition != null)
            {
                Debug.Assert(definition.MaxValue != 0);
                builder.Value = m_currentValue / (definition.MaxValue != 0 ? definition.MaxValue : 1);	// Save stat value relative to the definition maximum value
                builder.MaxValue = m_maxValue / (definition.MaxValue != 0 ? definition.MaxValue : 1);	// Save stat maximum value relative to the definition maximum value
            }
            else
            {
                builder.Value = m_currentValue / m_maxValue;
                builder.MaxValue = 1.0f;
            }

            if (m_regenAmountMultiplierActive)
            {
                builder.StatRegenAmountMultiplier = m_regenAmountMultiplier;
                builder.StatRegenAmountMultiplierDuration = m_regenAmountMultiplierDuration;
            }

            builder.Effects = null;
            if (m_effects != null && m_effects.Count > 0)
            {
                int savedEffectCount = m_effects.Count;

                foreach (var effectPair in m_effects)
                {
                    if (effectPair.Value.Duration < 0)
                        --savedEffectCount;			// Don't save the permanent effects
                }
                if (savedEffectCount > 0)
                {
                    builder.Effects = new MyObjectBuilder_EntityStatRegenEffect[savedEffectCount];
                    int effectIndex = 0;
                    foreach (var effectPair in m_effects)
                    {
                        if (effectPair.Value.Duration >= 0)
                            builder.Effects[effectIndex++] = effectPair.Value.GetObjectBuilder();
                    }
                }
            }

            return builder;
        }

        public void ApplyRegenAmountMultiplier(float amountMultiplier = 1.0f, float duration = 2.0f)
        {
            m_regenAmountMultiplier = amountMultiplier;
            m_regenAmountMultiplierDuration = duration;
            m_regenAmountMultiplierTimeStart = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_regenAmountMultiplierActive = duration > 0.0f;
        }

        public void ResetRegenAmountMultiplier()
        {
            m_regenAmountMultiplier = 1.0f;
            m_regenAmountMultiplierActive = false;
        }

        private void UpdateRegenAmountMultiplier()
        {
            if (m_regenAmountMultiplierActive)
            {
                m_regenAmountMultiplierTimeAlive = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_regenAmountMultiplierTimeStart;
                if (m_regenAmountMultiplierTimeAlive >= m_regenAmountMultiplierDuration * 1000)
                {
                    // reset to default
                    m_regenAmountMultiplier = 1.0f;
                    m_regenAmountMultiplierDuration = 0;
                    m_regenAmountMultiplierActive = false;
                }
            }
        }

        public float GetEfficiencyMultiplier(float multiplier, float threshold)
        {
            return CurrentRatio < threshold ? multiplier : 1.0f;
        }

        public int AddEffect(float amount, float interval, float duration = -1.0f, float minRegenRatio = 0.0f, float maxRegenRatio = 1.0f)
        {
            var builder = new MyObjectBuilder_EntityStatRegenEffect()
            {
                TickAmount = amount,
                Interval = interval,
                Duration = duration,
                MinRegenRatio = minRegenRatio,
                MaxRegenRatio = maxRegenRatio,
            };
            return AddEffect(builder);
        }

        public int AddEffect(MyObjectBuilder_EntityStatRegenEffect objectBuilder)
        {
            var effect = MyEntityStatEffectFactory.CreateInstance(objectBuilder);
            effect.Init(objectBuilder, this);

            int nextId = 0;

            for (; nextId < m_effects.Count; ++nextId)
            {
                if (!m_effects.ContainsKey(nextId))
                    break;
            }
            m_effects.Add(nextId, effect);

            return nextId;
        }

        public virtual void Update()
        {
            m_syncFlag = false;
            Debug.Assert(m_tmpRemoveEffects.Count == 0, "Effect remove list not cleared!");
            UpdateRegenAmountMultiplier();

            foreach (var effectPair in m_effects)
            {
                var effect = effectPair.Value;

                if (effect.Duration >= 0 && effect.AliveTime >= effect.Duration * 1000.0f)
                {
                    m_tmpRemoveEffects.Add(effectPair.Key);
                }
                if (Sync.IsServer && effect.Enabled)
                    if (m_regenAmountMultiplierActive)
                        effect.Update(m_regenAmountMultiplier);
                    else
                        effect.Update();
            }
            foreach (var key in m_tmpRemoveEffects)
                RemoveEffect(key);

            m_tmpRemoveEffects.Clear();

            if ((m_updateCounter++ % 10 == 0 || Math.Abs(Value - MinValue) <= 0.001) && m_lastSyncValue != m_currentValue)
            {
                m_syncFlag = true;
                m_lastSyncValue = m_currentValue;
            }
        }

        private void SetValue(float newValue, object statChangeData)
        {
            System.Diagnostics.Debug.Assert(!float.IsNaN(newValue) && !float.IsInfinity(newValue), "Invalid value!");
            float oldValue = m_currentValue;
            m_currentValue = MathHelper.Clamp(newValue, MinValue, MaxValue);
            if (OnStatChanged != null && newValue != oldValue)
                OnStatChanged(newValue, oldValue, statChangeData);
        }

        public bool RemoveEffect(int id)
        {
            MyEntityStatRegenEffect effect = null;
            if (m_effects.TryGetValue(id, out effect))
            {
                effect.Closing();
            }
            return m_effects.Remove(id);
        }

        public void ClearEffects()
        {
            foreach (var effect in m_effects)
            {
                effect.Value.Closing();
            }

            m_effects.Clear();
        }

        public bool TryGetEffect(int id, out MyEntityStatRegenEffect outEffect)
        {
            return m_effects.TryGetValue(id, out outEffect);
        }

        public DictionaryReader<int, MyEntityStatRegenEffect> GetEffects()
        {
            return m_effects;
        }

        public MyEntityStatRegenEffect GetEffect(int id)
        {
            MyEntityStatRegenEffect retVal = null;
            return (m_effects.TryGetValue(id, out retVal) ? retVal : null);
        }

        public override string ToString()
        {
            return m_statId.ToString();
        }


        public void Increase(float amount, object statChangeData) { SetValue(Value + amount, statChangeData); }
        public void Decrease(float amount, object statChangeData) { SetValue(Value - amount, statChangeData); }

        public float CalculateRegenLeftForLongestEffect()
        {
            MyEntityStatRegenEffect longestTimeLeftEffect = null;
            m_statRegenLeft = 0f;

            foreach (var effect in m_effects)	// Calculate the effect from non-permanent effects
            {
                if (effect.Value.Duration > 0)
                {
                    m_statRegenLeft += effect.Value.AmountLeftOverDuration;
                    if (longestTimeLeftEffect == null || effect.Value.DeathTime > longestTimeLeftEffect.DeathTime)
                        longestTimeLeftEffect = effect.Value;
                }
            }

            if (longestTimeLeftEffect == null)
                return m_statRegenLeft;

            foreach (var effect in m_effects)
            {
                if (effect.Value.Duration < 0)	// Calculate the effect from the permanent effects
                {
                    m_statRegenLeft += effect.Value.Amount * (float)effect.Value.CalculateTicksBetweenTimes(longestTimeLeftEffect.LastRegenTime, longestTimeLeftEffect.DeathTime);
                }
            }

            return m_statRegenLeft;
        }
    };
}
