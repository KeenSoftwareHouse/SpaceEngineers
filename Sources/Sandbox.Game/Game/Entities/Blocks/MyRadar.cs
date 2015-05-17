using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI.Ingame;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Radar))]
    class MyRadar : MyFunctionalBlock, IMyPowerConsumer, IMyComponentOwner<MyRadarComponent>, IMyRadar
    {
        private MyRadarDefinition m_definition;

        private MyRadarComponent m_radarComponent;

        public new MySyncRadar SyncObject;

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        static MyRadar()
        {
            var range = new MyTerminalControlSlider<MyRadar>("Range", MySpaceTexts.BlockPropertyTitle_OreDetectorRange, MySpaceTexts.BlockPropertyDescription_OreDetectorRange);
            range.SetLimits(1, 100);
            range.DefaultValue = 100;
            range.Getter = (x) => x.Range;
            range.Setter = (x, v) => x.Range = v;
            range.Writer = (x, result) => result.AppendInt32((int)x.m_radarComponent.DetectionRadius).Append(" m");

            var broadcastUsingAntennas = new MyTerminalControlCheckbox<MyRadar>("BroadcastUsingAntennas", MySpaceTexts.BlockPropertyDescription_BroadcastUsingAntennas, MySpaceTexts.BlockPropertyDescription_BroadcastUsingAntennas);
            broadcastUsingAntennas.Getter = (x) => x.m_radarComponent.BroadcastUsingAntennas;
            broadcastUsingAntennas.Setter = (x, v) => x.SyncObject.SendChangeOreDetector(v);
            MyTerminalControlFactory.AddControl(broadcastUsingAntennas);
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        public bool GetComponent(out MyRadarComponent component)
        {
            component = m_radarComponent;
            return IsWorking;
        }

        public float Range
        {
            get
            {
                return (m_radarComponent.DetectionRadius / m_definition.MaximumRange) * 100f;
            }
            set
            {
                if (m_radarComponent.DetectionRadius != value)
                {
                    m_radarComponent.DetectionRadius = (value / 100f) * m_definition.MaximumRange;
                    RaisePropertiesChanged();
                }
            }
        }


        public bool BroadcastUsingAntennas
        {
            get { return m_radarComponent.BroadcastUsingAntennas; }
            set
            {
                m_radarComponent.BroadcastUsingAntennas = value;
                RaisePropertiesChanged();
            }
        }
    }
}
