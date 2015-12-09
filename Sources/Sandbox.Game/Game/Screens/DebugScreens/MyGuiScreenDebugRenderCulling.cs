using Sandbox.Graphics.GUI;
using System.Text;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Render", "Culling settings")]
    class MyGuiScreenDebugRenderCulling : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderCulling()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Render debug culling", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            

            //AddSlider("Eye adaptation Tau", 0.0f, 10.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.AdaptationTau));
            //AddSlider("Exposure", -5.0f, 5.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.LuminanceExposure));
            //AddSlider("Contrast", -0.5f, 0.5f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.Contrast));
            //AddSlider("Brightness", -0.5f, 0.5f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.Brightness));
            //AddSlider("MiddleGrey", 0.0f, 1.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.MiddleGrey));
            //AddSlider("Bloom exposure", -5.0f, 5.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.BloomExposure));
            //AddSlider("Bloom magnitude", 0.0f, 10.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.BloomMult));

            //AddSlider("MiddleGreyCurveSharpness", 1.0f, 5.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.MiddleGreyCurveSharpness));
            //AddSlider("MiddleGreyAt0", 0.0f, 0.5f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.MiddleGreyAt0));
            //AddSlider("BlueShiftRapidness", 0.0001f, 0.1f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.BlueShiftRapidness));
            //AddSlider("BlueShiftScale", 0.0f, 1.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.BlueShiftScale));


            //m_currentPosition.Y += 0.01f;
            //AddLabel("Hacks", Color.Yellow.ToVector4(), 1.2f);
            //AddSlider("Environment multiplier", 0.0f, 10.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnvMult));
            //AddSlider("Backlight intensity", 0.0f, 10.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.BacklightMult));

            //m_currentPosition.Y += 0.01f;
            //AddLabel("Fog", Color.Yellow.ToVector4(), 1.2f);
            //AddSlider("Mult", 0.0f, 2.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FogMult));
            //AddSlider("Density", 0.0000001f, 0.05f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FogDensity));
            //AddSlider("Height offset", 0.0f, 1000.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FogYOffset));
            //AddColor(new StringBuilder("Color"), MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FogColor));
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderCulling";
        }

    }
}
