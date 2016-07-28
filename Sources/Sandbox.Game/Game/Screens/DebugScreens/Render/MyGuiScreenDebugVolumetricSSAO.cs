using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using VRage;
using Sandbox.Graphics.Render;

namespace Sandbox.Game.Gui
{
#if !XB1
    [MyDebugScreen("Render", "SSAO")]
    class MyGuiScreenDebugVolumetricSSAO : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugVolumetricSSAO()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption("Volumetric SSAO Debug", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            AddCheckBox("Use SSAO", null, MemberHelper.GetMember(() => MyPostProcessVolumetricSSAO2.Enabled));
            AddCheckBox("Use blur", null, MemberHelper.GetMember(() => MyPostProcessVolumetricSSAO2.UseBlur));
            AddCheckBox("Show only SSAO", null, MemberHelper.GetMember(() => MyPostProcessVolumetricSSAO2.ShowOnlySSAO));

            m_currentPosition.Y += 0.01f;

            AddSlider("MinRadius", 0, 10, null, MemberHelper.GetMember(() => MyPostProcessVolumetricSSAO2.MinRadius));
            AddSlider("MaxRadius", 0, 1000, null, MemberHelper.GetMember(() => MyPostProcessVolumetricSSAO2.MaxRadius));
            AddSlider("RadiusGrowZScale", 0, 10, null, MemberHelper.GetMember(() => MyPostProcessVolumetricSSAO2.RadiusGrowZScale));
            AddSlider("CameraZFarScale", 0.9f, 1.1f, null, MemberHelper.GetMember(() => MyPostProcessVolumetricSSAO2.CameraZFarScale));

            AddSlider("Bias", 0, 10, null, MemberHelper.GetMember(() => MyPostProcessVolumetricSSAO2.Bias));
            AddSlider("Falloff", 0, 10, null, MemberHelper.GetMember(() => MyPostProcessVolumetricSSAO2.Falloff));
            AddSlider("NormValue", 0, 10, null, MemberHelper.GetMember(() => MyPostProcessVolumetricSSAO2.NormValue));
            AddSlider("Contrast", 0, 10, null, MemberHelper.GetMember(() => MyPostProcessVolumetricSSAO2.Contrast));

            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugSSAO";
        }
    }
#endif
}
