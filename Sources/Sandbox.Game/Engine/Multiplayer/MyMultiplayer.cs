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
using VRage.Library.Collections;
using VRage.Network;
using VRage.Trace;


#endregion

namespace Sandbox.Engine.Multiplayer
{
    public static class MyMultiplayer
    {
        public const int ControlChannel = 0;
        public const int WorldDownloadChannel = 1;
        public const int GameEventChannel = 2;
        public const int VoiceChatChannel = 3;

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
        public const string BattleCanBeJoinedTag = "battleCanBeJoined";
        public const string BattleWorldWorkshopIdTag = "battleWorldWorkshopId";
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

        public const string ScenarioTag = "scenario";
        public const string ScenarioBriefingTag = "scenarioBriefing";
        public const string ScenarioStartTimeTag = "scenarioStartTime";

        public static MyMultiplayerBase Static;

        private static MyReplicationSingle m_replicationOffline;

        public static float ReplicationDistance
        {
            get { return MyFakes.MULTIPLAYER_REPLICATION_TEST ? 100 : MySession.Static.Settings.ViewDistance; }
        }

        private static MyReplicationLayerBase ReplicationLayer
        {
            get
            {
                if (Static == null)
                {
                    if (m_replicationOffline == null)
                    {
                        m_replicationOffline = new MyReplicationSingle();
                        m_replicationOffline.RegisterFromGameAssemblies();
                    }
                    return m_replicationOffline;
                }
                return Static.ReplicationLayer;
            }
        }

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
                    ret.RaiseJoined(result, info, success ? MyMultiplayer.Static = new MyMultiplayerLobbyClient(info.Lobby, new MySyncLayer(new MyTransportLayer(MyMultiplayer.GameEventChannel))) : null);
                }
            });
            return ret;
        }

        /// <summary>
        /// Raises static multiplayer event.
        /// Usage: MyMultiplayer.RaiseStaticEvent(s => MyClass.MyStaticFunction);
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseStaticEvent(Func<IMyEventOwner, Action> action, EndpointId targetEndpoint = default(EndpointId))
        {
            ReplicationLayer.RaiseEvent((IMyEventOwner)null, action, targetEndpoint);
        }

        /// <summary>
        /// Raises static multiplayer event.
        /// Usage: MyMultiplayer.RaiseStaticEvent(s => MyClass.MyStaticFunction, arg);
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseStaticEvent<T2>(Func<IMyEventOwner, Action<T2>> action, T2 arg2, EndpointId targetEndpoint = default(EndpointId))
        {
            ReplicationLayer.RaiseEvent((IMyEventOwner)null, action, arg2, targetEndpoint);
        }

        /// <summary>
        /// Raises static multiplayer event.
        /// Usage: MyMultiplayer.RaiseStaticEvent(s => MyClass.MyStaticFunction, arg2, arg3);
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseStaticEvent<T2, T3>(Func<IMyEventOwner, Action<T2, T3>> action, T2 arg2, T3 arg3, EndpointId targetEndpoint = default(EndpointId))
        {
            ReplicationLayer.RaiseEvent((IMyEventOwner)null, action, arg2, arg3, targetEndpoint);
        }

        /// <summary>
        /// Raises static multiplayer event.
        /// Usage: MyMultiplayer.RaiseStaticEvent(s => MyClass.MyStaticFunction, arg2, arg3, arg4);
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseStaticEvent<T2, T3, T4>(Func<IMyEventOwner, Action<T2, T3, T4>> action, T2 arg2, T3 arg3, T4 arg4, EndpointId targetEndpoint = default(EndpointId))
        {
            ReplicationLayer.RaiseEvent((IMyEventOwner)null, action, arg2, arg3, arg4, targetEndpoint);
        }

        /// <summary>
        /// Raises static multiplayer event.
        /// Usage: MyMultiplayer.RaiseStaticEvent(s => MyClass.MyStaticFunction, arg2, arg3, arg4, arg5);
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseStaticEvent<T2, T3, T4, T5>(Func<IMyEventOwner, Action<T2, T3, T4, T5>> action, T2 arg2, T3 arg3, T4 arg4, T5 arg5, EndpointId targetEndpoint = default(EndpointId))
        {
            ReplicationLayer.RaiseEvent((IMyEventOwner)null, action, arg2, arg3, arg4, arg5, targetEndpoint);
        }

        /// <summary>
        /// Raises static multiplayer event.
        /// Usage: MyMultiplayer.RaiseStaticEvent(s => MyClass.MyStaticFunction, arg2, arg3, arg4, arg5, arg6);
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseStaticEvent<T2, T3, T4, T5, T6>(Func<IMyEventOwner, Action<T2, T3, T4, T5, T6>> action, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, EndpointId targetEndpoint = default(EndpointId))
        {
            ReplicationLayer.RaiseEvent((IMyEventOwner)null, action, arg2, arg3, arg4, arg5, arg6, targetEndpoint);
        }

        /// <summary>
        /// Raises static multiplayer event.
        /// Usage: MyMultiplayer.RaiseStaticEvent(s => MyClass.MyStaticFunction, arg2, arg3, arg4, arg5, arg6);
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseStaticEvent<T2, T3, T4, T5, T6, T7>(Func<IMyEventOwner, Action<T2, T3, T4, T5, T6, T7>> action, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, EndpointId targetEndpoint = default(EndpointId))
        {
            ReplicationLayer.RaiseEvent((IMyEventOwner)null, action, arg2, arg3, arg4, arg5, arg6, arg7, targetEndpoint);
        }

        /// <summary>
        /// Raises multiplayer event.
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseEvent<T1>(T1 arg1, Func<T1, Action> action, EndpointId targetEndpoint = default(EndpointId))
            where T1 : IMyEventOwner
        {
            ReplicationLayer.RaiseEvent(arg1, action, targetEndpoint);
        }

        /// <summary>
        /// Raises multiplayer event.
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseEvent<T1, T2>(T1 arg1, Func<T1, Action<T2>> action, T2 arg2, EndpointId targetEndpoint = default(EndpointId))
            where T1 : IMyEventOwner
        {
            ReplicationLayer.RaiseEvent(arg1, action, arg2, targetEndpoint);
        }

        /// <summary>
        /// Raises multiplayer event.
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseEvent<T1, T2, T3>(T1 arg1, Func<T1, Action<T2, T3>> action, T2 arg2, T3 arg3, EndpointId targetEndpoint = default(EndpointId))
            where T1 : IMyEventOwner
        {
            ReplicationLayer.RaiseEvent(arg1, action, arg2, arg3, targetEndpoint);
        }

        /// <summary>
        /// Raises multiplayer event.
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseEvent<T1, T2, T3, T4>(T1 arg1, Func<T1, Action<T2, T3, T4>> action, T2 arg2, T3 arg3, T4 arg4, EndpointId targetEndpoint = default(EndpointId))
            where T1 : IMyEventOwner
        {
            ReplicationLayer.RaiseEvent(arg1, action, arg2, arg3, arg4, targetEndpoint);
        }

        /// <summary>
        /// Raises multiplayer event.
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseEvent<T1, T2, T3, T4, T5>(T1 arg1, Func<T1, Action<T2, T3, T4, T5>> action, T2 arg2, T3 arg3, T4 arg4, T5 arg5, EndpointId targetEndpoint = default(EndpointId))
            where T1 : IMyEventOwner
        {
            ReplicationLayer.RaiseEvent(arg1, action, arg2, arg3, arg4, arg5, targetEndpoint);
        }

        /// <summary>
        /// Raises multiplayer event.
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseEvent<T1, T2, T3, T4, T5, T6>(T1 arg1, Func<T1, Action<T2, T3, T4, T5, T6>> action, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, EndpointId targetEndpoint = default(EndpointId))
            where T1 : IMyEventOwner
        {
            ReplicationLayer.RaiseEvent(arg1, action, arg2, arg3, arg4, arg5, arg6, targetEndpoint);
        }

        /// <summary>
        /// Raises multiplayer event.
        /// </summary>
        /// <param name="targetEndpoint">Target of the event. When broadcasting, it's exclude endpoint.</param>
        public static void RaiseEvent<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, Func<T1, Action<T2, T3, T4, T5, T6, T7>> action, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, EndpointId targetEndpoint = default(EndpointId))
            where T1 : IMyEventOwner
        {
            ReplicationLayer.RaiseEvent(arg1, action, arg2, arg3, arg4, arg5, arg6, arg7, targetEndpoint);
        }

        private static MyReplicationServer GetReplicationServer()
        {
            if (Static != null)
            {
                var server = Static.ReplicationLayer as MyReplicationServer;
                Debug.Assert(server != null, "Cannot replicate from client!");
                return server;
            }
            return null;
        }

        /// <summary>
        /// This is hack for immediate replication, it's necessary because of logic dependency.
        /// E.g. Character is created on server, sent to client and respawn message sent immediatelly.
        /// Should be called only on server.
        /// </summary>
        /// <param name="replicable">Replicable to replicate to clients</param>
        /// <param name="dependency">Replicable will be replicated only to clients who has dependency.</param>
        public static void ReplicateImmediatelly(IMyReplicable replicable, IMyReplicable dependency = null)
        {
            var server = GetReplicationServer();
            if (server != null)
            {
                Debug.Assert(replicable != null, "Replicable cannot be null");
                server.ForceReplicable(replicable, dependency);
            }
        }

        /// <summary>
        /// This is hack for immediate replication, it's necessary because of logic dependency.
        /// E.g. Character is created on server, sent to client and respawn message sent immediatelly.
        /// Should be called only on server.
        /// </summary>
        /// <param name="replicable">Replicable to replicate.</param>
        /// <param name="clientEndpoint">Client who will receive the replicable immediatelly.</param>
        public static void ReplicateImmediatelly(IMyReplicable replicable, EndpointId clientEndpoint)
        {
            var server = GetReplicationServer();
            if (server != null)
            {
                Debug.Assert(replicable != null, "Replicable cannot be null");
                server.ForceReplicable(replicable, clientEndpoint);
            }
        }
    }
}
