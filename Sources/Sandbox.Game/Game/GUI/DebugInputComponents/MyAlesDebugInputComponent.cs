using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.Network;
using VRage.Utils;
using VRageMath;
using Sandbox.ModAPI;
using System;
using Sandbox.ModAPI;
using System;


namespace Sandbox.Game.Gui
{

    class MyGuiScreenDialogTeleportCheat : MyGuiScreenBase
    {
        List<IMyGps> m_prefabDefinitions = new List<IMyGps>();

        MyGuiControlButton m_confirmButton;
        MyGuiControlButton m_cancelButton;
        MyGuiControlCombobox m_prefabs;

        public MyGuiScreenDialogTeleportCheat() :
            base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, null)
        {
            CanHideOthers = false;
            EnabledBackgroundFade = true;
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDialogTravelToCheat";
        }

        public override void RecreateControls(bool contructor)
        {
            base.RecreateControls(contructor);

            this.Controls.Add(new MyGuiControlLabel(new Vector2(0.0f, -0.10f), text: "Select gps you want to reach. (Dont use for grids with subgrids.)", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));
            m_prefabs = new MyGuiControlCombobox(new Vector2(0.2f, 0.0f), new Vector2(0.3f, 0.05f), null, null, 10, null);
            m_confirmButton = new MyGuiControlButton(new Vector2(0.21f, 0.10f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Confirm"));
            m_cancelButton = new MyGuiControlButton(new Vector2(-0.21f, 0.10f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Cancel"));

            List<IMyGps> outlist = new List<IMyGps>();
            MySession.Static.Gpss.GetGpsList(MySession.Static.LocalPlayerId, outlist);
            foreach (var prefab in outlist)
            {
                int key = m_prefabDefinitions.Count;
                m_prefabDefinitions.Add(prefab);
                m_prefabs.AddItem(key, prefab.Name);
            }

            this.Controls.Add(m_prefabs);
            this.Controls.Add(m_confirmButton);
            this.Controls.Add(m_cancelButton);

            m_confirmButton.ButtonClicked += confirmButton_OnButtonClick;
            m_cancelButton.ButtonClicked += cancelButton_OnButtonClick;
        }

        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            base.HandleUnhandledInput(receivedFocusInThisUpdate);
        }

        void confirmButton_OnButtonClick(MyGuiControlButton sender)
        {
            int selected = (int)m_prefabs.GetSelectedKey();
            var prefabDefinition = m_prefabDefinitions[selected == -1 ? 0 : selected];
            MyMultiplayer.RaiseStaticEvent(s => MyAlesDebugInputComponent.TravelToWaypoint, prefabDefinition.Coords);

            CloseScreen();
        }

        void cancelButton_OnButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }
    }

    [StaticEventOwner]
    class MyAlesDebugInputComponent : MyDebugComponent
    {

        Random m_random;

        public override string GetName()
        {
            return "Ales";
        }

        public MyAlesDebugInputComponent()
        {
            m_random = new Random();
            AddShortcut(MyKeys.U, true, false, false, false,
               () => "Reload particles",
               delegate
               {
                   ReloadParticleDefinition();
                   return true;
               });
            AddShortcut(MyKeys.NumPad0, true, false, false, false,
                () => "Teleport to gps",
                delegate
                {
                    TravelToWaypointClient();
                    return true;
                });
            //AddShortcut(MyKeys.NumPad1, true, false, false, false, () => "Reorder cluster", delegate { ReorderCluster(); return true; });
            
        }

        private void TravelToWaypointClient()
        {
            var dialog = new MyGuiScreenDialogTeleportCheat();
            MyGuiSandbox.AddScreen(dialog);
        }

        [Event, Reliable, Server]
        public static void TravelToWaypoint(Vector3D pos)
        {
            MySession.Static.ControlledEntity.Teleport(pos);
        }


        private void ReloadParticleDefinition()
        {
            MyDefinitionManager.Static.ReloadParticles();
        }

    }
}
