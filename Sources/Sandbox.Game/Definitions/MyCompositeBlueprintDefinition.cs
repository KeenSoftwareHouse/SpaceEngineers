using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_CompositeBlueprintDefinition))]
    public class MyCompositeBlueprintDefinition : MyBlueprintDefinitionBase
    {
        private MyBlueprintDefinitionBase[] m_blueprints;
        private Item[] m_items;

        private static List<Item> m_tmpPrerequisiteList = new List<Item>();
        private static List<Item> m_tmpResultList = new List<Item>();

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_CompositeBlueprintDefinition;

            m_items = new Item[ob.Blueprints == null ? 0 : ob.Blueprints.Length];
            for (int i = 0; i < m_items.Length; ++i)
            {
                m_items[i] = Item.FromObjectBuilder(ob.Blueprints[i]);
            }

            PostprocessNeeded = true;
        }

        public override void Postprocess()
        {
            // First find out whether all the referenced blueprints are already preprocessed
            foreach (var item in m_items)
            {
                if (!MyDefinitionManager.Static.HasBlueprint(item.Id)) return;
                var blueprint = MyDefinitionManager.Static.GetBlueprintDefinition(item.Id);
                if (blueprint.PostprocessNeeded) return;
            }

            float volume = 0.0f;
            bool atomic = false;
            float productionTime = 0.0f;

            m_blueprints = new MyBlueprintDefinitionBase[m_items.Length];
            m_tmpPrerequisiteList.Clear();
            m_tmpResultList.Clear();

            for (int i = 0; i < m_items.Length; ++i)
            {
                MyFixedPoint blueprintAmount = m_items[i].Amount;
                var blueprint = MyDefinitionManager.Static.GetBlueprintDefinition(m_items[i].Id);
                m_blueprints[i] = blueprint;

                atomic = atomic || blueprint.Atomic;
                volume += blueprint.OutputVolume * (float)blueprintAmount;
                productionTime += blueprint.BaseProductionTimeInSeconds * (float)blueprintAmount;

                PostprocessAddSubblueprint(blueprint, blueprintAmount);
            }

            Prerequisites = m_tmpPrerequisiteList.ToArray();
            Results = m_tmpResultList.ToArray();
            m_tmpPrerequisiteList.Clear();
            m_tmpResultList.Clear();

            Atomic = atomic;
            OutputVolume = volume;
            BaseProductionTimeInSeconds = productionTime;

            PostprocessNeeded = false;
        }

        private void PostprocessAddSubblueprint(MyBlueprintDefinitionBase blueprint, MyFixedPoint blueprintAmount)
        {
            for (int i = 0; i < blueprint.Prerequisites.Length; ++i)
            {
                Item prerequisite = blueprint.Prerequisites[i];
                prerequisite.Amount *= blueprintAmount;
                AddToItemList(m_tmpPrerequisiteList, prerequisite);
            }

            for (int i = 0; i < blueprint.Results.Length; ++i)
            {
                Item result = blueprint.Results[i];
                result.Amount *= blueprintAmount;
                AddToItemList(m_tmpResultList, result);
            }
        }

        private void AddToItemList(List<Item> items, Item toAdd)
        {
            int insertAt = 0;
            Item oldItem = new Item();
            for (insertAt = 0; insertAt < items.Count; ++insertAt)
            {
                oldItem = items[insertAt];
                if (oldItem.Id == toAdd.Id) break;
            }

            if (insertAt >= items.Count)
            {
                items.Add(toAdd);
            }
            else
            {
                oldItem.Amount += toAdd.Amount;
                items[insertAt] = oldItem;
            }
        }

        public override int GetBlueprints(List<MyBlueprintDefinitionBase.ProductionInfo> blueprints)
        {
            int total = 0;
            for (int i = 0; i < m_blueprints.Length; ++i)
            {
                int added = m_blueprints[i].GetBlueprints(blueprints);
                int count = blueprints.Count;
                for (int j = count - 1; j >= count - added; --j)
                {
                    ProductionInfo info = blueprints[j];
                    info.Amount *= m_items[i].Amount;
                    blueprints[j] = info;
                }
                total += added;
            }
            return total;
        }
    }
}
