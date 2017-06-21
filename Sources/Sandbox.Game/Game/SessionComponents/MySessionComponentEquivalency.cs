using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.Components;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MySessionComponentEquivalency: MySessionComponentBase
    {
        public static MySessionComponentEquivalency Static;

        private Dictionary<MyDefinitionId, HashSet<MyDefinitionId>> m_equivalencyGroups;
        private Dictionary<MyDefinitionId, MyDefinitionId> m_groupMain;
        private HashSet<MyDefinitionId> m_forcedMain;

        public MySessionComponentEquivalency()
        {
            Static = this;

            m_equivalencyGroups = new Dictionary<MyDefinitionId, HashSet<MyDefinitionId>>(MyDefinitionId.Comparer);
            m_groupMain = new Dictionary<MyDefinitionId, MyDefinitionId>(MyDefinitionId.Comparer);
            m_forcedMain = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);
        }

        public override bool IsRequiredByGame
        {
            get { return MyPerGameSettings.Game == GameEnum.ME_GAME; }
        }

        public override void LoadData()
        {
            base.LoadData();

            m_equivalencyGroups = new Dictionary<MyDefinitionId, HashSet<MyDefinitionId>>(MyDefinitionId.Comparer);
            m_groupMain = new Dictionary<MyDefinitionId, MyDefinitionId>(MyDefinitionId.Comparer);
            m_forcedMain = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);

            var groups = MyDefinitionManager.Static.GetDefinitions<MyEquivalencyGroupDefinition>();
            foreach (var groupDef in groups)
            {
                var mainId = groupDef.MainElement;
                HashSet<MyDefinitionId> group;
                if (!m_equivalencyGroups.TryGetValue(mainId, out group)) {
                    group = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);
                }

                foreach (var equivalent in groupDef.Equivalents)
                {
                    Debug.Assert(!m_groupMain.ContainsKey(equivalent), String.Format("{0} is already registered with {1}, override can have unpredictable results!", equivalent, mainId));
                    m_groupMain[equivalent] = mainId;
                    if (groupDef.ForceMainElement)
                        m_forcedMain.Add(equivalent);
                    group.Add(equivalent);
                }

                group.Add(mainId);
                m_equivalencyGroups[mainId] = group;
            }
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            m_equivalencyGroups = null;
            m_groupMain = null;
            m_forcedMain = null;
        }

        public MyDefinitionId GetMainElement(MyDefinitionId id)
        {
            MyDefinitionId result;
            if (m_groupMain.TryGetValue(id, out result))
                return result;

            return id;
        }

        public bool ForceMainElement(MyDefinitionId id)
        {
            return m_forcedMain.Contains(id);
        }

        public MyDefinitionId Convert(MyDefinitionId id)
        {
            if (m_forcedMain.Contains(id))
                return m_groupMain[id];
            else
                return id;
        }

        public bool HasEquivalents(MyDefinitionId id)
        {
            return m_equivalencyGroups.ContainsKey(GetMainElement(id));
        }

        public HashSet<MyDefinitionId> GetEquivalents(MyDefinitionId id)
        {
            MyDefinitionId baseId = GetMainElement(id);
            HashSet<MyDefinitionId> group;
            if (m_equivalencyGroups.TryGetValue(baseId, out group))
                return group;

            return null;
        }

        public bool IsProvided(Dictionary<MyDefinitionId, MyFixedPoint> itemCounts, MyDefinitionId required, int amount = 1)
        {
            if (amount == 0)
                return true;

            var group = GetEquivalents(required);
            if (group == null)
                return false;

            int providedAmount = 0;
            foreach (var item in itemCounts)
            {
                if (group.Contains(item.Key))
                    providedAmount += item.Value.ToIntSafe();
            }
            return providedAmount >= amount;
        }

    }
}
