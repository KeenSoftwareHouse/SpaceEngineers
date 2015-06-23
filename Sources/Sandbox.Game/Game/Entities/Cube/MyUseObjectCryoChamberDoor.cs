using System.Diagnostics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    [MyUseObject("cryopod")]
    class MyUseObjectCryoChamberDoor : IMyUseObject
    {
        public readonly MyCryoChamber CryoChamber;
        public readonly Matrix LocalMatrix;

        public MyUseObjectCryoChamberDoor(IMyEntity owner, string dummyName, MyModelDummy dummyData, int key)
        {
            CryoChamber = owner as MyCryoChamber;
            Debug.Assert(CryoChamber != null, "MyUseObjectCryoChamberDoor should only be used with MyCryoChamber blocks!");
            
            LocalMatrix = dummyData.Matrix;
        }

        float IMyUseObject.InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        MatrixD IMyUseObject.ActivationMatrix
        {
            get { return LocalMatrix * CryoChamber.WorldMatrix; }
        }

        MatrixD IMyUseObject.WorldMatrix
        {
            get { return CryoChamber.WorldMatrix; }
        }

        int IMyUseObject.RenderObjectID
        {
            get
            {
                return CryoChamber.Render.GetRenderObjectID();
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
            var user = entity as MyCharacter;
            CryoChamber.RequestUse(actionEnum, user);
        }

        MyActionDescription IMyUseObject.GetActionInfo(UseActionEnum actionEnum)
        {
            return new MyActionDescription()
            {
                Text = MySpaceTexts.NotificationHintPressToEnterCryochamber,
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
