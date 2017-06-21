using VRageMath;
using VRage;
using VRageRender;

namespace Sandbox.Game.Gui
{
#if !XB1_TMP

    [MyDebugScreen("Render", "GBuffer Multipliers")]
    class MyGuiScreenDebugRenderGBufferMultipliers : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderGBufferMultipliers()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("GBuffer Multipliers", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            const float minMultiplier = 0.0f;
            const float maxMultiplier = 4.0f;
            const float minShift = -1.0f;
            const float maxShift = 1.0f;
            AddLabel("Multipliers", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Albedo *", MyRenderProxy.Settings.AlbedoMultiplier, minMultiplier, maxMultiplier, (x) => MyRenderProxy.Settings.AlbedoMultiplier = x.Value);
            AddSlider("Albedo +", MyRenderProxy.Settings.AlbedoShift, minShift, maxShift, (x) => MyRenderProxy.Settings.AlbedoShift = x.Value);
            AddSlider("Metalness *", MyRenderProxy.Settings.MetalnessMultiplier, minMultiplier, maxMultiplier, (x) => MyRenderProxy.Settings.MetalnessMultiplier = x.Value);
            AddSlider("Metalness +", MyRenderProxy.Settings.MetalnessShift, minShift, maxShift, (x) => MyRenderProxy.Settings.MetalnessShift = x.Value);
            AddSlider("Gloss *", MyRenderProxy.Settings.GlossMultiplier, minMultiplier, maxMultiplier, (x) => MyRenderProxy.Settings.GlossMultiplier = x.Value);
            AddSlider("Gloss +", MyRenderProxy.Settings.GlossShift, minShift, maxShift, (x) => MyRenderProxy.Settings.GlossShift = x.Value);
            AddSlider("AO *", MyRenderProxy.Settings.AoMultiplier, minMultiplier, maxMultiplier, (x) => MyRenderProxy.Settings.AoMultiplier = x.Value);
            AddSlider("AO +", MyRenderProxy.Settings.AoShift, minShift, maxShift, (x) => MyRenderProxy.Settings.AoShift = x.Value);
            AddSlider("Emissive *", MyRenderProxy.Settings.EmissiveMultiplier, minMultiplier, maxMultiplier, (x) => MyRenderProxy.Settings.EmissiveMultiplier = x.Value);
            AddSlider("Emissive +", MyRenderProxy.Settings.EmissiveShift, minShift, maxShift, (x) => MyRenderProxy.Settings.EmissiveShift = x.Value);
            AddSlider("Color Mask *", MyRenderProxy.Settings.ColorMaskMultiplier, minMultiplier, maxMultiplier, (x) => MyRenderProxy.Settings.ColorMaskMultiplier = x.Value);
            AddSlider("Color Mask +", MyRenderProxy.Settings.ColorMaskShift, minShift, maxShift, (x) => MyRenderProxy.Settings.ColorMaskShift = x.Value);
            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderGBufferMultipliers";
        }

        protected override void ValueChanged(Graphics.GUI.MyGuiControlBase sender)
        {
            MyRenderProxy.SetSettingsDirty();
        }
    }

#endif
}
