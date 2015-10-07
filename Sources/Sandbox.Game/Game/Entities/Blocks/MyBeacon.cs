using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Gui;
using Sandbox.Game.Lights;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Components;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Beacon))]
    class MyBeacon : MyFunctionalBlock, IMyComponentOwner<MyDataBroadcaster>, IMyBeacon
    {
        private static readonly Color COLOR_ON = new Color(255, 255, 128);
        private static readonly Color COLOR_OFF  = new Color(30, 30, 30);
        private static readonly float POINT_LIGHT_RANGE_SMALL = 2.0f;
        private static readonly float POINT_LIGHT_RANGE_LARGE = 7.5f;
        private static readonly float POINT_LIGHT_INTENSITY_SMALL = 4;
        private static readonly float POINT_LIGHT_INTENSITY_LARGE = 2;
        private static readonly float GLARE_MAX_DISTANCE = 10000.0f;
        private const float LIGHT_TURNING_ON_TIME_IN_SECONDS = 0.5f;

        private bool m_largeLight;
        private MyLight m_light;
        private Vector3 m_lightPositionOffset;
        private float m_currentLightPower;
        private int m_lastAnimationUpdateTime;

        MyRadioBroadcaster m_radioBroadcaster;

        static MyBeacon()
        {
            MyTerminalControlFactory.RemoveBaseClass<MyBeacon, MyTerminalBlock>();

            var show = new MyTerminalControlOnOffSwitch<MyBeacon>("ShowInTerminal", MySpaceTexts.Terminal_ShowInTerminal, MySpaceTexts.Terminal_ShowInTerminalToolTip);
            show.Getter = (x) => x.ShowInTerminal;
            show.Setter = (x, v) => x.RequestShowInTerminal(v);
            MyTerminalControlFactory.AddControl(show);

            var showConfig = new MyTerminalControlOnOffSwitch<MyBeacon>("ShowInToolbarConfig", MySpaceTexts.Terminal_ShowInToolbarConfig, MySpaceTexts.Terminal_ShowInToolbarConfigToolTip);
            showConfig.Getter = (x) => x.ShowInToolbarConfig;
            showConfig.Setter = (x, v) => x.RequestShowInToolbarConfig(v);
            MyTerminalControlFactory.AddControl(showConfig);

            var customName = new MyTerminalControlTextbox<MyBeacon>("CustomName", MySpaceTexts.Name, MySpaceTexts.Blank);
            customName.Getter = (x) => x.CustomName;
            customName.Setter = (x, v) => MySyncBlockHelpers.SendChangeNameRequest(x, v);
            customName.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(customName);
            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyBeacon>());

            var broadcastRadius = new MyTerminalControlSlider<MyBeacon>("Radius", MySpaceTexts.BlockPropertyTitle_BroadcastRadius, MySpaceTexts.BlockPropertyDescription_BroadcastRadius);
            broadcastRadius.SetLogLimits(1, MyEnergyConstants.MAX_RADIO_POWER_RANGE);
            broadcastRadius.DefaultValue = 10000;
            broadcastRadius.Getter = (x) => x.RadioBroadcaster.BroadcastRadius;
            broadcastRadius.Setter = (x, v) => x.RadioBroadcaster.SyncObject.SendChangeRadioAntennaRequest(v, x.RadioBroadcaster.Enabled);
            broadcastRadius.Writer = (x, result) => result.Append(new StringBuilder().AppendDecimal(x.RadioBroadcaster.BroadcastRadius, 0).Append(" m"));
            broadcastRadius.EnableActions();
            MyTerminalControlFactory.AddControl(broadcastRadius);
        }

        private bool m_animationRunning;
        internal bool AnimationRunning
        {
            get { return m_animationRunning; }
            private set
            {
                if (m_animationRunning != value)
                {
                    m_animationRunning = value;
                    if (value)
                    {
                        NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                        m_lastAnimationUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    }
                    else
                        NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
                }
            }
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPowered && base.CheckIsWorking();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            m_radioBroadcaster = new MyRadioBroadcaster(this, 10000);
            if (((MyObjectBuilder_Beacon)objectBuilder).BroadcastRadius != 0)
                m_radioBroadcaster.BroadcastRadius = ((MyObjectBuilder_Beacon)objectBuilder).BroadcastRadius;
            m_radioBroadcaster.OnBroadcastRadiusChanged += OnBroadcastRadiusChanged;

            m_largeLight = cubeGrid.GridSizeEnum == MyCubeSize.Large;

            m_light = MyLights.AddLight();

            m_light.Start(MyLight.LightTypeEnum.PointLight, 1.5f);
            m_light.LightOwner = MyLight.LightOwnerEnum.SmallShip;
            m_light.UseInForwardRender = true;
            m_light.Range = 1;

            m_light.GlareOn = true;
            m_light.GlareIntensity = m_largeLight ? 2 : 1;
            m_light.GlareQuerySize = m_largeLight ? 7.5f : 1.22f;
            m_light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Distant;
            m_light.GlareMaterial = m_largeLight ? "GlareLsLight"
                                                 : "GlareSsLight";
            m_light.GlareMaxDistance = GLARE_MAX_DISTANCE;

            if (m_largeLight)
                m_lightPositionOffset = new Vector3(0f, CubeGrid.GridSize * 0.5f, 0f);
            else
                m_lightPositionOffset = Vector3.Zero;

            UpdateLightPosition();

	        var beaconDefinition = BlockDefinition as MyBeaconDefinition;
			Debug.Assert(beaconDefinition != null);

			var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                MyStringHash.GetOrCompute(beaconDefinition.ResourceSinkGroup),
                MyEnergyConstants.MAX_REQUIRED_POWER_BEACON,
                UpdatePowerInput);
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
            sinkComp.Update();
	        ResourceSink = sinkComp;
            AddDebugRenderComponent(new MyDebugRenderComponentDrawPowerReciever(ResourceSink,this));

            AnimationRunning = true;
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            IsWorkingChanged += MyBeacon_IsWorkingChanged;

            ShowOnHUD = false;

            UpdateText();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_Beacon objectBuilder = (MyObjectBuilder_Beacon)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.BroadcastRadius = m_radioBroadcaster.BroadcastRadius;
            return objectBuilder;
        }

        void MyBeacon_IsWorkingChanged(MyCubeBlock obj)
        {
            m_radioBroadcaster.Enabled = IsWorking;

            if (!MyFakes.ENABLE_RADIO_HUD)
            {
                if (IsWorking)
                {
                    MyHud.LocationMarkers.RegisterMarker(this, new MyHudEntityParams()
                    {
                        FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_ALL,
                        Text = CustomName,
                        OffsetText = true,
                    });
                }
                else
                {
                    MyHud.LocationMarkers.UnregisterMarker(this);
                }
            }
        }

        public override List<MyHudEntityParams> GetHudParams(bool allowBlink)
        {
            if (IsWorking)
            {
                return base.GetHudParams(allowBlink);
            }
            else
            {
                m_hudParams.Clear();
                return m_hudParams;
            }
        }

        void Receiver_IsPoweredChanged()
        {

            m_radioBroadcaster.Enabled = IsWorking;

            UpdatePower();
            UpdateLightProperties();
            UpdateIsWorking();
            UpdateText();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            ResourceSink.Update();
            UpdatePower();
            UpdateLightProperties();
            UpdateText();
        }

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);

            if (m_radioBroadcaster != null)
                m_radioBroadcaster.MoveBroadcaster();

            UpdateLightPosition();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateLightPosition();
            UpdateLightProperties();
            UpdateEmissivity();
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            UpdateLightProperties();
            UpdateEmissivity();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            float oldPower = m_currentLightPower;
            float timeDeltaInSeconds = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastAnimationUpdateTime) / 1000f;
            float sign = (IsWorking) ? 1f : -1f;
            m_currentLightPower = MathHelper.Clamp(m_currentLightPower + sign * timeDeltaInSeconds / LIGHT_TURNING_ON_TIME_IN_SECONDS, 0, 1);
            m_lastAnimationUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (m_currentLightPower <= 0)
            {
                m_light.LightOn = false;
                m_light.GlareOn = false;
            }
            else
            {
                m_light.LightOn = true;
                m_light.GlareOn = true;
            }

            if (oldPower != m_currentLightPower)
            {
                UpdateLightPosition();
                m_light.UpdateLight();
                UpdateEmissivity();
                UpdateLightProperties();
            }

            if (m_currentLightPower == (sign * 0.5f + 0.5f))
                AnimationRunning = false;
        }

        private void UpdateEmissivity()
        {
            MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], m_currentLightPower, Color.Lerp(COLOR_OFF, COLOR_ON, m_currentLightPower), Color.White);
        }

        protected override void Closing()
        {
            MyLights.RemoveLight(m_light);

            base.Closing();
        }

        private void UpdateLightPosition()
        {
            if (m_light != null)
            {
                m_light.Position = PositionComp.GetPosition() + Vector3.TransformNormal(m_lightPositionOffset, PositionComp.WorldMatrix);
                if (!AnimationRunning)
                    m_light.UpdateLight();
            }
        }

        private void UpdatePower()
        {
            AnimationRunning = true;
        }

        private void UpdateLightProperties()
        {
            if (m_light != null)
            {
                Color color = Color.Lerp(COLOR_OFF, COLOR_ON, m_currentLightPower);
                float range = m_largeLight ? POINT_LIGHT_RANGE_LARGE : POINT_LIGHT_RANGE_SMALL;
                float intensity = m_currentLightPower * (m_largeLight ? POINT_LIGHT_INTENSITY_LARGE : POINT_LIGHT_INTENSITY_SMALL);

                bool lightChanged = false;

                lightChanged |= m_light.Color != color;
                m_light.Color = color;

                lightChanged |= m_light.SpecularColor != color;
                m_light.SpecularColor = color;

                lightChanged |= m_light.Range != range;
                m_light.Range = range;

                lightChanged |= m_light.Intensity != intensity;
                m_light.Intensity = intensity;

                lightChanged |= m_light.GlareIntensity != m_currentLightPower;
                m_light.GlareIntensity = m_currentLightPower;

                if (lightChanged)
                    m_light.UpdateLight();
            }
        }

        protected override void OnEnabledChanged()
        {
            ResourceSink.Update();
            UpdatePower();

            base.OnEnabledChanged();
        }

        public MyRadioBroadcaster RadioBroadcaster
        {
            get { return m_radioBroadcaster; }
        }

        bool IMyComponentOwner<MyDataBroadcaster>.GetComponent(out MyDataBroadcaster component)
        {
            component = m_radioBroadcaster;
            return m_radioBroadcaster != null;
        }
        
        void OnBroadcastRadiusChanged()
        {
            ResourceSink.Update();
            UpdateText();
        }

        float UpdatePowerInput()
        {
            float powerFraction = m_radioBroadcaster.BroadcastRadius / 100000f; // 100km broadcast is at full power
            return (Enabled && IsFunctional) ? powerFraction * MyEnergyConstants.MAX_REQUIRED_POWER_BEACON : 0.0f;
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.IsPowered ? ResourceSink.RequiredInput : 0, DetailedInfo);
            RaisePropertiesChanged();
        }
        float IMyBeacon.Radius
        {
            get { return RadioBroadcaster.BroadcastRadius; }
        }
    }
}
