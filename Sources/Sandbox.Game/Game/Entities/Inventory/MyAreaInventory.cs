using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRageMath;

namespace Sandbox.Game
{
	public class MyAreaInventory : IMyComponentInventory
	{
        private const float SEARCH_RADIUS = 7.0f;
        private const float BUILD_RADIUS = 8.0f;

        private Dictionary<MyDefinitionId, List<long>> m_componentLists = new Dictionary<MyDefinitionId, List<long>>();

		IMyInventoryOwner m_owner;
		public IMyInventoryOwner Owner { get { return m_owner; } }

		public MyAreaInventory(IMyInventoryOwner owner)
		{
			m_owner = owner;
		}

		public MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId)
		{
            return MyFixedPoint.MaxValue;
		}

		public MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None)
		{
            List<long> list = null;
            if (!m_componentLists.TryGetValue(contentId, out list))
            {
                return 0;
            }
            else
            {
                return list.Count;
            }
		}

		public void AddItems(MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, int index = -1)
		{
            // Spawn a component in the radius
		}

		public void RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false)
		{
            int removedCount = 0;

            List<long> list = null;
            if (m_componentLists.TryGetValue(contentId, out list))
            {
                int num = (int)amount;

                MyEntity entity;
                int i;
                for (i = list.Count - 1; i >= 0 && removedCount < num; i--)
                {
                    if (MyEntities.TryGetEntityById(list[i], out entity))
                    {
                        Debug.Assert(entity is MyCubeGrid && (entity as MyCubeGrid).BlocksCount == 1, "Entity was not a component!");
                        if (entity.MarkedForClose) continue;

                        entity.Close();
                        removedCount++;
                    }
                }

                // Move i back to the last valid index
                i++;

                list.RemoveRange(i, list.Count - i);
                if (list.Count == 0)
                {
                    m_componentLists.Remove(contentId);
                }
            }
		}

        public void Update(Vector3D position)
        {
            // CH: TODO: For now, just reset everything every update. In the next version, let's do it incrementally
            m_componentLists.Clear();

            var components = MyItemsCollector.FindComponentsInRadius(position, SEARCH_RADIUS);

            foreach (MyItemsCollector.BlockInfo info in components)
            {
                AddComponent(info.ComponentDefinition, info.GridEntityId);
            }

            components.Clear();
        }

        private void AddComponent(MyDefinitionId myDefinitionId, long entityId)
        {
            List<long> list = null;
            if (!m_componentLists.TryGetValue(myDefinitionId, out list))
            {
                list = new List<long>();
                m_componentLists.Add(myDefinitionId, list);
            }

            list.Add(entityId);
        }
    }
}
