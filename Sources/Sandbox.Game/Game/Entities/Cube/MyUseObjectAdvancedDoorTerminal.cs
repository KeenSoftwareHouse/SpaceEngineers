using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
<<<<<<< HEAD
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using System.Diagnostics;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
=======
using Sandbox.Game.Entities.UseObject;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using System.Diagnostics;
using VRage.Import;
using VRage.Input;
>>>>>>> origin/Advanced-Door
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
<<<<<<< HEAD
    public class MyUseObjectAdvancedDoorTerminal : IMyUseObject
=======
    [MyUseObject("terminal")]
    class MyUseObjectAdvancedDoorTerminal: IMyUseObject
>>>>>>> origin/Advanced-Door
    {
        public readonly MyAdvancedDoor Door;
        public readonly Matrix LocalMatrix;

<<<<<<< HEAD
        public MyUseObjectAdvancedDoorTerminal(IMyEntity owner, string dummyName, MyModelDummy dummyData, int key)
=======
        public MyUseObjectAdvancedDoorTerminal(MyCubeBlock owner, string dummyName, MyModelDummy dummyData, int key)
>>>>>>> origin/Advanced-Door
        {
            Door = (MyAdvancedDoor)owner;
            LocalMatrix = dummyData.Matrix;
        }

        float IMyUseObject.InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        MatrixD IMyUseObject.ActivationMatrix
        {
            get { return LocalMatrix * Door.WorldMatrix; }
        }

        MatrixD IMyUseObject.WorldMatrix
        {
            get { return Door.WorldMatrix; }
        }

        int IMyUseObject.RenderObjectID
        {
            get
            {
                return Door.Render.GetRenderObjectID();
            }
        }

        bool IMyUseObject.ShowOverlay
        {
            get { return true; }
        }

        UseActionEnum IMyUseObject.SupportedActions
        {
            get { return UseActionEnum.OpenTerminal | UseActionEnum.Manipulate; }
        }

<<<<<<< HEAD
        void IMyUseObject.Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            var user = entity as MyCharacter;
=======
        void IMyUseObject.Use(UseActionEnum actionEnum, MyCharacter user)
        {
>>>>>>> origin/Advanced-Door
            var relation = Door.GetUserRelationToOwner(user.ControllerInfo.ControllingIdentityId);
            if (relation != Common.MyRelationsBetweenPlayerAndBlock.Owner && relation != Common.MyRelationsBetweenPlayerAndBlock.FactionShare)
            {
                if (user.ControllerInfo.IsLocallyHumanControlled())
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
                }
                return;
            }

            switch (actionEnum)
            {
                case UseActionEnum.Manipulate:
                    Door.SetOpenRequest(!Door.Open, user.ControllerInfo.ControllingIdentityId);
                    break;

                case UseActionEnum.OpenTerminal:
                    MyGuiScreenTerminal.Show(MyTerminalPageEnum.ControlPanel, user, Door);
                    break;
            }
        }

        MyActionDescription IMyUseObject.GetActionInfo(UseActionEnum actionEnum)
        {
            switch(actionEnum)
            {
                case UseActionEnum.Manipulate:
                    return new MyActionDescription()
                    {
                        Text = MySpaceTexts.NotificationHintPressToOpenDoor,
                        FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.USE) },
                        IsTextControlHint = true,
                        JoystickFormatParams = new object[] { MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.USE) },
                    };

                case UseActionEnum.OpenTerminal:
                    return new MyActionDescription()
                    {
                        Text = MySpaceTexts.NotificationHintPressToOpenControlPanel,
                        FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL), Door.DefinitionDisplayNameText },
                        IsTextControlHint = true,
                        JoystickText = MySpaceTexts.NotificationHintJoystickPressToOpenControlPanel,
                        JoystickFormatParams = new object[] { Door.DefinitionDisplayNameText },
                    };

                default:
                    Debug.Fail("Invalid branch reached.");
                    return new MyActionDescription()
                    {
                        Text = MySpaceTexts.NotificationHintPressToOpenControlPanel,
                        FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL), Door.DefinitionDisplayNameText },
                        IsTextControlHint = true
                    };
            }
        }

        bool IMyUseObject.ContinuousUsage
        {
            get { return false; }
        }

        bool IMyUseObject.HandleInput() { return false; }

        void IMyUseObject.OnSelectionLost() { }
<<<<<<< HEAD

        
=======
>>>>>>> origin/Advanced-Door
    }
}
