using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;

namespace Sandbox.Game.Entities.Cube
{
    public class MyComponentList
    {
        // List of the total materials as they were added
        private List<MyTuple<MyDefinitionId, int>> m_displayList = new List<MyTuple<MyDefinitionId, int>>();

        // Total counts for the materials, merged by type
        private Dictionary<MyDefinitionId, int> m_totalMaterials = new Dictionary<MyDefinitionId, int>();

        // Only the materials that were flagged as required are added here
        private Dictionary<MyDefinitionId, int> m_requiredMaterials = new Dictionary<MyDefinitionId, int>();

        public DictionaryReader<MyDefinitionId, int> TotalMaterials { get { return new DictionaryReader<MyDefinitionId, int>(m_totalMaterials); } }
        public DictionaryReader<MyDefinitionId, int> RequiredMaterials { get { return new DictionaryReader<MyDefinitionId, int>(m_requiredMaterials); } }

        public void AddMaterial(MyDefinitionId myDefinitionId, int amount, int requiredAmount = 0, bool addToDisplayList = true)
        {
            Debug.Assert(requiredAmount <= amount);
            if (requiredAmount > amount)
            {
                requiredAmount = amount;
            }

            if (addToDisplayList)
            {
                m_displayList.Add(new MyTuple<MyDefinitionId, int>(myDefinitionId, amount));
            }
            AddToDictionary(m_totalMaterials, myDefinitionId, amount);
            if (requiredAmount > 0)
            {
                AddToDictionary(m_requiredMaterials, myDefinitionId, requiredAmount);
            }
        }

        public void Clear()
        {
            m_displayList.Clear();
            m_totalMaterials.Clear();
            m_requiredMaterials.Clear();
        }

        private void AddToDictionary(Dictionary<MyDefinitionId, int> dict, MyDefinitionId myDefinitionId, int amount)
        {
            int presentAmount = 0;
            dict.TryGetValue(myDefinitionId, out presentAmount);
            presentAmount += amount;
            dict[myDefinitionId] = presentAmount;
        }
    }
}
