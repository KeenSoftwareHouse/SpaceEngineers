#region Using

using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Localization;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;
using System.Diagnostics;
using VRage.Game;

#endregion

namespace Sandbox.Game.Gui
{
    #region Power Group Info
    public class MyHudSinkGroupInfo
    {
        private bool m_needsRefresh = true;
        private float[] m_missingPowerByGroup;
        private MyStringId[] m_groupNames;
        private float m_missingTotal;
        public int GroupCount;

        private int m_workingGroupCount;
        public int WorkingGroupCount
        {
            get { return m_workingGroupCount; }
            set
            {
                if (m_workingGroupCount != value)
                {
                    m_workingGroupCount = value;
                    m_needsRefresh = true;
                }
            }
        }

        private bool m_visible = false;
        public bool Visible
        {
            get { return m_visible && WorkingGroupCount != GroupCount; }
            set { m_visible = value; }
        }

        public MyHudNameValueData Data
        {
            get { if (m_needsRefresh) Refresh(); return m_data; }
        }
        private MyHudNameValueData m_data;

        public MyHudSinkGroupInfo()
        {
            Reload();
        }

	    public void Reload()
	    {
		    if (m_groupNames == null && MyResourceDistributorComponent.SinkGroupPrioritiesTotal != -1)
		    {
				m_groupNames = new MyStringId[MyResourceDistributorComponent.SinkGroupPrioritiesTotal];

			    GroupCount = m_groupNames.Length;
			    m_missingPowerByGroup = new float[GroupCount];
			    WorkingGroupCount = GroupCount;

			    m_data = new MyHudNameValueData(GroupCount + 1, showBackgroundFog: true);
		    }

		    if (m_groupNames == null)
			    return;

            MyResourceDistributorComponent.InitializeMappings();
		    var distributionGroups = MyDefinitionManager.Static.GetDefinitionsOfType<MyResourceDistributionGroupDefinition>();

			var sinkSubtypesToIndex = MyResourceDistributorComponent.SinkSubtypesToPriority;
		    foreach (var distributionGroup in distributionGroups)
		    {
                if (!distributionGroup.IsSource)
                {
                    int priorityIndex;
                    if(!sinkSubtypesToIndex.TryGetValue(distributionGroup.Id.SubtypeId, out priorityIndex))
                    {
                        Debug.Fail("Sink subtype " + distributionGroup.Id.SubtypeName + " not found!");
                        continue;
                    }
                    m_groupNames[priorityIndex] = MyStringId.GetOrCompute(distributionGroup.Id.SubtypeName);
                }
		    }

		    /*      m_groupNames[(int)MyResourceSinkGroupEnum.Charging]     = MySpaceTexts.HudEnergyGroupCharging;
            m_groupNames[(int)MyResourceSinkGroupEnum.Conveyors]    = MySpaceTexts.HudEnergyGroupConveyors;
            m_groupNames[(int)MyResourceSinkGroupEnum.Defense]      = MySpaceTexts.HudEnergyGroupDefense;
            m_groupNames[(int)MyResourceSinkGroupEnum.Doors]        = MySpaceTexts.HudEnergyGroupDoors;
            m_groupNames[(int)MyResourceSinkGroupEnum.Factory]      = MySpaceTexts.HudEnergyGroupFactory;
            m_groupNames[(int)MyResourceSinkGroupEnum.Gyro]         = MySpaceTexts.HudEnergyGroupGyroscope;
            m_groupNames[(int)MyResourceSinkGroupEnum.Thrust]       = MySpaceTexts.HudEnergyGroupThrust;
            m_groupNames[(int)MyResourceSinkGroupEnum.Utility]      = MySpaceTexts.HudEnergyGroupUtility;
            m_groupNames[(int)MyResourceSinkGroupEnum.BatteryBlock] = MySpaceTexts.HudEnergyGroupBatteries;*/
		    Data[GroupCount].NameFont = MyFontEnum.Red;
		    Data[GroupCount].ValueFont = MyFontEnum.Red;
	    }

	    internal void SetGroupDeficit(int groupIndex, float missingPower)
        {
            if(m_missingPowerByGroup == null)
                Reload();

			m_missingTotal += missingPower - m_missingPowerByGroup[groupIndex];
			m_missingPowerByGroup[groupIndex] = missingPower;
            m_needsRefresh = true;
        }

        private void Refresh()
        {
            m_needsRefresh = false;

            var data = Data;

            for (int i = 0; i < data.Count - 1; ++i)
            {
                data[i].Name.Clear().AppendStringBuilder(MyTexts.Get(m_groupNames[i]));
            }
            data[GroupCount].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudEnergyMissingTotal));

            var item = data[GroupCount];
            item.Value.Clear();
            MyValueFormatter.AppendWorkInBestUnit(-m_missingTotal, item.Value);

            for (int i = 0; i < GroupCount; ++i)
            {
                item = data[i];

                if (i < m_workingGroupCount)
                    item.NameFont = item.ValueFont = null;
                else
                    item.NameFont = item.ValueFont = MyFontEnum.Red;

                item.Value.Clear();
                MyValueFormatter.AppendWorkInBestUnit(-m_missingPowerByGroup[i], item.Value);
            }
        }
    }
    #endregion
}
