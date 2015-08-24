using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;
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
			public float Amount;
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

		public bool CanDoAction(string actionId)
		{
			MyStatAction action;
			if(!m_statActions.TryGetValue(actionId, out action))
				return true;

			MyEntityStat stat;
			if(!m_stats.TryGetValue(action.StatId, out stat))
				return true;

			if (stat.Value < action.Amount)
				return false;

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

			if((action.Amount >= 0 && stat.Value >= action.Amount)
				|| action.Amount < 0)
				stat.Value -= action.Amount;
			return true;
		}
	}
}
