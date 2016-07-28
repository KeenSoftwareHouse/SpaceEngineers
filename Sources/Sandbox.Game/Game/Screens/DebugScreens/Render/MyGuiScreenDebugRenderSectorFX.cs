using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;
using Sandbox.Definitions;
using System;

namespace Sandbox.Game.Gui
{
#if !XB1
    [MyDebugScreen("Render", "Sector FX", MyDirectXSupport.DX9)]
    class MyGuiScreenDebugRenderSectorFX : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderSectorFX()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.65f;

            AddCaption("Render sector FX", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f * m_scale;
            AddCheckBox("Enable dust", MySector.ParticleDustProperties, MemberHelper.GetMember(() => MySector.ParticleDustProperties.Enabled));
                                       
            m_currentPosition.Y += 0.01f * m_scale;
            AddLabel("Nebula", Color.Yellow.ToVector4(), 1.2f);

            VRageRender.MyImpostorProperties nebulaObj = new VRageRender.MyImpostorProperties();
            bool found = false;
            foreach (VRageRender.MyImpostorProperties nebulaObjIt in MySector.ImpostorProperties)
            {
                if (nebulaObjIt.ImpostorType == VRageRender.MyImpostorType.Nebula)
                {
                    nebulaObj = nebulaObjIt;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                AddCheckBox("Enable", nebulaObj, MemberHelper.GetMember(() => nebulaObj.Enabled));
                AddColor(new StringBuilder("Color"), nebulaObj, MemberHelper.GetMember(() => nebulaObj.Color));
                AddSlider("Contrast", 0, 20, nebulaObj, MemberHelper.GetMember(() => nebulaObj.Contrast));
                AddSlider("Intensity", 0, 20, nebulaObj, MemberHelper.GetMember(() => nebulaObj.Intensity));
                AddSlider("Radius", 0, 10, nebulaObj, MemberHelper.GetMember(() => nebulaObj.Radius));
                AddSlider("Anim1", -0.1f, 0.1f, nebulaObj, MemberHelper.GetMember(() => nebulaObj.Anim1));
                AddSlider("Anim2", -0.1f, 0.1f, nebulaObj, MemberHelper.GetMember(() => nebulaObj.Anim2));
                AddSlider("Anim3", -0.1f, 0.1f, nebulaObj, MemberHelper.GetMember(() => nebulaObj.Anim3));
            }

                                     
            m_currentPosition.Y += 0.01f * m_scale;
            AddLabel("God rays", Color.Yellow.ToVector4(), 1.2f);

            AddCheckBox("Enable", MySector.GodRaysProperties, MemberHelper.GetMember(() => MySector.GodRaysProperties.Enabled));
            AddSlider("Density", 0, 2, MySector.GodRaysProperties, MemberHelper.GetMember(() => MySector.GodRaysProperties.Density));
            AddSlider("Weight", 0, 2, MySector.GodRaysProperties, MemberHelper.GetMember(() => MySector.GodRaysProperties.Weight));
            AddSlider("Decay", 0, 2, MySector.GodRaysProperties, MemberHelper.GetMember(() => MySector.GodRaysProperties.Decay));
            AddSlider("Exposition", 0, 2, MySector.GodRaysProperties, MemberHelper.GetMember(() => MySector.GodRaysProperties.Exposition));

            m_currentPosition.Y += 0.01f * m_scale;
            AddLabel("Particle dust", Color.Yellow.ToVector4(), 1.2f);

            AddSlider("Dust radius", 0.01f, 200, MySector.ParticleDustProperties , MemberHelper.GetMember(() => MySector.ParticleDustProperties.DustBillboardRadius));
            AddSlider("Dust count in dir half", 0.01f, 20, MySector.ParticleDustProperties , MemberHelper.GetMember(() => MySector.ParticleDustProperties.DustFieldCountInDirectionHalf));
            AddSlider("Distance between", 1f, 500, MySector.ParticleDustProperties , MemberHelper.GetMember(() => MySector.ParticleDustProperties.DistanceBetween));
            AddSlider("Anim speed", 0.0f, 0.1f, MySector.ParticleDustProperties, MemberHelper.GetMember(() => MySector.ParticleDustProperties.AnimSpeed));
            AddColor(new StringBuilder("Color"), MySector.ParticleDustProperties, MemberHelper.GetMember(() => MySector.ParticleDustProperties.Color));

            m_currentPosition.Y += 0.01f * m_scale;
            var env = MySector.EnvironmentDefinition;
            if (false)
            {
                AddSlider("Bg. Yaw",   env.BackgroundOrientation.Yaw,   0f, MathHelper.TwoPi, (s) => { env.BackgroundOrientation.Yaw   = s.Value; });
                AddSlider("Bg. Pitch", env.BackgroundOrientation.Pitch, 0f, MathHelper.TwoPi, (s) => { env.BackgroundOrientation.Pitch = s.Value; });
                AddSlider("Bg. Roll",  env.BackgroundOrientation.Roll,  0f, MathHelper.TwoPi, (s) => { env.BackgroundOrientation.Roll  = s.Value; });
            }

            Vector3.GetAzimuthAndElevation(env != null ? env.SunProperties.SunDirectionNormalized : Vector3.Down, out m_azimuth, out m_elevation);
            AddSlider("Sun Azimuth",   m_azimuth,   -MathHelper.TwoPi,   MathHelper.TwoPi,   (s) => { m_azimuth   = s.Value; });
            AddSlider("Sun Elevation", m_elevation, -MathHelper.PiOver2, MathHelper.PiOver2, (s) => { m_elevation = s.Value; });
            //AddButton(new StringBuilder("Save environment"), (s) => { MyDefinitionManager.Static.SaveEnvironmentDefinition(); });

        }
        static float m_azimuth, m_elevation;

        private bool nebula_selector(VRageRender.MyImpostorProperties properties)
        {
            return properties.ImpostorType == VRageRender.MyImpostorType.Nebula;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderSectorFX";
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);

            VRageRender.MyRenderProxy.UpdateGodRaysSettings(
                MySector.GodRaysProperties.Enabled,
                MySector.GodRaysProperties.Density,
                MySector.GodRaysProperties.Weight,
                MySector.GodRaysProperties.Decay,
                MySector.GodRaysProperties.Exposition,
                false);

            Vector3 dir;
            Vector3.CreateFromAzimuthAndElevation(m_azimuth, m_elevation, out dir);
            MySector.EnvironmentDefinition.SunProperties.SunDirectionNormalized = dir;

            MySector.InitEnvironmentSettings();
        }

    }
#endif
}
