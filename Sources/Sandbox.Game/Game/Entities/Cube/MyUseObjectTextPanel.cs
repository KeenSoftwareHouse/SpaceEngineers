
using System.Diagnostics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Localization;
using VRage.Game;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;
using VRageRender.Import;

namespace Sandbox.Game.Entities.Cube
{
    [MyUseObject("textpanel")]
    public class MyUseObjectTextPanel : MyUseObjectBase
    {
        private MyTextPanel m_textPanel;
        private Matrix m_localMatrix;

        public MyUseObjectTextPanel(IMyEntity owner, string dummyName, MyModelDummy dummyData, uint key)
            : base(owner, dummyData)
        {
            m_textPanel = (MyTextPanel)owner;
            m_localMatrix = dummyData.Matrix;
        }

        public override float InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        public override MatrixD ActivationMatrix
        {
            get { return m_localMatrix * m_textPanel.WorldMatrix; }
        }

        public override MatrixD WorldMatrix
        {
            get { return m_textPanel.WorldMatrix; }
        }

        public override int RenderObjectID
        {
            get
            {
                if (m_textPanel.Render == null)
                    return -1;
                var renderObjectIds = m_textPanel.Render.RenderObjectIDs;
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
            get 
            {
                UseActionEnum actions = UseActionEnum.None;

                if (m_textPanel.GetPlayerRelationToOwner() != MyRelationsBetweenPlayerAndBlock.Enemies)
                    actions |= UseActionEnum.Manipulate | UseActionEnum.OpenTerminal;

                return actions; 
            }
        }

        public override bool ContinuousUsage
        {
            get { return false; }
        }

        public override void Use(UseActionEnum actionEnum, IMyEntity user)
        {
            m_textPanel.Use(actionEnum, user);
        }

        public override MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            switch (actionEnum)
            {
                case UseActionEnum.Manipulate:
                    return new MyActionDescription()
                    {
                        Text = MySpaceTexts.NotificationHintPressToShowScreen,
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
