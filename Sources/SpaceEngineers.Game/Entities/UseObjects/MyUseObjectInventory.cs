
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Common;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;
using VRage.Game;
using VRage.Game.Entity;

namespace SpaceEngineers.Game.Entities.UseObjects
{
    [MyUseObject("inventory")]
    [MyUseObject("conveyor")]
    class MyUseObjectInventory : MyUseObjectBase
    {
        public readonly MyEntity Entity;
        public readonly Matrix LocalMatrix;

        public MyUseObjectInventory(IMyEntity owner, string dummyName, MyModelDummy dummyData, uint key)
            : base(owner, dummyData)
        {
            Entity = owner as MyEntity;
            LocalMatrix = dummyData.Matrix;
        }

        public override float InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        public override MatrixD ActivationMatrix
        {
            get { return LocalMatrix * Entity.WorldMatrix; }
        }

        public override MatrixD WorldMatrix
        {
            get { return Entity.WorldMatrix; }
        }

        public override int RenderObjectID
        {
            get
            {
                return Entity.Render.GetRenderObjectID();
            }
        }

        public override bool ShowOverlay
        {
            get { return true; }
        }

        public override UseActionEnum SupportedActions
        {
            get { return UseActionEnum.OpenInventory | UseActionEnum.OpenTerminal; }
        }

        public override void Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            var user = entity as MyCharacter;
            var block = Entity as MyCubeBlock;

            if (block != null)
            {
                var relation = block.GetUserRelationToOwner(user.ControllerInfo.ControllingIdentityId);
                if (!relation.IsFriendly())
                {
                    if (user.ControllerInfo.IsLocallyHumanControlled())
                    {
                        MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
                    }
                    return;
                }
            }

            switch (actionEnum)
            {
                case UseActionEnum.OpenInventory:
                case UseActionEnum.OpenTerminal:
                    MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, user, Entity);
                    break;
                default:
                    //MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, user, Block);
                    break;
            }
        }

        public override MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            var block = Entity as MyCubeBlock;
            var text = block != null ? block.DefinitionDisplayNameText : Entity.DisplayNameText;
            return new MyActionDescription()
            {
                Text = MySpaceTexts.NotificationHintPressToOpenInventory,
                FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL), text },
                IsTextControlHint = true,
                JoystickText = MySpaceTexts.NotificationHintJoystickPressToOpenControlPanel,
                JoystickFormatParams = new object[] { text },
            };
        }

        public override bool ContinuousUsage
        {
            get { return false; }
        }

        public override bool HandleInput() { return false; }

        public override void OnSelectionLost() { }

        public override bool PlayIndicatorSound
        {
            get { return true; }
        }
    }
}
