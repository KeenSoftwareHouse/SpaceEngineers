using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    [MyUseObject("cockpit")]
    class MyUseObjectCockpitDoor : IMyUseObject
    {
        public readonly IMyEntity Cockpit;
        public readonly Matrix LocalMatrix;

        public MyUseObjectCockpitDoor(IMyEntity owner, string dummyName, MyModelDummy dummyData, int key)
        {
            Cockpit = owner;
            LocalMatrix = dummyData.Matrix;
        }

        float IMyUseObject.InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        MatrixD IMyUseObject.ActivationMatrix
        {
            get { return LocalMatrix * Cockpit.WorldMatrix; }
        }

        MatrixD IMyUseObject.WorldMatrix
        {
            get { return Cockpit.WorldMatrix; }
        }

        int IMyUseObject.RenderObjectID
        {
            get
            {
                return Cockpit.Render.GetRenderObjectID();
            }
        }

        bool IMyUseObject.ShowOverlay
        {
            get { return true; }
        }

        UseActionEnum IMyUseObject.SupportedActions
        {
            get { return UseActionEnum.Manipulate; }
        }

        void IMyUseObject.Use(UseActionEnum actionEnum, IMyEntity entity)
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

        MyActionDescription IMyUseObject.GetActionInfo(UseActionEnum actionEnum)
        {
            return new MyActionDescription()
            {
                Text = MySpaceTexts.NotificationHintPressToEnterCockpit,
                FormatParams = new object[] { MyGuiSandbox.GetKeyName(MyControlsSpace.USE) },
                IsTextControlHint = true,
                JoystickFormatParams = new object[] { MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.USE) },
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
