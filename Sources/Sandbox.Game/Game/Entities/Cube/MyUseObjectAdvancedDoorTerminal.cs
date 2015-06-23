using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.UseObject;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using System.Diagnostics;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRageMath;
using VRage.Generics;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Cube
{
    [MyUseObject("advanceddoor")]
    public class MyUseObjectAdvancedDoorTerminal: IMyUseObject
    {
        public readonly MyAdvancedDoor Door;
        public readonly Matrix LocalMatrix;

        public MyUseObjectAdvancedDoorTerminal(IMyEntity owner, string dummyName, MyModelDummy dummyData, int key)
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

        void IMyUseObject.Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            var user = entity as MyCharacter;
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

        bool IMyUseObject.PlayIndicatorSound
        {
            get { return true; }
        }
    }
}
