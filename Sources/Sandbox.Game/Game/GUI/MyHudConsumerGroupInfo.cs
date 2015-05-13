#region Using

using Sandbox.Common;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Localization;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;


#endregion

namespace Sandbox.Game.Gui
{
    #region Power Group Info
    public class MyHudConsumerGroupInfo
    {
        private bool m_needsRefresh = true;
        private float[] m_missingPowerByGroup;
        private MyStringId[] m_groupNames;
        private float m_missingTotal;
        public readonly int GroupCount;

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

        public MyHudConsumerGroupInfo()
        {
            m_groupNames = new MyStringId[typeof(MyConsumerGroupEnum).GetEnumValues().Length];

            GroupCount            = m_groupNames.Length;
            m_missingPowerByGroup = new float[GroupCount];
            WorkingGroupCount     = GroupCount;

            m_data = new MyHudNameValueData(GroupCount+1, showBackgroundFog: true);

            Reload();
        }

        public void Reload()
        {
            m_groupNames[(int)MyConsumerGroupEnum.Charging]     = MySpaceTexts.HudEnergyGroupCharging;
            m_groupNames[(int)MyConsumerGroupEnum.Conveyors]    = MySpaceTexts.HudEnergyGroupConveyors;
            m_groupNames[(int)MyConsumerGroupEnum.Defense]      = MySpaceTexts.HudEnergyGroupDefense;
            m_groupNames[(int)MyConsumerGroupEnum.Doors]        = MySpaceTexts.HudEnergyGroupDoors;
            m_groupNames[(int)MyConsumerGroupEnum.Factory]      = MySpaceTexts.HudEnergyGroupFactory;
            m_groupNames[(int)MyConsumerGroupEnum.Gyro]         = MySpaceTexts.HudEnergyGroupGyroscope;
            m_groupNames[(int)MyConsumerGroupEnum.Thrust]       = MySpaceTexts.HudEnergyGroupThrust;
            m_groupNames[(int)MyConsumerGroupEnum.Utility]      = MySpaceTexts.HudEnergyGroupUtility;
            m_groupNames[(int)MyConsumerGroupEnum.BatteryBlock] = MySpaceTexts.HudEnergyGroupBatteries;
            Data[GroupCount].NameFont                           = MyFontEnum.Red;
            Data[GroupCount].ValueFont                          = MyFontEnum.Red;
        }

        internal void SetGroupDeficit(MyConsumerGroupEnum group, float missingPower)
        {
            m_missingTotal += missingPower - m_missingPowerByGroup[(int)group];
            m_missingPowerByGroup[(int)group] = missingPower;
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
