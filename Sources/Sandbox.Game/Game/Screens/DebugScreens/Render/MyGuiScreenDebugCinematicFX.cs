using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using VRage;
using VRageMath;
using Sandbox.Graphics.Render;

namespace Sandbox.Game.Gui
{
#if !XB1

    [MyDebugScreen("Render", "Cinematic FX", MyDirectXSupport.DX9)]
    class MyGuiScreenDebugCinematicFX : MyGuiScreenDebugBase
    {
        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugCinematicFX";
        }

        public MyGuiScreenDebugCinematicFX()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.6f;
            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            AddCaption("Cinematic Effects settings", Color.Yellow.ToVector4());
            AddShareFocusHint();


            AddLabel("Post process - Vignetting", Color.Yellow.ToVector4(), 1.2f);
            //m_currentPosition.Y += 0.01f;
            {
                AddCheckBox("Enable", null, MemberHelper.GetMember(() => MyPostProcessVignetting.Enabled));
                AddSlider("Vignetting Power", 0.1f, 10.0f, null, MemberHelper.GetMember(() => MyPostProcessVignetting.VignettingPower));
            }

            AddLabel("Post process - Chromatic Aberration", Color.Yellow.ToVector4(), 1.2f);
            {
                AddCheckBox("Enable", null, MemberHelper.GetMember(() => MyPostProcessChromaticAberration.Enabled));
                AddSlider("Lens Distortion", -3.0f, 0.0f, null, MemberHelper.GetMember(() => MyPostProcessChromaticAberration.DistortionLens));
                AddSlider("Cubic Distortion", -3.0f, 3.0f, null, MemberHelper.GetMember(() => MyPostProcessChromaticAberration.DistortionCubic));
                AddSlider("Red Distortion Weight", 0.0f, 2.0f, null, MemberHelper.GetMember(() => MyPostProcessChromaticAberration.DistortionWeightRed));
                AddSlider("Green Distortion Weight", 0.0f, 2.0f, null, MemberHelper.GetMember(() => MyPostProcessChromaticAberration.DistortionWeightGreen));
                AddSlider("Blue Distortion Weight", 0.0f, 2.0f, null, MemberHelper.GetMember(() => MyPostProcessChromaticAberration.DistortionWeightBlue));
            }

        }

    }

#endif
}
