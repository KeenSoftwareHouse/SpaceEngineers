
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens;
using Sandbox.Graphics.GUI;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;
using System.Collections.Generic;
using VRage.Game;

namespace Sandbox.Game.Entities.Cube
{
    [MyUseObject("wardrobe")]
    class MyUseObjectWardrobe : MyUseObjectBase
    {
        public readonly MyCubeBlock Block;
        public readonly Matrix LocalMatrix;

        public MyUseObjectWardrobe(IMyEntity owner, string dummyName, MyModelDummy dummyData, uint key)
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
            get { return (MatrixD)LocalMatrix * Block.WorldMatrix; }
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
            get { return UseActionEnum.Manipulate; }
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
                case UseActionEnum.Manipulate:
                    if (Block is MyMedicalRoom)
                    {
                        MyMedicalRoom medRoom = (MyMedicalRoom)Block;
                        if (!medRoom.SuitChangeAllowed)
                        {
                            MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
                            break;
                        }

                        if (medRoom.CustomWardrobesEnabled)
                        {
                            MyGuiSandbox.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = new MyGuiScreenWardrobe(user, medRoom.CustomWardrobeNames));
                            break;
                        }
                    }
 
                    MyGuiSandbox.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = new MyGuiScreenWardrobe(user));
                    break;
            }
        }

        public override MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            return new MyActionDescription()
            {
                Text = MyCommonTexts.NotificationHintPressToUseWardrobe,
                FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.USE) },
                IsTextControlHint = true,
                JoystickFormatParams = new object[] { MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.USE) },
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
