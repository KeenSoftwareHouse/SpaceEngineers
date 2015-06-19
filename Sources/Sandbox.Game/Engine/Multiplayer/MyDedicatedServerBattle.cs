using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using SteamSDK;

namespace Sandbox.Engine.Multiplayer
{
    public class MyDedicatedServerBattle : MyDedicatedServerBase
    {
        private MyMultiplayerBattleData m_battleData = new MyMultiplayerBattleData();

        public override float InventoryMultiplier
        {
            get;
            set;
        }

        public override float AssemblerMultiplier
        {
            get;
            set;
        }

        public override float RefineryMultiplier
        {
            get;
            set;
        }

        public override float WelderMultiplier
        {
            get;
            set;
        }

        public override float GrinderMultiplier
        {
            get;
            set;
        }

        public override bool Scenario
        {
            get;
            set;
        }

        public override string ScenarioBriefing
        {
            get;
            set;
        }

        public override DateTime ScenarioStartTime
        {
            get;
            set;
        }

        public override bool Battle
        {
            get { return true; }
            set { }
        }

        public override bool BattleCanBeJoined
        {
            get;
            set;
        }

        public override ulong BattleWorldWorkshopId
        {
            get;
            set;
        }

        public override int BattleFaction1MaxBlueprintPoints
        {
            get;
            set;
        }

        public override int BattleFaction2MaxBlueprintPoints
        {
            get;
            set;
        }

        public override int BattleFaction1BlueprintPoints
        {
            get;
            set;
        }

        public override int BattleFaction2BlueprintPoints
        {
            get;
            set;
        }

        public override int BattleMapAttackerSlotsCount
        {
            get;
            set;
        }

        public override long BattleFaction1Id
        {
            get;
            set;
        }

        public override long BattleFaction2Id
        {
            get;
            set;
        }

        public override int BattleFaction1Slot
        {
            get;
            set;
        }

        public override int BattleFaction2Slot
        {
            get;
            set;
        }

        public override bool BattleFaction1Ready
        {
            get;
            set;
        }

        public override bool BattleFaction2Ready
        {
            get;
            set;
        }

        public override int BattleTimeLimit
        {
            get;
            set;
        }


        internal MyDedicatedServerBattle(IPEndPoint serverEndpoint)
            : base(new MySyncLayer(new MyTransportLayer(MyMultiplayer.GameEventChannel)))
        {
            RegisterControlMessage<ChatMsg>(MyControlMessageEnum.Chat, OnChatMessage);
            RegisterControlMessage<ServerDataMsg>(MyControlMessageEnum.ServerData, OnServerData);
            RegisterControlMessage<JoinResultMsg>(MyControlMessageEnum.JoinResult, OnJoinResult);

            Initialize(serverEndpoint);
        }

        internal override void SendGameTagsToSteam()
        {
            //RKTODO
            if (SteamSDK.SteamServerAPI.Instance != null)
            {
                var serverName = MySandboxGame.ConfigDedicated.ServerName.Replace(":", "a58").Replace(";", "a59");

                var gamemode = new StringBuilder();

                switch (GameMode)
                {
                    case MyGameModeEnum.Survival:
                        gamemode.Append(String.Format("S{0}-{1}-{2}", (int)InventoryMultiplier, (int)AssemblerMultiplier, (int)RefineryMultiplier));
                        break;
                    case MyGameModeEnum.Creative:
                        gamemode.Append("C");
                        break;

                    default:
                        Debug.Fail("Unknown game type");
                        break;
                }

                SteamSDK.SteamServerAPI.Instance.GameServer.SetGameTags(
                    "groupId" + m_groupId.ToString() +
                    " version" + MyFinalBuildConstants.APP_VERSION.ToString() +
                    " datahash" + MyDataIntegrityChecker.GetHashBase64() +
                    " " + MyMultiplayer.ModCountTag + ModCount +
                    " gamemode" + gamemode +
                    " " + MyMultiplayer.ViewDistanceTag + ViewDistance);
            }
        }

        protected override void SendServerData()
        {
            //RKTODO
        }

        void OnChatMessage(ref ChatMsg msg, ulong sender)
        {
            if (m_memberData.ContainsKey(sender))
            {
                if (m_memberData[sender].IsAdmin)
                {
                    if (msg.Text.ToLower().Contains("+unban"))
                    {
                        string[] parts = msg.Text.Split(' ');
                        if (parts.Length > 1)
                        {
                            ulong user = 0;
                            if (ulong.TryParse(parts[1], out user))
                            {
                                BanClient(user, false);
                            }
                        }
                    }
                }
            }

            RaiseChatMessageReceived(sender, msg.Text, ChatEntryTypeEnum.ChatMsg);
        }

        void OnServerData(ref ServerDataMsg msg, ulong sender)
        {
            System.Diagnostics.Debug.Fail("None can send server data to server");
        }

        void OnJoinResult(ref JoinResultMsg msg, ulong sender)
        {
            System.Diagnostics.Debug.Fail("Server cannot join anywhere!");
        }


    }
}
