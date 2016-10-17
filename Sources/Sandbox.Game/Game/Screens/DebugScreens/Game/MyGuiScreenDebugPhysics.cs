using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;

namespace Sandbox.Game.Screens.DebugScreens
{

#if !XB1

    [MyDebugScreen("VRage", "Physics")]
    public class MyGuiScreenDebugPhysics : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugPhysics()
        {
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugPhysics";
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            BackgroundColor = new Vector4(1f, 1f, 1f, 0.5f);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.13f);

            AddCaption("Physics", Color.Yellow.ToVector4());
            AddShareFocusHint();

            AddCaption("DebugDraw");
            AddCheckBox("Shapes", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_SHAPES));
            AddCheckBox("Inertia tensors", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_INERTIA_TENSORS));
            AddCheckBox("Clusters", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_CLUSTERS));
            AddCheckBox("Forces", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_FORCES));
            AddCheckBox("Friction", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_FRICTION));
            AddCheckBox("Constraints", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CONSTRAINTS));
            AddSubcaption("Physics options");//,Color.Yellow, 0.7f);
            AddCheckBox("Enable Welding", null, MemberHelper.GetMember(() => MyFakes.WELD_LANDING_GEARS));
            AddCheckBox("Weld pistons", null, MemberHelper.GetMember(() => MyFakes.WELD_PISTONS));
            var box = AddCheckBox("Wheel softness", null, MemberHelper.GetMember(() => MyFakes.WHEEL_SOFTNESS));
            box.SetToolTip("Needs to be true at world load.");
            AddSlider("Softness ratio", 0, 1, () => MyPhysicsConfig.WheelSoftnessRatio, (v) => MyPhysicsConfig.WheelSoftnessRatio = v);
            AddSlider("Max velocity", 0, 100, () => MyPhysicsConfig.WheelSoftnessVelocity, (v) => MyPhysicsConfig.WheelSoftnessVelocity = v);
            AddCheckBox("Suspension power ratio", null, MemberHelper.GetMember(() => MyFakes.SUSPENSION_POWER_RATIO));
        }
    }

#endif
}
