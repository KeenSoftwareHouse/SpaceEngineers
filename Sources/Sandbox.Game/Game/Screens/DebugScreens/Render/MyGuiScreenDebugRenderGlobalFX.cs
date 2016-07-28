using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using VRage;


using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics.Render;
using VRageRender;

namespace Sandbox.Game.Gui
{
#if !XB1

    [MyDebugScreen("Render", "Global FX")]
    class MyGuiScreenDebugRenderGlobalFX : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderGlobalFX()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = .9f;

            AddCaption("Render Global FX", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f * m_scale;

            bool rendererIsDirectX11 = MySandboxGame.Config.GraphicsRenderer.ToString().Equals("DirectX 11");
            if (!rendererIsDirectX11)
            {
                AddLabel("FXAA (DX9)", Color.Yellow.ToVector4(), 1.2f);

                AddCheckBox("Enable FXAA", null, MemberHelper.GetMember(() => MyPostProcessAntiAlias.Enabled));
            }

            m_currentPosition.Y += 0.01f * m_scale;
            AddLabel("Fog", Color.Yellow.ToVector4(), 1.2f);

            var fogObj = MySector.FogProperties;
            if (MySector.MainCamera != null)
            {
                if (!rendererIsDirectX11)
                {
                    AddCheckBox("Enable fog", MySector.FogProperties, MemberHelper.GetMember(() => MySector.FogProperties.EnableFog));
                    AddSlider("Fog near distance", 1.0f, MySector.MainCamera.FarPlaneDistance, fogObj, MemberHelper.GetMember(() => MySector.FogProperties.FogNear));
                    AddSlider("Fog far distance", 1.0f, MySector.MainCamera.FarPlaneDistance, fogObj, MemberHelper.GetMember(() => MySector.FogProperties.FogFar));
                }
                AddSlider("Fog multiplier", 0.0f, 0.5f, fogObj, MemberHelper.GetMember(() => MySector.FogProperties.FogMultiplier));
                AddSlider("Fog backlight multiplier", 0.0f, 5.0f, fogObj, MemberHelper.GetMember(() => MySector.FogProperties.FogBacklightMultiplier));
                if (rendererIsDirectX11) 
                    AddSlider("Fog density", 0.0f, 0.2f, fogObj, MemberHelper.GetMember(() => MySector.FogProperties.FogDensity));
                AddColor(new StringBuilder("Fog color"), fogObj, MemberHelper.GetMember(() => MySector.FogProperties.FogColor));
            }

        }

        private bool nebula_selector(VRageRender.MyImpostorProperties properties)
        {
            return properties.ImpostorType == VRageRender.MyImpostorType.Nebula;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderGlobalFX";
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);

            VRageRender.MyRenderProxy.UpdateAntiAliasSettings(
                MyPostProcessAntiAlias.Enabled
                );

            VRageRender.MyRenderProxy.UpdateFogSettings(
               MySector.FogProperties.EnableFog, 
               MySector.FogProperties.FogNear,
               MySector.FogProperties.FogFar,
               MySector.FogProperties.FogMultiplier,
               MySector.FogProperties.FogBacklightMultiplier,
               MySector.FogProperties.FogColor);
        }
    }

#endif
}
