using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities.EnvironmentItems;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using System;
using VRage.Game;
using VRage.Game.Definitions;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_EnvironmentItemsDefinition))]
    public class MyEnvironmentItemsDefinition : MyDefinitionBase
    {
        private HashSet<MyStringHash> m_itemDefinitions;
        private List<MyStringHash> m_definitionList;

        private List<float> Frequencies;
        private float[] Intervals;

        private MyObjectBuilderType m_itemDefinitionType = MyObjectBuilderType.Invalid;
        public MyObjectBuilderType ItemDefinitionType { get { return m_itemDefinitionType; } }

        public int Channel { get; private set; }
        public float MaxViewDistance { get; private set; }
        public float SectorSize { get; private set; }

        // A rule-of-thumb size of an item for various uses like generation, ai, etc...
        // You can imagine it as an average diameter of an item's bounding sphere
        public float ItemSize { get; private set; }

        public MyStringHash Material { get; private set; }

        public int ItemDefinitionCount { get { return m_definitionList.Count; } }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_EnvironmentItemsDefinition;
            MyDebug.AssertDebug(ob != null);

            m_itemDefinitions = new HashSet<MyStringHash>(MyStringHash.Comparer);
            m_definitionList = new List<MyStringHash>();

            System.Type classType = builder.Id.TypeId;
            var attribs = classType.GetCustomAttributes(typeof(MyEnvironmentItemsAttribute), inherit: false);
            Debug.Assert(attribs.Length <= 1, "Environment item class can only have one EnvironmentItemDefinition attribute!");
            if (attribs.Length == 1)
            {
                var attrib = attribs[0] as MyEnvironmentItemsAttribute;
                m_itemDefinitionType = attrib.ItemDefinitionType;
            }
            else
            {
                m_itemDefinitionType = typeof(MyObjectBuilder_EnvironmentItemDefinition);
            }

            Channel = ob.Channel;
            MaxViewDistance = ob.MaxViewDistance;
            SectorSize = ob.SectorSize;
            ItemSize = ob.ItemSize;
            Material = MyStringHash.GetOrCompute(ob.PhysicalMaterial);

            Frequencies = new List<float>();
        }

        public void AddItemDefinition(MyStringHash definition, float frequency, bool recompute = true)
        {
            Debug.Assert(!m_itemDefinitions.Contains(definition));
            if (m_itemDefinitions.Contains(definition)) return;

            m_itemDefinitions.Add(definition);
            m_definitionList.Add(definition);

            Frequencies.Add(frequency);

            if (recompute)
                RecomputeFrequencies();
        }

        public void RecomputeFrequencies()
        {
            if (m_definitionList.Count == 0)
            {
                Intervals = null;
                return;
            }

            Intervals = new float[m_definitionList.Count-1];
            float totalFrequency = 0;

            foreach (var f in Frequencies)
            {
                totalFrequency += f;
            }

            float acc = 0;

            for (int i = 0; i < Intervals.Length; i++)
            {
                acc += Frequencies[i];
                Intervals[i] = acc / totalFrequency;
            }
        }

        public MyEnvironmentItemDefinition GetItemDefinition(MyStringHash subtypeId)
        {
            MyEnvironmentItemDefinition retval = null;

            MyDefinitionId defId = new MyDefinitionId(m_itemDefinitionType, subtypeId);
            MyDefinitionManager.Static.TryGetDefinition(defId, out retval);
            Debug.Assert(retval != null, "Could not find environment item with definition id " + defId);

            return retval;
        }

        public MyEnvironmentItemDefinition GetItemDefinition(int index)
        {
            Debug.Assert(index >= 0 && index < m_definitionList.Count);
            if (index < 0 || index >= m_definitionList.Count) return null;

            return GetItemDefinition(m_definitionList[index]);
        }

        public MyEnvironmentItemDefinition GetRandomItemDefinition()
        {
            if (m_definitionList.Count == 0) return null;

            float value = MyRandom.Instance.Next(0, 65536) / 65536.0f;

            return GetItemDefinition(m_definitionList[Intervals.BinaryIntervalSearch(value)]);
        }

        public MyEnvironmentItemDefinition GetRandomItemDefinition(MyRandom instance)
        {
            if (m_definitionList.Count == 0) return null;

            float value = instance.Next(0, 65536) / 65536.0f;

            return GetItemDefinition(m_definitionList[Intervals.BinaryIntervalSearch(value)]);
        }

        public bool ContainsItemDefinition(MyStringHash subtypeId)
        {
            return m_itemDefinitions.Contains(subtypeId);
        }

        public bool ContainsItemDefinition(MyDefinitionId definitionId)
        {
            return definitionId.TypeId == m_itemDefinitionType && m_itemDefinitions.Contains(definitionId.SubtypeId);
        }

        public bool ContainsItemDefinition(MyEnvironmentItemDefinition itemDefinition)
        {
            return ContainsItemDefinition(itemDefinition.Id);
        }
    }
}
