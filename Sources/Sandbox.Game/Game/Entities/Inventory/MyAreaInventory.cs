using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace Sandbox.Game
{
	public class MyAreaInventory : IMyComponentInventory
	{
		IMyInventoryOwner m_owner;
		public IMyInventoryOwner Owner { get { return m_owner; } }

		public MyAreaInventory(IMyInventoryOwner owner)
		{
			m_owner = owner;
		}

		public MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId)
		{
			MyFixedPoint retVal = default(MyFixedPoint);
			return retVal;
		}

		public MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None)
		{
			MyFixedPoint retVal = default(MyFixedPoint);
			return retVal;
		}

		public void AddItems(MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, int index = -1)
		{

		}

		public void RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false)
		{

		}
	}
}
