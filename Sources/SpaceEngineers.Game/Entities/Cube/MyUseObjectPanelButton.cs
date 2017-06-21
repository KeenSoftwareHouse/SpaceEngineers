using System.Diagnostics;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Game;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender.Import;

namespace SpaceEngineers.Game.Entities.Cube
{
    [MyUseObject("panel")]
    public class MyUseObjectPanelButton : MyUseObjectBase
    {
        private readonly MyButtonPanel m_buttonPanel;
        private readonly Matrix m_localMatrix;
        private int m_index;
        MyGps m_buttonDesc = null;

        public MyUseObjectPanelButton(IMyEntity owner, string dummyName, MyModelDummy dummyData, uint key)
            : base(owner, dummyData)
        {
            m_buttonPanel = owner as MyButtonPanel;
            m_localMatrix = dummyData.Matrix;

            int orderNumber = 0;
            var parts =  dummyName.Split('_');
            int.TryParse(parts[parts.Length - 1], out orderNumber);
            m_index = orderNumber - 1;
            if (m_index >= m_buttonPanel.BlockDefinition.ButtonCount)
            {
                MyLog.Default.WriteLine(string.Format("{0} Button index higher than defined count.", m_buttonPanel.BlockDefinition.Id.SubtypeName));
                Debug.Fail(string.Format("{0} Button index higher than defined count.", m_buttonPanel.BlockDefinition.Id.SubtypeName));
                m_index = m_buttonPanel.BlockDefinition.ButtonCount - 1;
            }
        }

        public override float InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        public override MatrixD ActivationMatrix
        {
            get { return m_localMatrix * m_buttonPanel.WorldMatrix; }
        }

        public override MatrixD WorldMatrix
        {
            get { return m_buttonPanel.WorldMatrix; }
        }

        public override int RenderObjectID
        {
            get
            {
                return m_buttonPanel.Render.GetRenderObjectID();
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
            get { return false; }
        }

        public override void Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            var user = entity as MyCharacter;
            switch(actionEnum)
            {
                case UseActionEnum.Manipulate:
                    if (!m_buttonPanel.IsWorking)
                        return;
                    if (!m_buttonPanel.AnyoneCanUse && !m_buttonPanel.HasLocalPlayerAccess())
                    {
                        MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
                        return;
                    }
                    m_buttonPanel.Toolbar.UpdateItem(m_index);
                    m_buttonPanel.Toolbar.ActivateItemAtIndex(m_index);
                    m_buttonPanel.PressButton(m_index);
                    break;
                case UseActionEnum.OpenTerminal:
                    if (!m_buttonPanel.HasLocalPlayerAccess())
                        return;
                    MyToolbarComponent.CurrentToolbar = m_buttonPanel.Toolbar;
                    MyGuiScreenBase screen = MyGuiScreenCubeBuilder.Static;
                    if (screen == null)
                        screen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.ToolbarConfigScreen, 0, m_buttonPanel);
                    MyToolbarComponent.AutoUpdate = false;
                    screen.Closed += (source) => MyToolbarComponent.AutoUpdate = true;
                    MyGuiSandbox.AddScreen(screen);
                    break;
                default:
                    break;
            }
        }

        public override MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            m_buttonPanel.Toolbar.UpdateItem(m_index);
            var slot = m_buttonPanel.Toolbar.GetItemAtIndex(m_index);
            switch (actionEnum)
            {
                case UseActionEnum.Manipulate:

                    if (m_buttonDesc == null)
                    {
                        m_buttonDesc = new MyGps();
                        
                        m_buttonDesc.Description = "";
                        m_buttonDesc.Coords = ActivationMatrix.Translation;
                        m_buttonDesc.ShowOnHud = true;
                        m_buttonDesc.DiscardAt = null;
                        m_buttonDesc.AlwaysVisible = true;
                    }

                    MyHud.ButtonPanelMarkers.RegisterMarker(m_buttonDesc);

                    SetButtonName(m_buttonPanel.GetCustomButtonName(m_index));

                    if (slot != null)
                    {
                        return new MyActionDescription()
                        {
                            Text = MyCommonTexts.NotificationHintPressToUse,
                            FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.USE), slot.DisplayName },
                            IsTextControlHint = true,
                            JoystickFormatParams = new object[] { MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.USE), slot.DisplayName },
                        };
                    }
                    else
                    {
                        return new MyActionDescription() { Text = MySpaceTexts.Blank };
                    }

                case UseActionEnum.OpenTerminal:
                    return new MyActionDescription()
                    {
                        Text = MySpaceTexts.NotificationHintPressToOpenButtonPanel,
                        FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL) },
                        IsTextControlHint = true,
                        JoystickText = MySpaceTexts.NotificationHintJoystickPressToOpenButtonPanel,
                    };

                default:
                    Debug.Fail("Invalid branch reached.");
                    return new MyActionDescription()
                    {
                        Text = MySpaceTexts.NotificationHintPressToOpenButtonPanel,
                        FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL) },
                        IsTextControlHint = true
                    };
            }
        }

        public override bool HandleInput() 
        {             
            return false; 
        }

        public override bool PlayIndicatorSound
        {
            get { return true; }
        }

        public void RemoveButtonMarker()
        {
            if (m_buttonDesc != null)
            {
                MyHud.ButtonPanelMarkers.UnregisterMarker(m_buttonDesc);
            }
        }

        public override void OnSelectionLost()
        {
            RemoveButtonMarker();
        }

        void SetButtonName(string name)
        {
            if (m_buttonPanel.IsFunctional && m_buttonPanel.IsWorking && (m_buttonPanel.HasLocalPlayerAccess() || m_buttonPanel.AnyoneCanUse))
            {
                m_buttonDesc.Name = name;
            }
            else
            {
                m_buttonDesc.Name = "";
            }
        }

        public void UpdateMarkerPosition()
        {
            if (m_buttonDesc!= null)
            {
                m_buttonDesc.Coords = ActivationMatrix.Translation;
            }
        }
    }
}
