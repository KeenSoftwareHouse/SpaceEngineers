using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Sandbox.Game
{
	[AttributeUsage(AttributeTargets.Class)]
	public class MyStatLogicDescriptor : System.Attribute
	{
		public string ComponentName;

		public MyStatLogicDescriptor(string componentName)
		{
			ComponentName = componentName;
		}
	}

	public class MyStatLogic
	{
		[ProtoContract]
		public struct MyStatAction
		{
			[ProtoMember]
			public MyStringHash StatId;

			[ProtoMember]
			public float Cost;

            [ProtoMember]
            public float AmountToActivate;

            [ProtoMember]
            public bool CanPerformWithout;
		}

        [ProtoContract]
        public struct MyStatRegenModifier
        {
            [ProtoMember]
            public MyStringHash StatId;

            [ProtoMember]
            public float AmountMultiplier;

            [ProtoMember]
            public float Duration;
        }

        [ProtoContract]
        public struct MyStatEfficiencyModifier
        {
            [ProtoMember]
            public MyStringHash StatId;

            [ProtoMember]
            public float Threshold;

            [ProtoMember]
            public float EfficiencyMultiplier;
        }

		private string m_scriptName;
		public string Name { get { return m_scriptName; } }

		private IMyCharacter m_character;
		public IMyCharacter Character { get { return m_character; } set { var oldCharacter = m_character; m_character = value; OnCharacterChanged(oldCharacter); } }

		protected Dictionary<MyStringHash, MyEntityStat> m_stats;

		private bool m_enableAutoHealing = true;
		protected bool EnableAutoHealing { get { return m_enableAutoHealing; } }

		private Dictionary<string, MyStatAction> m_statActions = new Dictionary<string, MyStatAction>();
		public Dictionary<string, MyStatAction> StatActions { get { return m_statActions; } }

        private Dictionary<string, MyStatRegenModifier> m_statRegenModifiers = new Dictionary<string, MyStatRegenModifier>();
        public Dictionary<string, MyStatRegenModifier> StatRegenModifiers { get { return m_statRegenModifiers; } }

        private Dictionary<string, MyStatEfficiencyModifier> m_statEfficiencyModifiers = new Dictionary<string, MyStatEfficiencyModifier>();
        public Dictionary<string, MyStatEfficiencyModifier> StatEfficiencyModifiers { get { return m_statEfficiencyModifiers; } }

        public const int STAT_VALUE_TOO_LOW = 4;

		public virtual void Init(IMyCharacter character, Dictionary<MyStringHash, MyEntityStat> stats, string scriptName)
		{
			m_scriptName = scriptName;
			Character = character;
			m_stats = stats;

			InitSettings();
		}

		private void InitSettings()
		{
			m_enableAutoHealing = MySession.Static.Settings.AutoHealing;
		}

		public virtual void Update() {}
		public virtual void Update10() {}

		public virtual void Close() {}
		protected virtual void OnCharacterChanged(IMyCharacter oldCharacter) {}

		public void AddAction(string actionId, MyStatAction action)
		{
			m_statActions.Add(actionId, action);
		}

        public void AddModifier(string modifierId, MyStatRegenModifier modifier)
        {
            m_statRegenModifiers.Add(modifierId, modifier);
        }

        public void AddEfficiency(string modifierId, MyStatEfficiencyModifier modifier)
        {
            m_statEfficiencyModifiers.Add(modifierId, modifier);
        }

		public bool CanDoAction(string actionId, bool continuous, out MyTuple<ushort, MyStringHash> message)
		{
			MyStatAction action;
			if(!m_statActions.TryGetValue(actionId, out action)) 
            {
                message = new MyTuple<ushort, MyStringHash>(0, action.StatId);
				return true;
            }

            if (action.CanPerformWithout)
            {
                message = new MyTuple<ushort, MyStringHash>(0, action.StatId);
                return true;
            }

			MyEntityStat stat;
			if(!m_stats.TryGetValue(action.StatId, out stat))
            {
                message = new MyTuple<ushort, MyStringHash>(0, action.StatId);
				return true;
            }

            if (continuous)
            {
                if (stat.Value < action.Cost)
                {
                    message = new MyTuple<ushort, MyStringHash>(STAT_VALUE_TOO_LOW, action.StatId);
                    return false;
                }
            }
            else
            {
                if (stat.Value < action.Cost || stat.Value < action.AmountToActivate)
                {
                    message = new MyTuple<ushort, MyStringHash>(STAT_VALUE_TOO_LOW, action.StatId);
                    //Debug.Write(String.Format("value: {0}, cost: {1}, activation: {2}", stat.Value, action.Cost, action.AmountToActivate));
                    return false;
                }
            }

            message = new MyTuple<ushort, MyStringHash>(0, action.StatId);
			return true;
		}

		public bool DoAction(string actionId)
		{
			MyStatAction action;
			if (!m_statActions.TryGetValue(actionId, out action))
				return false;

			MyEntityStat stat;
			if (!m_stats.TryGetValue(action.StatId, out stat))
				return false;

            if (action.CanPerformWithout)
            {
                stat.Value = stat.Value - Math.Min(stat.Value, action.Cost);
                return true;
            }

			if(((action.Cost >= 0 && stat.Value >= action.Cost)
                || action.Cost < 0) && stat.Value >= action.AmountToActivate)
				stat.Value -= action.Cost;
			return true;
		}

        public void ApplyModifier(string modifierId)
        {
            MyStatRegenModifier modifier;
            if (!m_statRegenModifiers.TryGetValue(modifierId, out modifier))
            {
                return;
            }

            MyEntityStat stat;
            if (!m_stats.TryGetValue(modifier.StatId, out stat))
            {
                return;
            }

            stat.ApplyRegenAmountMultiplier(modifier.AmountMultiplier, modifier.Duration);
        }

        public float GetEfficiencyModifier(string modifierId)
        {
            MyStatEfficiencyModifier modifier;
            if (!m_statEfficiencyModifiers.TryGetValue(modifierId, out modifier))
            {
                return 1.0f;
            }

            MyEntityStat stat;
            if (!m_stats.TryGetValue(modifier.StatId, out stat))
            {
                return 1.0f;
            }

            return stat.GetEfficiencyMultiplier(modifier.EfficiencyMultiplier, modifier.Threshold);
        }
	}
}
