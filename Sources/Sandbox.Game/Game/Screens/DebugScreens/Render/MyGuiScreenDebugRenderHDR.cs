﻿using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using VRage;
using Sandbox.Graphics.Render;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Render", "HDR", MyDirectXSupport.DX9)]
    class MyGuiScreenDebugRenderHDR : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderHDR()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Render HDR", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f * m_scale;

            AddLabel("HDR", Color.Yellow.ToVector4(), 1.2f);

            AddCheckBox("Enable HDR and bloom", null, MemberHelper.GetMember(() => MyPostProcessHDR.DebugHDRChecked));

            m_currentPosition.Y += 0.01f * m_scale;

            AddSlider("Exposure", 0, 6.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.Exposure));
            AddSlider("Bloom Threshold", 0, 4.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.Threshold));
            AddSlider("Bloom Intensity", 0, 4.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.BloomIntensity));
            AddSlider("Bloom Intensity for Background", 0, 1.5f, null, MemberHelper.GetMember(() => MyPostProcessHDR.BloomIntensityBackground));
            AddSlider("Vertical Blur Amount", 1.0f, 8.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.VerticalBlurAmount));
            AddSlider("Horizontal Blur Amount", 1.0f, 8.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.HorizontalBlurAmount));
            AddSlider("Number of blur passes (integer)", 1.0f, 8.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.NumberOfBlurPasses));

            m_currentPosition.Y += 0.01f * m_scale;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderHDR";
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);

            VRageRender.MyRenderProxy.UpdateHDRSettings(
                MyPostProcessHDR.DebugHDRChecked,
                MyPostProcessHDR.Exposure,
                MyPostProcessHDR.Threshold,
                MyPostProcessHDR.BloomIntensity,
                MyPostProcessHDR.BloomIntensityBackground,
                MyPostProcessHDR.VerticalBlurAmount,
                MyPostProcessHDR.HorizontalBlurAmount,
                (int)MyPostProcessHDR.NumberOfBlurPasses
                );
        }
    }
}
