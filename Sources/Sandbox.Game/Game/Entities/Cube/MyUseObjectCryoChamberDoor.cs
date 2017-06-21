using System.Diagnostics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;
using VRageRender.Import;

namespace Sandbox.Game.Entities.Cube
{
    [MyUseObject("cryopod")]
    class MyUseObjectCryoChamberDoor : MyUseObjectBase
    {
        public readonly MyCryoChamber CryoChamber;
        public readonly Matrix LocalMatrix;

        public MyUseObjectCryoChamberDoor(IMyEntity owner, string dummyName, MyModelDummy dummyData, uint key)
            : base(owner, dummyData)
        {
            CryoChamber = owner as MyCryoChamber;
            Debug.Assert(CryoChamber != null, "MyUseObjectCryoChamberDoor should only be used with MyCryoChamber blocks!");
            
            LocalMatrix = dummyData.Matrix;
        }

        public override float InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        public override MatrixD ActivationMatrix
        {
            get { return LocalMatrix * CryoChamber.WorldMatrix; }
        }

        public override MatrixD WorldMatrix
        {
            get { return CryoChamber.WorldMatrix; }
        }

        public override int RenderObjectID
        {
            get
            {
                return CryoChamber.Render.GetRenderObjectID();
            }
        }

        public override int InstanceID
        {
            get { return -1; }
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
            CryoChamber.RequestUse(actionEnum, user);
        }

        public override MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            return new MyActionDescription()
            {
                Text = MySpaceTexts.NotificationHintPressToEnterCryochamber,
                FormatParams = new object[] { MyGuiSandbox.GetKeyName(MyControlsSpace.USE) },
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
