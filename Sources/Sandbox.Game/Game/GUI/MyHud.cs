#region Using

using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.SessionComponents;

#endregion

namespace Sandbox.Game.Gui
{
    // The only reason this is component is because of MyHudNotifications (it requires updates).
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MyHud : MySessionComponentBase
    {
        public static MyHudCrosshair Crosshair = new MyHudCrosshair();
        public static MyHudNotifications Notifications = new MyHudNotifications();
        public static MyHudShipInfo ShipInfo = new MyHudShipInfo();
        public static MyHudCharacterInfo CharacterInfo = new MyHudCharacterInfo();
        public static MyHudScenarioInfo ScenarioInfo = new MyHudScenarioInfo();
        public static MyHudBlockInfo BlockInfo = new MyHudBlockInfo();
        public static MyHudGravityIndicator GravityIndicator = new MyHudGravityIndicator();
        public static MyHudSinkGroupInfo SinkGroupInfo = new MyHudSinkGroupInfo();
        public static MyHudSelectedObject SelectedObjectHighlight = new MyHudSelectedObject();
        public static MyHudLocationMarkers LocationMarkers = new MyHudLocationMarkers();
        public static MyHudGpsMarkers ButtonPanelMarkers = new MyHudGpsMarkers();
        public static MyHudGpsMarkers GpsMarkers = new MyHudGpsMarkers();
        public static MyHudOreMarkers OreMarkers = new MyHudOreMarkers();
        public static MyHudChat Chat = new MyHudChat();
        public static MyHudLargeTurretTargets LargeTurretTargets = new MyHudLargeTurretTargets();
        public static MyHudWorldBorderChecker WorldBorderChecker = new MyHudWorldBorderChecker();
        public static MyHudHackingMarkers HackingMarkers = new MyHudHackingMarkers();
        public static MyHudCameraInfo CameraInfo = new MyHudCameraInfo();
        public static MyHudObjectiveLine ObjectiveLine = new MyHudObjectiveLine();
        public static MyHudNetgraph Netgraph = new MyHudNetgraph();
        public static MyHudVoiceChat VoiceChat = new MyHudVoiceChat();
        public static MyHudChangedInventoryItems ChangedInventoryItems = new MyHudChangedInventoryItems();
        public static MyHudQuestlog Questlog = new MyHudQuestlog();
        public static MyHudText BlocksLeft = new MyHudText();
        public static MyHudScreenEffects ScreenEffects = new MyHudScreenEffects();

        private static int m_rotatingWheelVisibleCounter;
        public static bool RotatingWheelVisible
        {
            get { return m_rotatingWheelVisibleCounter > 0; }
        }

        public static bool CheckShowPlayerNamesOnHud()
        {
            return MySession.Static.ShowPlayerNamesOnHud && MySandboxGame.Config.ShowPlayerNamesOnHud;
        }

        static bool m_minimalHud = false;
        public static bool MinimalHud
        {
            get { return m_minimalHud; }
            set
            {
                if (m_minimalHud != value)
                {
                    m_minimalHud = value;
                    MySandboxGame.Config.MinimalHud = value;
                    //MyConfig.Save(); //dont save, it make lags. Save it at the end of the game.
                }
            }
        }

        public static bool CutsceneHud = false;

        static bool m_netgraph = false;
        public static bool IsNetgraphVisible
        {
            get { return m_netgraph; }
            set { m_netgraph = value; }
        }

        static bool m_buildMode = false;
        public static bool IsBuildMode
        {
            get { return m_buildMode; }
            set { m_buildMode = value; }
        }

        public static void ReloadTexts()
        {
            Notifications.ReloadTexts();
            ShipInfo.Reload();
            CharacterInfo.Reload();
            SinkGroupInfo.Reload();
            ScenarioInfo.Reload();
        }

        public static void PushRotatingWheelVisible()
        {
            Debug.Assert(m_rotatingWheelVisibleCounter >= 0);
            ++m_rotatingWheelVisibleCounter;
        }

        public static void PopRotatingWheelVisible()
        {
            --m_rotatingWheelVisibleCounter;
            Debug.Assert(m_rotatingWheelVisibleCounter >= 0);
        }

        public override void LoadData()
        {
            base.LoadData();
            m_minimalHud = MySandboxGame.Config.MinimalHud;
        }

        public override void BeforeStart()
        {
            Questlog.Init();
        }

        public override void SaveData()
        {
            Questlog.Save();
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Notifications.Clear();
            OreMarkers.Clear();
            LocationMarkers.Clear();
            GpsMarkers.Clear();
            HackingMarkers.Clear();
            ObjectiveLine.Clear();
            ChangedInventoryItems.Clear();
            Chat.MessagesQueue.Clear();
            MyGuiScreenToolbarConfigBase.Reset();
            if (MyFakes.ENABLE_NETGRAPH)
            {
                Netgraph.ClearNetgraph();
            }
        }

        public override void UpdateBeforeSimulation()
        {
            Notifications.UpdateBeforeSimulation();
            Chat.Update();
            WorldBorderChecker.Update();
            ScreenEffects.Update();
            base.UpdateBeforeSimulation();
        }

        internal static void HideAll()
        {
            Crosshair.HideDefaultSprite();
            ShipInfo.Hide();
            CharacterInfo.Hide();
            BlockInfo.Visible = false;
            GravityIndicator.Hide();
            SinkGroupInfo.Visible = false;
            LargeTurretTargets.Visible = false;
            //Questlog.Visible = false;
        }
    }
}
