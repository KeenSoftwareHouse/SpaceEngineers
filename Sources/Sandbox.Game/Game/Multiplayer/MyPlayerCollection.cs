using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
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
using VRage.Serialization;
using VRage.Trace;
using VRageMath;
using VRageRender;
using PlayerId = Sandbox.Game.World.MyPlayer.PlayerId;
using Sandbox.Game.SessionComponents;
using Sandbox.Common.ObjectBuilders.Definitions;

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
        }

        [MessageId(31, P2PMessageEnum.Reliable)]
        struct PlayerRemoveMsg
        {
            public ulong ClientSteamId;
            public int PlayerSerialId;
        }

        [MessageId(32, P2PMessageEnum.Reliable)]
        struct IdentityRemoveMsg
        {
            public ulong ClientSteamId;
            public int PlayerSerialId;
            public long IdentityId;
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

        [ProtoContract]
        struct AllPlayerData 
        {
            [ProtoMember]
            public ulong SteamId;
            [ProtoMember]
            public int SerialId;
            [ProtoMember]
            public MyObjectBuilder_Player Player;
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
        public IMyRespawnComponent RespawnComponent { get; set; }

        #endregion

        public event Action<int> NewPlayerRequestSucceeded;
        public event Action<int> NewPlayerRequestFailed;
        public event Action<int> LocalPlayerRemoved;
        public event Action<int> LocalPlayerLoaded;
        public event Action LocalRespawnRequested;
        public event Action<PlayerId> PlayerRemoved;

        public event PlayerRequestDelegate PlayerRequesting;

        public event Action<bool, ulong> PlayersChanged;

        public event Action<long> PlayerCharacterDied;

        #region Construction & (de)serialization

        static MyPlayerCollection()
        {
            MySyncLayer.RegisterEntityMessage<MySyncEntity, ControlChangedMsg>(OnControlChangedRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncEntity, ControlChangedMsg>(OnControlChangedSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterEntityMessage<MySyncEntity, CharacterChangedMsg>(OnCharacterChanged, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncEntity, ControlReleasedMsg>(OnControlReleased, MyMessagePermissions.Any);
            MySyncLayer.RegisterMessage<IdentityCreatedMsg>(OnIdentityCreated, MyMessagePermissions.FromServer);
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
            MySyncLayer.RegisterMessage<IdentityRemoveMsg>(OnIdentityRemoveRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<IdentityRemoveMsg>(OnIdentityRemoveSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

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

                MyPlayer player = Sync.Players.TryGetPlayerById(playerId);

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

        private void LoadPlayers(List<AllPlayerData> allPlayersData)
        {
            foreach (var playerItem in allPlayersData)
            {
                var playerId = new MyPlayer.PlayerId(playerItem.SteamId, playerItem.SerialId);
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
                    var playerId = new MyPlayer.PlayerId(playerItem.Key.ClientId, playerItem.Key.SerialId);
                    if (savingPlayerId != null && playerId == savingPlayerId)
                    {
                        playerId = new PlayerId(MySteam.UserId);
                        ChangeDisplayNameOfPlayerAndIdentity(playerItem.Value, MySteam.UserName);
                    }

                    LoadPlayerInternal(ref playerId, playerItem.Value, obsolete: false);
                }
            }

            /*long identityId = FindLocalIdentityId(checkpoint);

            //Player was saved in death state or there is no player
            if (identityId == 0)
            {
                checkpoint.ControlledObject = 0;  //This will lead to RequestRespawn
                checkpoint.CameraController = MyCameraControllerEnum.Entity;
                IsCameraAwaitingEntity = true;
            }
            else
            {
                // TODO: Refactor this later
                MyEntity controlledObject = null;
                if (checkpoint.ControlledObject != -1)
                {
                    MyEntities.TryGetEntityById(checkpoint.ControlledObject, out controlledObject);

                    System.Diagnostics.Debug.Assert(controlledObject != null);

                    if (controlledObject is IMyControllableEntity)
                    {
                        var cockpit = controlledObject as MyCockpit;
                        if (cockpit != null)
                        {
                            var pilot = cockpit.Pilot;
                            if (pilot == null)
                            {
                                Debug.Fail("Creating new pilot for cockpit, because saved cockpit was controlled and had no pilot sitting in it.");
                                MySandboxGame.Log.WriteLine("Creating new pilot for cockpit, because saved cockpit was controlled and had no pilot sitting in it.");
                                var characterOb = MyCharacter.Random();
                                characterOb.Battery = new MyObjectBuilder_Battery() { CurrentCapacity = MyEnergyConstants.BATTERY_MAX_CAPACITY };
                                pilot = (MyCharacter)MyEntityFactory.CreateEntity(characterOb);
                                pilot.Init(characterOb);
                                MyWorldGenerator.InitInventoryWithDefaults(pilot.GetInventory());
                                cockpit.RequestUse(UseActionEnum.Manipulate, pilot);
                            }
                            MySession.Player.Init(pilot, null, identityId);
                        }
                        else if (controlledObject.Parent is MyCockpit)
                        {
                            MySession.Player.Init((MyCharacter)controlledObject, null, identityId);
                            controlledObject = controlledObject.Parent;
                        }
                        else
                        {
                            if (!MySandboxGame.IsDedicated)
                            {
                                if (controlledObject is MyCharacter)
                                {
                                    MySession.Player.Init((MyCharacter)controlledObject, null, identityId);
                                }
                            }
                        }

                        if (!MySandboxGame.IsDedicated)
                            MySession.Player.Controller.TakeControl((IMyControllableEntity)controlledObject);
                    }
                }

                if (checkpoint.Players != null)
                {
                    foreach (var playerIt in checkpoint.Players.Dictionary)
                    {
                        if (playerIt.Key == Player.SteamUserId)
                        {
                            Player.PlayerId = identityId;
                            if (string.IsNullOrEmpty(Player.Model))
                            {
                                if (!string.IsNullOrEmpty(playerIt.Value.PlayerModel))
                                    Player.Model = playerIt.Value.PlayerModel;
                                else
                                    Player.Model = MyCharacter.DefaultModel;
                            }
                        }

                        MyPlayer player;
                        if (Sync.Controllers.TryGetPlayer(playerIt.Key, out player))
                        {
                            MyCharacter playerEntity;
                            if (MyEntities.TryGetEntityById<MyCharacter>(playerIt.Value.PlayerEntity, out playerEntity))
                            {
                                player.Init(playerEntity, playerIt.Value.PlayerModel, playerIt.Key == Player.SteamUserId ? identityId : playerIt.Value.PlayerId);
                            }
                        }
                    }
                }
            }*/
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

            foreach (var player in m_players.Values)
            {
                var id = new MyObjectBuilder_Checkpoint.PlayerId() { ClientId = player.Id.SteamId, SerialId = player.Id.SerialId };

                MyObjectBuilder_Player playerOb = new MyObjectBuilder_Player();
                playerOb.DisplayName = player.DisplayName;
                playerOb.IdentityId = player.Identity.IdentityId;
                playerOb.Connected = true;

                checkpoint.AllPlayersData.Dictionary.Add(id, playerOb);
            }

            foreach (var entry in m_playerIdentityIds)
            {
                if (m_players.ContainsKey(entry.Key) || entry.Key.SerialId != 0) continue;

                var id = new MyObjectBuilder_Checkpoint.PlayerId() { ClientId = entry.Key.SteamId, SerialId = entry.Key.SerialId };
                MyObjectBuilder_Player playerOb = new MyObjectBuilder_Player();
                playerOb.Connected = false;
                playerOb.IdentityId = entry.Value;
                var identity = TryGetIdentity(entry.Value);
                playerOb.DisplayName = identity != null ? identity.DisplayName : null;

                checkpoint.AllPlayersData.Dictionary.Add(id, playerOb);
            }

            //foreach (var entry in m_playerIdentityIds)
            //{
            //    // Skip connected players
            //    if (m_players.ContainsKey(entry.Key)) continue;

            //    var id = new MyObjectBuilder_Checkpoint.PlayerId() { ClientId = entry.Key.SteamId, SerialId = entry.Key.SerialId };

            //    checkpoint.DisconnectedPlayers.Dictionary.Add(id, entry.Value);
            //}
        }

        private List<AllPlayerData> SavePlayers()
        {
            var allPlayersData = new List<AllPlayerData>();

            foreach (var player in m_players.Values)
            {
                AllPlayerData data = new AllPlayerData();
                data.SteamId = player.Id.SteamId;
                data.SerialId = player.Id.SerialId;

                MyObjectBuilder_Player playerOb = new MyObjectBuilder_Player();
                playerOb.DisplayName = player.DisplayName;
                playerOb.IdentityId = player.Identity.IdentityId;
                playerOb.Connected = true;

                data.Player = playerOb;

                allPlayersData.Add(data);
            }

            return allPlayersData;
        }

        private void RemovePlayerFromDictionary(PlayerId playerId)
        {
            m_players.Remove(playerId);
            OnPlayersChanged(false, playerId.SteamId);
        }

        private void AddPlayer(PlayerId playerId, MyPlayer newPlayer)
        {
            m_players.Add(playerId, newPlayer);
            OnPlayersChanged(true, playerId.SteamId);
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

        private void OnPlayersChanged(bool added, ulong steamId)
        {
            var handler = PlayersChanged;
            if (handler != null)
            {
                handler(added, steamId);
            }
        }

        #endregion

        #region Multiplayer handlers

        static void OnControlChangedRequest(MySyncEntity entity, ref ControlChangedMsg msg, MyNetworkClient sender)
        {
            PlayerId id = new PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            MyTrace.Send(TraceWindow.Multiplayer, "OnControlChanged to entity: " + msg.EntityId, id.ToString());

            Sync.Players.SetControlledEntityInternal(id, entity.Entity);
            
            Debug.Assert(sender.SteamUserId == msg.ClientSteamId);
            msg.ClientSteamId = sender.SteamUserId;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void OnControlChangedSuccess(MySyncEntity entity, ref ControlChangedMsg msg, MyNetworkClient sender)
        {
            PlayerId id = new PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            MyTrace.Send(TraceWindow.Multiplayer, "OnControlChanged to entity: " + msg.EntityId, id.ToString());

            Sync.Players.SetControlledEntityInternal(id, entity.Entity);
        }

        static void OnCharacterChanged(MySyncEntity entity, ref CharacterChangedMsg msg, MyNetworkClient sender)
        {
            MyCharacter characterEntity = entity.Entity as MyCharacter;
            MyEntity controlledEntity;
            if (!MyEntities.TryGetEntity(msg.ControlledEntityId, out controlledEntity))
            {
                MySandboxGame.Log.WriteLine("Controlled entity not found");
                Debug.Fail("Controlled entity not found");
            }

            PlayerId id = new PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            MyPlayer player = Sync.Players.TryGetPlayerById(id);

            ChangePlayerCharacterInternal(player, characterEntity, controlledEntity);
        }

        static void OnControlReleased(MySyncEntity entity, ref ControlReleasedMsg msg, MyNetworkClient sender)
        {
            Sync.Players.RemoveControlledEntityInternal(entity.Entity);

            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAll(ref msg);
            }
        }

        static void OnIdentityCreated(ref IdentityCreatedMsg msg, MyNetworkClient sender)
        {
            var identity = Sync.Players.CreateNewIdentity(msg.DisplayName, msg.IdentityId, model: null);
            if (msg.IsNPC)
                Sync.Players.MarkIdentityAsNPC(identity.IdentityId);
        }

        static void OnPlayerIdentityChanged(ref PlayerIdentityChangedMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            var playerId = new PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            var player = Sync.Players.TryGetPlayerById(playerId);

            Debug.Assert(player != null, "Changing identity of an unknown or unconnected player!");
            if (player == null) return;

            MyIdentity identity = null;
            Sync.Players.m_allIdentities.TryGetValue(msg.IdentityId, out identity);
            Debug.Assert(identity != null, "Changing player's identity to an unknown identity!");
            if (identity == null) return;

            player.ChangeIdentity(identity);
        }

        static void OnRespawnRequestFailure(ref RespawnMsg msg, MyNetworkClient sender)
        {
            MyPlayerCollection.RequestLocalRespawn();
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

            Sync.Players.CreateNewPlayer(identity, playerId, msg.DisplayName);

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

            var playerId = new MyPlayer.PlayerId(msg.ClientSteamId, msg.PlayerSerialId);

            Sync.Players.CreateNewPlayerInternal(identity, client, msg.DisplayName, ref playerId);
        }

        static void OnAll_Identities_Players_Factions_Request(ref All_Identities_Players_Factions_RequestMsg msg, MyNetworkClient sender)
        {
            var response = new All_Identities_Players_Factions_SuccessMsg();
            response.Identities = Sync.Players.SaveIdentities();
            response.Players = Sync.Players.SavePlayers();
            response.Factions = MySession.Static.Factions.SaveFactions();

            Sync.Layer.SendMessage(ref response, sender.SteamUserId, messageType: MyTransportMessageEnum.Success);
        }

        static void OnAll_Identities_Players_Factions_Success(ref All_Identities_Players_Factions_SuccessMsg msg, MyNetworkClient sender)
        {
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

            var player = Sync.Players.TryGetPlayerById(new PlayerId(msg.ClientSteamId, msg.PlayerSerialId));
            Debug.Assert(player != null, "Could not find a player to remove!");
            if (player == null) return;

            Sync.Players.RemovePlayer(player);
        }

        static void OnPlayerRemoved(ref PlayerRemoveMsg msg, MyNetworkClient sender)
        {
            var playerId = new MyPlayer.PlayerId(msg.ClientSteamId, msg.PlayerSerialId);
            Debug.Assert(Sync.Players.m_players.ContainsKey(playerId));

            if (msg.ClientSteamId == Sync.MyId)
                Sync.Players.RaiseLocalPlayerRemoved(msg.PlayerSerialId);

            Sync.Players.RemovePlayerFromDictionary(playerId);
        }

        #endregion

        #region Public methods

        public void RequestNewPlayer(int serialNumber, string playerName, string characterModel)
        {
            var msg = new NewPlayerRequestMsg();
            msg.ClientSteamId = MySteam.UserId;
            msg.PlayerSerialId = serialNumber;
            msg.DisplayName = playerName;
            msg.CharacterModel = characterModel;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        public MyPlayer CreateNewPlayer(MyIdentity identity, MyNetworkClient steamClient, string playerName)
        {
            Debug.Assert(Sync.IsServer);

            // TODO: Limit number of players per client
            var playerId = FindFreePlayerId(steamClient.SteamUserId);
            return CreateNewPlayerInternal(identity, steamClient, playerName, ref playerId);
        }

        public MyPlayer CreateNewPlayer(MyIdentity identity, PlayerId id, string playerName)
        {
            Debug.Assert(Sync.IsServer);

            MyNetworkClient steamClient;
            Sync.Clients.TryGetClient(id.SteamId, out steamClient);
            Debug.Assert(steamClient != null, "Could not find a client for the new player!");
            if (steamClient == null) return null;

            var player = CreateNewPlayerInternal(identity, steamClient, playerName, ref id);
            if (player != null)
            {
                var msg = new PlayerCreatedMsg();
                msg.ClientSteamId = id.SteamId;
                msg.PlayerSerialId = id.SerialId;
                msg.IdentityId = identity.IdentityId;
                msg.DisplayName = playerName;

                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
            return player;
        }

        //public MyPlayer InitNewPlayer(MyIdentity identity, PlayerId id, string playerName)
        //{
        //    MyNetworkClient steamClient;
        //    Sync.Clients.TryGetClient(id.SteamId, out steamClient);
        //    Debug.Assert(steamClient != null, "Could not find a client for the new player!");
        //    if (steamClient == null) return null;

        //    return CreateNewPlayerInternal(identity, steamClient, playerName, ref id);
        //}

        public MyPlayer InitNewPlayer(MyIdentity identity, PlayerId id, MyObjectBuilder_Player playerOb)
        {
            MyNetworkClient steamClient;
            Sync.Clients.TryGetClient(id.SteamId, out steamClient);
            Debug.Assert(steamClient != null, "Could not find a client for the new player!");
            if (steamClient == null) return null;

            MyPlayer playerInstance = CreateNewPlayerInternal(identity, steamClient, playerOb.DisplayName, ref id);
            return playerInstance;
        }

        public void RemovePlayer(MyPlayer player)
        {
            if (Sync.IsServer)
            {
                if (player.Controller.ControlledEntity != null)
                {
                    foreach (var controlEntry in m_controlledEntities)
                    {
                        if (controlEntry.Value != player.Id) continue;
                        if (controlEntry.Key == player.Controller.ControlledEntity.Entity.EntityId) continue;

                        m_controlledEntities.Remove(controlEntry.Key);
                    }
                    m_controlledEntities.ApplyRemovals();
                    player.Controller.TakeControl(null);
                }

                if (player.Character != null)
                {
                    //Dont remove character if he's sleeping in a cryo chamber
                    if (!(player.Character.Parent is Sandbox.Game.Entities.Blocks.MyCryoChamber))
                    {
                        player.Character.SyncObject.SendCloseRequest();
                    }
                }

                KillPlayer(player);

                if (player.IsLocalPlayer())
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
                Debug.Assert(player.IsLocalPlayer(), "Client can only remove local players!");
                if (player.IsRemotePlayer()) return;

                var msg = new PlayerRemoveMsg();
                msg.ClientSteamId = player.Id.SteamId;
                msg.PlayerSerialId = player.Id.SerialId;
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }

        public MyPlayer TryGetPlayerById(PlayerId id)
        {
            MyPlayer player = null;
            m_players.TryGetValue(id, out player);
            return player;
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
                MyEntities.TryGetEntity(entry.Key, out entity);
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

        #endregion

        #region Respawn

        public void SetRespawnComponent(IMyRespawnComponent respawnComponent)
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
                player.Identity.SetDead(resetIdentity);
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
            else
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        private bool SetPlayerDeadInternal(ulong playerSteamId, int playerSerialId, bool deadState, bool resetIdentity)
        {
            PlayerId id = new PlayerId(playerSteamId, playerSerialId);
            var player = Sync.Players.TryGetPlayerById(id);
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

        private MyPlayer CreateNewPlayerInternal(MyIdentity identity, MyNetworkClient steamClient, string playerName, ref PlayerId playerId)
        {
            MyPlayer newPlayer = new MyPlayer(steamClient, playerName, playerId);

            if (!m_playerIdentityIds.ContainsKey(playerId))
            {
                m_playerIdentityIds.Add(playerId, identity.IdentityId);
            }

            newPlayer.ChangeIdentity(identity);
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
                Sync.Players.TryGetPlayerById(id) == null || Sync.Players.TryGetPlayerById(id).Controller.ControlledEntity != entity,
                "Setting the controlled entity to the same value. Is that what you wanted?");

            RemoveControlledEntityInternal(entity);

            entity.OnClosing += m_entityClosingHandler;

            // TakeControl will take care of setting controlled entity into m_controlledEntities
            var player = Sync.Players.TryGetPlayerById(id);
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

        public void RemoveIdentity(MyPlayer.PlayerId pid, long identityId)
        {
            var msg = new IdentityRemoveMsg();
            msg.IdentityId = identityId;
            msg.ClientSteamId = pid.SteamId;
            msg.PlayerSerialId = pid.SerialId;

            if (Sync.IsServer)
            {
                RemoveIdentityInternal(pid, identityId);
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
            else
            {
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }

        // remember about ownership when removing identity
        public void RemoveIdentity(MyPlayer.PlayerId id)
        { 
            var identity = TryGetPlayerIdentity(id);
            Debug.Assert(identity != null, "Identity not found.");
            if (identity == null)
                return;

            RemoveIdentity(id, identity.IdentityId);
        }

        private static void OnIdentityRemoveRequest(ref IdentityRemoveMsg msg, MyNetworkClient sender)
        {
            Sync.Players.RemoveIdentityInternal(new PlayerId(msg.ClientSteamId, msg.PlayerSerialId), msg.IdentityId);
        }

        private static void OnIdentityRemoveSuccess(ref IdentityRemoveMsg msg, MyNetworkClient sender)
        {
            Sync.Players.RemoveIdentityInternal(new PlayerId(msg.ClientSteamId, msg.PlayerSerialId), msg.IdentityId);
        }

        private bool RemoveIdentityInternal(PlayerId id, long identityId)
        {
            Debug.Assert(m_allIdentities.ContainsKey(identityId), "Identity could not be found");
            Debug.Assert(m_playerIdentityIds.ContainsKey(id), "Identity could not be found");
            Debug.Assert(!m_players.ContainsKey(id), "Cannot remove identity of active player");
            if (m_players.ContainsKey(id))
                return false;

            MyIdentity identity;
            if (m_allIdentities.TryGetValue(identityId, out identity))
            {
                identity.CharacterChanged -= Identity_CharacterChanged;
                if (identity.Character != null)
                {
                    identity.Character.CharacterDied -= Character_CharacterDied;
                }
            }

            m_allIdentities.Remove(identityId);
            m_playerIdentityIds.Remove(id);
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

        private void LoadIdentities(List<MyObjectBuilder_Identity> list)
        {
            foreach (var objectBuilder in list)
            {
                var identity = CreateNewIdentity(objectBuilder);
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
        }

        void Character_CharacterDied(MyCharacter diedCharacter)
        {
            if (PlayerCharacterDied != null && diedCharacter != null && diedCharacter.ControllerInfo.ControllingIdentityId != 0)
                PlayerCharacterDied(diedCharacter.ControllerInfo.ControllingIdentityId);
        }

        void Identity_CharacterChanged(MyCharacter oldCharacter, MyCharacter newCharacter)
        {
            if (oldCharacter != null)
                oldCharacter.CharacterDied -= Character_CharacterDied;

            if (newCharacter != null)
                newCharacter.CharacterDied += Character_CharacterDied;
        }

        //private void LoadPlayerInternal(long identityId, ref PlayerId playerId, string playerName, bool obsolete = false)
        //{
        //    var identity = TryGetIdentity(identityId);
        //    Debug.Assert(identity != null, "Identity of a player was null when loading! Inconsistency!");
        //    if (identity == null) return;
        //    if (obsolete && identity.IsDead) return;

        //    // This happens when you load an existing game - only the local player will be in Sync.Clients, but there were
        //    // more connected players at the time of the save. In this case, we have to consider them as disconnected players
        //    if (!Sync.Clients.HasClient(playerId.SteamId))
        //    {
        //        if (!m_playerIdentityIds.ContainsKey(playerId))
        //            m_playerIdentityIds.Add(playerId, identityId);
        //        identity.SetDead(true);
        //        return;
        //    }

        //    InitNewPlayer(identity, playerId, playerName);
        //    return;
        //}

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

            var player = InitNewPlayer(identity, playerId, playerOb);

            if (player.IsLocalPlayer())
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
                bool isLocal = player.Value.IsLocalPlayer();
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
