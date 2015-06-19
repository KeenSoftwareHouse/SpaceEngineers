using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.Entities.UseObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Screens.Helpers
{
    public class MySpaceControlMenuInitializer : IMyControlMenuInitializer
    {
        private MyGuiScreenControlMenu m_controlMenu;
        private bool IsControlMenuInitialized { get { return m_controlMenu != null; } }

        private MyControllableEntityControlHelper m_lightsControlHelper;
        private MyControllableEntityControlHelper m_helmetControlHelper;
        private MyControllableEntityControlHelper m_dampingControlHelper;
        private MyControllableEntityControlHelper m_broadcastingControlHelper;
        //private MyControllableEntityControlHelper m_landingGearsControlHelper;
        private MyControllableEntityControlHelper m_reactorsControlHelper;
        private MyControllableEntityControlHelper m_jetpackControlHelper;
        private MyLandingGearControlHelper m_landingGearsControlHelper;

        private MyQuickLoadControlHelper m_quickLoadControlHelper;
        private MyHudToggleControlHelper m_hudToggleControlHelper;
        private MyCameraModeControlHelper m_cameraModeControlHelper;
        private MyShowTerminalControlHelper m_showTerminalControlHelper;
        private MyShowBuildScreenControlHelper m_showBuildScreenControlHelper;
        private MySuicideControlHelper m_suicideControlHelper;
        private MyUseTerminalControlHelper m_terminalControlHelper;
        private MyEnableStationRotationControlHelper m_enableStationRotationControlHelper;

        public MySpaceControlMenuInitializer()
        {
            m_lightsControlHelper = new MyControllableEntityControlHelper(
                MyControlsSpace.HEADLIGHTS,
                x => x.SwitchLights(),
                x => x.EnabledLights,
                MySpaceTexts.ControlMenuItemLabel_Lights);
            m_helmetControlHelper = new MyControllableEntityControlHelper(
                MyControlsSpace.HELMET,
                x => x.SwitchHelmet(),
                x => x.EnabledHelmet,
                MySpaceTexts.ControlMenuItemLabel_Helmet);
            m_dampingControlHelper = new MyControllableEntityControlHelper(
                MyControlsSpace.DAMPING,
                x => x.SwitchDamping(),
                x => x.EnabledDamping,
                MySpaceTexts.ControlMenuItemLabel_Dampeners);
            m_broadcastingControlHelper = new MyControllableEntityControlHelper(
                MyControlsSpace.BROADCASTING,
                x => x.SwitchBroadcasting(),
                x => x.EnabledBroadcasting,
                MySpaceTexts.ControlMenuItemLabel_Broadcasting);
            m_landingGearsControlHelper = new MyLandingGearControlHelper();
            m_reactorsControlHelper = new MyControllableEntityControlHelper(
                MyControlsSpace.TOGGLE_REACTORS,
                x => x.SwitchReactors(),
                x => x.EnabledReactors,
                MySpaceTexts.ControlMenuItemLabel_Reactors);
            m_jetpackControlHelper = new MyControllableEntityControlHelper(
                MyControlsSpace.THRUSTS,
                x => x.SwitchThrusts(),
                x => x.EnabledThrusts,
                MySpaceTexts.ControlMenuItemLabel_Jetpack);

            m_quickLoadControlHelper = new MyQuickLoadControlHelper();
            m_hudToggleControlHelper = new MyHudToggleControlHelper();
            m_cameraModeControlHelper = new MyCameraModeControlHelper();
            m_showTerminalControlHelper = new MyShowTerminalControlHelper();
            m_showBuildScreenControlHelper = new MyShowBuildScreenControlHelper();
            m_suicideControlHelper = new MySuicideControlHelper();
            m_terminalControlHelper = new MyUseTerminalControlHelper();

            m_enableStationRotationControlHelper = new MyEnableStationRotationControlHelper();
        }

        public void OpenControlMenu(IMyControllableEntity controlledEntity)
        {
            m_controlMenu = null;

            if (controlledEntity is MyCharacter)
            {
                SetupCharacterScreen(controlledEntity as MyCharacter);
            }
            else if (controlledEntity is MyShipController)
            {
                SetupSpaceshipScreen(controlledEntity as MyShipController);
            }

            if (IsControlMenuInitialized)
            {
                m_controlMenu.RecreateControls(false);
                MyGuiSandbox.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = m_controlMenu);
            }
        }

        private void SetupCharacterScreen(MyCharacter character)
        {
            m_lightsControlHelper.SetEntity(character);
            m_dampingControlHelper.SetEntity(character);
            m_broadcastingControlHelper.SetEntity(character);
            m_helmetControlHelper.SetEntity(character);
            m_jetpackControlHelper.SetEntity(character);
            m_showBuildScreenControlHelper.SetEntity(character);
            m_showTerminalControlHelper.SetEntity(character);
            m_suicideControlHelper.SetCharacter(character);
            m_terminalControlHelper.SetCharacter(character);

            m_controlMenu = new MyGuiScreenControlMenu();

            m_controlMenu.AddItem(m_showTerminalControlHelper);
            m_controlMenu.AddItem(m_showBuildScreenControlHelper);

            if (MyCubeBuilder.Static.ShipCreationIsActivated)
            {
                m_controlMenu.AddItem(m_enableStationRotationControlHelper);
            }

            m_controlMenu.AddItem(m_quickLoadControlHelper);
            m_controlMenu.AddItem(m_hudToggleControlHelper);

            m_controlMenu.AddItem(m_jetpackControlHelper);
            m_controlMenu.AddItem(m_lightsControlHelper);
            m_controlMenu.AddItem(m_dampingControlHelper);
            m_controlMenu.AddItem(m_helmetControlHelper);
            m_controlMenu.AddItem(m_broadcastingControlHelper);

            m_controlMenu.AddItem(m_cameraModeControlHelper);

            AddUseObjectControl(character);

            if (MySession.Static.SurvivalMode)
                m_controlMenu.AddItem(m_suicideControlHelper);
        }

        private void SetupSpaceshipScreen(MyShipController ship)
        {
            m_lightsControlHelper.SetEntity(ship);
            m_dampingControlHelper.SetEntity(ship);
            m_landingGearsControlHelper.SetEntity(ship);
            m_reactorsControlHelper.SetEntity(ship);
            m_showBuildScreenControlHelper.SetEntity(ship);
            m_showTerminalControlHelper.SetEntity(ship);

            m_controlMenu = new MyGuiScreenControlMenu();

            m_controlMenu.AddItem(m_showTerminalControlHelper);
            m_controlMenu.AddItem(m_showBuildScreenControlHelper);

            m_controlMenu.AddItem(m_quickLoadControlHelper);
            m_controlMenu.AddItem(m_hudToggleControlHelper);

            m_controlMenu.AddItem(m_lightsControlHelper);
            m_controlMenu.AddItem(m_dampingControlHelper);
            m_controlMenu.AddItem(m_landingGearsControlHelper);
            m_controlMenu.AddItem(m_reactorsControlHelper);

            m_controlMenu.AddItem(m_cameraModeControlHelper);
        }

        private void AddUseObjectControl(MyCharacter character)
        {
            MyCharacterDetectorComponent detectorComponent = character.Components.Get<MyCharacterDetectorComponent>();
            if (detectorComponent != null)
            {
                if (detectorComponent.UseObject is MyUseObjectDoorTerminal
                    || detectorComponent.UseObject is MyUseObjectTerminal
                    || detectorComponent.UseObject is MyUseObjectTextPanel)
                {
                    m_terminalControlHelper.SetLabel(MySpaceTexts.ControlMenuItemLabel_ShowControlPanel);
                    m_controlMenu.AddItem(m_terminalControlHelper);
                }
                else if (detectorComponent.UseObject is MyUseObjectInventory)
                {
                    m_terminalControlHelper.SetLabel(MySpaceTexts.ControlMenuItemLabel_OpenInventory);
                    m_controlMenu.AddItem(m_terminalControlHelper);
                }
                else if (detectorComponent.UseObject is MyUseObjectPanelButton)
                {
                    m_terminalControlHelper.SetLabel(MySpaceTexts.ControlMenuItemLabel_SetupButtons);
                    m_controlMenu.AddItem(m_terminalControlHelper);
                }
                //else if (character.IsUseObjectOfType<MyUseObjectWardrobe>())
                //{
                //    m_terminalControlHelper.SetLabel(MySpaceTexts.ControlMenuItemLabel_Wardrobe);
                //    m_controlMenu.AddItem(m_terminalControlHelper);
                //}
            }
        }
    }
}
