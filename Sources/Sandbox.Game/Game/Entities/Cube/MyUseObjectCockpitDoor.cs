using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;
using VRageRender.Import;

namespace Sandbox.Game.Entities.Cube
{
    [MyUseObject("cockpit")]
    class MyUseObjectCockpitDoor : MyUseObjectBase
    {
        public readonly IMyEntity Cockpit;
        public readonly Matrix LocalMatrix;

        public MyUseObjectCockpitDoor(IMyEntity owner, string dummyName, MyModelDummy dummyData, uint key)
            : base(owner, dummyData)
        {
            Cockpit = owner;
            LocalMatrix = dummyData.Matrix;
        }

        public override float InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        public override MatrixD ActivationMatrix
        {
            get { return LocalMatrix * Cockpit.WorldMatrix; }
        }

        public override MatrixD WorldMatrix
        {
            get { return Cockpit.WorldMatrix; }
        }

        public override int RenderObjectID
        {
            get
            {
                return Cockpit.Render.GetRenderObjectID();
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
            // How to distinct between server sending message?
            // - it's response...always

            // This is request
            // 1) use - server call
            // -- on server: take control of entity
            // -- -- on success, use it, broadcast
            // -- -- on failure, don't use it, report back

            // Something like:
            // -- extension method IControllableEntity.RequestUse(actionEnum, user, handler)
            var user = entity as MyCharacter;
            if(Cockpit is MyCockpit)
                (Cockpit as MyCockpit).RequestUse(actionEnum, user);
        }

        public override MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            return new MyActionDescription()
            {
                Text = MySpaceTexts.NotificationHintPressToEnterCockpit,
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
            get
            {
                if (Cockpit is MyShipController)
                    return (Cockpit as MyShipController).PlayDefaultUseSound;
                return true;
            }
        }
    }
}
