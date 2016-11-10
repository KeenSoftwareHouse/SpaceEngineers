using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyControllableEntityControlHelper : MyAbstractControlMenuItem
    {
        protected IMyControllableEntity m_entity;
        private Action<IMyControllableEntity> m_action;
        private Func<IMyControllableEntity, bool> m_valueGetter;
        private string m_label;
        private string m_value;
        private string m_onValue;
        private string m_offValue;

        public MyControllableEntityControlHelper(
            MyStringId controlId,
            Action<IMyControllableEntity> action,
            Func<IMyControllableEntity, bool> valueGetter,
            MyStringId label,
            MySupportKeysEnum supportKeys = MySupportKeysEnum.NONE)
            : this(controlId, action, valueGetter, label, MyCommonTexts.ControlMenuItemValue_On, MyCommonTexts.ControlMenuItemValue_Off, supportKeys)
        {
        }

        public MyControllableEntityControlHelper(
            MyStringId controlId,
            Action<IMyControllableEntity> action,
            Func<IMyControllableEntity, bool> valueGetter,
            MyStringId label, 
            MyStringId onValue, 
            MyStringId offValue,
            MySupportKeysEnum supportKeys = MySupportKeysEnum.NONE)
            : base(controlId, supportKeys)
        {
            m_action = action;
            m_valueGetter = valueGetter;
            m_label = MyTexts.GetString(label);
            m_onValue = MyTexts.GetString(onValue);
            m_offValue = MyTexts.GetString(offValue);
        }

        public void SetEntity(IMyControllableEntity entity)
        {
            m_entity = entity;
        }

        public override string Label
        {
            get { return m_label; }
        }

        public override string CurrentValue
        {
            get
            {
                return m_value;
            }
        }

        public override void Activate()
        {
            m_action(m_entity);
        }

        public override void Next()
        {
            Activate();
        }

        public override void Previous()
        {
            Activate();
        }

        public override void UpdateValue()
        {
            if (m_valueGetter(m_entity))
                m_value = m_onValue;
            else
                m_value = m_offValue;
        }
    }

    public class MyShowTerminalControlHelper : MyAbstractControlMenuItem
    {
        private IMyControllableEntity m_entity;

        public MyShowTerminalControlHelper()
            : base(MyControlsSpace.TERMINAL)
        {
        }

        public override void Activate()
        {
            MyScreenManager.CloseScreen(typeof(MyGuiScreenControlMenu));
            m_entity.ShowTerminal();
        }

        public void SetEntity(IMyControllableEntity entity)
        {
            m_entity = entity;
        }

        public override string Label
        {
            get { return MyTexts.GetString(MySpaceTexts.ControlMenuItemLabel_Terminal); }
        }
    }

    public class MyShowBuildScreenControlHelper : MyAbstractControlMenuItem
    {
        private IMyControllableEntity m_entity;    

        public MyShowBuildScreenControlHelper()
            : base(MyControlsSpace.BUILD_SCREEN)
        {
        }

        public override string Label
        {
            get { return MyTexts.GetString(MySpaceTexts.ControlMenuItemLabel_ShowBuildScreen); }
        }

        public void SetEntity(IMyControllableEntity entity)
        {
            m_entity = entity;
        }

        public override void Activate()
        {
            MyScreenManager.CloseScreen(typeof(MyGuiScreenControlMenu));
            MyGuiSandbox.AddScreen
            (
                MyGuiScreenGamePlay.ActiveGameplayScreen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.ToolbarConfigScreen, 0, m_entity as MyShipController)
            );
        }
    }

    public class MyCameraModeControlHelper : MyAbstractControlMenuItem
    {
        private string m_value;

        public MyCameraModeControlHelper()
            : base(MyControlsSpace.CAMERA_MODE)
        {
        }

        public override bool Enabled
        {
            get
            {
                return MyGuiScreenGamePlay.Static.CanSwitchCamera;
            }
        }

        public override string CurrentValue
        {
            get { return m_value; }
        }

        public override string Label
        {
            get { return MyTexts.GetString(MyCommonTexts.ControlMenuItemLabel_CameraMode); }
        }

        public override void Activate()
        {
            MyGuiScreenGamePlay.Static.SwitchCamera();
        }

        public override void UpdateValue()
        {
            if (MySession.Static.CameraController.IsInFirstPersonView)
                m_value = MyTexts.GetString(MyCommonTexts.ControlMenuItemValue_FPP);
            else
                m_value = MyTexts.GetString(MyCommonTexts.ControlMenuItemValue_TPP);
        }

        public override void Next()
        {
            Activate();
        }

        public override void Previous()
        {
            Activate();
        }
    }

    public class MyPauseToggleControlHelper : MyAbstractControlMenuItem
    {
        public MyPauseToggleControlHelper()
            : base(MyControlsSpace.PAUSE_GAME)
        {

        }

        public override string Label
        {
            get { return MyTexts.GetString(MyCommonTexts.ControlMenuItemLabel_PauseGame); }
        }

        public override void Activate()
        {
            MySandboxGame.PauseToggle();
        }
    }

    public class MySuicideControlHelper : MyAbstractControlMenuItem
    {
        private MyCharacter m_character;

        public MySuicideControlHelper()
            : base(MyControlsSpace.SUICIDE)
        {
        }

        public override string Label
        {
            get { return MyTexts.GetString(MyCommonTexts.ControlMenuItemLabel_CommitSuicide); ; }
        }

        public override void Activate()
        {
            m_character.Die();
        }

        public void SetCharacter(MyCharacter character)
        {
            m_character = character;
        }
    }

    public class MyQuickLoadControlHelper : MyAbstractControlMenuItem
    {
        public MyQuickLoadControlHelper()
            : base("F5")
        {

        }

        public override string Label
        {
            get { return MyTexts.GetString(MyCommonTexts.ControlMenuItemLabel_QuickLoad); }
        }

        public override void Activate()
        {
            MyScreenManager.CloseScreen(typeof(MyGuiScreenControlMenu));
            if (Sync.IsServer)
            {
                MyGuiScreenGamePlay.Static.ShowLoadMessageBox(MySession.Static.CurrentPath);
            }
            else
            {
                MyGuiScreenGamePlay.Static.ShowReconnectMessageBox();
            }
        }
    }

    public class MyHudToggleControlHelper : MyAbstractControlMenuItem
    {
        private string m_value;

        public MyHudToggleControlHelper()
            : base(MyControlsSpace.TOGGLE_HUD)
        {
        }

        public override string CurrentValue
        {
            get
            {
                return m_value;
            }
        }

        public override string Label
        {
            get { return MyTexts.GetString(MyCommonTexts.ControlMenuItemLabel_ToggleHud); }
        }

        public override void Activate()
        {
            MyHud.MinimalHud = !MyHud.MinimalHud;
        }

        public override void UpdateValue()
        {
            if (!MyHud.MinimalHud)
                m_value = MyTexts.GetString(MyCommonTexts.ControlMenuItemValue_On);
            else
                m_value = MyTexts.GetString(MyCommonTexts.ControlMenuItemValue_Off);
        }

        public override void Next()
        {
            Activate();
        }

        public override void Previous()
        {
            Activate();
        }
    }

    public class MyColorPickerControlHelper : MyAbstractControlMenuItem
    {
        public MyColorPickerControlHelper()
            : base(MyControlsSpace.LANDING_GEAR, MySupportKeysEnum.SHIFT)
        {
        }

        public override string Label
        {
            get { return MyTexts.GetString(MyCommonTexts.ControlMenuItemLabel_ShowColorPicker); }
        }

        public override void Activate()
        {
            MyScreenManager.CloseScreen(typeof(MyGuiScreenControlMenu));
            MyGuiSandbox.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = new MyGuiScreenColorPicker());
        }
    }

    public class MyLandingGearControlHelper : MyControllableEntityControlHelper
    {
        private MyShipController ShipController { get { return m_entity as MyShipController; } }

        public override bool Enabled
        {
            get
            {
                return ShipController.CubeGrid.GridSystems.LandingSystem.Locked != MyMultipleEnabledEnum.NoObjects;
            }
        }

        public MyLandingGearControlHelper()
            : base(MyControlsSpace.LANDING_GEAR, x => x.SwitchLeadingGears(), x => x.EnabledLeadingGears, MySpaceTexts.ControlMenuItemLabel_LandingGear)
        {
        }

        public new void SetEntity(IMyControllableEntity entity)
        {
            m_entity = entity as MyShipController;
        }
    }

    public class MyConnectorControlHelper : MyControllableEntityControlHelper
    {
        private MyShipController ShipController { get { return m_entity as MyShipController; } }

        public override bool Enabled
        {
            get
            {
                return ShipController.CubeGrid.GridSystems.ConveyorSystem.IsInteractionPossible;
            }
        }

        public MyConnectorControlHelper()
            : base(MyControlsSpace.LANDING_GEAR, x => x.SwitchLeadingGears(), x => { return GetConnectorStatus(x); }, MySpaceTexts.ControlMenuItemLabel_Connectors)
        {
        }

        public new void SetEntity(IMyControllableEntity entity)
        {
            m_entity = entity as MyShipController;
        }

        private static bool GetConnectorStatus(IMyControllableEntity shipController)
        {
            return (shipController as MyShipController).CubeGrid.GridSystems.ConveyorSystem.Connected;
        }
    }

    public class MyUseTerminalControlHelper : MyAbstractControlMenuItem
    {
        private MyCharacter m_character;
        private string m_label;

        public MyUseTerminalControlHelper()
            : base(MyControlsSpace.TERMINAL)
        {

        }

        public void SetCharacter(MyCharacter character)
        {
            m_character = character;
        }

        public override void Activate()
        {
            MyScreenManager.CloseScreen(typeof(MyGuiScreenControlMenu));
            m_character.UseTerminal();
        }

        public override string Label
        {
            get { return m_label; }
        }

        public void SetLabel(MyStringId id)
        {
            m_label = MyTexts.GetString(id);
        }
    }

    public class MyEnableStationRotationControlHelper : MyAbstractControlMenuItem
    {
        private IMyControllableEntity m_entity;

        public MyEnableStationRotationControlHelper()
            : base(MyControlsSpace.FREE_ROTATION)
        {
        }

        public override void Activate()
        {
            MyScreenManager.CloseScreen(typeof(MyGuiScreenControlMenu));
            //MyCubeBuilder.Static.EnableStationRotation();
        }

        public override string Label
        {
            get { return MyTexts.GetString(MySpaceTexts.StationRotation_Static); }
        }
    }

    public class MyBriefingMenuControlHelper : MyAbstractControlMenuItem
    {
        private IMyControllableEntity m_entity;

        public override bool Enabled
        {
            get
            {
                return base.Enabled && MyFakes.ENABLE_MISSION_TRIGGERS;
            }
        }

        public MyBriefingMenuControlHelper()
            : base(MyControlsSpace.MISSION_SETTINGS)
        {
        }

        public override void Activate()
        {
            MyScreenManager.CloseScreen(typeof(MyGuiScreenControlMenu));
            MyGuiSandbox.AddScreen(new Sandbox.Game.Screens.MyGuiScreenBriefing());
        }

        public override string Label
        {
            get { return MyTexts.GetString(MySpaceTexts.ControlMenuItemLabel_ScenarioBriefing); }
        }
    }
}

