using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Library.Utils;
using VRage.Serialization;
using VRage.Trace;
using VRageMath;
using VRageRender;
using PlayerId = Sandbox.Game.World.MyPlayer.PlayerId;

namespace Sandbox.Game.Multiplayer
{
    public class PlayerRequestArgs
    {
        public PlayerId PlayerId;
        public bool Cancel;

        public PlayerRequestArgs(PlayerId playerId)
        {
            PlayerId = playerId;
            Cancel = false;
        }
    }

    public delegate void NewPlayerCreatedDelegate(PlayerId playerId);
    public delegate void PlayerRequestDelegate(PlayerRequestArgs args);

    [PreloadRequired]
    public partial class MyPlayerCollection : MyIdentity.Friend
    {
        #region Sync messages
        [MessageId(1, P2PMessageEnum.Reliable)]
        struct ControlChangedMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public ulong ClientSteamId;
            public int PlayerSerialId;
        }

        [MessageId(5, P2PMessageEnum.Reliable)]
        struct CharacterChangedMsg : IEntityMessage
        {
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            public long ControlledEntityId;
            public ulong ClientSteamId;
            public int PlayerSerialId;
        }

        [MessageId(9, P2PMessageEnum.Reliable)]
        struct ControlReleasedMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
        }

        [ProtoContract]
        [MessageId(13, P2PMessageEnum.Reliable)]
        struct IdentityCreatedMsg
        {
            [ProtoMember]
            public bool IsNPC;

            [ProtoMember]
            public long IdentityId;

            [ProtoMember]
            public string DisplayName;

            [ProtoMember]
            public string Model;
        }

        [MessageId(20, P2PMessageEnum.Reliable)]
        struct PlayerIdentityChangedMsg
        {
            public ulong ClientSteamId;
            public int PlayerSerialId;
            public long IdentityId;
        }

        // This is the only way to set the identity dead or alive - over the controller.
        // Uncontrolled identities should always be dead
        [MessageId(21, P2PMessageEnum.Reliable)]
        struct SetPlayerDeadMsg
        {
            public ulong ClientSteamId;
            public int PlayerSerialId;
            public BoolBlit IsDead;
            public BoolBlit ResetIdentity;
        }

        [MessageId(22, P2PMessageEnum.Reliable)]
        [ProtoContract]
        public struct RespawnMsg
        {
            [ProtoMember]
            public bool JoinGame;
            [ProtoMember]
            public bool NewIdentity;
            [ProtoMember]
            public long MedicalRoom;
            [ProtoMember]
            public string RespawnShipId;
            [ProtoMember]
            public int PlayerSerialId;
            [ProtoMember]
            public Vector3D? SpawnPosition;
        }

        [MessageId(23, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct NewPlayerRequestMsg
        {
            [ProtoMember]
            public ulong ClientSteamId;
            [ProtoMember]
            public int PlayerSerialId;
            [ProtoMember]
            public string DisplayName;
            [ProtoMember]
            public string CharacterModel;
        }

        [MessageId(24, P2PMessageEnum.Reliable)]
        struct NewPlayerSuccessMsg
        {
            public ulong ClientSteamId;
            public int PlayerSerialId;
        }

        [MessageId(33, P2PMessageEnum.Reliable)]
        struct NewPlayerFailureMsg
        {
            public ulong ClientSteamId;
            public int PlayerSerialId;
        }

        [MessageId(30, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct PlayerCreatedMsg
        {
            [ProtoMember]
            public ulong ClientSteamId;
            [ProtoMember]
            public int PlayerSerialId;
            [ProtoMember]
            public long IdentityId;
            [ProtoMember]
            public string DisplayName;
			[ProtoMember]
			public List<Vector3> BuildColors;
        }

        [ProtoContract]
        [MessageId(31, P2PMessageEnum.Reliable)]
        struct PlayerRemoveMsg
        {
            [ProtoMember]
            public ulong ClientSteamId;
            [ProtoMember]
            public int PlayerSerialId;
            [ProtoMember]
            public bool RemoveCharacter;
        }

        [MessageId(7351, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct All_Identities_Players_Factions_RequestMsg
        {
            [ProtoMember]
            public ulong ClientSteamId;
            [ProtoMember]
            public int PlayerSerialId;
        }

        [MessageId(7352, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct All_Identities_Players_Factions_SuccessMsg
        {
            [ProtoMember]
            public List<MyObjectBuilder_Identity> Identities;
            [ProtoMember]
            public List<AllPlayerData> Players;
            [ProtoMember]
            public List<MyObjectBuilder_Faction> Factions;
        }

		[MessageId(7353, P2PMessageEnum.Reliable)]
		[ProtoContract]
		struct PlayerChangeColorMsg
		{
			[ProtoMember]
			public int SerialId;

			[ProtoMember]
			public int ColorIndex;

			[ProtoMember]
			public Vector3 NewColor;
		}

		[MessageId(7354, P2PMessageEnum.Reliable)]
		[ProtoContract]
		struct PlayerChangeColorsMsg
		{
			[ProtoMember]
			public int SerialId;

			[ProtoMember]
			public List<Vector3> NewColors;
		}

        [ProtoContract]
        public struct AllPlayerData 
        {
            [ProtoMember]
            public ulong SteamId;
            [ProtoMember]
            public int SerialId;
            [ProtoMember]
            public MyObjectBuilder_Player Player;
        }

        [MessageId(7355, P2PMessageEnum.Reliable)]
        struct IdentityRemovedMsg
        {
            [ProtoMember]
            public long IdentityId;
            [ProtoMember]
            public ulong SteamId;
            [ProtoMember]
            public int SerialId;
        }

        [MessageId(7356, P2PMessageEnum.Reliable)]
        struct NewNpcIdentityMsg
        {
            [ProtoMember]
            public long IdentityId; // Ignored in request
        }
        
        public delegate void RespawnRequestedDelegate(ref RespawnMsg respawnMsg, MyNetworkClient client);

        #endregion

        #region Fields

        // Collection of players, indexed by the player ID. If a player is not here, it means that the player is not online right now
        private Dictionary<PlayerId, MyPlayer> m_players = new Dictionary<PlayerId, MyPlayer>();
        private List<MyPlayer> m_tmpRemovedPlayers = new List<MyPlayer>();

        // Which entity is controlled by which player id
        private CachingDictionary<long, PlayerId> m_controlledEntities = new CachingDictionary<long, PlayerId>();

        // All identities in the game - controlled or uncontrolled, living or dead
        private Dictionary<long, MyIdentity> m_allIdentities = new Dictionary<long, MyIdentity>();
        // All identities that were controlled by a player are here - currently controlled or not.
        // Only one identity per player is allowed. If a player wants another one, the old one should be removed from this
        // dictionary and stay only in m_allIdentities (i.e. death of old identity)
        private Dictionary<PlayerId, long> m_playerIdentityIds = new Dictionary<PlayerId, long>();

        // These identities are NPCs generated by the player and can be assigned ownership of blocks
        private HashSet<long> m_npcIdentities = new HashSet<long>();

        Action<MyEntity> m_entityClosingHandler;

        // The game component that handles respawns. This can be different per each game
        public MyRespawnComponentBase RespawnComponent { get; set; }

        #endregion

        public event Action<int> NewPlayerRequestSucceeded;
        public event Action<int> NewPlayerRequestFailed;
        public event Action<int> LocalPlayerRemoved;
        public event Action<int> LocalPlayerLoaded;
        public event Action LocalRespawnRequested;
        public event Action<PlayerId> PlayerRemoved;

        public event PlayerRequestDelegate PlayerRequesting;

        public event Action<bool, PlayerId> PlayersChanged;

        public event Action<long> PlayerCharacterDied;
        public event Action IdentitiesChanged;

        public DictionaryReader<long, PlayerId> ControlledEntities
        {
            get { return m_controlledEntities.Reader; }
        }

        #region Construction & (de)serialization

        static MyPlayerCollection()
        {
            MySyncLayer.RegisterMessage<ControlChangedMsg>(OnControlChangedRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ControlChangedMsg>(OnControlChangedSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterEntityMessage<MySyncEntity, CharacterChangedMsg>(OnCharacterChanged, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<ControlReleasedMsg>(OnControlReleased, MyMessagePermissions.ToServer|MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<IdentityCreatedMsg>(OnIdentityCreated, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<IdentityRemovedMsg>(OnIdentityRemoved, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<IdentityRemovedMsg>(OnIdentityRemoved, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<PlayerIdentityChangedMsg>(OnPlayerIdentityChanged, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<RespawnMsg>(OnRespawnRequest, MyMessagePermissions.ToServer);
            MySyncLayer.RegisterMessage<RespawnMsg>(OnRespawnRequestFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);
            MySyncLayer.RegisterMessage<SetPlayerDeadMsg>(OnSetPlayerDeadRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<SetPlayerDeadMsg>(OnSetPlayerDeadSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<NewPlayerRequestMsg>(OnNewPlayerRequest, MyMessagePermissions.ToServer);
            MySyncLayer.RegisterMessage<NewPlayerSuccessMsg>(OnNewPlayerSuccess, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<NewPlayerFailureMsg>(OnNewPlayerFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);
            MySyncLayer.RegisterMessage<PlayerCreatedMsg>(OnPlayerCreated, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<PlayerRemoveMsg>(OnPlayerRemoveRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<PlayerRemoveMsg>(OnPlayerRemoved, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
			MySyncLayer.RegisterMessage<PlayerChangeColorMsg>(OnPlayerColorChangedRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
			MySyncLayer.RegisterMessage<PlayerChangeColorsMsg>(OnPlayerColorsChangedRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<NewNpcIdentityMsg>(OnNpcIdentityRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<NewNpcIdentityMsg>(OnNpcIdentitySuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<All_Identities_Players_Factions_RequestMsg>(OnAll_Identities_Players_Factions_Request, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<All_Identities_Players_Factions_SuccessMsg>(OnAll_Identities_Players_Factions_Success, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public MyPlayerCollection()
        {
            m_entityClosingHandler = new Action<MyEntity>(EntityClosing);
        }

        public void LoadIdentities(MyObjectBuilder_Checkpoint checkpoint, MyPlayer.PlayerId? savingPlayerId = null)
        {
            if (checkpoint.NonPlayerIdentities != null)
            {
                LoadNpcIdentities(checkpoint.NonPlayerIdentities);
            }

            //checkpoint.Identities or checkpoint.AllPlayers will never be null, because it's a .NET bug :-(
            if (checkpoint.AllPlayers.Count != 0)
            {
                Debug.Assert(checkpoint.Identities.Count == 0, "Both AllPlayers and Identities were present in checkpoint object builder!");
                LoadIdentitiesObsolete(checkpoint.AllPlayers, savingPlayerId);
            }
            else
            {
                LoadIdentities(checkpoint.Identities);
            }
        }

        private void LoadNpcIdentities(List<long> list)
        {
            foreach (var identityId in list)
            {
                MarkIdentityAsNPC(identityId);
            }
        }

        public List<MyObjectBuilder_Identity> SaveIdentities()
        {
            var output = new List<MyObjectBuilder_Identity>();
            foreach (var identity in m_allIdentities)
            {
                output.Add(identity.Value.GetObjectBuilder());
            }
            return output;
        }

        public List<long> SaveNpcIdentities()
        {
            var output = new List<long>();
            foreach (var identityId in m_npcIdentities)
            {
                output.Add(identityId);
            }
            return output;
        }

        public void LoadControlledEntities(SerializableDictionaryCompat<long, MyObjectBuilder_Checkpoint.PlayerId, ulong> controlledEntities, long controlledObject, MyPlayer.PlayerId? savingPlayerId = null)
        {
            if (controlledEntities == null) return;

            foreach (var controlledEntityIt in controlledEntities.Dictionary)
            {
                MyEntity controlledEntity;
                MyEntities.TryGetEntityById(controlledEntityIt.Key, out controlledEntity);

                var playerId = new PlayerId(controlledEntityIt.Value.ClientId, controlledEntityIt.Value.SerialId);
                if (savingPlayerId != null && playerId == savingPlayerId) playerId = new PlayerId(MySteam.UserId);

                MyPlayer player = Sync.Players.GetPlayerById(playerId);

                if (player != null && controlledEntity != null)
                {
                    if (!MySandboxGame.IsDedicated && controlledEntity is IMyControllableEntity)
                    {
                        player.Controller.TakeControl(controlledEntity as IMyControllableEntity);
                        if (controlledEntity is MyCharacter)
                            player.Identity.ChangeCharacter(controlledEntity as MyCharacter);
                        if (controlledEntity is MyShipController)
                            player.Identity.ChangeCharacter((controlledEntity as MyShipController).Pilot);
                        if (controlledEntity is MyLargeTurretBase)
                            player.Identity.ChangeCharacter((controlledEntity as MyLargeTurretBase).Pilot);
                    }
                    else
                    {
                        m_controlledEntities.Add(controlledEntityIt.Key, playerId, true);
                    }
                }
            }
        }

        public SerializableDictionaryCompat<long, MyObjectBuilder_Checkpoint.PlayerId, ulong> SerializeControlledEntities()
        {
            var output = new SerializableDictionaryCompat<long,MyObjectBuilder_Checkpoint.PlayerId,ulong>();

            foreach (var entry in m_controlledEntities)
            {
                MyObjectBuilder_Checkpoint.PlayerId playerId = new MyObjectBuilder_Checkpoint.PlayerId();
                playerId.ClientId = entry.Value.SteamId;
                playerId.SerialId = entry.Value.SerialId;
                output.Dictionary.Add(entry.Key, playerId);
            }

            return output;
        }

        private void ChangeDisplayNameOfPlayerAndIdentity(MyObjectBuilder_Player playerOb, string name)
        {
            playerOb.DisplayName = MySteam.UserName;

            var identity = TryGetIdentity(playerOb.IdentityId);
            Debug.Assert(identity != null, "Identity of a player was null when loading! Inconsistency!");
            if (identity != null)
            {
                identity.SetDisplayName(MySteam.UserName);
            }
        }

        public void LoadPlayers(List<AllPlayerData> allPlayersData)
        {
            if (allPlayersData == null)
                return;

            foreach (var playerItem in allPlayersData)
            {
                var playerId = new PlayerId(playerItem.SteamId, playerItem.SerialId);
                LoadPlayerInternal(ref playerId, playerItem.Player, obsolete: false);
            }
        }

        public void LoadConnectedPlayers(MyObjectBuilder_Checkpoint checkpoint, MyPlayer.PlayerId? savingPlayerId = null)
        {
            #warning TODO: Probably not needed? If not, remove the method
            //long identityId = FindLocalIdentityId(checkpoint);

            // Backward compatibility
            if (checkpoint.AllPlayers != null && checkpoint.AllPlayers.Count != 0)
            {
                foreach (var playerItem in checkpoint.AllPlayers)
                {
                    long identityId = playerItem.PlayerId;

                    var playerOb = new MyObjectBuilder_Player();
                    playerOb.Connected = true;
                    playerOb.DisplayName = playerItem.Name;
                    playerOb.IdentityId = identityId;

                    var playerId = new PlayerId(playerItem.SteamId, 0);
                    if (savingPlayerId != null && playerId == savingPlayerId)
                    {
                        playerId = new PlayerId(MySteam.UserId);
                        ChangeDisplayNameOfPlayerAndIdentity(playerOb, MySteam.UserName);
                    }

                    LoadPlayerInternal(ref playerId, playerOb, obsolete: true);
                }
            }
            // Backward compatibility
            else if (checkpoint.ConnectedPlayers != null && checkpoint.ConnectedPlayers.Dictionary.Count != 0)
            {
                Debug.Assert(checkpoint.DisconnectedPlayers != null, "Inconsistency in save! ConnectedPlayers were present, but DisconnectedPlayers not!");
                foreach (var playerItem in checkpoint.ConnectedPlayers.Dictionary)
                {
                    var playerId = new PlayerId(playerItem.Key.ClientId, playerItem.Key.SerialId);
                    if (savingPlayerId != null && playerId == savingPlayerId)
                    {
                        playerId = new PlayerId(MySteam.UserId);
                        ChangeDisplayNameOfPlayerAndIdentity(playerItem.Value, MySteam.UserName);
                    }

                    playerItem.Value.Connected = true;
                    LoadPlayerInternal(ref playerId, playerItem.Value, obsolete: false);
                }

                foreach (var playerItem in checkpoint.DisconnectedPlayers.Dictionary)
                {
                    var playerId = new PlayerId(playerItem.Key.ClientId, playerItem.Key.SerialId);
                    
                    var playerOb = new MyObjectBuilder_Player();
                    playerOb.Connected = false;
                    playerOb.IdentityId = playerItem.Value;
                    playerOb.DisplayName = null;

                    if (savingPlayerId != null && playerId == savingPlayerId)
                    {
                        playerId = new PlayerId(MySteam.UserId);
                        ChangeDisplayNameOfPlayerAndIdentity(playerOb, MySteam.UserName);
                    }

                    LoadPlayerInternal(ref playerId, playerOb, obsolete: false);
                }

                //LoadDisconnectedPlayers(checkpoint.DisconnectedPlayers.Dictionary);
            }
            else if (checkpoint.AllPlayersData != null)
            {
                foreach (var playerItem in checkpoint.AllPlayersData.Dictionary)
                {
                    var playerId = new PlayerId(playerItem.Key.ClientId, playerItem.Key.SerialId);
                    if (savingPlayerId != null && playerId == savingPlayerId)
                    {
                        playerId = new PlayerId(MySteam.UserId);
                        ChangeDisplayNameOfPlayerAndIdentity(playerItem.Value, MySteam.UserName);
                    }

                    LoadPlayerInternal(ref playerId, playerItem.Value, obsolete: false);

					MyPlayer player = null;
					if(m_players.TryGetValue(playerId, out player))
					{
						List<Vector3> buildColors = null;
						if(checkpoint.AllPlayersColors != null && checkpoint.AllPlayersColors.Dictionary.TryGetValue(playerItem.Key, out buildColors))
						{
							player.SetBuildColorSlots(buildColors);
						}
						else if(checkpoint.CharacterToolbar != null && checkpoint.CharacterToolbar.ColorMaskHSVList != null && checkpoint.CharacterToolbar.ColorMaskHSVList.Count > 0) // Backwards compatibility
						{
							player.SetBuildColorSlots(checkpoint.CharacterToolbar.ColorMaskHSVList);
						}
					}
                }
            }

			if (MyCubeBuilder.AllPlayersColors != null && checkpoint.AllPlayersColors != null)
			{
				foreach(var colorPair in checkpoint.AllPlayersColors.Dictionary)
				{
					var playerId = new PlayerId(colorPair.Key.ClientId, colorPair.Key.SerialId);
					if(!MyCubeBuilder.AllPlayersColors.ContainsKey(playerId))
						MyCubeBuilder.AllPlayersColors.Add(playerId, colorPair.Value);
				}
			}
        }

        public void LoadDisconnectedPlayers(Dictionary<MyObjectBuilder_Checkpoint.PlayerId, long> dictionary)
        {
            foreach (var item in dictionary)
            {
                var playerId = new PlayerId(item.Key.ClientId, item.Key.SerialId);
                m_playerIdentityIds.Add(playerId, item.Value);
            }
        }

        public void SavePlayers(MyObjectBuilder_Checkpoint checkpoint)
        {
            checkpoint.ConnectedPlayers = new SerializableDictionary<MyObjectBuilder_Checkpoint.PlayerId, MyObjectBuilder_Player>();
            checkpoint.DisconnectedPlayers = new SerializableDictionary<MyObjectBuilder_Checkpoint.PlayerId, long>();
            checkpoint.AllPlayersData = new SerializableDictionary<MyObjectBuilder_Checkpoint.PlayerId, MyObjectBuilder_Player>();
			checkpoint.AllPlayersColors = new SerializableDictionary<MyObjectBuilder_Checkpoint.PlayerId, List<Vector3>>();

            foreach (var player in m_players.Values)
            {
                var id = new MyObjectBuilder_Checkpoint.PlayerId() { ClientId = player.Id.SteamId, SerialId = player.Id.SerialId };
                MyObjectBuilder_Player playerOb = player.GetObjectBuilder();

                checkpoint.AllPlayersData.Dictionary.Add(id, playerOb);
            }

            foreach (var identityPair in m_playerIdentityIds)
            {
				if (m_players.ContainsKey(identityPair.Key)) continue;

				var id = new MyObjectBuilder_Checkpoint.PlayerId() { ClientId = identityPair.Key.SteamId, SerialId = identityPair.Key.SerialId };
				var identity = TryGetIdentity(identityPair.Value);
                MyObjectBuilder_Player playerOb = new MyObjectBuilder_Player();
                playerOb.DisplayName = identity != null ? identity.DisplayName : null;
				playerOb.IdentityId = identityPair.Value;
                playerOb.Connected = false;
				if(MyCubeBuilder.AllPlayersColors != null)
					MyCubeBuilder.AllPlayersColors.TryGetValue(identityPair.Key, out playerOb.BuildColorSlots);

                checkpoint.AllPlayersData.Dictionary.Add(id, playerOb);
            }

			if (MyCubeBuilder.AllPlayersColors != null)
			{
				foreach (var colorPair in MyCubeBuilder.AllPlayersColors)
				{
					if (m_players.ContainsKey(colorPair.Key) || m_playerIdentityIds.ContainsKey(colorPair.Key)) continue;	// avoid data duplication in saves

					var id = new MyObjectBuilder_Checkpoint.PlayerId() { ClientId = colorPair.Key.SteamId, SerialId = colorPair.Key.SerialId };
					checkpoint.AllPlayersColors.Dictionary.Add(id, colorPair.Value);
				}
			}
        }

        public List<AllPlayerData> SavePlayers()
        {
            var allPlayersData = new List<AllPlayerData>();

            foreach (var player in m_players.Values)
            {
                AllPlayerData data = new AllPlayerData();
                data.SteamId = player.Id.SteamId;
                data.SerialId = player.Id.SerialId;

                MyObjectBuilder_Player playerOb = player.GetObjectBuilder();

                data.Player = playerOb;

                allPlayersData.Add(data);
            }

            return allPlayersData;
        }

        private void RemovePlayerFromDictionary(PlayerId playerId)
        {
            m_players.Remove(playerId);
            OnPlayersChanged(false, playerId);
        }

        private void AddPlayer(PlayerId playerId, MyPlayer newPlayer)
        {
            m_players.Add(playerId, newPlayer);
            OnPlayersChanged(true, playerId);
        }

        public void RegisterEvents()
        {
            Sync.Clients.ClientRemoved += Multiplayer_ClientRemoved;
        }

        public void UnregisterEvents()
        {
            if (Sync.Clients != null)
            {
                Sync.Clients.ClientRemoved -= Multiplayer_ClientRemoved;
            }
        }

        private void OnPlayersChanged(bool added, PlayerId playerId)
        {
            var handler = PlayersChanged;
            if (handler != null)
            {
                handler(added, playerId);
            }
        }

        public void ClearPlayers()
        {
            m_players.Clear();
            m_controlledEntities.Clear();
            m_playerIdentityIds.Clear();
        }

        #endregion

        #region Multiplayer handlers

        static void OnControlChangedRequest(ref ControlChangedMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            if (!MyEntities.TryGetEntityById(msg.EntityId, out entity))
                return;

            PlayerId id = new PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            MyTrace.Send(TraceWindow.Multiplayer, "OnControlChanged to entity: " + msg.EntityId, id.ToString());

            Sync.Players.SetControlledEntityInternal(id, entity);
            
            Debug.Assert(sender.SteamUserId == msg.ClientSteamId);
            msg.ClientSteamId = sender.SteamUserId;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void OnControlChangedSuccess(ref ControlChangedMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            if (!MyEntities.TryGetEntityById(msg.EntityId, out entity))
                return;

            PlayerId id = new PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            MyTrace.Send(TraceWindow.Multiplayer, "OnControlChanged to entity: " + msg.EntityId, id.ToString());

            Sync.Players.SetControlledEntityInternal(id, entity);
        }

        static void OnCharacterChanged(MySyncEntity entity, ref CharacterChangedMsg msg, MyNetworkClient sender)
        {
            MyCharacter characterEntity = entity.Entity as MyCharacter;
            MyEntity controlledEntity;
            if (!MyEntities.TryGetEntityById(msg.ControlledEntityId, out controlledEntity))
            {
                MySandboxGame.Log.WriteLine("Controlled entity not found");
                Debug.Fail("Controlled entity not found");
            }

            PlayerId id = new PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            MyPlayer player;
            if(!Sync.Players.TryGetPlayerById(id, out player))
            {
                MySandboxGame.Log.WriteLine("Player " + id + " not found");
                Debug.Fail("Player " + id + " not found");
            }

            ChangePlayerCharacterInternal(player, characterEntity, controlledEntity);
        }

        static void OnControlReleased(ref ControlReleasedMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            if (!MyEntities.TryGetEntityById(msg.EntityId, out entity))
                return;

            Sync.Players.RemoveControlledEntityInternal(entity);

            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAll(ref msg);
            }
        }

        static void OnIdentityCreated(ref IdentityCreatedMsg msg, MyNetworkClient sender)
        {
            if (msg.IsNPC)
            {
                Sync.Players.CreateNewNpcIdentity(msg.DisplayName);
            }
            else
            {
                Sync.Players.CreateNewIdentity(msg.DisplayName, msg.IdentityId, model: null);
            }
        }

        static void OnIdentityRemoved(ref IdentityRemovedMsg msg, MyNetworkClient sender)
        {
            if (Sync.IsServer)
            {
                Debug.Assert(msg.SteamId == sender.SteamUserId, "A client was requesting identity removal for a different client!");
                if (msg.SteamId != sender.SteamUserId) return;

                if (Sync.Players.RemoveIdentityInternal(msg.IdentityId, new PlayerId(msg.SteamId, msg.SerialId)))
                {
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                }
            }
            else
            {
                Sync.Players.RemoveIdentityInternal(msg.IdentityId, new PlayerId(msg.SteamId, msg.SerialId));
            }
        }

        static void OnPlayerIdentityChanged(ref PlayerIdentityChangedMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            var playerId = new PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            var player = Sync.Players.GetPlayerById(playerId);

            Debug.Assert(player != null, "Changing identity of an unknown or unconnected player!");
            if (player == null) return;

            MyIdentity identity = null;
            Sync.Players.m_allIdentities.TryGetValue(msg.IdentityId, out identity);
            Debug.Assert(identity != null, "Changing player's identity to an unknown identity!");
            if (identity == null) return;

            player.Identity = identity;
        }

        static void OnRespawnRequestFailure(ref RespawnMsg msg, MyNetworkClient sender)
        {
            if (msg.PlayerSerialId == 0)
            {
                MyPlayerCollection.RequestLocalRespawn();
            }
        }

        static void OnSetPlayerDeadRequest(ref SetPlayerDeadMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(msg.ClientSteamId == sender.SteamUserId, "Received invalid SetPlayerDeadMsg!");
            if (msg.ClientSteamId != sender.SteamUserId) return;

            if (Sync.Players.SetPlayerDeadInternal(msg.ClientSteamId, msg.PlayerSerialId, msg.IsDead, msg.ResetIdentity))
            {
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void OnSetPlayerDeadSuccess(ref SetPlayerDeadMsg msg, MyNetworkClient sender)
        {
            Sync.Players.SetPlayerDeadInternal(msg.ClientSteamId, msg.PlayerSerialId, msg.IsDead, msg.ResetIdentity);
        }

        static void OnNewPlayerRequest(ref NewPlayerRequestMsg msg, MyNetworkClient sender)
        {
            if (msg.ClientSteamId != sender.SteamUserId)
            {
                Debug.Assert(false, "A client requested player for another client!");
                return;
            }

            var playerId = new MyPlayer.PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            if (Sync.Players.m_players.ContainsKey(playerId))
                return;
            if (Sync.Players.PlayerRequesting != null)
            {
                var args = new PlayerRequestArgs(playerId);
                Sync.Players.PlayerRequesting(args);
                if (args.Cancel)
                {
                    var failMsg = new NewPlayerFailureMsg();
                    failMsg.ClientSteamId = msg.ClientSteamId;
                    failMsg.PlayerSerialId = msg.PlayerSerialId;

                    Sync.Layer.SendMessage(ref failMsg, sender.SteamUserId, MyTransportMessageEnum.Failure);
                    return;
                }
            } 

            var identity = Sync.Players.TryGetPlayerIdentity(playerId);
            if (identity == null)
            {
                identity = Sync.Players.RespawnComponent.CreateNewIdentity(msg.DisplayName, playerId, msg.CharacterModel);
            }

            Sync.Players.CreateNewPlayer(identity, playerId, identity.DisplayName);

            var response = new NewPlayerSuccessMsg();
            response.ClientSteamId = msg.ClientSteamId;
            response.PlayerSerialId = msg.PlayerSerialId;

            Sync.Layer.SendMessage(ref response, sender.SteamUserId);
        }

        static void OnNewPlayerSuccess(ref NewPlayerSuccessMsg msg, MyNetworkClient sender)
        {
            var localHumanPlayerId = new MyPlayer.PlayerId(MySteam.UserId, 0);
            var playerId = new MyPlayer.PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            if (playerId == localHumanPlayerId && (!MyFakes.ENABLE_BATTLE_SYSTEM || !MySession.Static.Battle))
            {
                if (!MySession.Static.IsScenario || MySession.Static.OnlineMode == MyOnlineModeEnum.OFFLINE)
                    MyPlayerCollection.RequestLocalRespawn();
            }

            var handler = Sync.Players.NewPlayerRequestSucceeded;
            if (handler != null)
                handler(msg.PlayerSerialId);
        }

        static void OnNewPlayerFailure(ref NewPlayerFailureMsg msg, MyNetworkClient sender)
        {
            if (msg.ClientSteamId != MySteam.UserId)
            {
                Debug.Assert(false, "Your SteamId differs from message steam id");
                return;
            }

            var playerId = new PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            if (Sync.Players.NewPlayerRequestFailed != null)
            {
                Sync.Players.NewPlayerRequestFailed(playerId.SerialId);
            }
        }

        static void OnPlayerCreated(ref PlayerCreatedMsg msg, MyNetworkClient sender)
        {
            var identity = Sync.Players.TryGetIdentity(msg.IdentityId);
            Debug.Assert(identity != null, "Identity for the new player not found!");
            if (identity == null) return;

            MyNetworkClient client = null;
            Sync.Clients.TryGetClient(msg.ClientSteamId, out client);
            Debug.Assert(client != null, "Could not find client of the new player!");
			if (client == null) return;

            var playerId = new MyPlayer.PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
			var playerBuilder = new MyObjectBuilder_Player();
			playerBuilder.DisplayName = msg.DisplayName;
			playerBuilder.IdentityId = msg.IdentityId;
			playerBuilder.BuildColorSlots = msg.BuildColors;

            Sync.Players.CreateNewPlayerInternal(client, playerId, playerBuilder);
        }

        static void OnAll_Identities_Players_Factions_Request(ref All_Identities_Players_Factions_RequestMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(Sync.IsServer);

            var response = new All_Identities_Players_Factions_SuccessMsg();
            response.Identities = Sync.Players.SaveIdentities();
            response.Players = Sync.Players.SavePlayers();
            response.Factions = MySession.Static.Factions.SaveFactions();

            Sync.Layer.SendMessage(ref response, sender.SteamUserId, messageType: MyTransportMessageEnum.Success);
        }

        static void OnAll_Identities_Players_Factions_Success(ref All_Identities_Players_Factions_SuccessMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
           // Multiplayer client must setup clients
            if ((MyMultiplayer.Static is MyMultiplayerClient) && msg.Players != null)
            {
                foreach (var player in msg.Players)
                {
                    if (!Sync.Clients.HasClient(player.SteamId))
                        Sync.Clients.AddClient(player.SteamId);
                }
            }
            Sync.Players.m_allIdentities.Clear();
            Sync.Players.m_npcIdentities.Clear();
            Sync.Players.LoadIdentities(msg.Identities);

            Sync.Players.m_players.Clear();
            Sync.Players.m_controlledEntities.Clear();
            Sync.Players.m_playerIdentityIds.Clear();
            Sync.Players.LoadPlayers(msg.Players);

            MySession.Static.Factions.LoadFactions(msg.Factions, true);
        }

        static void OnPlayerRemoveRequest(ref PlayerRemoveMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(msg.ClientSteamId == sender.SteamUserId, "A client requests removal of different client's player. That's not allowed!");
            if (msg.ClientSteamId != sender.SteamUserId) return;

            var player = Sync.Players.GetPlayerById(new PlayerId(msg.ClientSteamId, msg.PlayerSerialId));
            Debug.Assert(player != null, "Could not find a player to remove!");
            if (player == null) return;

            Sync.Players.RemovePlayer(player, msg.RemoveCharacter);
        }

        static void OnPlayerRemoved(ref PlayerRemoveMsg msg, MyNetworkClient sender)
        {
            var playerId = new MyPlayer.PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            Debug.Assert(Sync.Players.m_players.ContainsKey(playerId));

            if (msg.ClientSteamId == Sync.MyId)
                Sync.Players.RaiseLocalPlayerRemoved(msg.PlayerSerialId);

            Sync.Players.RemovePlayerFromDictionary(playerId);
        }

		static void OnPlayerColorChangedRequest(ref PlayerChangeColorMsg msg, MyNetworkClient sender)
		{
			PlayerId playerId = new PlayerId(sender.SteamUserId, msg.SerialId);
			var player = Sync.Players.GetPlayerById(playerId);
			if(player == null)
			{
				List<Vector3> colors;
				if (!MyCubeBuilder.AllPlayersColors.TryGetValue(playerId, out colors))
					return;

				colors[msg.ColorIndex] = msg.NewColor;
				return;
			}

			player.SelectedBuildColorSlot = msg.ColorIndex;
			player.ChangeOrSwitchToColor(msg.NewColor);
		}

		static void OnPlayerColorsChangedRequest(ref PlayerChangeColorsMsg msg, MyNetworkClient sender)
		{
			PlayerId playerId = new PlayerId(sender.SteamUserId, msg.SerialId);
			var player = Sync.Players.GetPlayerById(playerId);
			if (player == null)
			{
				List<Vector3> colors;
				if (!MyCubeBuilder.AllPlayersColors.TryGetValue(playerId, out colors))
					return;

				colors.Clear();
				foreach(var color in msg.NewColors)
				{
					colors.Add(color);
				}
				
				return;
			}

			player.SetBuildColorSlots(msg.NewColors);
		}

        static void OnNpcIdentityRequest(ref NewNpcIdentityMsg msg, MyNetworkClient sender)
        {
            string npcName = "NPC " + MyRandom.Instance.Next(1000, 9999);
            var identity = Sync.Players.CreateNewNpcIdentity(npcName);

            if (identity != null)
            {
                msg.IdentityId = identity.IdentityId;
                Sync.Layer.SendMessage(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);
            }
        }

        static void OnNpcIdentitySuccess(ref NewNpcIdentityMsg msg, MyNetworkClient sender)
        {
            var identity = Sync.Players.TryGetIdentity(msg.IdentityId);
            Debug.Assert(identity != null, "Server told me identity was created, but I cannot find it!");
            if (identity == null) return;

            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.OK,
                messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionInfo),
                messageText: new StringBuilder().AppendFormat(MyTexts.GetString(MySpaceTexts.NPCIdentityAdded), identity.DisplayName)));
        }
        
        #endregion

        #region Public methods

		public void RequestPlayerColorChanged(int playerSerialId, int colorIndex, Vector3 newColor)
		{
			PlayerChangeColorMsg msg = new PlayerChangeColorMsg()
			{
				SerialId = playerSerialId,
				ColorIndex = colorIndex,
				NewColor = newColor,
			};

			Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
		}

		public void RequestPlayerColorsChanged(int playerSerialId, List<Vector3> newColors)
		{
			PlayerChangeColorsMsg msg = new PlayerChangeColorsMsg()
			{
				SerialId = playerSerialId,
				NewColors = newColors,
			};

			Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
		}

        public void RequestNewPlayer(int serialNumber, string playerName, string characterModel)
        {
            var msg = new NewPlayerRequestMsg();
            msg.ClientSteamId = MySteam.UserId;
            msg.PlayerSerialId = serialNumber;
            msg.DisplayName = playerName;
            msg.CharacterModel = characterModel;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        public void RequestNewNpcIdentity()
        {
            var msg = new NewNpcIdentityMsg();
            Sync.Layer.SendMessageToServer(ref msg);
        }

        public MyPlayer CreateNewPlayer(MyIdentity identity, MyNetworkClient steamClient, string playerName)
        {
            Debug.Assert(Sync.IsServer);

            // TODO: Limit number of players per client
            var playerId = FindFreePlayerId(steamClient.SteamUserId);

			var playerBuilder = new MyObjectBuilder_Player();
			playerBuilder.DisplayName = playerName;
			playerBuilder.IdentityId = identity.IdentityId;

            return CreateNewPlayerInternal(steamClient, playerId, playerBuilder);
        }

        public MyPlayer CreateNewPlayer(MyIdentity identity, PlayerId id, string playerName)
        {
            Debug.Assert(Sync.IsServer);

            MyNetworkClient steamClient;
            Sync.Clients.TryGetClient(id.SteamId, out steamClient);
            Debug.Assert(steamClient != null, "Could not find a client for the new player!");
            if (steamClient == null) return null;

			var playerBuilder = new MyObjectBuilder_Player();
			playerBuilder.DisplayName = playerName;
			playerBuilder.IdentityId = identity.IdentityId;

            var player = CreateNewPlayerInternal(steamClient, id, playerBuilder);
            if (player != null)
            {
                var msg = new PlayerCreatedMsg();
                msg.ClientSteamId = id.SteamId;
                msg.PlayerSerialId = id.SerialId;
                msg.IdentityId = identity.IdentityId;
                msg.DisplayName = playerName;
				msg.BuildColors = null;
				if (!MyPlayer.IsColorsSetToDefaults(player.BuildColorSlots))
					msg.BuildColors = player.BuildColorSlots;

                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
            return player;
        }

        public MyPlayer InitNewPlayer(PlayerId id, MyObjectBuilder_Player playerOb)
        {
            MyNetworkClient steamClient;
            Sync.Clients.TryGetClient(id.SteamId, out steamClient);
            Debug.Assert(steamClient != null, "Could not find a client for the new player!");
            if (steamClient == null) return null;

            MyPlayer playerInstance = CreateNewPlayerInternal(steamClient, id, playerOb);
            return playerInstance;
        }

        public void RemovePlayer(MyPlayer player, bool removeCharacter = true)
        {
            if (Sync.IsServer)
            {
                if (removeCharacter && player.Character != null)
                {
                    //Dont remove character if he's sleeping in a cryo chamber
                    if (!(player.Character.Parent is Sandbox.Game.Entities.Blocks.MyCryoChamber))
                    {
                        player.Character.SyncObject.SendCloseRequest();
                    }
                }

                KillPlayer(player);

                if (player.IsLocalPlayer)
                   RaiseLocalPlayerRemoved(player.Id.SerialId);
                if (PlayerRemoved != null)
                    PlayerRemoved(player.Id);

                RespawnComponent.AfterRemovePlayer(player);

                var msg = new PlayerRemoveMsg();
                msg.ClientSteamId = player.Id.SteamId;
                msg.PlayerSerialId = player.Id.SerialId;
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);

                RemovePlayerFromDictionary(player.Id);
            }
            else
            {
                Debug.Assert(player.IsLocalPlayer, "Client can only remove local players!");
                if (player.IsRemotePlayer) return;

                var msg = new PlayerRemoveMsg();
                msg.ClientSteamId = player.Id.SteamId;
                msg.PlayerSerialId = player.Id.SerialId;
                msg.RemoveCharacter = removeCharacter;
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }

        public MyPlayer GetPlayerById(PlayerId id)
        {
            MyPlayer player = null;
            m_players.TryGetValue(id, out player);
            return player;
        }

        public bool TryGetPlayerById(PlayerId id, out MyPlayer player)
        {
            return m_players.TryGetValue(id, out player);
        }

        public void RequestAll_Identities_Players_Factions()
        {
            var msg = new All_Identities_Players_Factions_RequestMsg();
            msg.ClientSteamId = MySteam.UserId;
            msg.PlayerSerialId = 0;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        public bool TrySetControlledEntity(PlayerId id, MyEntity entity)
        {
            var player = GetControllingPlayer(entity);

            if (player == null)
            {
                SetControlledEntity(id, entity);
                return true;
            }
            return player.Id == id; // When already controlled, return true
        }

        /// <summary>
        /// Shortcut for setting the first controller of the given player
        /// </summary>
        /// <param name="steamUserId"></param>
        /// <param name="entity"></param>
        public void SetControlledEntity(ulong steamUserId, MyEntity entity)
        {
            PlayerId id = new PlayerId(steamUserId);
            SetControlledEntity(id, entity);
        }

        public void SetControlledEntity(PlayerId id, MyEntity entity)
        {
            var msg = new ControlChangedMsg();
            msg.EntityId = entity.EntityId;
            msg.PlayerSerialId = id.SerialId;
            msg.ClientSteamId = id.SteamId;

            if (Sync.IsServer)
            {
                SetControlledEntityInternal(id, entity);

                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
            else
            {
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }

        public void RemoveControlledEntity(MyEntity entity)
        {
            RemoveControlledEntityProxy(entity, immediateOnServer: true);
        }

        private void RemoveControlledEntityProxy(MyEntity entity, bool immediateOnServer)
        {
            var msg = new ControlReleasedMsg();
            msg.EntityId = entity.EntityId;

            if (Sync.IsServer)
            {
                RemoveControlledEntityInternal(entity, immediateOnServer);

                Sync.Layer.SendMessageToAll(ref msg);
            }
            else
            {
                Sync.Layer.SendMessageToServer(ref msg);
            }
        }

        public void SetPlayerCharacter(MyPlayer player, MyCharacter newCharacter, MyEntity controlledEntity = null)
        {
            Debug.Assert(Sync.IsServer, "SetPlayerCharacter can be only called on the server!");

            if (controlledEntity == null) controlledEntity = newCharacter;

            ChangePlayerCharacterInternal(player, newCharacter, controlledEntity);

            var msg = new CharacterChangedMsg();
            msg.ClientSteamId = player.Id.SteamId;
            msg.PlayerSerialId = player.Id.SerialId;
            msg.CharacterEntityId = newCharacter.EntityId;
            msg.ControlledEntityId = controlledEntity.EntityId;

            Sync.Layer.SendMessageToAll(ref msg);
        }

        public MyPlayer GetControllingPlayer(MyEntity entity)
        {
            MyPlayer player;
            PlayerId playerId;
            if (m_controlledEntities.TryGetValue(entity.EntityId, out playerId) && m_players.TryGetValue(playerId, out player))
            {
                return player;
            }
            return null;
        }

        public MyEntityController GetEntityController(MyEntity entity)
        {
            MyPlayer controllingPlayer = GetControllingPlayer(entity);
            if (controllingPlayer == null) return null;
            else return controllingPlayer.Controller;
        }

        public Dictionary<PlayerId, MyPlayer>.ValueCollection GetOnlinePlayers()
        {
            return m_players.Values;
        }

        public Dictionary<long, MyIdentity>.ValueCollection GetAllIdentities()
        {
            return m_allIdentities.Values;
        }

        public HashSet<long> GetNPCIdentities()
        {
            return m_npcIdentities;
        }

        public Dictionary<PlayerId, long>.KeyCollection GetAllPlayers()
        {
            return m_playerIdentityIds.Keys;
        }

        #endregion

        #region Control extension & reduction

        // Control extension and reduction is a mechanism that saves the control of other entities than IMyControllableEntities.
        // A typical example is a cockpit inside a cube grid - you control the cockpit, but you want to "extend" the control to
        // the cube grid as well. You can extend the control multiple times, but you always have to extend from the "base" entity,
        // i.e. the cockpit in our example. The same goes for control reduction: always reduce to the "base" entity

        public void TryExtendControl(IMyControllableEntity baseEntity, MyEntity entityGettingControl)
        {
            var controller = baseEntity.ControllerInfo.Controller;
            if (controller != null)
            {
                // This can fail when something else is controlling entityGettingControl
                // This is case when player entered second cockpit (and first cockpit is controlled by someone)
                TrySetControlledEntity(controller.Player.Id, entityGettingControl);
            }
        }

        public void ExtendControl(IMyControllableEntity baseEntity, MyEntity entityGettingControl)
        {
            var controller = baseEntity.ControllerInfo.Controller;
            if (controller != null)
            {
                // This can fail when something else is controlling entityGettingControl
                // This is case when player entered second cockpit (and first cockpit is controlled by someone)
                TrySetControlledEntity(controller.Player.Id, entityGettingControl);
            }
            else if (!(baseEntity is MyRemoteControl))
            {
                Debug.Fail("'entityWithControl' is not controlled");
            }
        }

        public bool TryReduceControl(IMyControllableEntity baseEntity, MyEntity entityWhichLoosesControl)
        {
            MyPlayer.PlayerId playerB;
            var controller = baseEntity.ControllerInfo.Controller;
            if (controller != null && m_controlledEntities.TryGetValue(entityWhichLoosesControl.EntityId, out playerB) && controller.Player.Id == playerB)
            {
                RemoveControlledEntity(entityWhichLoosesControl);
                return true;
            }
            return false;
        }

        public void ReduceControl(IMyControllableEntity baseEntity, MyEntity entityWhichLoosesControl)
        {
            if (!TryReduceControl(baseEntity, entityWhichLoosesControl) && !(baseEntity is MyRemoteControl))
            {
                Debug.Fail("Both entities must be controlled by same player");
            }
        }

        public void ReduceAllControl(IMyControllableEntity baseEntity)
        {
            MyPlayer.PlayerId playerId;
            bool success = m_controlledEntities.TryGetValue(baseEntity.Entity.EntityId, out playerId);
            Debug.Assert(success || baseEntity is MyRemoteControl, "Could not get the controller of the base entity!");
            if (!success) return;

            foreach (var entry in m_controlledEntities)
            {
                if (entry.Value != playerId) continue; // Only take entities controlled by the same controller as baseEntity
                if (entry.Key == baseEntity.Entity.EntityId) continue; // But don't reduce control from the base entity itself

                MyEntity entity = null;
                MyEntities.TryGetEntityById(entry.Key, out entity);
                Debug.Assert(entity != null, "Could not find controlled entity!");
                if (entity == null) continue;

                RemoveControlledEntityProxy(entity, immediateOnServer: false);
            }
            m_controlledEntities.ApplyRemovals();

            WriteDebugInfo();
        }

        public bool HasExtendedControl(IMyControllableEntity baseEntity, MyEntity secondEntity)
        {
            return baseEntity.ControllerInfo.Controller == GetEntityController(secondEntity);
        }

        #endregion

        #region Identities

        public MyIdentity CreateNewNpcIdentity(string name)
        {
            MyIdentity identity = base.CreateNewIdentity(name, null);
            AfterCreateIdentity(identity, true);
            return identity;
        }

        public override MyIdentity CreateNewIdentity(string name, string model = null)
        {
            MyIdentity identity = base.CreateNewIdentity(name, model);
            AfterCreateIdentity(identity);
            return identity;
        }

        public override MyIdentity CreateNewIdentity(string name, long identityId, string model)
        {
            bool obsoleteNpc = false;
            MyEntityIdentifier.ID_OBJECT_TYPE objectType = MyEntityIdentifier.GetIdObjectType(identityId);
            if (objectType == MyEntityIdentifier.ID_OBJECT_TYPE.NPC || objectType == MyEntityIdentifier.ID_OBJECT_TYPE.SPAWN_GROUP)
                obsoleteNpc = true;

            MyIdentity identity = base.CreateNewIdentity(name, identityId, model);
            AfterCreateIdentity(identity, obsoleteNpc);
            return identity;
        }

        public override MyIdentity CreateNewIdentity(MyObjectBuilder_Identity objectBuilder)
        {
            bool obsoleteNpc = false;
            MyEntityIdentifier.ID_OBJECT_TYPE objectType = MyEntityIdentifier.GetIdObjectType(objectBuilder.IdentityId);
            if (objectType == MyEntityIdentifier.ID_OBJECT_TYPE.NPC || objectType == MyEntityIdentifier.ID_OBJECT_TYPE.SPAWN_GROUP)
                obsoleteNpc = true;

            MyIdentity identity = base.CreateNewIdentity(objectBuilder);
            AfterCreateIdentity(identity, obsoleteNpc);
            return identity;
        }

        public void RemoveIdentity(long identityId, PlayerId playerId = new PlayerId())
        {
            if (Sync.IsServer)
            {
                if (!RemoveIdentityInternal(identityId, playerId)) return;
            }

            var msg = new IdentityRemovedMsg();
            msg.IdentityId = identityId;
            msg.SteamId = playerId.SteamId;
            msg.SerialId = playerId.SerialId;

            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
            else
            {
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }

        public bool HasIdentity(long identityId)
        {
            return m_allIdentities.ContainsKey(identityId);
        }

        public MyIdentity TryGetIdentity(long identityId)
        {
            MyIdentity identity;
            m_allIdentities.TryGetValue(identityId, out identity);
            return identity;
        }

        /// <summary>
        /// Does a linear search through a dictionary. Do not use unless you actually need this
        /// </summary>
        public bool TryGetPlayerId(long identityId, out MyPlayer.PlayerId result)
        {
            foreach (var identity in m_playerIdentityIds)
            {
                if (identity.Value == identityId)
                {
                    result = identity.Key;
                    return true;
                }
            }

            result = new PlayerId();
            return false;
        }

        public MyIdentity TryGetPlayerIdentity(MyPlayer.PlayerId playerId)
        {
            MyIdentity identity = null;
            long identityId = TryGetIdentityId(playerId.SteamId, playerId.SerialId);

            if (identityId != 0)
            {
                identity = TryGetIdentity(identityId);
            }

            return identity;
        }

        public long TryGetIdentityId(ulong steamId, int serialId = 0)
        {
            long identityId = 0;
            var playerId = new PlayerId(steamId, serialId);
            m_playerIdentityIds.TryGetValue(playerId, out identityId);
            return identityId;
        }

        public void MarkIdentityAsNPC(long identityId)
        {
            Debug.Assert(MyEntityIdentifier.GetIdObjectType(identityId) == MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, "Invalid identity ID!");
            Debug.Assert(!m_npcIdentities.Contains(identityId), "Identity is already an NPC!");

            m_npcIdentities.Add(identityId);
        }

        public void UnmarkIdentityAsNPC(long identityId)
        {
            Debug.Assert(MyEntityIdentifier.GetIdObjectType(identityId) == MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, "Invalid identity ID!");
            Debug.Assert(m_npcIdentities.Contains(identityId), "Identity is not an NPC!");

            m_npcIdentities.Remove(identityId);   
        }

        public bool IdentityIsNpc(long identityId)
        {
            return m_npcIdentities.Contains(identityId);
        }

        public void LoadIdentities(List<MyObjectBuilder_Identity> list)
        {
            if (list == null)
                return;
            
            foreach (var objectBuilder in list)
            {
                var identity = CreateNewIdentity(objectBuilder);
            }
        }

        public void ClearIdentities()
        {
            m_allIdentities.Clear();
            m_npcIdentities.Clear();
        }

        #endregion

        #region Respawn

        public void SetRespawnComponent(MyRespawnComponentBase respawnComponent)
        {
            RespawnComponent = respawnComponent;
        }

        public static void RequestLocalRespawn()
        {
            MySandboxGame.Log.WriteLine("RequestRespawn");
            //Local player on dedicated?
            Debug.Assert(!MySandboxGame.IsDedicated, "Dedicated server shouldnt have local players");
            if (MySandboxGame.IsDedicated)
                return;

            Debug.Assert(Sync.Players != null, "Sync.Players was null in MyPlayerCollection.cs");
            if (Sync.Players == null) return;

            var handler = Sync.Players.LocalRespawnRequested;
            if (handler != null)
                handler();
        }

        public static void RespawnRequest(bool joinGame, bool newPlayer, long medicalId, string shipPrefabId, int playerSerialId = 0, Vector3D? spawnPosition = null)
        {
            var msg = new RespawnMsg();
            msg.JoinGame = joinGame;
            msg.MedicalRoom = medicalId;
            msg.NewIdentity = newPlayer;
            msg.RespawnShipId = shipPrefabId;
            msg.PlayerSerialId = playerSerialId;
            msg.SpawnPosition = spawnPosition;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg, Engine.Multiplayer.MyTransportMessageEnum.Request);
        }

        public void KillPlayer(MyPlayer player)
        {
            Debug.Assert(player.Identity != null, "Player identity was null!");
            SetPlayerDead(player, true, MySession.Static.Settings.PermanentDeath.Value);
        }

        public void RevivePlayer(MyPlayer player)
        {
            Debug.Assert(player.Identity != null, "Player identity was null!");
            SetPlayerDead(player, false, false);
        }

        private void SetPlayerDead(MyPlayer player, bool deadState, bool resetIdentity)
        {
            var msg = new SetPlayerDeadMsg();
            msg.ClientSteamId = player.Id.SteamId;
            msg.PlayerSerialId = player.Id.SerialId;
            msg.IsDead = deadState;
            msg.ResetIdentity = resetIdentity;

            if (Sync.IsServer)
            {
                // CH: This was here before and I think it's wrong! :-)
                //player.Identity.SetDead(resetIdentity);
                if (SetPlayerDeadInternal(player.Id.SteamId, player.Id.SerialId, deadState, resetIdentity))
                {
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                }
            }
            else
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        private bool SetPlayerDeadInternal(ulong playerSteamId, int playerSerialId, bool deadState, bool resetIdentity)
        {
            PlayerId id = new PlayerId(playerSteamId, playerSerialId);
            var player = Sync.Players.GetPlayerById(id);
            Debug.Assert(player != null, "Could not find player "+id);
            if (player == null) return false;

            Debug.Assert(player.Identity != null, "Setting dead state of an uncontrolled identity!");
            if (player.Identity == null) return false;

            player.Identity.SetDead(resetIdentity);

            if (deadState == true)
            {
                if (player == Sync.Clients.LocalClient.FirstPlayer)
                    MyPlayerCollection.RequestLocalRespawn();

                player.Controller.TakeControl(null);

                foreach (var entry in m_controlledEntities)
                {
                    if (entry.Value == player.Id)
                    {
                        MyEntity entity = null;
                        MyEntities.TryGetEntityById(entry.Key, out entity);
                        Debug.Assert(entity != null, "Could not find controlled entity in KillController! Inconsistency!");
                        if (entity == null) continue;

                        RemoveControlledEntityInternal(entity, false);
                    }
                }
                m_controlledEntities.ApplyRemovals();
            }

            return true;
        }

        static void OnRespawnRequest(ref RespawnMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(Sync.Players.RespawnComponent != null, "The respawn component is not set! Cannot handle respawn request!");
            if (Sync.Players.RespawnComponent == null)
            {
                return;
            }

            bool respawnSuccessful = Sync.Players.RespawnComponent.HandleRespawnRequest(
                msg.JoinGame,
                msg.NewIdentity,
                msg.MedicalRoom,
                msg.RespawnShipId,
                new PlayerId(sender.SteamUserId, msg.PlayerSerialId),
                msg.SpawnPosition
            );

            if (!respawnSuccessful)
            {
                MySession.Static.SyncLayer.SendMessage(ref msg, sender.SteamUserId, MyTransportMessageEnum.Failure);
            }
        }

        #endregion

        #region Private methods

        private MyPlayer CreateNewPlayerInternal(MyNetworkClient steamClient, PlayerId playerId, MyObjectBuilder_Player playerBuilder)
        {
            if (!m_playerIdentityIds.ContainsKey(playerId))
            {
                m_playerIdentityIds.Add(playerId, playerBuilder.IdentityId);
            }

            MyPlayer newPlayer = new MyPlayer(steamClient, playerId);
			newPlayer.Init(playerBuilder);

            newPlayer.IdentityChanged += player_IdentityChanged;
            newPlayer.Controller.ControlledEntityChanged += controller_ControlledEntityChanged;

            AddPlayer(playerId, newPlayer);

            if (MyFakes.ENABLE_MISSION_TRIGGERS && MySessionComponentMissionTriggers.Static!=null)
                MySessionComponentMissionTriggers.Static.TryCreateFromDefault(playerId);

            return newPlayer;
        }

        private static void ChangePlayerCharacterInternal(MyPlayer player, MyCharacter characterEntity, MyEntity entity)
        {
            if (player == null)
            {
                MySandboxGame.Log.WriteLine("Player not found");
                Debug.Fail("Player not found");
            }

            if (player.Identity == null)
            {
                MySandboxGame.Log.WriteLine("Player identity was null");
                Debug.Fail("Player identity was null");
            }

            MyTrace.Send(TraceWindow.Multiplayer, "Player character changed to: " + characterEntity.EntityId + ", controlledEntity: " + entity.EntityId, player.Id.ToString());

            player.Identity.ChangeCharacter(characterEntity);
            Sync.Players.SetControlledEntityInternal(player.Id, entity);

            if (player == MySession.LocalHumanPlayer)
                MySession.SetCameraController(MyCameraControllerEnum.Entity, MySession.LocalCharacter);
        }

        private void SetControlledEntityInternal(PlayerId id, MyEntity entity)
        {
            Debug.Assert(
                Sync.Players.GetPlayerById(id) == null || Sync.Players.GetPlayerById(id).Controller.ControlledEntity != entity,
                "Setting the controlled entity to the same value. Is that what you wanted?");

            RemoveControlledEntityInternal(entity);

            entity.OnClosing += m_entityClosingHandler;

            // TakeControl will take care of setting controlled entity into m_controlledEntities
            var player = Sync.Players.GetPlayerById(id);
            if (player != null && entity is IMyControllableEntity)
            {
                player.Controller.TakeControl((IMyControllableEntity)entity);
            }
            else
            {
                m_controlledEntities.Add(entity.EntityId, player.Id, immediate: true);
            }

            Sync.Players.WriteDebugInfo();
        }

        private void controller_ControlledEntityChanged(IMyControllableEntity oldEntity, IMyControllableEntity newEntity)
        {
            Debug.Assert(oldEntity != null || newEntity != null, "Both old and new entity cannot be null!");
            Debug.Assert(oldEntity == null || oldEntity.ControllerInfo.Controller == null, "Inconsistency! Controller of old entity is not empty!");
            Debug.Assert(oldEntity == null || m_controlledEntities.ContainsKey((oldEntity as MyEntity).EntityId), "Old entity control not in controller collection!");
            Debug.Assert(newEntity == null || !m_controlledEntities.ContainsKey((newEntity as MyEntity).EntityId), "New entity control is already in the controller collection!");

            var controller = (newEntity == null ? oldEntity.ControllerInfo.Controller : newEntity.ControllerInfo.Controller);

            if (oldEntity != null)
                m_controlledEntities.Remove((oldEntity as MyEntity).EntityId, immediate: true);
            if (newEntity != null)
                m_controlledEntities.Add((newEntity as MyEntity).EntityId, controller.Player.Id, immediate: true);
        }

        private void RemoveControlledEntityInternal(MyEntity entity, bool immediate = true)
        {
            entity.OnClosing -= m_entityClosingHandler;

            m_controlledEntities.Remove(entity.EntityId, immediate: immediate);

            Sync.Players.WriteDebugInfo();
        }

        private void EntityClosing(MyEntity entity)
        {
            entity.OnClosing -= m_entityClosingHandler;

            // Controllable entities will be bound to a player's entity controller when in m_controlledEntities and the
            // controller will take care of removing control from m_controlledEntities
            if (!(entity is IMyControllableEntity))
                m_controlledEntities.Remove(entity.EntityId, immediate: true);
        }

        private void Multiplayer_ClientRemoved(ulong steamId)
        {
            if (Sync.IsServer)
            {
                m_tmpRemovedPlayers.Clear();

                foreach (var entry in m_players)
                {
                    if (entry.Key.SteamId != steamId) continue;
                    m_tmpRemovedPlayers.Add(entry.Value);
                }

                foreach (var player in m_tmpRemovedPlayers)
                {
                    RemovePlayer(player);
                }

                m_tmpRemovedPlayers.Clear();
            }
        }

        private void RaiseLocalPlayerRemoved(int serialId)
        {
            var handler = LocalPlayerRemoved;
            if (handler != null)
                handler(serialId);
        }

        private bool RemoveIdentityInternal(long identityId, PlayerId playerId)
        {
            Debug.Assert(m_allIdentities.ContainsKey(identityId), "Identity could not be found");
            Debug.Assert(!playerId.IsValid || !m_players.ContainsKey(playerId), "Cannot remove identity of active player!");
            if (playerId.IsValid && m_players.ContainsKey(playerId)) return false;

            MyIdentity identity;
            if (m_allIdentities.TryGetValue(identityId, out identity))
            {
                identity.ChangeCharacter(null);
                identity.CharacterChanged -= Identity_CharacterChanged;
            }

            m_allIdentities.Remove(identityId);
            m_npcIdentities.Remove(identityId);
            if (playerId.IsValid)
            {
                m_playerIdentityIds.Remove(playerId);
            }

            var handler = IdentitiesChanged;
            if (handler != null) handler();

            return true;
        }

        private void LoadIdentitiesObsolete(List<Sandbox.Common.ObjectBuilders.MyObjectBuilder_Checkpoint.PlayerItem> playersFromSession, MyPlayer.PlayerId? savingPlayerId = null)
        {
            foreach (var entry in playersFromSession)
            {
                var identity = CreateNewIdentity(entry.Name, entry.PlayerId, entry.Model);
                var playerId = new PlayerId(entry.SteamId);

                // If savingPlayerId matches, we replace the identity player ID with the local player
                if (savingPlayerId != null && playerId == savingPlayerId.Value) playerId = new PlayerId(MySteam.UserId);

                if (!entry.IsDead)
                {
                    Debug.Assert(!m_playerIdentityIds.ContainsKey(playerId), "Loaded player has two live identities!");
                    if (m_playerIdentityIds.ContainsKey(playerId)) continue;
                    m_playerIdentityIds.Add(playerId, identity.IdentityId);
                    identity.SetDead(false);
                }
            }
        }

        private void AfterCreateIdentity(MyIdentity identity, bool addToNpcs = false)
        {
            if (addToNpcs)
            {
                MarkIdentityAsNPC(identity.IdentityId);
            }

            m_allIdentities.Add(identity.IdentityId, identity);

            identity.CharacterChanged += Identity_CharacterChanged;
            if (identity.Character != null)
            {
                identity.Character.CharacterDied += Character_CharacterDied;
            }

            if (Sync.IsServer)
            {
                IdentityCreatedMsg msg = new IdentityCreatedMsg();
                msg.IsNPC = addToNpcs;
                msg.IdentityId = identity.IdentityId;
                msg.DisplayName = identity.DisplayName;
                msg.Model = identity.Model;

                Sync.Layer.SendMessageToAll(ref msg);
            }

            var handler = IdentitiesChanged;
            if (handler != null) handler();
        }

        private void Character_CharacterDied(MyCharacter diedCharacter)
        {
            if (PlayerCharacterDied != null && diedCharacter != null && diedCharacter.ControllerInfo.ControllingIdentityId != 0)
                PlayerCharacterDied(diedCharacter.ControllerInfo.ControllingIdentityId);
        }

        private void Identity_CharacterChanged(MyCharacter oldCharacter, MyCharacter newCharacter)
        {
            if (oldCharacter != null)
                oldCharacter.CharacterDied -= Character_CharacterDied;

            if (newCharacter != null)
                newCharacter.CharacterDied += Character_CharacterDied;
        }

        private void LoadPlayerInternal(ref PlayerId playerId, MyObjectBuilder_Player playerOb, bool obsolete = false)
        {
            var identity = TryGetIdentity(playerOb.IdentityId);
            Debug.Assert(identity != null, "Identity of a player was null when loading! Inconsistency!");
            if (identity == null) return;
            if (obsolete && identity.IsDead) return;

            // This happens when you load an existing game - only the local player will be in Sync.Clients, but there were
            // more connected players at the time of the save. In this case, we have to consider them as disconnected players
            if (Sync.IsServer && MySteam.UserId != playerId.SteamId)
                playerOb.Connected = Sync.Clients.HasClient(playerId.SteamId);
            if (!playerOb.Connected)
            {
                if (!m_playerIdentityIds.ContainsKey(playerId))
                    m_playerIdentityIds.Add(playerId, playerOb.IdentityId);
                identity.SetDead(true);
                return;
            }

            var player = InitNewPlayer(playerId, playerOb);

            if (player.IsLocalPlayer)
            {
                var handler = Sync.Players.LocalPlayerLoaded;
                if (handler != null)
                    handler(playerId.SerialId);
            }
        }

        private PlayerId FindFreePlayerId(ulong steamId)
        {
            PlayerId currentId = new PlayerId(steamId);
            while (m_playerIdentityIds.ContainsKey(currentId))
            {
                currentId++;
            }
            return currentId;
        }

        private long FindLocalIdentityId(MyObjectBuilder_Checkpoint checkpoint)
        {
            long playerId = 0;

            playerId = TryGetIdentityId(MySteam.UserId);
            if (playerId != 0)
                return playerId;

            // Backward compatibility:
            if (checkpoint.Players != null)
            {
                if (checkpoint.Players.Dictionary.ContainsKey(MySteam.UserId))
                    playerId = checkpoint.Players[MySteam.UserId].PlayerId != 0 ? checkpoint.Players[MySteam.UserId].PlayerId : playerId;
            }
            if (checkpoint.AllPlayers != null)
            {
                foreach (var player in checkpoint.AllPlayers)
                {
                    if (player.SteamId == MySteam.UserId && !player.IsDead)
                    {
                        playerId = player.PlayerId;
                        break;
                    }

                    if (player.SteamId == MySteam.UserId && playerId == player.PlayerId && player.IsDead)
                    {
                        playerId = 0;
                        break;
                    }
                }
            }

            return playerId;
        }

        private void player_IdentityChanged(MyPlayer player, MyIdentity identity)
        {
            Debug.Assert(m_playerIdentityIds[player.Id] != identity.IdentityId, "Setting player identity to the same value. Is that what you want?");

            m_playerIdentityIds[player.Id] = identity.IdentityId;

            if (Sync.IsServer)
            {
                var msg = new PlayerIdentityChangedMsg();
                msg.ClientSteamId = player.Id.SteamId;
                msg.PlayerSerialId = player.Id.SerialId;
                msg.IdentityId = identity.IdentityId;

                Sync.Layer.SendMessageToAll(ref msg);
            }
        }

        #endregion

        #region Debug

        private string GetPlayerCharacter(MyPlayer player)
        {
            return player.Identity.Character == null ? "<empty>" : player.Identity.Character.Entity.ToString();
        }

        private string GetControlledEntity(MyPlayer player)
        {
            return player.Controller.ControlledEntity == null ? "<empty>" : player.Controller.ControlledEntity.Entity.ToString();
        }

        [Conditional("DEBUG")]
        public void WriteDebugInfo()
        {
            var trace = new StackTrace();
            var previousFrame = trace.GetFrame(1);

            foreach (var player in m_players)
            {
                bool isLocal = player.Value.IsLocalPlayer;
                string traceInfo = previousFrame.GetMethod().Name;
                traceInfo += isLocal ? "; Control: [L] " : "; Control: ";
                traceInfo += player.Value.Id.ToString();

                var ids = m_controlledEntities.Where(s => s.Value == player.Value.Id).Select(s => s.Key.ToString("X")).ToArray();
                MyTrace.Send(TraceWindow.MultiplayerFiltered, traceInfo,
                    "Character: " + GetPlayerCharacter(player.Value) +
                    ", Control: " + GetControlledEntity(player.Value) +
                    ", ExtendedTo: " + (ids.Length > 0 ? ids.Aggregate((a, b) => a + ", " + b) : "<none>"));
            }

            foreach (var entity in MyEntities.GetEntities())
            {
                MyTrace.Send(TraceWindow.MultiplayerFiltered, entity.EntityId.ToString("X8"), entity.ToString());
            }
        }

        [Conditional("DEBUG")]
        public void DebugDraw()
        {  
            int y = 0;
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "Steam clients:", Color.GreenYellow, 0.5f);
            foreach (var client in Sync.Clients.GetClients())
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "  SteamId: " + client.SteamUserId + ", Name: " + client.DisplayName, Color.LightYellow, 0.5f);
            }
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "Online players:", Color.GreenYellow, 0.5f);
            foreach (var player in m_players)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "  PlayerId: " + player.Key.ToString() + ", Name: " + player.Value.DisplayName, Color.LightYellow, 0.5f);
            }
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "Player identities:", Color.GreenYellow, 0.5f);
            foreach (var identityId in m_playerIdentityIds)
            {
                MyPlayer player;
                m_players.TryGetValue(identityId.Key, out player);
                string ctrlName = player == null ? "N.A." : player.DisplayName;

                MyIdentity identity;
                m_allIdentities.TryGetValue(identityId.Value, out identity);
                Color color = (identity == null || identity.IsDead) ? Color.Salmon : Color.LightYellow;
                string name = identity == null ? "N.A." : identity.DisplayName;

                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "  PlayerId: " + identityId.Key.ToString() + ", Name: " + ctrlName + "; IdentityId: " + identityId.Value.ToString() + ", Name: " + name, color, 0.5f);
            }
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "All identities:", Color.GreenYellow, 0.5f);
            foreach (var identity in m_allIdentities)
            {
                bool dead = identity.Value.IsDead;
                Color color = dead ? Color.Salmon : Color.LightYellow;
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "  IdentityId: " + identity.Key.ToString() + ", Name: " + identity.Value.DisplayName + ", State: " + (dead ? "DEAD" : "ALIVE"), color, 0.5f);
            }
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "Control:", Color.GreenYellow, 0.5f);
            foreach (var entry in m_controlledEntities)
            {
                MyEntity controlledEntity;
                MyEntities.TryGetEntityById(entry.Key, out controlledEntity);
                Color color = controlledEntity == null ? Color.Salmon : Color.LightYellow;
                string entityName = controlledEntity == null ? "Unknown entity" : controlledEntity.ToString();
                string entityId = controlledEntity == null ? "N.A." : controlledEntity.EntityId.ToString();

                MyPlayer player;
                m_players.TryGetValue(entry.Value, out player);
                string ctrlName = player == null ? "N.A." : player.DisplayName;

                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "  " + entityName + " controlled by " + ctrlName + " (entityId = " + entityId + ", playerId = " + entry.Value.ToString() + ")", color, 0.5f);
            }

            if (MySession.ControlledEntity != null)
            {
                var cockpit = MySession.ControlledEntity as MyShipController;
                if (cockpit != null)
                {
                    var grid = cockpit.Parent as MyCubeGrid;
                    if (grid != null)
                    {
                        grid.GridSystems.ControlSystem.DebugDraw((++y) * 13.0f);
                    }
                }
            }
        }

        #endregion
    }
}
