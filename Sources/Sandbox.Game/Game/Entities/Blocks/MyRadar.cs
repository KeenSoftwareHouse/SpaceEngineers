using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Radar))]
    class MyRadar : MyFunctionalBlock, IMyPowerConsumer, IMyComponentOwner<MyRadarComponent>, IMyRadar
    {
        private MyRadarDefinition m_definition;
        private MyRadarComponent m_radarComponent;
        private MyRadioBroadcaster m_radioBroadcaster;
        public new MySyncRadar SyncObject;

        public const int InfiniteTracking = 101;
        public const float InfiniteSize = 10000;

        static MyRadar()
        {
            var range = new MyTerminalControlSlider<MyRadar>("Range", MySpaceTexts.BlockPropertyTitle_RadarRange, MySpaceTexts.BlockPropertyDescription_RadarRange);
            range.SetLogLimits(block => 1000, block => block.m_definition.MaximumRange);
            range.DefaultValue = 10000;
            range.Getter = (x) => x.Range;
            range.Setter = (x, v) => x.SyncObject.SendChangeRadar(v, x.MinimumSize, x.MaximumSize, x.TrackingLimit, x.BroadcastUsingAntennas);
            range.Writer = (x, result) => result.AppendInt32((int)x.Range).Append(" m");
            MyTerminalControlFactory.AddControl(range);

            var minimumSize = new MyTerminalControlSlider<MyRadar>("MinimumSize",
                MySpaceTexts.BlockPropertyTitle_RadarMinimumSize, MySpaceTexts.BlockPropertyDescription_RadarMinimumSize);
            minimumSize.SetLogLimits(block => 1, block => 500);
            minimumSize.DefaultValue = 1;
            minimumSize.Getter = (x) => x.MinimumSize;
            minimumSize.Setter = (x, v) => x.SyncObject.SendChangeRadar(x.Range, v, Math.Max(x.MaximumSize, v * 2), x.TrackingLimit, x.BroadcastUsingAntennas);
            minimumSize.Writer = (x, result) => { result.AppendInt32((int) x.MinimumSize).Append(" m"); };
            MyTerminalControlFactory.AddControl(minimumSize);

            var maximumSize = new MyTerminalControlSlider<MyRadar>("MaximumSize",
                MySpaceTexts.BlockPropertyTitle_RadarMaximumSize, MySpaceTexts.BlockPropertyDescription_RadarMaximumSize);
            maximumSize.SetLogLimits(block => 5, block => InfiniteSize);
            maximumSize.DefaultValue = InfiniteSize;
            maximumSize.Getter = (x) => x.MaximumSize;
            maximumSize.Setter = (x, v) => x.SyncObject.SendChangeRadar(x.Range, Math.Min(x.MinimumSize, v / 2), v, x.TrackingLimit, x.BroadcastUsingAntennas);
            maximumSize.Writer = (x, result) =>
            {
                if (x.MaximumSize >= InfiniteSize)
                    result.Append("Unlimited");
                else
                    result.AppendInt32((int) x.MaximumSize).Append(" m");
            };
            MyTerminalControlFactory.AddControl(maximumSize);

            var trackingLimit = new MyTerminalControlSlider<MyRadar>("TrackingLimit",
                MySpaceTexts.BlockPropertyTitle_RadarTrackingLimit, MySpaceTexts.BlockPropertyDescription_RadarTrackingLimit);
            trackingLimit.SetLimits(1, InfiniteTracking + 1);
            trackingLimit.DefaultValue = 20;
            trackingLimit.Getter = (x) => x.TrackingLimit;
            trackingLimit.Setter = (x, v) => x.SyncObject.SendChangeRadar(x.Range, x.MinimumSize, x.MaximumSize, (int)v, x.BroadcastUsingAntennas);
            trackingLimit.Writer = (x, result) =>
            {
                if (x.TrackingLimit >= InfiniteTracking) 
                    result.Append("Unlimited");
                else 
                    result.AppendInt32(x.TrackingLimit);
            };
            MyTerminalControlFactory.AddControl(trackingLimit);

            var broadcastUsingAntennas = new MyTerminalControlCheckbox<MyRadar>("BroadcastUsingAntennas", MySpaceTexts.BlockPropertyDescription_BroadcastUsingAntennas, MySpaceTexts.BlockPropertyDescription_BroadcastUsingAntennas);
            broadcastUsingAntennas.Getter = (x) => x.m_radarComponent.BroadcastUsingAntennas;
            broadcastUsingAntennas.Setter = (x, v) => x.SyncObject.SendChangeRadar(x.Range, x.MinimumSize, x.MaximumSize, x.TrackingLimit, v);
            MyTerminalControlFactory.AddControl(broadcastUsingAntennas);
        }

        public float MinimumSize
        {
            get { return m_radarComponent.MinimumSize; }
            set
            {
                if (m_radarComponent.MinimumSize != value)
                {
                    m_radarComponent.MinimumSize = value;
                    RaisePropertiesChanged();
                }
            }
        }

        public float MaximumSize
        {
            get { return m_radarComponent.MaximumSize; }
            set
            {
                if (m_radarComponent.MaximumSize != value)
                {
                    m_radarComponent.MaximumSize = value;
                    RaisePropertiesChanged();
                }
            }
        }

        public int TrackingLimit
        {
            get { return m_radarComponent.TrackingLimit; }
            set
            {
                if (m_radarComponent.TrackingLimit != value)
                {
                    m_radarComponent.TrackingLimit = value;
                    PowerReceiver.Update();
                    RaisePropertiesChanged();
                    UpdateText();
                }
            }
        }

        public bool GetComponent(out MyRadarComponent component)
        {
            component = m_radarComponent;
            return IsWorking;
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        public float Range
        {
            get
            {
                return m_radarComponent.DetectionRadius;
            }
            set
            {
                if (m_radarComponent.DetectionRadius != value || m_radioBroadcaster.BroadcastRadius != value)
                {
                    m_radarComponent.DetectionRadius = value;
                    m_radioBroadcaster.BroadcastRadius = value;
                    PowerReceiver.Update();
                    RaisePropertiesChanged();
                    UpdateText();
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

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            m_definition = BlockDefinition as MyRadarDefinition;

            SyncObject = new MySyncRadar(this);

            var ob = objectBuilder as MyObjectBuilder_Radar;

            m_radioBroadcaster = new MyRadioBroadcaster(this);
            m_radarComponent = new MyRadarComponent();

            m_radarComponent.DetectionRadius = ob.DetectionRadius;
            if (m_radarComponent.DetectionRadius == 0)
                m_radarComponent.DetectionRadius = m_definition.MaximumRange;
            m_radioBroadcaster.BroadcastRadius = m_radarComponent.DetectionRadius;

            m_radarComponent.MinimumSize = ob.MinimumSize;
            if (m_radarComponent.MinimumSize == 0)
                m_radarComponent.MinimumSize = 1;

            m_radarComponent.MaximumSize = ob.MaximumSize;
            if (m_radarComponent.MaximumSize == 0)
                m_radarComponent.MaximumSize = 10000;

            m_radarComponent.TrackingLimit = ob.TrackingLimit;
            if (m_radarComponent.TrackingLimit == 0)
                m_radarComponent.TrackingLimit = 20;

            m_radarComponent.BroadcastUsingAntennas = ob.BroadcastUsingAntennas;

            m_radarComponent.OnCheckControl += OnCheckControl;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Factory,
                false,
                m_definition.MaxRequiredPowerInput,
                UpdatePowerInput);
            PowerReceiver.Update();
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawPowerReciever(PowerReceiver, this));
        }

        private float UpdatePowerInput()
        {
            float trackingRatio = (Math.Min(TrackingLimit, 100) + 25f) / 125f;
            float rangeRatio = Range / m_definition.MaximumRange;
            float requiredInput = m_definition.MaxRequiredPowerInput * trackingRatio * rangeRatio;
            UpdateText();
            return (Enabled && IsFunctional) ? requiredInput : 0f;
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.IsPowered ? PowerReceiver.RequiredInput : 0, DetailedInfo);
            RaisePropertiesChanged();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_Radar;
            builder.DetectionRadius = m_radarComponent.DetectionRadius;
            builder.MaximumSize = m_radarComponent.MaximumSize;
            builder.MinimumSize = m_radarComponent.MinimumSize;
            builder.TrackingLimit = m_radarComponent.TrackingLimit;
            builder.BroadcastUsingAntennas = m_radarComponent.BroadcastUsingAntennas;
            return builder;
        }

        protected override void OnEnabledChanged()
        {
            PowerReceiver.Update();
            base.OnEnabledChanged();
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            m_radioBroadcaster.Enabled = IsWorking;
            UpdateEmissivity();
            UpdateText();
        }

        private void UpdateEmissivity()
        {
            if (IsWorking)
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
            else
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White); ;
        }

        public override void OnUnregisteredFromGridSystems()
        {
            m_radarComponent.Clear();
            base.OnUnregisteredFromGridSystems();
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            if (HasLocalPlayerAccess())
            {
                m_radarComponent.Update(PositionComp.GetPosition());
            }
            else
            {
                m_radarComponent.Clear();
            }
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
            UpdateText();
        }

        private bool OnCheckControl()
        {
            bool isControlled = Sandbox.Game.World.MySession.ControlledEntity != null && ((MyEntity)Sandbox.Game.World.MySession.ControlledEntity).Parent == Parent;
            return IsWorking && isControlled;
        }

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);
            if (m_radioBroadcaster != null)
                m_radioBroadcaster.MoveBroadcaster();
        }
    }
}
