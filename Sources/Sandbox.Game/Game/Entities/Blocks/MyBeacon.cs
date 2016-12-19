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
using Sandbox.ModAPI;
using Sandbox.Game.Localization;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRage.ModAPI;
using VRage.Game.Gui;
using VRage.Sync;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Beacon))]
    public class MyBeacon : MyFunctionalBlock, IMyBeacon
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

        internal MyRadioBroadcaster RadioBroadcaster 
        { 
            get { return (MyRadioBroadcaster)Components.Get<MyDataBroadcaster>(); }
            private set { Components.Add<MyDataBroadcaster>(value); }
        }
        readonly Sync<float> m_radius;

        public MyBeacon()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_radius = SyncType.CreateAndAddProp<float>();
#endif // XB1
            CreateTerminalControls();

            m_radius.ValueChanged += (obj) => ChangeRadius();
        }

        void ChangeRadius()
        {
            RadioBroadcaster.BroadcastRadius = m_radius;
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyBeacon>())
                return;
            base.CreateTerminalControls();
            //MyTerminalControlFactory.RemoveBaseClass<MyBeacon, MyTerminalBlock>(); // this removed also controls shared with other blocks

            //removed unnecessary controls
            var controlList = MyTerminalControlFactory.GetList(typeof(MyBeacon)).Controls;
            controlList.Remove(controlList[4]);//name
            controlList.Remove(controlList[4]);//show on HUD

            var customName = new MyTerminalControlTextbox<MyBeacon>("CustomName", MyCommonTexts.Name, MySpaceTexts.Blank);
            customName.Getter = (x) => x.CustomName;
            customName.Setter = (x, v) => x.SetCustomName(v);
            customName.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(customName);
            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyBeacon>());

            var broadcastRadius = new MyTerminalControlSlider<MyBeacon>("Radius", MySpaceTexts.BlockPropertyTitle_BroadcastRadius, MySpaceTexts.BlockPropertyDescription_BroadcastRadius);
            broadcastRadius.SetLogLimits(1, MyEnergyConstants.MAX_RADIO_POWER_RANGE);
            broadcastRadius.DefaultValue = 10000;
            broadcastRadius.Getter = (x) => x.RadioBroadcaster.BroadcastRadius;
            broadcastRadius.Setter = (x, v) => x.m_radius.Value = v;
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
                    else if (IsFunctional)
                        NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
                }
            }
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            var beaconDefinition = BlockDefinition as MyBeaconDefinition;
            Debug.Assert(beaconDefinition != null);

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                MyStringHash.GetOrCompute(beaconDefinition.ResourceSinkGroup),
                MyEnergyConstants.MAX_REQUIRED_POWER_BEACON,
                UpdatePowerInput);
    
            ResourceSink = sinkComp;

            RadioBroadcaster = new MyRadioBroadcaster(10000);
            if (((MyObjectBuilder_Beacon)objectBuilder).BroadcastRadius != 0)
                RadioBroadcaster.BroadcastRadius = ((MyObjectBuilder_Beacon)objectBuilder).BroadcastRadius;

            base.Init(objectBuilder, cubeGrid);

            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
            sinkComp.Update();
        
            RadioBroadcaster.OnBroadcastRadiusChanged += OnBroadcastRadiusChanged;

            m_largeLight = cubeGrid.GridSizeEnum == MyCubeSize.Large;

            m_light = MyLights.AddLight();

            m_light.Start(MyLight.LightTypeEnum.PointLight, 1.5f);
            m_light.LightOwner = m_largeLight ? MyLight.LightOwnerEnum.LargeShip : MyLight.LightOwnerEnum.SmallShip;
            m_light.UseInForwardRender = true;
            m_light.Range = 1;

            m_light.GlareOn = true;
            m_light.GlareIntensity = m_largeLight ? 2 : 1;
            m_light.GlareQuerySize = m_largeLight ? 1.0f : 0.2f;
            m_light.GlareSize = 1.0f;
            m_light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Distant;
            m_light.GlareMaterial = m_largeLight ? "GlareLsLight"
                                                 : "GlareSsLight";
            m_light.GlareMaxDistance = GLARE_MAX_DISTANCE;

            if (m_largeLight)
                m_lightPositionOffset = new Vector3(0f, CubeGrid.GridSize * 0.5f, 0f);
            else
                m_lightPositionOffset = Vector3.Zero;

            UpdateLightPosition();

	       
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
            objectBuilder.BroadcastRadius = RadioBroadcaster.BroadcastRadius;
            return objectBuilder;
        }

        void MyBeacon_IsWorkingChanged(MyCubeBlock obj)
        {
            if(RadioBroadcaster != null)
                RadioBroadcaster.Enabled = IsWorking;

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
            if (RadioBroadcaster != null)
                RadioBroadcaster.Enabled = IsWorking;
            else
                Debug.Fail("Radio broadcaster is null in Beacon");

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

            if (RadioBroadcaster != null)
                RadioBroadcaster.MoveBroadcaster();

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

        void OnBroadcastRadiusChanged()
        {
            ResourceSink.Update();
            UpdateText();
        }

        float UpdatePowerInput()
        {
            float powerFraction = RadioBroadcaster.BroadcastRadius / 100000f; // 100km broadcast is at full power
            return (Enabled && IsFunctional) ? powerFraction * MyEnergyConstants.MAX_REQUIRED_POWER_BEACON : 0.0f;
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) ? ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0, DetailedInfo);
            RaisePropertiesChanged();
        }
        float ModAPI.Ingame.IMyBeacon.Radius
        {
            get { return RadioBroadcaster.BroadcastRadius; }
        }
    }
}
