
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
using SpaceEngineers.Game.Entities.Blocks;

namespace SpaceEngineers.Game.Entities.UseObjects
{
    [MyUseObject("block")]
    class MyUseObjectMedicalRoom : IMyUseObject
    {
        private MyMedicalRoom m_medicalRoom;
        private Matrix m_localMatrix;

        public MyUseObjectMedicalRoom(IMyEntity owner, string dummyName, MyModelDummy dummyData, int key)
        {
            m_medicalRoom = (MyMedicalRoom)owner;
            m_localMatrix = dummyData.Matrix;
        }

        float IMyUseObject.InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        MatrixD IMyUseObject.ActivationMatrix
        {
            get { return m_localMatrix * m_medicalRoom.WorldMatrix; }
        }

        MatrixD IMyUseObject.WorldMatrix
        {
            get { return m_medicalRoom.WorldMatrix; }
        }

        int IMyUseObject.RenderObjectID
        {
            get
            {
                var renderObjectIds = m_medicalRoom.Render.RenderObjectIDs;
                if (renderObjectIds.Length > 0)
                    return (int)renderObjectIds[0];
                return -1;
            }
        }

        bool IMyUseObject.ShowOverlay
        {
            get { return true; }
        }

        UseActionEnum IMyUseObject.SupportedActions
        {
            get { return UseActionEnum.Manipulate | UseActionEnum.OpenTerminal; }
        }

        bool IMyUseObject.ContinuousUsage
        {
            get { return true; }
        }

        void IMyUseObject.Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            var user = entity as MyCharacter;
            m_medicalRoom.Use(actionEnum, user);
        }

        MyActionDescription IMyUseObject.GetActionInfo(UseActionEnum actionEnum)
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

        bool IMyUseObject.HandleInput() { return false; }

        void IMyUseObject.OnSelectionLost() { }

        bool IMyUseObject.PlayIndicatorSound
        {
            get { return true; }
        }
    }
}
