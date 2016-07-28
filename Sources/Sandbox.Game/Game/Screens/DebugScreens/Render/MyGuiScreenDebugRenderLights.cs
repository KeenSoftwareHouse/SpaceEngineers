using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;
using VRageRender;
using System;
using VRage.Utils;

namespace Sandbox.Game.Gui
{
#if !XB1
    [MyDebugScreen("Render", "Lights settings")]
    class MyGuiScreenDebugRenderLights : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderLights()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Render Lights debug", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;
            AddLabel("Lights", Color.Yellow.ToVector4(), 1.2f);

            if (MySandboxGame.Config.GraphicsRenderer == MyStringId.GetOrCompute("DirectX 9"))
            {
                AddCheckBox("Enable lights", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableLightsRuntime));
                AddCheckBox("Enable point lights", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnablePointLights));
                AddCheckBox("Enable spot lights", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableSpotLights));
                AddCheckBox("Enable light glares", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableLightGlares));


                AddCheckBox("Enable spot shadows", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableSpotShadows));
                //AddCheckBox(new StringBuilder("Enable spectator light"), null, MemberHelper.GetMember(() => MyRender.EnableSpectatorReflector));
                AddCheckBox("Only specular intensity", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowSpecularIntensity));
                AddCheckBox("Only specular power", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowSpecularPower));
                AddCheckBox("Only emissivity", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowEmissivity));
                AddCheckBox("Only reflectivity", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowReflectivity));


                m_currentPosition.Y += 0.01f;
                AddLabel("Sun", Color.Yellow.ToVector4(), 1.2f);
                AddCheckBox("Enable sun", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableSun));
                AddCheckBox("Enable shadows", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableShadows));
                AddCheckBox("Enable asteroid shadows", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableAsteroidShadows));
                AddCheckBox("Enable ambient map", () => MyRenderProxy.Settings.EnableEnvironmentMapAmbient, (x) => MyRenderProxy.Settings.EnableEnvironmentMapAmbient = x);
                AddCheckBox("Enable reflection map", () => MyRenderProxy.Settings.EnableEnvironmentMapReflection, (x) => MyRenderProxy.Settings.EnableEnvironmentMapReflection = x);
            }
            AddCheckBox("Enable voxel ambient", null, MemberHelper.GetMember(() => MyRenderSettings.EnableVoxelAo));
            AddSlider("Voxel AO min", 0f, 1f, null, MemberHelper.GetMember(() => MyRenderSettings.VoxelAoMin));
            AddSlider("Voxel AO max", 0f, 1f, null, MemberHelper.GetMember(() => MyRenderSettings.VoxelAoMax));
            AddSlider("Voxel AO offset", -2f, 2f, null, MemberHelper.GetMember(() => MyRenderSettings.VoxelAoOffset));
            m_currentPosition.Y += 0.01f;

            AddSlider("Sun Intensity", 0, 10.0f, MySector.SunProperties, MemberHelper.GetMember(() => MySector.SunProperties.SunIntensity));
            m_currentPosition += 0.01f;
            AddLabel("Additional directional lights", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Draw light source directions", () => MyDebugDrawSettings.DEBUG_DRAW_ADDITIONAL_ENVIRONMENTAL_LIGHTS, (x) => MyDebugDrawSettings.DEBUG_DRAW_ADDITIONAL_ENVIRONMENTAL_LIGHTS = x);

            float maxIntensity = 8f;
            AddSlider("Additional Sun Intensity", 0f, maxIntensity, () => MySector.SunProperties.AdditionalSunIntensity[0], (float v) => MySector.SunProperties.AdditionalSunIntensity[0] = v);
            
            for (int lightIndex = 0; lightIndex < MySector.SunProperties.AdditionalSunDirection.Length; ++lightIndex)
            {
                int capturedIndex = lightIndex;
                AddSlider("Additional Sun " + (capturedIndex + 1).ToString() + " Phi", 0, 360.0f, () => MySector.SunProperties.AdditionalSunDirection[capturedIndex][0] * 180f / (float)Math.PI, (float v) => MySector.SunProperties.AdditionalSunDirection[capturedIndex][0] = v * (float)Math.PI / 180f);
                AddSlider("Additional Sun " + (capturedIndex + 1).ToString() + " Theta", 0, 180, () => MySector.SunProperties.AdditionalSunDirection[capturedIndex][1] * 180f / (float)Math.PI, (float v) => MySector.SunProperties.AdditionalSunDirection[capturedIndex][1] = v * (float)Math.PI / 180f);
            }
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderLights";
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);
        }

    }
#endif
}
