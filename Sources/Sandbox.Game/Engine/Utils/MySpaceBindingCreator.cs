using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Input;
using VRage.Utils;

namespace Sandbox.Engine.Utils
{
    public static class MySpaceBindingCreator
    {
        public static readonly MyStringId CX_BASE       = MyControllerHelper.CX_BASE;
        public static readonly MyStringId CX_GUI        = MyControllerHelper.CX_GUI;
        public static readonly MyStringId CX_CHARACTER  = MyControllerHelper.CX_CHARACTER;
        public static readonly MyStringId CX_SPACESHIP  = MyStringId.GetOrCompute("SPACESHIP");
        public static readonly MyStringId CX_BUILD_MODE = MyStringId.GetOrCompute("BUILD_MODE");
        public static readonly MyStringId CX_VOXEL = MyStringId.GetOrCompute("VOXEL");

        public static void CreateBinding()
        {
            // XBOX CONTROLLER
            CreateForBase();
            CreateForGUI();
            CreateForCharacter();
            CreateForSpaceship();
            CreateForBuildMode();
            CreateForVoxelHands();
        }

        private static void CreateForBase()
        {
            MyControllerHelper.AddContext(CX_BASE);
            MyControllerHelper.AddControl(CX_BASE, MyControlsSpace.CONTROL_MENU, MyJoystickButtonsEnum.J07);
            MyControllerHelper.AddControl(CX_BASE, MyControlsGUI.MAIN_MENU, MyJoystickButtonsEnum.J08);
        }

        private static void CreateForGUI()
        {
            MyControllerHelper.AddContext(CX_GUI, CX_BASE);
            MyControllerHelper.AddControl(CX_GUI, MyControlsGUI.ACCEPT,    MyJoystickButtonsEnum.J01);
            MyControllerHelper.AddControl(CX_GUI, MyControlsGUI.CANCEL,    MyJoystickButtonsEnum.J02);
            MyControllerHelper.AddControl(CX_GUI, MyControlsGUI.MOVE_UP, MyJoystickButtonsEnum.JDUp);
            MyControllerHelper.AddControl(CX_GUI, MyControlsGUI.MOVE_DOWN, MyJoystickButtonsEnum.JDDown);
            MyControllerHelper.AddControl(CX_GUI, MyControlsGUI.MOVE_LEFT, MyJoystickButtonsEnum.JDLeft);
            MyControllerHelper.AddControl(CX_GUI, MyControlsGUI.MOVE_RIGHT, MyJoystickButtonsEnum.JDRight);
        }

        private static void CreateForCharacter()
        {
            MyControllerHelper.AddContext(CX_CHARACTER, CX_BASE);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.FORWARD,               MyJoystickAxesEnum.Yneg);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.BACKWARD,              MyJoystickAxesEnum.Ypos);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.STRAFE_LEFT,           MyJoystickAxesEnum.Xneg);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.STRAFE_RIGHT,          MyJoystickAxesEnum.Xpos);
#if !XB1
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.PRIMARY_TOOL_ACTION, MyJoystickAxesEnum.Zneg);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.SECONDARY_TOOL_ACTION, MyJoystickAxesEnum.Zpos);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.PRIMARY_BUILD_ACTION, MyJoystickAxesEnum.Zneg); // MW:TODO shouldn't be this way I think
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.SECONDARY_BUILD_ACTION, MyJoystickAxesEnum.Zpos); // this too
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.COPY_PASTE_ACTION, MyJoystickAxesEnum.Zneg); // this too
#else
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.PRIMARY_TOOL_ACTION,    MyJoystickButtonsEnum.J12);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.SECONDARY_TOOL_ACTION,  MyJoystickButtonsEnum.J11);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.PRIMARY_BUILD_ACTION,   MyJoystickButtonsEnum.J12); // MW:TODO shouldn't be this way I think
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.SECONDARY_BUILD_ACTION, MyJoystickButtonsEnum.J11); // this too
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.COPY_PASTE_ACTION,      MyJoystickButtonsEnum.J12); // this too
#endif
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.ROTATION_LEFT,         MyJoystickAxesEnum.RotationXneg);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.ROTATION_RIGHT,        MyJoystickAxesEnum.RotationXpos);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.ROTATION_UP,           MyJoystickAxesEnum.RotationYneg);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.ROTATION_DOWN,         MyJoystickAxesEnum.RotationYpos);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.JUMP,                  MyJoystickButtonsEnum.J01);
            //MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.SLOT0,                 MyJoystickButtonsEnum.J02);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.CROUCH,                MyJoystickButtonsEnum.J02);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.USE,                   MyJoystickButtonsEnum.J03);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.THRUSTS,               MyJoystickButtonsEnum.J04);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.ROLL_LEFT,             MyJoystickButtonsEnum.J05);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.ROLL_RIGHT,            MyJoystickButtonsEnum.J06);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.SPRINT,                MyJoystickButtonsEnum.J08);            
            //MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.CROUCH,                MyJoystickButtonsEnum.J09);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.SPRINT,                MyJoystickButtonsEnum.J09);
            //MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.BUILD_MODE,            MyJoystickButtonsEnum.J10);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.CAMERA_MODE,           MyJoystickButtonsEnum.J10);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.TOOLBAR_UP,            MyJoystickButtonsEnum.JDUp);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.TOOLBAR_DOWN,          MyJoystickButtonsEnum.JDDown);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.TOOLBAR_NEXT_ITEM,     MyJoystickButtonsEnum.JDRight);
            MyControllerHelper.AddControl(CX_CHARACTER, MyControlsSpace.TOOLBAR_PREV_ITEM,     MyJoystickButtonsEnum.JDLeft);
        }

        private static void CreateForSpaceship()
        {
            MyControllerHelper.AddContext(CX_SPACESHIP, CX_CHARACTER);
            MyControllerHelper.AddControl(CX_SPACESHIP, MyControlsSpace.LANDING_GEAR, MyJoystickButtonsEnum.J02);
            MyControllerHelper.AddControl(CX_SPACESHIP, MyControlsSpace.TOGGLE_REACTORS, MyJoystickButtonsEnum.J04);
            MyControllerHelper.NullControl(CX_SPACESHIP, MyControlsSpace.PRIMARY_BUILD_ACTION);
            MyControllerHelper.NullControl(CX_SPACESHIP, MyControlsSpace.SECONDARY_BUILD_ACTION);
        }

        private static void CreateForBuildMode()
        {
            MyControllerHelper.AddContext(CX_BUILD_MODE, CX_CHARACTER);
            MyControllerHelper.AddControl(CX_BUILD_MODE, MyControlsSpace.CUBE_COLOR_CHANGE, MyJoystickButtonsEnum.J01);
            MyControllerHelper.AddControl(CX_BUILD_MODE, MyControlsSpace.USE_SYMMETRY, MyJoystickButtonsEnum.J03);
            MyControllerHelper.AddControl(CX_BUILD_MODE, MyControlsSpace.SYMMETRY_SWITCH, MyJoystickButtonsEnum.J04);
            MyControllerHelper.AddControl(CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE, MyJoystickButtonsEnum.J05);
            MyControllerHelper.AddControl(CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE, MyJoystickButtonsEnum.J06);
            MyControllerHelper.AddControl(CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE, MyJoystickAxesEnum.Xneg);
            MyControllerHelper.AddControl(CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE, MyJoystickAxesEnum.Xpos);
            MyControllerHelper.AddControl(CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE, MyJoystickAxesEnum.Yneg);
            MyControllerHelper.AddControl(CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE, MyJoystickAxesEnum.Ypos);
        }

        private static void CreateForVoxelHands()
        {
            MyControllerHelper.AddContext(CX_VOXEL, CX_CHARACTER);
            MyControllerHelper.AddControl(CX_VOXEL, MyControlsSpace.VOXEL_PAINT, MyJoystickButtonsEnum.J01);
            MyControllerHelper.AddControl(CX_VOXEL, MyControlsSpace.SWITCH_LEFT, MyJoystickButtonsEnum.J03);
            MyControllerHelper.AddControl(CX_VOXEL, MyControlsSpace.VOXEL_HAND_SETTINGS, MyJoystickButtonsEnum.J04);
            MyControllerHelper.AddControl(CX_VOXEL, MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE, MyJoystickButtonsEnum.J05);
            MyControllerHelper.AddControl(CX_VOXEL, MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE, MyJoystickButtonsEnum.J06);
            MyControllerHelper.NullControl(CX_VOXEL, MyControlsSpace.CROUCH);
            MyControllerHelper.NullControl(CX_VOXEL, MyControlsSpace.PRIMARY_BUILD_ACTION);
            MyControllerHelper.NullControl(CX_VOXEL, MyControlsSpace.SECONDARY_BUILD_ACTION);
            MyControllerHelper.AddControl(CX_VOXEL, MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE, MyJoystickAxesEnum.Xneg);
            MyControllerHelper.AddControl(CX_VOXEL, MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE, MyJoystickAxesEnum.Xpos);
            MyControllerHelper.AddControl(CX_VOXEL, MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE, MyJoystickAxesEnum.Yneg);
            MyControllerHelper.AddControl(CX_VOXEL, MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE, MyJoystickAxesEnum.Ypos);
        }
    }
}
