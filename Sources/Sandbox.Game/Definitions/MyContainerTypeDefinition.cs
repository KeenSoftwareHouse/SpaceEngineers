using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ContainerTypeDefinition))]
    public class MyContainerTypeDefinition : MyDefinitionBase
    {
        public struct ContainerTypeItem
        {
            public MyFixedPoint AmountMin;
            public MyFixedPoint AmountMax;
            public float Frequency;
            public MyDefinitionId DefinitionId;
            public bool HasIntegralAmount;
        }

        public int CountMin;
        public int CountMax;
        public float ItemsCumulativeFrequency;
        private float m_tempCumulativeFreq;
        public ContainerTypeItem[] Items;
        private bool[] m_itemSelection;

        public MyContainerTypeDefinition() { }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            MyObjectBuilder_ContainerTypeDefinition definition = builder as MyObjectBuilder_ContainerTypeDefinition;

            CountMin = definition.CountMin;
            CountMax = definition.CountMax;
            ItemsCumulativeFrequency = 0.0f;
            
            int i = 0;
            Items = new ContainerTypeItem[definition.Items.Length];
            m_itemSelection = new bool[definition.Items.Length];
            foreach (var itemBuilder in definition.Items)
            {
                ContainerTypeItem item = new ContainerTypeItem();
                item.AmountMax = MyFixedPoint.DeserializeStringSafe(itemBuilder.AmountMax);
                item.AmountMin = MyFixedPoint.DeserializeStringSafe(itemBuilder.AmountMin);
                item.Frequency = Math.Max(itemBuilder.Frequency, 0.0f);
                item.DefinitionId = itemBuilder.Id;

                ItemsCumulativeFrequency += item.Frequency;

                Items[i] = item;
                m_itemSelection[i] = false;
                ++i;
            }

            m_tempCumulativeFreq = ItemsCumulativeFrequency;
        }

        public void DeselectAll()
        {
            for (int i = 0; i < Items.Length; ++i)
            {
                m_itemSelection[i] = false;
            }
            m_tempCumulativeFreq = ItemsCumulativeFrequency;
        }

        public ContainerTypeItem SelectNextRandomItem()
        {
            float rnd = MyRandom.Instance.NextFloat(0.0f, m_tempCumulativeFreq);
            int i = 0;
            while (i < Items.Length - 1)
            {
                if (m_itemSelection[i] == true)
                {
                    ++i;
                    continue;
                }
                rnd -= Items[i].Frequency;
                if (rnd < 0.0f)
                    break;
                ++i;
            }

            m_tempCumulativeFreq -= Items[i].Frequency;
            m_itemSelection[i] = true;
            return Items[i];
        }
    }
}
