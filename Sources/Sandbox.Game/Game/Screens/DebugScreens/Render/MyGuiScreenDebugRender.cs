using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using Sandbox.Game.World;
using VRage;
using System;
using Sandbox.Graphics.Render;
using VRageRender;

namespace Sandbox.Game.Gui
{
#if !XB1
    [MyDebugScreen("Render", "Overall settings", MyDirectXSupport.DX9)]
    class MyGuiScreenDebugRender : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRender()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Render debug", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;
            AddLabel("LODs", Color.Yellow.ToVector4(), 1.2f);

            var profile = MyRenderConstants.RenderQualityProfile; // Obfuscated MemberHelper can't access property, so store it to field

            //AddCheckBox(new StringBuilder("Show LOD screens"), null, MemberHelper.GetMember(() => MyRender.ShowLODScreens));
            AddCheckBox("Show blended screens", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowBlendedScreens));
            AddCheckBox("Show LOD1 red overlay", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowLod1WithRedOverlay));
            AddCheckBox("Show green background", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowGreenBackground));
            AddCheckBox("Show environment screens", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowEnvironmentScreens));
            AddCheckBox("Tearing test", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.TearingTest));
            AddCheckBox("Multimon test", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.MultimonTest));

            AddCheckBox("Camera Interpolation", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableCameraInterpolation));
            AddCheckBox("Object Interpolation", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableObjectInterpolation));
            AddSlider("Interpolation lag", 0.0f, 100.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.InterpolationLagMs));
            AddSlider("Interpolation lag feedback mult", 0.1f, 1.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.LagFeedbackMult));

            if (MySector.MainCamera != null)
            {
                m_currentPosition.Y += 0.01f;
                AddLabel("Textures", Color.Yellow.ToVector4(), 1.2f);

                AddCheckBox("Check diffuse textures", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.CheckDiffuseTextures));
                AddCheckBox("Check normals textures", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.CheckNormalTextures));

                m_currentPosition.Y += 0.01f;
                AddLabel("Clip planes", Color.Yellow.ToVector4(), 1.2f);
                AddSlider("Near clip", 0.05f, 10.0f, MySector.MainCamera, MemberHelper.GetMember(() => MySector.MainCamera.NearPlaneDistance));
                AddSlider("Far clip", 100.0f, 100000.0f, MySector.MainCamera, MemberHelper.GetMember(() => MySector.MainCamera.FarPlaneDistance));

                AddSlider("FOV", MySector.MainCamera.FieldOfViewDegrees, 1.00f, 120.0f, new Action<MyGuiControlSlider>(OnFovSlider));
                //AddSlider("Near FOV", MySector.MainCamera.FieldOfViewDegreesForNearObjects, 1.00f, 120.0f, new Action<MyGuiControlSlider>(OnNearFovSlider));
            }
        }

        void OnFovSlider(MyGuiControlSlider slider)
        {
            MySector.MainCamera.FieldOfViewDegrees = slider.Value;
        }

        //void OnNearFovSlider(MyGuiControlSlider slider)
        //{
        //    MySector.MainCamera.FieldOfViewDegreesForNearObjects = slider.Value;
        //}

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRender";
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);

            MyRenderProxy.UpdateRenderQuality(
                MyRenderConstants.RenderQualityProfile.RenderQuality,
                MyRenderConstants.RenderQualityProfile.EnableCascadeBlending);
        }

    }
#endif
}
