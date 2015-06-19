
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;

namespace SpaceEngineers.Game.Entities.UseObjects
{
    [MyUseObject("inventory")]
    [MyUseObject("conveyor")]
    class MyUseObjectInventory : IMyUseObject
    {
        public readonly MyCubeBlock Block;
        public readonly Matrix LocalMatrix;

        public MyUseObjectInventory(IMyEntity owner, string dummyName, MyModelDummy dummyData, int key)
        {
            Block = owner as MyCubeBlock;
            LocalMatrix = dummyData.Matrix;
        }

        float IMyUseObject.InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        MatrixD IMyUseObject.ActivationMatrix
        {
            get { return LocalMatrix * Block.WorldMatrix; }
        }

        MatrixD IMyUseObject.WorldMatrix
        {
            get { return Block.WorldMatrix; }
        }

        int IMyUseObject.RenderObjectID
        {
            get
            {
                return Block.Render.GetRenderObjectID();
            }
        }

        bool IMyUseObject.ShowOverlay
        {
            get { return true; }
        }

        UseActionEnum IMyUseObject.SupportedActions
        {
            get { return UseActionEnum.OpenInventory | UseActionEnum.OpenTerminal; }
        }

        void IMyUseObject.Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            var user = entity as MyCharacter;
            var relation = Block.GetUserRelationToOwner(user.ControllerInfo.ControllingIdentityId);
            if (relation != Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Owner && relation != Sandbox.Common.MyRelationsBetweenPlayerAndBlock.FactionShare)
            {
                if (user.ControllerInfo.IsLocallyHumanControlled())
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
                }
                return;
            }

            switch (actionEnum)
            {
                case UseActionEnum.OpenInventory:
                case UseActionEnum.OpenTerminal:
                    MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, user, Block);
                    break;
                default:
                    //MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, user, Block);
                    break;
            }
        }

        MyActionDescription IMyUseObject.GetActionInfo(UseActionEnum actionEnum)
        {
            return new MyActionDescription()
            {
                Text = MySpaceTexts.NotificationHintPressToOpenInventory,
                FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL), Block.DefinitionDisplayNameText },
                IsTextControlHint = true,
                JoystickText = MySpaceTexts.NotificationHintJoystickPressToOpenControlPanel,
                JoystickFormatParams = new object[] { Block.DefinitionDisplayNameText },
            };
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
