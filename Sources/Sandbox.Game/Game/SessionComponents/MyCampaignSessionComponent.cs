using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using ParallelTasks;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders;
using VRage.Game.VisualScripting.Campaign;
using VRage.Network;
using VRage.Utils;

namespace Sandbox.Game.SessionComponents
{
    /// <summary>
    /// Maintains a state machine that holds campain progress.
    /// This session component is shared with newly loaded
    /// campaign worlds and serialized on session saving.
    /// </summary>
    [StaticEventOwner]
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, 666, typeof(MyObjectBuilder_CampaignSessionComponent))]
    public class MyCampaignSessionComponent : MySessionComponentBase
    {
        #region Private Fields
        // Active campaign SM
        private MyCampaignStateMachine  m_runningCampaignSM;
        // Storage filled between levels by player data
        private readonly Dictionary<ulong, MyObjectBuilder_Inventory> m_savedCharacterInventoriesPlayerIds 
            = new Dictionary<ulong, MyObjectBuilder_Inventory>(); 
        #endregion

        #region Public Properties
        // Holds the name of the edge that should be followed on SM update.
        public string   CampaignLevelOutcome { get; set; }
        // Tells of the Component is active.
        public bool     Running { get {return m_runningCampaignSM != null && m_runningCampaignSM.CurrentNode != null; } }
        #endregion

        #region Constructor and Loading
        // Loads the SM and tries to restore it progress.
        private void LoadCampaignStateMachine(string activeState = null)
        {
            m_runningCampaignSM = new MyCampaignStateMachine();
            // Use the active definition
            m_runningCampaignSM.Deserialize(MyCampaignManager.Static.ActiveCampaign.StateMachine);

            // Do we have an active state from saved game?
            if(activeState != null)
                m_runningCampaignSM.SetState(activeState);
            else
                m_runningCampaignSM.ResetToStart();

            // This needs to be done for newly loaded state machines.
            // Otherwise the nodes finished flag would not be updated at first.
            m_runningCampaignSM.CurrentNode.OnUpdate(m_runningCampaignSM);
        }
        #endregion

        #region Campaign Control
        // Updates the state machine before the session is completely unloaded.
        private void UpdateStateMachine()
        {
            var oldStateName = m_runningCampaignSM.CurrentNode.Name;
            // Trigger state change
            m_runningCampaignSM.TriggerAction(MyStringId.GetOrCompute(CampaignLevelOutcome));
            m_runningCampaignSM.Update();
            var newState = m_runningCampaignSM.CurrentNode as MyCampaignStateMachineNode;
            // Notify if the get stuck in one state.
            if (oldStateName == newState.Name)
            {
                MySandboxGame.Log.WriteLine("ERROR: Campaign is stuck in one state! Check the campaign file.");
                Debug.Fail("ERROR: Campaign is stuck in one state! Check the campaign file.");
            }

            // Remove the outdated outcome.
            CampaignLevelOutcome = null;
        }

        // Sets callbacks for successful restoration of all players inventories.
        private void LoadPlayersInventories()
        {
            // Load local player inventory.
            MyObjectBuilder_Inventory inv;
            if (m_savedCharacterInventoriesPlayerIds.TryGetValue(MySession.Static.LocalHumanPlayer.Id.SteamId, out inv))
            {
                if(MySession.Static.LocalCharacter != null)
                {
                    var charactersInventory = MySession.Static.LocalCharacter.GetInventory();
                    foreach (var inventoryItem in inv.Items)
                    {
                        charactersInventory.AddItems(inventoryItem.Amount, inventoryItem.PhysicalContent);
                    }
                }
            }

            // The rest needs to be done for multiplayer only.
            if (MyMultiplayer.Static == null || !MyMultiplayer.Static.IsServer) return;

            // Init the players inventory with stored values.
            MySession.Static.Players.PlayersChanged += (added, id) =>
            {
                var player = MySession.Static.Players.GetPlayerById(id);
                Debug.Assert(player.Character != null);
                MyObjectBuilder_Inventory inventoryOb;
                if (player.Character != null && m_savedCharacterInventoriesPlayerIds.TryGetValue(player.Id.SteamId, out inventoryOb))
                {
                    var charactersInventory = MySession.Static.LocalCharacter.GetInventory();
                    foreach (var inventoryItem in inventoryOb.Items)
                    {
                        charactersInventory.AddItems(inventoryItem.Amount, inventoryItem.PhysicalContent);
                    }
                }
            };
        }

        // Stores the contents of players inventory in the component.
        private void SavePlayersInventories()
        {
            m_savedCharacterInventoriesPlayerIds.Clear();
            foreach (var onlinePlayer in MySession.Static.Players.GetOnlinePlayers())
            {
                if(onlinePlayer.Character != null)
                {
                    var inventory = onlinePlayer.Character.GetInventory();
                    if(inventory != null)
                    {
                        var inventoryData = inventory.GetObjectBuilder();
                        m_savedCharacterInventoriesPlayerIds[onlinePlayer.Id.SteamId] = inventoryData;
                    }
                }
            }
        }

        // unregisters callbacks and runs new mission when the session is being unloaded.
        public void LoadNextCampaignMission()
        {
            // Only server can switch missions
            if (MyMultiplayer.Static != null && !MyMultiplayer.Static.IsServer)
                return;

            // Save inventories
            SavePlayersInventories();

            var savePath = MySession.Static.CurrentPath;
            var folderName = Path.GetDirectoryName(savePath.Replace(MyFileSystem.SavesPath + "\\", ""));
            // Campaign is finished
            if (m_runningCampaignSM.Finished)
            {
                // Disconnect and close clients
                CallCloseOnClients();
                // Return to main menu
                MySessionLoader.UnloadAndExitToMenu();
                
                // Call event OnCampaignFinished
                MyCampaignManager.Static.NotifyCampaignFinished();

                // Start Credits when the vanilla game ends.
                if(MyCampaignManager.Static.ActiveCampaign.IsVanilla)
                {
                    MyScreenManager.AddScreen(new MyGuiScreenGameCredits());
                }

                return;
            }

            // Load new level if campaign mode is still active
            // In case of exit to main menu
            UpdateStateMachine();

            // Check the state data
            var currentCampaignNode = m_runningCampaignSM.CurrentNode as MyCampaignStateMachineNode;
            Debug.Assert(currentCampaignNode != null);
            var sessionPathToLoad = currentCampaignNode.SavePath;
            Debug.Assert(!string.IsNullOrEmpty(sessionPathToLoad), "ERROR: Missing campaign world file!");

            // Reconnect clients to new session
            CallReconnectOnClients();

            // Load new session and add this session component to it
            MyCampaignManager.Static.LoadSessionFromActiveCampaign(sessionPathToLoad, () =>
            {
                // Recycle this component
                MySession.Static.RegisterComponent(this, MyUpdateOrder.NoUpdate, 555);
                LoadPlayersInventories();
            }, folderName
            );
        }

        // Loads the data from campaign manager
        public void InitFromActive()
        {
            LoadCampaignStateMachine();
        }
        #endregion

        #region Session Component methods

        // Load of campaign mission
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            // This session component is irrelevant for clients
            if(MyMultiplayer.Static != null && !MyMultiplayer.Static.IsServer)
                return;

            // Do nothing for empty object builder
            var ob = sessionComponent as MyObjectBuilder_CampaignSessionComponent;
            if (ob == null || string.IsNullOrEmpty(ob.CampaignName))
                return;

            // Restore the SM for valid OB data
            CampaignLevelOutcome = ob.CurrentOutcome;
            MyCampaignManager.Static.SwitchCampaign(ob.CampaignName, ob.IsVanilla, ob.Mod.PublishedFileId != 0);
            LoadCampaignStateMachine(ob.ActiveState);
        }

        // Save campaign mission
        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_CampaignSessionComponent;
            if (ob != null && Running)
            {
                ob.ActiveState = m_runningCampaignSM.CurrentNode.Name;
                ob.CampaignName = MyCampaignManager.Static.ActiveCampaign.Name;
                ob.CurrentOutcome = CampaignLevelOutcome;
                ob.IsVanilla = MyCampaignManager.Static.ActiveCampaign.IsVanilla;
            }

            return ob;
        }

        #endregion

        // Raises reconnect on clients
        private void CallReconnectOnClients()
        {
            foreach (var onlinePlayer in MySession.Static.Players.GetOnlinePlayers())
            {
                if(onlinePlayer.Identity.IdentityId != MySession.Static.LocalPlayerId)
                    MyMultiplayer.RaiseStaticEvent(s => Reconnect, new EndpointId(onlinePlayer.Id.SteamId));
            }
        }
        // Raises Close on clients
        private void CallCloseOnClients()
        {
            foreach (var onlinePlayer in MySession.Static.Players.GetOnlinePlayers())
            {
                if (onlinePlayer.Identity.IdentityId != MySession.Static.LocalPlayerId)
                    MyMultiplayer.RaiseStaticEvent(s => CloseGame, new EndpointId(onlinePlayer.Id.SteamId));
            }
        }
        // server owner id
        static ulong m_ownerId;
        // id of old lobby
        static ulong m_oldLobbyId;
        // time taken for reconnection
        static ulong m_elapsedMs;

        [Event, Reliable, Client]
        private static void Reconnect()
        {
            // Store previous server data
            m_ownerId = MyMultiplayer.Static.ServerId;
            m_elapsedMs = 0;
            m_oldLobbyId = (MyMultiplayer.Static as MyMultiplayerLobbyClient).LobbyId;

            // Get to main menu
            MySessionLoader.UnloadAndExitToMenu();
            // Show loading wheel
            StringBuilder text = MyTexts.Get(MyCommonTexts.LoadingDialogServerIsLoadingWorld);
            var progress = new MyGuiScreenProgress(text, MyCommonTexts.Cancel);
            MyGuiSandbox.AddScreen(progress);
            // Start checking for new lobby
            Parallel.Start(FindLobby);
        }

        [Event, Reliable, Client]
        private static void CloseGame()
        {
            MySessionLoader.UnloadAndExitToMenu();
        }

        private static void FindLobby()
        {
            // Wait 5s
            Thread.Sleep(5000);

            // Request lobbies from steam
            LobbySearch.AddRequestLobbyListNumericalFilter(
            MyMultiplayer.AppVersionTag,
            MyFinalBuildConstants.APP_VERSION,
            LobbyComparison.LobbyComparisonEqual
            );

            LobbySearch.RequestLobbyList(LobbiesRequestCompleted);
        }

        private static void LobbiesRequestCompleted(Result result)
        {
            // if the request is ok
            if(result != Result.OK) return;

            var lobbies = new List<Lobby>();
            // Add all lobbies
            LobbySearch.AddPublicLobbies(lobbies);
            LobbySearch.AddFriendLobbies(lobbies);

            // search for new lobby
            foreach (var lobby in lobbies)
            {
                var owner = MyMultiplayerLobby.GetLobbyHostSteamId(lobby);
                if (owner == m_ownerId && lobby.LobbyId != m_oldLobbyId)
                {
                    //
                    MyScreenManager.RemoveScreenByType(typeof(MyGuiScreenProgress));
                    // Join the game
                    MyJoinGameHelper.JoinGame(lobby);
                    return;
                }
            }

            // Exit if the search is taking too long
            m_elapsedMs += 5000;
            if(m_elapsedMs > 120000)
            {
                // Remove the progress wheel
                MyScreenManager.RemoveScreenByType(typeof(MyGuiScreenProgress));
                return;
            }

            // Repeat the search
            FindLobby();
        }
    }
}
