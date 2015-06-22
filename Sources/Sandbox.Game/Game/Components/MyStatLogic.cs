using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
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

	public abstract class MyStatLogic
	{
		protected IMyCharacter m_character;
		protected Dictionary<MyStringHash, MyEntityStat> m_stats;

		public virtual void Init(IMyCharacter character, Dictionary<MyStringHash, MyEntityStat> stats)
		{
			m_character = character;
			m_stats = stats;
		}

		public virtual void Update() {}
		public virtual void Update10() {}

		public virtual void Close() {}
	}
}
