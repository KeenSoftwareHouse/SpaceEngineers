using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Screens.DebugScreens
{

#if !XB1

    [MyDebugScreen("Render", "Overrides")]
    class MyGuiScreenDebugRenderOverrides : MyGuiScreenDebugBase
    {
        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderLayers";
        }

        public MyGuiScreenDebugRenderOverrides()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            m_currentPosition.Y += 0.01f;

            AddCaption("Overrides", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition.Y += 0.01f;
            AddLabel("Lighting Pass", Color.Yellow.ToVector4(), 1.2f);
            m_lighting = AddCheckBox("Enabled", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Lighting));
            m_sun = AddCheckBox("Sun", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Sun));
            m_backLight = AddCheckBox("Back light", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.BackLight));
            m_pointLights = AddCheckBox("Point lights", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.PointLights));
            m_spotLights = AddCheckBox("Spot lights", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.SpotLights));
            m_envLight = AddCheckBox("Env light", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.EnvLight));
            m_currentPosition.Y += 0.01f;
            AddCheckBox("Shadows", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Shadows));
            AddCheckBox("Fog", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Fog));
            AddCheckBox("Flares", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Flares));

            m_currentPosition.Y += 0.01f;
            AddLabel("Transparent Pass", Color.Yellow.ToVector4(), 1.2f);
            m_transparent = AddCheckBox("Enabled", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Transparent));
            m_oit = AddCheckBox("Order independent", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.OIT));
            m_billboardsDynamic = AddCheckBox("Billboards dynamic", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.BillboardsDynamic));
            m_billboardsStatic = AddCheckBox("Billboards static", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.BillboardsStatic));
            m_gpuParticles = AddCheckBox("GPU Particles", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.GPUParticles));
            m_cloud = AddCheckBox("Cloud", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Clouds));
            m_atmosphere = AddCheckBox("Atmosphere", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Atmosphere));

            m_currentPosition.Y += 0.01f;
            AddLabel("Postprocessing", Color.Yellow.ToVector4(), 1.2f);
            m_postprocess = AddCheckBox("Enabled", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Postprocessing));
            m_ssao = AddCheckBox("SSAO", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.SSAO));
            m_bloom = AddCheckBox("Bloom", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Bloom));
            m_fxaa = AddCheckBox("Fxaa", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Fxaa));
            m_tonemapping = AddCheckBox("Tonemapping", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Tonemapping));

            m_currentPosition.Y += 0.01f;
            //m_pause = AddCheckBox("Enabled", MyRenderProxy.DebugOverrides, MemberHelper.GetMember(() => MyRenderProxy.DebugOverrides.Postprocessing));
        }

        private MyGuiControlCheckbox m_lighting;
        private MyGuiControlCheckbox m_sun;
        private MyGuiControlCheckbox m_backLight;
        private MyGuiControlCheckbox m_pointLights;
        private MyGuiControlCheckbox m_spotLights;
        private MyGuiControlCheckbox m_envLight;

        private MyGuiControlCheckbox m_transparent;
        private MyGuiControlCheckbox m_oit;
        private MyGuiControlCheckbox m_billboardsDynamic;
        private MyGuiControlCheckbox m_billboardsStatic;
        private MyGuiControlCheckbox m_gpuParticles;
        private MyGuiControlCheckbox m_atmosphere;
        private MyGuiControlCheckbox m_cloud;

        private MyGuiControlCheckbox m_postprocess;
        private MyGuiControlCheckbox m_ssao;
        private MyGuiControlCheckbox m_bloom;
        private MyGuiControlCheckbox m_fxaa;
        private MyGuiControlCheckbox m_tonemapping;

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);

            MyRenderProxy.UpdateDebugOverrides();

            m_sun.Enabled = m_lighting.IsChecked;
            m_backLight.Enabled = m_lighting.IsChecked;
            m_pointLights.Enabled = m_lighting.IsChecked;
            m_spotLights.Enabled = m_lighting.IsChecked;
            m_envLight.Enabled = m_lighting.IsChecked;

            m_oit.Enabled = m_transparent.IsChecked;
            m_billboardsDynamic.Enabled = m_transparent.IsChecked;
            m_billboardsStatic.Enabled = m_transparent.IsChecked;
            m_gpuParticles.Enabled = m_transparent.IsChecked;
            m_atmosphere.Enabled = m_transparent.IsChecked;
            m_cloud.Enabled = m_transparent.IsChecked;

            m_ssao.Enabled = m_postprocess.IsChecked;
            m_bloom.Enabled = m_postprocess.IsChecked;
            m_fxaa.Enabled = m_postprocess.IsChecked;
            m_tonemapping.Enabled = m_postprocess.IsChecked;
        }
    }

#endif
}
