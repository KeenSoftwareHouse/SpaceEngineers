using System;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Lights;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.Gui;

using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Blocks
{
    public abstract class MyLightingBlock : MyFunctionalBlock, IMyLightingBlock
    {
        private const int NUM_DECIMALS = 1;
        private float m_blinkIntervalSeconds;
        private float m_blinkLength;
        private float m_blinkOffset;

        protected MyLight m_light;
        private float m_intesity;
        private new MySyncLightingBlock SyncObject;

        private Vector3D m_lightWorldPosition;
        private Vector3 m_lightLocalPosition;
        private float m_lightTurningOnSpeed = 0.05f;
        private bool m_positionDirty = true;

        #region Properties

        public new MyLightingBlockDefinition BlockDefinition
        {
            get { return (MyLightingBlockDefinition)base.BlockDefinition; }
        }

        public MyBounds BlinkIntervalSecondsBounds
        {
            get { return BlockDefinition.BlinkIntervalSeconds; }
        }
        public MyBounds BlinkLenghtBounds
        {
            get { return BlockDefinition.BlinkLenght; }
        }
        public MyBounds BlinkOffsetBounds
        {
            get { return BlockDefinition.BlinkOffset; }
        }
        public MyBounds FalloffBounds
        {
            get { return BlockDefinition.LightFalloff; }
        }

        public MyBounds RadiusBounds
        {
            get { return BlockDefinition.LightRadius; }
        }

        public MyBounds IntensityBounds
        {
            get { return BlockDefinition.LightIntensity; }
        }

        public float ShortReflectorRangeDef
        {
            get { return IsLargeLight ? 120f : 60f; }
        }

        public float ShortReflectorForwardConeAngleDef
        {
            get { return IsLargeLight ? 0.311f : 0.44f; }
        }

        public Vector4 LightColorDef
        {
            get { return ((IsLargeLight) ? new Color(255, 255, 222) : new Color(206, 235, 255)).ToVector4(); }
        }

        public float ReflectorIntensityDef
        {
            get { return IsLargeLight ? 0.5f : 1.137f; }
        }

        protected override bool CheckIsWorking()
        {
			return (MyFakes.ENABLE_LIGHT_WITHOUT_POWER || ResourceSink.IsPowered) && base.CheckIsWorking();
        }

        public bool IsLargeLight { get; private set; }

        public MyLight Light
        {
            get { return m_light; }
        }

        internal new MyRenderComponentLight Render
        {
            get { return (MyRenderComponentLight)base.Render; }
            set { base.Render = value; }
        }

        #endregion

        #region Terminal properties
        static MyLightingBlock()
        {
            var lightColor = new MyTerminalControlColor<MyLightingBlock>("Color", MySpaceTexts.BlockPropertyTitle_LightColor);
            lightColor.Getter = (x) => x.Color;
            lightColor.Setter = (x, v) => x.SyncObject.SendChangeLightColorRequest(v);
            MyTerminalControlFactory.AddControl(lightColor);

            var lightRadius = new MyTerminalControlSlider<MyLightingBlock>("Radius", MySpaceTexts.BlockPropertyTitle_LightRadius, MySpaceTexts.BlockPropertyDescription_LightRadius);
            lightRadius.SetLimits((x) => x.RadiusBounds.Min, (x) => x.RadiusBounds.Max);
            lightRadius.DefaultValueGetter = (x) => x.RadiusBounds.Default;
            lightRadius.Getter = (x) => x.Radius;
            lightRadius.Setter = (x, v) => x.SyncObject.SendChangeLightRadiusRequest(v);
            lightRadius.Writer = (x, result) => result.Append(MyValueFormatter.GetFormatedFloat(x.m_light.Range, 1)).Append(" m");
            lightRadius.EnableActions();
            MyTerminalControlFactory.AddControl(lightRadius);

            var lightFalloff = new MyTerminalControlSlider<MyLightingBlock>("Falloff", MySpaceTexts.BlockPropertyTitle_LightFalloff, MySpaceTexts.BlockPropertyDescription_LightFalloff);
            lightFalloff.SetLimits((x) => x.FalloffBounds.Min, (x) => x.FalloffBounds.Max);
            lightFalloff.DefaultValueGetter = (x) => x.FalloffBounds.Default;
            lightFalloff.Getter = (x) => x.Falloff;
            lightFalloff.Setter = (x, v) => x.SyncObject.SendChangeLightFalloffRequest(v);
            lightFalloff.Writer = (x, result) => result.Append(MyValueFormatter.GetFormatedFloat(x.m_light.Falloff, 1));
            lightRadius.EnableActions();
            MyTerminalControlFactory.AddControl(lightFalloff);

            var lightIntensity = new MyTerminalControlSlider<MyLightingBlock>("Intensity", MySpaceTexts.BlockPropertyTitle_LightIntensity, MySpaceTexts.BlockPropertyDescription_LightIntensity);
            lightIntensity.SetLimits((x) => x.IntensityBounds.Min, (x) => x.IntensityBounds.Max);
            lightIntensity.DefaultValueGetter = (x) => x.IntensityBounds.Default;
            lightIntensity.Getter = (x) => x.Intensity;
            lightIntensity.Setter = (x, v) => x.SyncObject.SendChangeLightIntensityRequest(v);
            lightIntensity.Writer = (x, result) => result.Append(MyValueFormatter.GetFormatedFloat(x.Intensity, 1));
            lightRadius.EnableActions();
            MyTerminalControlFactory.AddControl(lightIntensity);

            var lightBlinkTime = new MyTerminalControlSlider<MyLightingBlock>("Blink Interval", MySpaceTexts.BlockPropertyTitle_LightBlinkInterval, MySpaceTexts.BlockPropertyDescription_LightBlinkInterval);
            lightBlinkTime.SetLimits((x) => x.BlinkIntervalSecondsBounds.Min, (x) => x.BlinkIntervalSecondsBounds.Max);
            lightBlinkTime.DefaultValueGetter = (x) => x.BlinkIntervalSecondsBounds.Default;
            lightBlinkTime.Getter = (x) => x.BlinkIntervalSeconds;
            lightBlinkTime.Setter = (x, v) => x.SyncObject.SendChangeLightBlinkIntervalRequest(v);
            lightBlinkTime.Writer = (x, result) => result.Append(MyValueFormatter.GetFormatedFloat(x.BlinkIntervalSeconds, NUM_DECIMALS)).Append(" s");
            lightBlinkTime.EnableActions();
            MyTerminalControlFactory.AddControl(lightBlinkTime);

            var lightBlinkLenght = new MyTerminalControlSlider<MyLightingBlock>("Blink Lenght", MySpaceTexts.BlockPropertyTitle_LightBlinkLenght, MySpaceTexts.BlockPropertyDescription_LightBlinkLenght);
            lightBlinkLenght.SetLimits((x) => x.BlinkLenghtBounds.Min, (x) => x.BlinkLenghtBounds.Max);
            lightBlinkLenght.DefaultValueGetter = (x) => x.BlinkLenghtBounds.Default;
            lightBlinkLenght.Getter = (x) => x.BlinkLength;
            lightBlinkLenght.Setter = (x, v) => x.SyncObject.SendChangeLightBlinkLengthRequest(v);
            lightBlinkLenght.Writer = (x, result) => result.Append(MyValueFormatter.GetFormatedFloat(x.BlinkLength, NUM_DECIMALS)).Append(" %");
            lightBlinkLenght.EnableActions();
            MyTerminalControlFactory.AddControl(lightBlinkLenght);

            var ligthBlinkOffset = new MyTerminalControlSlider<MyLightingBlock>("Blink Offset", MySpaceTexts.BlockPropertyTitle_LightBlinkOffset, MySpaceTexts.BlockPropertyDescription_LightBlinkOffset);
            ligthBlinkOffset.SetLimits((x) => x.BlinkOffsetBounds.Min, (x) => x.BlinkOffsetBounds.Max);
            ligthBlinkOffset.DefaultValueGetter = (x) => x.BlinkOffsetBounds.Default;
            ligthBlinkOffset.Getter = (x) => x.BlinkOffset;
            ligthBlinkOffset.Setter = (x, v) => x.SyncObject.SendChangeLightBlinkOffsetRequest(v);
            ligthBlinkOffset.Writer = (x, result) => result.Append(MyValueFormatter.GetFormatedFloat(x.BlinkOffset, NUM_DECIMALS)).Append(" %");
            ligthBlinkOffset.EnableActions();
            MyTerminalControlFactory.AddControl(ligthBlinkOffset);
        }

        public Color Color
        {
            get { return m_light.Color; }
            set
            {
                if (m_light.Color != value)
                {
                    m_light.SpecularColor = value;
                    m_light.Color = value;
                    m_light.ReflectorColor = value;
					Render.BulbColor = ComputeBulbColor();
                    UpdateEmissivity(true);
                    RaisePropertiesChanged();
                }
            }
        }

        public float Radius
        {
            get { return m_light.Range; }
            set
            {
                if (m_light.Range != value)
                {
                    m_light.Range = value;
                    RaisePropertiesChanged();
                }
            }
        }

        public float BlinkLength
        {
            get { return m_blinkLength; }
            set
            {
                if (m_blinkLength != value)
                {
                    m_blinkLength = (float)Math.Round(value, NUM_DECIMALS);
                    RaisePropertiesChanged();
                }
            }
        }

        public float BlinkOffset
        {
            get { return m_blinkOffset; }
            set
            {
                if (m_blinkOffset != value)
                {
                    m_blinkOffset = (float)Math.Round(value, NUM_DECIMALS);
                    RaisePropertiesChanged();
                }
            }
        }

        public float BlinkIntervalSeconds
        {
            get { return m_blinkIntervalSeconds; }
            set
            {
                if (m_blinkIntervalSeconds != value)
                {
                    if (value > m_blinkIntervalSeconds)
                        m_blinkIntervalSeconds = (float)Math.Round(value + 0.04999f, NUM_DECIMALS);
                    else
                        m_blinkIntervalSeconds = (float)Math.Round(value - 0.04999f, NUM_DECIMALS);
                    if (m_blinkIntervalSeconds == 0.0f && Enabled)
                    {
                        m_light.ReflectorOn = true;
                        m_light.GlareOn = true;
                        m_light.LightOn = true;
                    }
                    RaisePropertiesChanged();
                }
            }
        }

        public float Falloff
        {
            get { return m_light.Falloff; }
            set
            {
                if (m_light.Falloff != value)
                {
                    m_light.Falloff = value;
                    RaisePropertiesChanged();
                }
            }
        }

        public float Intensity
        {
            get { return m_intesity; }
            set
            {
                if (m_intesity != value)
                {
                    m_intesity = value;
                    UpdateIntensity();
                    RaisePropertiesChanged();
                }
            }
        }
        #endregion

        #region Construction and serialization

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);
            this.IsLargeLight = cubeGrid.GridSizeEnum == MyCubeSize.Large;
            this.SyncObject = new MySyncLightingBlock(this);

            var builder = (MyObjectBuilder_LightingBlock)objectBuilder;
            MyModel lightModel = MyModels.GetModelOnlyDummies(BlockDefinition.Model);
            foreach (var pair in lightModel.Dummies)
            {
                if (!pair.Key.ToLower().Contains("light"))
                    continue;

                m_lightLocalPosition = pair.Value.Matrix.Translation;
                break;
            }

            Vector4 color = (builder.ColorAlpha == -1)
                ? LightColorDef
                : new Vector4(builder.ColorRed, builder.ColorGreen, builder.ColorBlue, builder.ColorAlpha);

            float radius = RadiusBounds.Clamp((builder.Radius == -1f) ? RadiusBounds.Default : builder.Radius);
            float falloff = FalloffBounds.Clamp(builder.Falloff);

            m_blinkIntervalSeconds = BlinkIntervalSecondsBounds.Clamp((builder.BlinkIntervalSeconds == -1f) ? BlinkIntervalSecondsBounds.Default : builder.BlinkIntervalSeconds);

            m_blinkLength = BlinkLenghtBounds.Clamp((builder.BlinkLenght == -1f) ? BlinkLenghtBounds.Default : builder.BlinkLenght);

            m_blinkOffset = BlinkOffsetBounds.Clamp((builder.BlinkOffset == -1f) ? BlinkOffsetBounds.Default : builder.BlinkOffset);

            m_intesity = IntensityBounds.Clamp((builder.Intensity == -1f) ? IntensityBounds.Default : builder.Intensity);


            m_positionDirty = true;
            m_light = MyLights.AddLight();
            InitLight(m_light, color, radius, falloff);
            
            m_light.ReflectorRange = ShortReflectorRangeDef;
            m_light.Range = radius;
            m_light.PointLightOffset = 0.5f;
            m_light.ReflectorOn = false;
            m_light.LightOn = false;
            m_light.GlareOn = false;
            
            UpdateIntensity();
            UpdateLightPosition();

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            Render.NeedsDrawFromParent = true;

			var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                BlockDefinition.ResourceSinkGroup, 
                BlockDefinition.RequiredPowerInput,
                () => (Enabled && IsFunctional) ? ResourceSink.MaxRequiredInput : 0f);
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
	        ResourceSink = sinkComp;
			AddDebugRenderComponent(new MyDebugRenderComponentDrawPowerReciever(ResourceSink, this));
			ResourceSink.Update();
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }
        protected abstract void InitLight(MyLight light, Vector4 color, float radius, float falloff);

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_LightingBlock)base.GetObjectBuilderCubeBlock(copy);
            var colV4 = m_light.Color.ToVector4();
            builder.ColorRed = colV4.X;
            builder.ColorGreen = colV4.Y;
            builder.ColorBlue = colV4.Z;
            builder.ColorAlpha = colV4.W;
            builder.Radius = m_light.Range;
            builder.Falloff = m_light.Falloff;
            builder.Intensity = m_intesity;
            builder.BlinkIntervalSeconds = m_blinkIntervalSeconds;
            builder.BlinkLenght = m_blinkLength;
            builder.BlinkOffset = m_blinkOffset;
            return builder;
        }

        protected override void Closing()
        {
            MyLights.RemoveLight(m_light);

            base.Closing();
        }

        public MyLightingBlock()
        {
            this.Render = new MyRenderComponentLight();
        }
        #endregion

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            m_light.ParentID = Render.GetRenderObjectID();
        }

        //lights wont update at all when further any axis
        const int MaxLightUpdateDistance = 5000;
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if ((MySector.MainCamera.Position - PositionComp.GetPosition()).AbsMax() > MaxLightUpdateDistance)
                return;

            float newLightPower = MathHelper.Clamp(Render.CurrentLightPower + (IsWorking ? 1 : -1) * m_lightTurningOnSpeed, 0, 1);

            if (newLightPower != Render.CurrentLightPower)
            {
                Render.CurrentLightPower = newLightPower;

                bool on = newLightPower > 0;
                m_light.ReflectorOn = on;
                m_light.LightOn = on;
                m_light.GlareOn = on;

                UpdateIntensity();
            }
            UpdateLightBlink();
            UpdateLightPosition();
            UpdateLightProperties();
            UpdateEmissivity(false);
        }

        private void UpdateIntensity()
        {
            ProfilerShort.Begin("UpdateIntensity");
            var intensity = Render.CurrentLightPower * Intensity;
            var reflIntensity = Render.CurrentLightPower * ReflectorIntensityDef;
            m_light.ReflectorIntensity = reflIntensity;
            m_light.Intensity = intensity;
            m_light.GlareIntensity = intensity;
            Render.BulbColor = ComputeBulbColor();
            ProfilerShort.End();
        }

        private void UpdateLightBlink()
        {
            if (m_blinkIntervalSeconds > 0.00099f) // At least 1ms
            {
                ProfilerShort.Begin("UpdateLightBlink");

                ulong blinkIntervalMiliseconds = (ulong)(m_blinkIntervalSeconds * 1000.0f);

                const float FROM_PERCENT = 1 / 100.0f;
                float blinkOffsetMiliseconds = blinkIntervalMiliseconds * m_blinkOffset * FROM_PERCENT;

                ulong elapsedTimeMilisecond = (ulong)(MySession.Static.ElapsedGameTime.TotalMilliseconds - blinkOffsetMiliseconds);

                ulong blinkProgressMilisecond = elapsedTimeMilisecond % blinkIntervalMiliseconds;
                ulong blinkLenghtMilisecond = (ulong)(blinkIntervalMiliseconds * m_blinkLength * FROM_PERCENT);

                if (blinkLenghtMilisecond > blinkProgressMilisecond)
                {
                    m_light.ReflectorOn = true;
                    m_light.GlareOn = true;
                    m_light.LightOn = true;
                }
                else
                {
                    m_light.ReflectorOn = false;
                    m_light.GlareOn = false;
                    m_light.LightOn = false;
                }

                ProfilerShort.End();
            }
        }

        protected virtual void UpdateEmissivity(bool force=false)
        {
        }

        protected override void OnEnabledChanged()
        {
            ResourceSink.Update();
            base.OnEnabledChanged();
        }

        private Color ComputeBulbColor()
        {
            if (IsWorking)
            {
                float i = IntensityBounds.Normalize(Intensity);
                float v = 0.125f + i * 0.25f;
                return new Color(Color.R * 0.5f + v, Color.G * 0.5f + v, Color.B * 0.5f + v);
            }
            else
            {
                return Color.DarkGray;
            }
        }

        private void UpdateLightProperties()
        {
            if (m_light == null)
                return;

            ProfilerShort.Begin("UpdateLightProperties");
            m_light.UpdateLight();
            ProfilerShort.End();
        }

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);
            if (m_light != null)
                m_light.MarkPropertiesDirty();
        }

        private void UpdateLightPosition()
        {
            if (m_light == null || !m_positionDirty)
                return;

            ProfilerShort.Begin("UpdateLightPosition");

            var newPos = PositionComp.GetPosition() + Vector3.TransformNormal(m_lightLocalPosition, WorldMatrix);
            
            if (Vector3D.DistanceSquared(m_lightWorldPosition, newPos) > 0.0001)
            {
                m_lightWorldPosition = newPos;
                m_light.MarkPositionDirty();
            }

            m_light.ParentID = Render.GetRenderObjectID();

            MatrixD toLocal = PositionComp.WorldMatrixNormalizedInv;
            m_light.Position = Vector3D.Transform(m_lightWorldPosition, toLocal);
            m_light.ReflectorDirection = Vector3D.TransformNormal(WorldMatrix.Forward, toLocal);
            m_light.ReflectorUp = Vector3D.TransformNormal(WorldMatrix.Up, toLocal);
            m_light.MarkPropertiesDirty();
            m_positionDirty = false;

            ProfilerShort.End();
        }


        private void ComponentStack_IsFunctionalChanged()
        {
            ResourceSink.Update();
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        public override void OnCubeGridChanged(MyCubeGrid oldGrid)
        {
            base.OnCubeGridChanged(oldGrid);
            m_positionDirty = true;
        }

        float IMyLightingBlock.Radius { get { return Radius;} }
        float IMyLightingBlock.Intensity { get { return Intensity; } }
        float IMyLightingBlock.BlinkIntervalSeconds { get { return BlinkIntervalSeconds; } }
        float IMyLightingBlock.BlinkLenght { get { return BlinkLength;} }
        float IMyLightingBlock.BlinkOffset { get {return BlinkOffset;} }
    }
}
