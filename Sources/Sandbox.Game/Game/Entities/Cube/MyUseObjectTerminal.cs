using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.Common;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;
using VRage.Game;

namespace Sandbox.Game.Entities.Cube
{
    [MyUseObject("terminal")]
    public class MyUseObjectTerminal : MyUseObjectBase
    {
        public readonly MyCubeBlock Block;
        public readonly Matrix LocalMatrix;

        public MyUseObjectTerminal(IMyEntity owner, string dummyName, MyModelDummy dummyData, uint key)
            : base(owner, dummyData)
        {
            Block = owner as MyCubeBlock;
            LocalMatrix = dummyData.Matrix;
        }

        public override float InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        public override MatrixD ActivationMatrix
        {
            get { return LocalMatrix * Block.WorldMatrix; }
        }

        public override MatrixD WorldMatrix
        {
            get { return Block.WorldMatrix; }
        }

        public override int RenderObjectID
        {
            get
            {
                return Block.Render.GetRenderObjectID();
            }
        }

        public override bool ShowOverlay
        {
            get { return true; }
        }

        public override UseActionEnum SupportedActions
        {
            get { return UseActionEnum.OpenTerminal | UseActionEnum.OpenInventory; }
        }

        public override void Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            var user = entity as MyCharacter;
            var relation = Block.GetUserRelationToOwner(user.ControllerInfo.ControllingIdentityId);
            if (!relation.IsFriendly())
            {
                if (user.ControllerInfo.IsLocallyHumanControlled())
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
                }
                return;
            }

            switch (actionEnum)
            {
                case UseActionEnum.OpenTerminal:
                    MyGuiScreenTerminal.Show(MyTerminalPageEnum.ControlPanel, user, Block);
                    break;
                case UseActionEnum.OpenInventory:
                    if (Block.GetInventory(0) as MyInventory != null)
                        MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, user, Block);
                    break;
            }
        }

        public override MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            return new MyActionDescription()
            {
                Text = MySpaceTexts.NotificationHintPressToOpenControlPanel,
                FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL), Block.DefinitionDisplayNameText },
                IsTextControlHint = true,
                JoystickText = MySpaceTexts.NotificationHintJoystickPressToOpenControlPanel,
                JoystickFormatParams = new object[] { Block.DefinitionDisplayNameText },
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
