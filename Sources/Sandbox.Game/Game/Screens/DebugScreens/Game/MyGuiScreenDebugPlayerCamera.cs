using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities;
using VRage;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Game", "Player Camera")]
    class MyGuiScreenDebugPlayerCamera : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugPlayerCamera()
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
            
            if (MySector.MainCamera != null)
            {
                var cameraSpring = MySector.MainCamera.CameraSpring;
                AddLabel("Camera target spring", Color.Yellow.ToVector4(), 1);
                AddSlider("Stiffness", 0, 50, () => cameraSpring.SpringStiffness, (s) => { cameraSpring.SpringStiffness = s; });
                AddSlider("Dampening", 0, 1, () => cameraSpring.SpringDampening, (s) => { cameraSpring.SpringDampening = s; });
                AddSlider("CenterMaxVelocity", 0, 10, () => cameraSpring.SpringMaxVelocity, (s) => { cameraSpring.SpringMaxVelocity = s; });
                AddSlider("SpringMaxLength", 0, 2, () => cameraSpring.SpringMaxLength, (s) => { cameraSpring.SpringMaxLength = s; });
            }   

            m_currentPosition.Y += 0.01f;

            if (MyThirdPersonSpectator.Static != null)
            {
                AddLabel("Third person spectator", Color.Yellow.ToVector4(), 1);

                m_currentPosition.Y += 0.01f;
                AddCheckBox("Debug draw", () => MyThirdPersonSpectator.Static.EnableDebugDraw,
                    (s) => MyThirdPersonSpectator.Static.EnableDebugDraw = s);

                AddLabel("Normal spring", Color.Yellow.ToVector4(), 0.7f);
                AddSlider("Stiffness", 1, 50000, () => MyThirdPersonSpectator.Static.NormalSpring.Stiffness,
                    (s) => MyThirdPersonSpectator.Static.NormalSpring.Stiffness = s);
                AddSlider("Damping", 1, 5000, () => MyThirdPersonSpectator.Static.NormalSpring.Dampening,
                    (s) => MyThirdPersonSpectator.Static.NormalSpring.Dampening = s);
                AddSlider("Mass", 0.1f, 500, () => MyThirdPersonSpectator.Static.NormalSpring.Mass,
                    (s) => MyThirdPersonSpectator.Static.NormalSpring.Mass = s);
            }
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugPlayerShake";
        }
    }
}
