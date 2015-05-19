#region Using


using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using VRage.Trace;


#endregion

namespace Sandbox.Engine.Multiplayer
{
    public static class MyMultiplayer 
    {
        public const int ControlChannel = 0;
        public const int WorldDownloadChannel = 1;
        public const int GameEventChannel = 2;

        public const string HostNameTag = "host";
        public const string WorldNameTag = "world";
        public const string WorldSizeTag = "worldSize";
        public const string AppVersionTag = "appVersion";
        public const string GameModeTag = "gameMode";
        public const string DataHashTag = "dataHash";
        public const string ModCountTag = "mods";
        public const string ModItemTag = "mod";
        public const string ViewDistanceTag = "view";
        public const string InventoryMultiplierTag = "inventoryMultiplier";
        public const string AssemblerMultiplierTag = "assemblerMultiplier";
        public const string RefineryMultiplierTag = "refineryMultiplier";
        public const string WelderMultiplierTag = "welderMultiplier";
        public const string GrinderMultiplierTag = "grinderMultiplier";

        public const string BattleTag = "battle";
        public const string BattleStartedTag = "battleStarted";
        public const string BattleFaction1MaxBlueprintPointsTag = "battleFaction1MaxBlueprintPoints";
        public const string BattleFaction2MaxBlueprintPointsTag = "battleFaction2MaxBlueprintPoints";
        public const string BattleFaction1BlueprintPointsTag = "battleFaction1BlueprintPoints";
        public const string BattleFaction2BlueprintPointsTag = "battleFaction2BlueprintPoints";
        public const string BattleMapAttackerSlotsCountTag = "battleMapAttackerSlotsCount";
        public const string BattleFaction1IdTag = "battleFaction1Id";
        public const string BattleFaction2IdTag = "battleFaction2Id";
        public const string BattleFaction1SlotTag = "battleFaction1Slot";
        public const string BattleFaction2SlotTag = "battleFaction2Slot";
        public const string BattleFaction1ReadyTag = "battleFaction1Ready";
        public const string BattleFaction2ReadyTag = "battleFaction2Ready";
        public const string BattleTimeLimitTag = "battleTimeLimit";

        public static MyMultiplayerBase Static;

        public static MyMultiplayerHostResult HostLobby(LobbyTypeEnum lobbyType, int maxPlayers, MySyncLayer syncLayer)
        {
            System.Diagnostics.Debug.Assert(syncLayer != null);
            MyTrace.Send(TraceWindow.Multiplayer, "Host game");

            MyMultiplayerHostResult ret = new MyMultiplayerHostResult();
            SteamSDK.Lobby.Create(lobbyType, maxPlayers, (lobby, result) =>
            {
                if (!ret.Cancelled)
                {
                    if (result == Result.OK && lobby.GetOwner() != MySteam.UserId)
                    {
                        result = Result.Fail;
                        lobby.Leave();
                    }

                    MyTrace.Send(TraceWindow.Multiplayer, "Lobby created");
                    lobby.SetLobbyType(lobbyType);
                    ret.RaiseDone(result, result == Result.OK ? MyMultiplayer.Static = new MyMultiplayerLobby(lobby, syncLayer) : null);
                }
            });
            return ret;
        }

        public static MyMultiplayerJoinResult JoinLobby(ulong lobbyId)
        {
            MyTrace.Send(TraceWindow.Multiplayer, "Join game");
            MyMultiplayerJoinResult ret = new MyMultiplayerJoinResult();
            Lobby.Join(lobbyId, (info, result) =>
            {
                if (!ret.Cancelled)
                {
                    if (result == Result.OK && info.EnterState == LobbyEnterResponseEnum.Success && info.Lobby.GetOwner() == MySteam.UserId)
                    {
                        // Joining lobby as server is dead-end, nobody has world. It's considered doesn't exists
                        info.EnterState = LobbyEnterResponseEnum.DoesntExist;
                        info.Lobby.Leave();
                    }

                    MyTrace.Send(TraceWindow.Multiplayer, "Lobby joined");
                    bool success = result == Result.OK && info.EnterState == LobbyEnterResponseEnum.Success;
                    ret.RaiseJoined(result, info, success ? MyMultiplayer.Static = new MyMultiplayerLobby(info.Lobby, new MySyncLayer(new MyTransportLayer(MyMultiplayer.GameEventChannel))) : null);
                }
            });
            return ret;
        }
    }
}
