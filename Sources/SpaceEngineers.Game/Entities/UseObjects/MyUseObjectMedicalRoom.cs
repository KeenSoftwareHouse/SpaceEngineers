
using System.Diagnostics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Localization;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Game;
using VRageRender.Import;

namespace SpaceEngineers.Game.Entities.UseObjects
{
    [MyUseObject("block")]
    class MyUseObjectMedicalRoom : MyUseObjectBase
    {
        private MyMedicalRoom m_medicalRoom;
        private Matrix m_localMatrix;

        public MyUseObjectMedicalRoom(IMyEntity owner, string dummyName, MyModelDummy dummyData, uint key)
            : base(owner, dummyData)
        {
            m_medicalRoom = (MyMedicalRoom)owner;
            m_localMatrix = dummyData.Matrix;
        }

        public override float InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        public override MatrixD ActivationMatrix
        {
            get { return m_localMatrix * m_medicalRoom.WorldMatrix; }
        }

        public override MatrixD WorldMatrix
        {
            get { return m_medicalRoom.WorldMatrix; }
        }

        public override int RenderObjectID
        {
            get
            {
                var renderObjectIds = m_medicalRoom.Render.RenderObjectIDs;
                if (renderObjectIds.Length > 0)
                    return (int)renderObjectIds[0];
                return -1;
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
            get { return UseActionEnum.Manipulate | UseActionEnum.OpenTerminal; }
        }

        public override bool ContinuousUsage
        {
            get { return true; }
        }

        public override void Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            var user = entity as MyCharacter;
            m_medicalRoom.Use(actionEnum, user);
        }

        public override MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            switch (actionEnum)
            {
                case UseActionEnum.Manipulate:
                    return new MyActionDescription()
                    {
                        Text = MySpaceTexts.NotificationHintPressToRechargeInMedicalRoom,
                        FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.USE) },
                        IsTextControlHint = true,
                        JoystickFormatParams = new object[] { MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.USE) },
                    };

                case UseActionEnum.OpenTerminal:
                    return new MyActionDescription()
                    {
                        Text = MySpaceTexts.NotificationHintPressToOpenTerminal,
                        FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL) },
                        IsTextControlHint = true,
                        JoystickText = MySpaceTexts.NotificationHintJoystickPressToOpenTerminal,
                    };

                default:
                    Debug.Fail("Invalid branch reached.");
                    return new MyActionDescription()
                    {
                        Text = MySpaceTexts.NotificationHintPressToOpenTerminal,
                        FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL) },
                        IsTextControlHint = true
                    };
            }
        }

        public override bool HandleInput() { return false; }

        public override void OnSelectionLost() { }

        public override bool PlayIndicatorSound
        {
            get { return true; }
        }
    }
}
