using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Entities
{
	[MyEntityType(typeof(MyObjectBuilder_BattleAreaMarker))]
	public class MyBattleAreaMarker : MyAreaMarker
	{
		private uint m_battleSlot = 0;
		public uint BattleSlot { get { return m_battleSlot; } }

		public bool IsAttackerSpawn { get { return m_definition.Id == AttackerSpawnDefId; } }

		public static MyDefinitionId AttackerSpawnDefId = new MyDefinitionId(typeof(MyObjectBuilder_AreaMarkerDefinition), "BattleFlag_AttackerSpawn");
		public static MyDefinitionId DefenderSpawnDefId = new MyDefinitionId(typeof(MyObjectBuilder_AreaMarkerDefinition), "BattleFlag_DefenderSpawn");

		private static HashSet<uint> m_battleSlotsAttacker = new HashSet<uint>();
		private static HashSet<uint> m_battleSlotsDefender = new HashSet<uint>();
		private static List<MyPlaceArea> m_tmpPlaceAreas = new List<MyPlaceArea>();
		private static bool nextIdCalled = false;

		public MyBattleAreaMarker()
		{
		}

		public MyBattleAreaMarker(MyPositionAndOrientation positionAndOrientation, MyAreaMarkerDefinition definition, uint battleSlot)
			: base(positionAndOrientation, definition)
		{
			m_battleSlot = battleSlot;

			InitInternal();
		}

		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			base.Init(objectBuilder);

			var ob = objectBuilder as MyObjectBuilder_BattleAreaMarker;

			if (ob != null)
			{
				m_battleSlot = ob.BattleSlot;
			}

			InitInternal();
		}

		private void InitInternal()
		{
			StringBuilder tmpString = new StringBuilder();
			if (m_definition.DisplayNameEnum.HasValue)
				tmpString.Append(MyTexts.Get(m_definition.DisplayNameEnum.Value));
			tmpString.Append(" ");
			tmpString.Append(m_battleSlot);
			RenameHudMarker(tmpString);
		}

		private bool RenameHudMarker(StringBuilder newName)
		{
			MyHudEntityParams hudParams;
			if (MyHud.LocationMarkers.MarkerEntities.TryGetValue(this, out hudParams))
			{
				hudParams.Text = newName;
				MyHud.LocationMarkers.RegisterMarker(this, hudParams);
				return true;
			}

			return false;
		}

		public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
		{
			var ob = base.GetObjectBuilder(copy) as MyObjectBuilder_BattleAreaMarker;

			ob.BattleSlot = m_battleSlot;

			return ob;
		}

		protected override void Closing()
		{
			var battleSlots = (m_definition.Id == AttackerSpawnDefId ? m_battleSlotsAttacker : m_battleSlotsDefender);
			battleSlots.Remove(m_battleSlot);
			
			base.Closing();
		}

		private static void RebuildSlotLists()
		{
			m_battleSlotsAttacker.Clear();
			m_battleSlotsDefender.Clear();
			m_tmpPlaceAreas.Clear();
			MyPlaceAreas.Static.GetAllAreas(m_tmpPlaceAreas);
			foreach (var placeArea in m_tmpPlaceAreas)
			{
				var place = placeArea as MySpherePlaceArea;
				if (place == null)
					continue;

				var areaMarker = placeArea.Container.Entity as MyBattleAreaMarker;
				if (areaMarker == null)
					continue;

				HashSet<uint> slots = null;
				if (areaMarker.IsAttackerSpawn)
					slots = m_battleSlotsAttacker;
				else
					slots = m_battleSlotsDefender;

				slots.Add(areaMarker.BattleSlot);
			}
			m_tmpPlaceAreas.Clear();
		}

		public static bool GetNextBattleSlot(out uint freeSlot, bool attacker)
		{
			if (!nextIdCalled)
			{
				RebuildSlotLists();
				nextIdCalled = true;
			}

			var battleSlots = (attacker ? m_battleSlotsAttacker : m_battleSlotsDefender);
			freeSlot = 0;
			for (uint index = 1; index < battleSlots.Count + 2; ++index)
			{
				if (!battleSlots.Contains(index))
				{
					battleSlots.Add(index);
					freeSlot = index;
					return true;
				}
			}
			return false;
		}
	}
}
