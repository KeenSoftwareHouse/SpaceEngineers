using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities;
using VRage;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Game", "Player Camera Spring")]
    class MyGuiScreenDebugPlayerCameraSpring : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugPlayerCameraSpring()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption("Player Head Shake", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_scale = 0.7f;

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;
              
            var cockpit = MySession.Static.ControlledEntity as MyCockpit;

            if (cockpit != null)
            {
                AddSlider("MaxVelocity", 0, 1, cockpit.CameraSpring, MemberHelper.GetMember(() => cockpit.CameraSpring.MaxVelocity));
                AddSlider("MaxAccel", 0, 80, cockpit.CameraSpring, MemberHelper.GetMember(() => cockpit.CameraSpring.MaxAccel));
                AddSlider("MaxDistanceSpeed", 0, 500, cockpit.CameraSpring, MemberHelper.GetMember(() => cockpit.CameraSpring.MaxDistanceSpeed));
                AddSlider("linearVelocityDumping", 0, 1, cockpit.CameraSpring, MemberHelper.GetMember(() => cockpit.CameraSpring.LinearVelocityDumping));
                AddSlider("localTranslationDumping", 0, 1, cockpit.CameraSpring, MemberHelper.GetMember(() => cockpit.CameraSpring.LocalTranslationDumping));
            }   

            m_currentPosition.Y += 0.01f;

            if (MyThirdPersonSpectator.Static != null)
            {
                AddLabel("Third person spectator", Color.Yellow.ToVector4(), 1);

                m_currentPosition.Y += 0.01f;

                AddLabel("Normal spring", Color.Yellow.ToVector4(), 0.7f);
                AddSlider("Stiffness", 1, 50000, MyThirdPersonSpectator.Static.NormalSpring, MemberHelper.GetMember(() => MyThirdPersonSpectator.Static.NormalSpring.Stiffness));
                AddSlider("Damping", 1, 5000, MyThirdPersonSpectator.Static.NormalSpring, MemberHelper.GetMember(() => MyThirdPersonSpectator.Static.NormalSpring.Damping));
                AddSlider("Mass", 0.1f, 500, MyThirdPersonSpectator.Static.NormalSpring, MemberHelper.GetMember(() => MyThirdPersonSpectator.Static.NormalSpring.Mass));

                AddLabel("Strafing spring", Color.Yellow.ToVector4(), 0.7f);
                AddSlider("Stiffness", 1, 50000, MyThirdPersonSpectator.Static.StrafingSpring, MemberHelper.GetMember(() => MyThirdPersonSpectator.Static.StrafingSpring.Stiffness));
                AddSlider("Damping", 1, 5000, MyThirdPersonSpectator.Static.StrafingSpring, MemberHelper.GetMember(() => MyThirdPersonSpectator.Static.StrafingSpring.Damping));
                AddSlider("Mass", 0.1f, 500, MyThirdPersonSpectator.Static.StrafingSpring, MemberHelper.GetMember(() => MyThirdPersonSpectator.Static.StrafingSpring.Mass));

                AddLabel("Angle spring", Color.Yellow.ToVector4(), 0.7f);
                AddSlider("Stiffness", 1, 50000, MyThirdPersonSpectator.Static.AngleSpring, MemberHelper.GetMember(() => MyThirdPersonSpectator.Static.AngleSpring.Stiffness));
                AddSlider("Damping", 1, 5000, MyThirdPersonSpectator.Static.AngleSpring, MemberHelper.GetMember(() => MyThirdPersonSpectator.Static.AngleSpring.Damping));
                AddSlider("Mass", 0.1f, 500, MyThirdPersonSpectator.Static.AngleSpring, MemberHelper.GetMember(() => MyThirdPersonSpectator.Static.AngleSpring.Mass));
            }
            

        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugPlayerShake";
        }
    }
}
