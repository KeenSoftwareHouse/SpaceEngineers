using ProtoBuf;
using Sandbox;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
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
using VRage.Utils;
using VRageMath;
using VRageRender;
using Sandbox.Game.SessionComponents;
using VRage.Components;
using VRage.Network;
using Sandbox.Game.GameSystems;

namespace SpaceEngineers.Game.Players
{
    [PreloadRequired]
    [StaticEventOwner]
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MySpaceRespawnComponent : MyRespawnComponentBase
    {
        [ProtoContract]
        struct RespawnCooldownEntry
        {
            [ProtoMember]
            public int ControllerId;

            [ProtoMember]
            public string ShipId;

            [ProtoMember]
            public int RelativeRespawnTime;
        }

        [MessageId(9384, P2PMessageEnum.Reliable)]
        struct SyncCooldownRequestMessage { }

        [ProtoContract]
        [MessageId(9385, P2PMessageEnum.Reliable)]
        struct SyncCooldownResponseMessage
        {
            [ProtoMember]
            public RespawnCooldownEntry[] RespawnTimes;
        }

        int m_lastUpdate;
        bool m_updatingStopped;
        int m_updateCtr;

        bool m_synced;
        public bool IsSynced { get { return m_synced; } }

        public static MySpaceRespawnComponent Static { get { return Sync.Players.RespawnComponent as MySpaceRespawnComponent; } }

        private List<RespawnCooldownEntry> m_tmpRespawnTimes = new List<RespawnCooldownEntry>();

        int MAX_DISTANCE_TO_RESPAWN = 50000;

        struct RespawnKey : IEquatable<RespawnKey>
        {
            public MyPlayer.PlayerId ControllerId;
            public string RespawnShipId;

            public bool Equals(RespawnKey other)
            {
                return ControllerId == other.ControllerId && RespawnShipId == other.RespawnShipId;
            }

            public override int GetHashCode()
            {
                return ControllerId.GetHashCode() ^ (RespawnShipId == null ? 0 : RespawnShipId.GetHashCode());
            }
        }

        private CachingDictionary<RespawnKey, int> m_globalRespawnTimesMs = new CachingDictionary<RespawnKey, int>();

        static MySpaceRespawnComponent()
        {
            MySyncLayer.RegisterMessage<SyncCooldownRequestMessage>(OnSyncCooldownRequest, MyMessagePermissions.ToServer);
            MySyncLayer.RegisterMessage<SyncCooldownResponseMessage>(OnSyncCooldownResponse, MyMessagePermissions.FromServer);
        }

        public void RequestSync()
        {
            var msg = new SyncCooldownRequestMessage();

            Sync.Layer.SendMessageToServer(ref msg);
        }

        public override void InitFromCheckpoint(MyObjectBuilder_Checkpoint checkpoint)
        {
            var cooldowns = checkpoint.RespawnCooldowns;

            m_lastUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_globalRespawnTimesMs.Clear();

            if (cooldowns == null) return;

            foreach (var item in cooldowns)
            {
                var controllerId = new MyPlayer.PlayerId() { SteamId = item.PlayerSteamId, SerialId = item.PlayerSerialId };
                var key = new RespawnKey() { ControllerId = controllerId, RespawnShipId = item.RespawnShipId };
                m_globalRespawnTimesMs.Add(key, item.Cooldown + m_lastUpdate, immediate: true);
            }
        }

        public override void SaveToCheckpoint(MyObjectBuilder_Checkpoint checkpoint)
        {
            var cooldowns = checkpoint.RespawnCooldowns;

            foreach (var pair in m_globalRespawnTimesMs)
            {
                int cooldown = pair.Value - m_lastUpdate;
                if (cooldown <= 0) continue;

                var item = new Sandbox.Common.ObjectBuilders.MyObjectBuilder_Checkpoint.RespawnCooldownItem();
                item.PlayerSteamId = pair.Key.ControllerId.SteamId;
                item.PlayerSerialId = pair.Key.ControllerId.SerialId;
                item.RespawnShipId = pair.Key.RespawnShipId;
                item.Cooldown = cooldown;

                cooldowns.Add(item);
            }
        }

        public override void BeforeStart()
        {
            base.BeforeStart();

            m_lastUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_updatingStopped = false;
            m_updateCtr = 0;

            if (!Sync.IsServer)
            {
                m_synced = false;
                RequestSync();
            }
            else
            {
                m_globalRespawnTimesMs.Clear();
                m_synced = true;
            }
        }

        public override void LoadData()
        {
            base.LoadData();

            Sync.Players.RespawnComponent = this;
            Sync.Players.LocalRespawnRequested += OnLocalRespawnRequest;
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            Sync.Players.LocalRespawnRequested -= OnLocalRespawnRequest;
            Sync.Players.RespawnComponent = null;
        }

        static void OnSyncCooldownRequest(ref SyncCooldownRequestMessage msg, MyNetworkClient sender)
        {
            MySpaceRespawnComponent.Static.SyncCooldownToPlayer(sender.SteamUserId);
        }

        static void OnSyncCooldownResponse(ref SyncCooldownResponseMessage msg, MyNetworkClient sender)
        {
            msg = MySpaceRespawnComponent.Static.SyncCooldownResponse(msg);
        }

        private SyncCooldownResponseMessage SyncCooldownResponse(SyncCooldownResponseMessage msg)
        {
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            // msg.respawnTimes can be null, if the server sent empty list
            if (msg.RespawnTimes != null)
            {
                foreach (var respawnTime in msg.RespawnTimes)
                {
                    var controllerId = new MyPlayer.PlayerId() { SteamId = MySteam.UserId, SerialId = respawnTime.ControllerId };
                    var key = new RespawnKey() { ControllerId = controllerId, RespawnShipId = respawnTime.ShipId };

                    m_globalRespawnTimesMs.Add(key, currentTime + respawnTime.RelativeRespawnTime, immediate: true);
                }
            }

            m_synced = true;
            return msg;
        }

        public void SyncCooldownToPlayer(ulong steamId)
        {
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            m_tmpRespawnTimes.Clear();
            foreach (var entry in m_globalRespawnTimesMs)
            {
                // Send only those respawn times that concern the given steam player
                if (entry.Key.ControllerId.SteamId != steamId) continue;

                RespawnCooldownEntry syncEntry = new RespawnCooldownEntry();
                syncEntry.ControllerId = entry.Key.ControllerId.SerialId;
                syncEntry.ShipId = entry.Key.RespawnShipId;
                syncEntry.RelativeRespawnTime = entry.Value - currentTime;

                m_tmpRespawnTimes.Add(syncEntry);
            }

            SyncCooldownResponseMessage response = new SyncCooldownResponseMessage();
            response.RespawnTimes = m_tmpRespawnTimes.ToArray();

            Sync.Layer.SendMessage(ref response, steamId);
            m_tmpRespawnTimes.Clear();
        }

        public override void UpdatingStopped()
        {
            base.UpdatingStopped();

            m_updatingStopped = true;
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            int dt = currentTime - m_lastUpdate;
            if (m_updatingStopped)
            {
                // We need to shift the last respawn times towards the future, so that the respawn countdowns correspond to the paused time
                UpdateRespawnTimes(dt);

                m_lastUpdate = currentTime;
                m_updatingStopped = false;
            }
            else
            {
                m_updateCtr++;
                m_lastUpdate = currentTime;
                if (m_updateCtr % 100 == 0)
                {
                    RemoveOldRespawnTimes();
                }
            }

            // Debug draw
            /*if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
            {
                List<MyCubeBlock> respawns = null;
                GetNearestRespawn(MySession.LocalCharacter == null ? Vector3.Zero : (Vector3)MySession.LocalCharacter.PositionComp.GetPosition(), out respawns, MySession.LocalHumanPlayer.Identity.IdentityId);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "No. of respawn points: " + respawns.Count, Color.Red, 1.0f);
            }*/
        }

        private void UpdateRespawnTimes(int delta)
        {
            foreach (var key in m_globalRespawnTimesMs.Keys)
            {
                m_globalRespawnTimesMs[key] = m_globalRespawnTimesMs[key] + delta;
            }
            m_globalRespawnTimesMs.ApplyAdditionsAndModifications();
        }

        private void RemoveOldRespawnTimes()
        {
            var respawnShips = MyDefinitionManager.Static.GetRespawnShipDefinitions();
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            foreach (var key in m_globalRespawnTimesMs.Keys)
            {
                int time = m_globalRespawnTimesMs[key];
                if (currentTime - time >= 0)
                    m_globalRespawnTimesMs.Remove(key);
            }
            m_globalRespawnTimesMs.ApplyRemovals();
        }

        public void ResetRespawnCooldown(MyPlayer.PlayerId controllerId)
        {
            var respawnShips = MyDefinitionManager.Static.GetRespawnShipDefinitions();
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            float multiplier = MySession.Static.Settings.SpawnShipTimeMultiplier;

            foreach (var pair in respawnShips)
            {
                var key = new RespawnKey() { ControllerId = controllerId, RespawnShipId = pair.Key };
                if (multiplier != 0)
                {
                    m_globalRespawnTimesMs.Add(key, currentTime + (int)(pair.Value.Cooldown * 1000 * multiplier), immediate: true);
                }
                else
                {
                    m_globalRespawnTimesMs.Remove(key);
                }
            }
        }

        public int GetRespawnCooldownSeconds(MyPlayer.PlayerId controllerId, string respawnShipId)
        {
            var respawnShip = MyDefinitionManager.Static.GetRespawnShipDefinition(respawnShipId);
            System.Diagnostics.Debug.Assert(respawnShip != null);
            if (respawnShip == null) return 0;

            var key = new RespawnKey() { ControllerId = controllerId, RespawnShipId = respawnShipId };
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            int time = currentTime;
            m_globalRespawnTimesMs.TryGetValue(key, out time);
            return Math.Max((time - currentTime) / 1000, 0);
        }

        private void OnLocalRespawnRequest()
        {
            if (MyFakes.SHOW_FACTIONS_GUI && !MySession.Static.CreativeMode)
            {
                ulong playerId = MySession.LocalHumanPlayer != null ? MySession.LocalHumanPlayer.Id.SteamId : 0;
                int serialId = MySession.LocalHumanPlayer != null ? MySession.LocalHumanPlayer.Id.SerialId : 0;
                MyMultiplayer.RaiseStaticEvent(s => RespawnRequest_Implementation, playerId, serialId);
            }
            else
            {
                MyPlayerCollection.RespawnRequest(MySession.LocalHumanPlayer == null, false, 0, null);
            }
        }

        [Event, Reliable, Server]
        static void RespawnRequest_Implementation(ulong steamPlayerId, int serialId)
        {
            var playerId = new MyPlayer.PlayerId(steamPlayerId, serialId);
            var player = Sync.Players.GetPlayerById(playerId);

            if(false == TryFindCryoChamberCharacter(player))
            {
                MyMultiplayer.RaiseStaticEvent(s => ShowMedicalScreen_Implementation, new EndpointId(steamPlayerId));
            }
        }

        static bool TryFindCryoChamberCharacter(MyPlayer player)
        {
            if (player == null)
            {
                return false;
            }

            var entities = MyEntities.GetEntities();

            foreach (var entity in entities)
            {
                var cubeGrid = entity as MyCubeGrid;
                if (cubeGrid != null)
                {
                    var blocks = cubeGrid.GetFatBlocks<Sandbox.Game.Entities.Blocks.MyCryoChamber>();
                    foreach (var cryoChamber in blocks)
                    {
                        if (cryoChamber.TryToControlPilot(player))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        [Event, Reliable, Client]
        static void ShowMedicalScreen_Implementation()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenMedicals());
        }

        public override bool HandleRespawnRequest(bool joinGame, bool newIdentity, long medicalRoomId, string respawnShipId, MyPlayer.PlayerId playerId, Vector3D? spawnPosition)
        {
            MyPlayer player = Sync.Players.GetPlayerById(playerId);

            bool spawnAsNewPlayer = newIdentity || player == null;
            Debug.Assert(player == null || player.Identity != null, "Respawning player has no identity!");

            if (!MySessionComponentMissionTriggers.CanRespawn(playerId))
                return false;

            Vector3D currentPosition = Vector3D.Zero;
            if (player != null && player.Character != null) currentPosition = player.Character.PositionComp.GetPosition();

            if (TryFindCryoChamberCharacter(player))
            {
                //Player found in chamber;
                return true;
            }

            if (!spawnAsNewPlayer)
            {
                if (spawnPosition.HasValue)
                {
                    Vector3D gravity = MyGravityProviderSystem.CalculateTotalGravityInPoint(spawnPosition.Value);
                    if (Vector3D.IsZero(gravity))
                        gravity = Vector3D.Down;
                    else
                        gravity.Normalize();
                    Vector3D perpendicular;
                    gravity.CalculatePerpendicularVector(out perpendicular);
                    player.SpawnAt(MatrixD.CreateWorld(spawnPosition.Value, perpendicular, -gravity), Vector3.Zero, true);

                    return true;
                }

                // Find respawn block to spawn at
                MyRespawnComponent foundRespawn = null;
                if (medicalRoomId == 0 || !MyFakes.SHOW_FACTIONS_GUI)
                {
                    List<MyRespawnComponent> respawns = null;
                    var nearestRespawn = GetNearestRespawn(currentPosition, out respawns, MySession.Static.CreativeMode ? (long?)null : player.Identity.IdentityId);
                    if (joinGame && respawns.Count > 0)
                    {
                        foundRespawn = respawns[MyRandom.Instance.Next(0, respawns.Count)];
                    }
                }
                else
                {
                    foundRespawn = FindRespawnById(medicalRoomId, player);
                    if (foundRespawn == null)
                    {
                        return false;
                    }
                }

                // If spawning in respawn block fails, we will spawn as a new player
                if (foundRespawn != null)
                    SpawnInRespawn(player, foundRespawn);
                else
                    spawnAsNewPlayer = true;
            }

            if (spawnAsNewPlayer)
            {
                bool resetIdentity = MySession.Static.Settings.PermanentDeath.Value;
                if (player == null)
                {
                    var identity = Sync.Players.CreateNewIdentity(player.DisplayName);
                    player = Sync.Players.CreateNewPlayer(identity, playerId, player.DisplayName);
                    resetIdentity = false;
                }

                if (MySession.Static.CreativeMode)
                {
                    Vector3D? correctedPos = MyEntities.FindFreePlace(currentPosition, 1, 200);
                    if (correctedPos.HasValue) currentPosition = correctedPos.Value;
                    player.SpawnAt(Matrix.CreateTranslation(currentPosition), Vector3.Zero);
                }
                else
                {
                    SpawnAsNewPlayer(player, currentPosition, respawnShipId, resetIdentity);
                }
            }

            return true;
        }

        private void SpawnInRespawn(MyPlayer player, MyRespawnComponent respawn)
        {
            if (MySession.Static.Settings.EnableOxygen)
            {
                player.Identity.ChangeToOxygenSafeSuit();
            }

            if (respawn.Entity == null)
            {
                Debug.Assert(false, "Respawn does not have entity!");
                SpawnInSuit(player);
                return;
            }
            var parent = respawn.Entity.GetTopMostParent();

            if (parent.Physics == null)
            {
                Debug.Assert(false, "Respawn entity parent does not have physics!");
                SpawnInSuit(player);
                return;
            }

            MatrixD pos;

            var medRoom = respawn.Entity as MyMedicalRoom;
            if (medRoom != null)
            {
                pos = medRoom.GetSpawnPosition();
            }
            else
            {
                pos = respawn.GetSpawnPosition(respawn.Entity.WorldMatrix);
            }
                
            Vector3 velocity = parent.Physics.GetVelocityAtPoint(pos.Translation);

            player.SpawnAt(pos, velocity, false);

            if (medRoom != null)
            {
                medRoom.TryTakeSpawneeOwnership(player);
                medRoom.TrySetFaction(player);
            }
        }

        private MyRespawnComponent FindRespawnById(long respawnBlockId, MyPlayer player)
        {
            MyCubeBlock respawnBlock = null;
            if (!MyEntities.TryGetEntityById(respawnBlockId, out respawnBlock)) return null;

            if (!respawnBlock.IsWorking) return null;

            var medicalRoom = respawnBlock as MyMedicalRoom;
            if (medicalRoom == null) return null;
            // CH: TODO: Move the extra functionality to SpaceRespawnEntityComponent or something...
            if (player != null && !medicalRoom.HasPlayerAccess(player.Identity.IdentityId) && !medicalRoom.SetFactionToSpawnee)
                return null;

            var respawnComponent = respawnBlock.Components.Get<MyRespawnComponent>();
            if (respawnComponent == null) return null;

            return respawnComponent;
        }

        private MyRespawnComponent GetNearestRespawn(Vector3 position, out List<MyRespawnComponent> respawns, long? identityId = null)
        {
            respawns = new List<MyRespawnComponent>();
            MyRespawnComponent closestRespawn = null;
            float closestDistance = float.MaxValue;
            foreach (var respawn in MyRespawnComponent.GetAllRespawns())
            {
                float distance = float.MaxValue;
                var block = respawn.Entity as MyCubeBlock;
                if (block != null)
                {
                    if (!block.IsWorking) continue;
                    if (identityId.HasValue && !block.GetUserRelationToOwner(identityId.Value).IsFriendly()) continue;

                    float distanceFromCenter = (float)block.PositionComp.GetPosition().Length();

                    //Limit spawn position to be inside the world (with some safe margin)
                    if ((!MyEntities.IsWorldLimited() && distanceFromCenter > MAX_DISTANCE_TO_RESPAWN) ||
                        (MyEntities.IsWorldLimited() && distanceFromCenter > MyEntities.WorldSafeHalfExtent()))
                        continue;

                    distance = Vector3.Distance(position, block.PositionComp.GetPosition());
                }
                else
                {
                    if (respawn.Entity == null) continue;
                    if (respawn.Entity.PositionComp == null) continue;

                    distance = Vector3.Distance(position, respawn.Entity.PositionComp.GetPosition());
                }

                if (distance < closestDistance)
                {
                    closestRespawn = respawn;
                    closestDistance = distance;
                }

                respawns.Add(respawn);
            }

            return closestRespawn;
        }

        public void SpawnAsNewPlayer(MyPlayer player, Vector3 currentPosition, string respawnShipId, bool resetIdentity)
        {
            Debug.Assert(Sync.IsServer, "Calling SpawnAsNewPlayer on client!");
            Debug.Assert(player.Identity != null, "Spawning with empty identity!");
            if (!Sync.IsServer || player.Identity == null) return;

            if (resetIdentity)
            {
                ResetPlayerIdentity(player);
            }

            if (MySession.Static.Settings.EnableOxygen)
            {
                player.Identity.ChangeToOxygenSafeSuit();
            }

            if (respawnShipId != null)
            {
                SpawnAtShip(player, respawnShipId);
            }
            else
            {
                SpawnInSuit(player);
            }
        }

        public void SpawnAtShip(MyPlayer player, string respawnShipId)
        {
            Debug.Assert(Sync.IsServer, "Spawning can only be called on the server!");
            if (!Sync.IsServer) return;

            ResetRespawnCooldown(player.Id);
            if (Sync.MultiplayerActive)
                SyncCooldownToPlayer(player.Id.SteamId);

            MyCharacter character = null;
            MyCockpit cockpit = null;
            List<MyCubeGrid> respawnGrids = new List<MyCubeGrid>();

            var respawnShipDef = MyDefinitionManager.Static.GetRespawnShipDefinition(respawnShipId);
            MyPrefabDefinition prefabDef = respawnShipDef.Prefab;

            // Deploy ship
            Vector3 direction, position;
            GetSpawnPosition(prefabDef.BoundingSphere.Radius, out direction, out position);
            MyPrefabManager.Static.SpawnPrefab(
                respawnGrids,
                prefabDef.Id.SubtypeName,
                position,
                -direction,
                Vector3.CalculatePerpendicularVector(-direction),
                spawningOptions: Sandbox.ModAPI.SpawningOptions.RotateFirstCockpitTowardsDirection,
                updateSync: true);

            // Find cockpit
            foreach (var grid in respawnGrids)
            {
                foreach (var block in grid.GetBlocks())
                {
                    if (block.FatBlock is MyCockpit)
                    {
                        cockpit = (MyCockpit)block.FatBlock;
                        break;
                    }
                }
                if (cockpit != null) break;
            }
            System.Diagnostics.Debug.Assert(cockpit != null, "character is spawning in ship without cockpit !");

            // Create character
            Matrix matrix = Matrix.Identity;
            if (cockpit != null)
            {
                matrix = cockpit.WorldMatrix;
                matrix.Translation = cockpit.WorldMatrix.Translation - Vector3.Up - Vector3.Forward;
            }
            else if (respawnGrids.Count > 0)
            {
                matrix.Translation = respawnGrids[0].PositionComp.WorldAABB.Center + respawnGrids[0].PositionComp.WorldAABB.HalfExtents;
            }

            character = MyCharacter.CreateCharacter(matrix, Vector3.Zero, player.Identity.DisplayName, player.Identity.Model, null, cockpit: cockpit,playerSteamId: player.Id.SteamId);

            if (cockpit != null)
            {
                cockpit.AttachPilot(character, false);
            }

            CloseRespawnShip(player);
            foreach (var respawnGrid in respawnGrids)
            {
                respawnGrid.ChangeGridOwnership(player.Identity.IdentityId, MyOwnershipShareModeEnum.None);
                player.RespawnShip.Add(respawnGrid.EntityId);
            }

            Sync.Players.SetPlayerCharacter(player, character, cockpit);
            Sync.Players.RevivePlayer(player);
        }

        public override void AfterRemovePlayer(MyPlayer player)
        {
            CloseRespawnShip(player);
        }

        private static void CloseRespawnShip(MyPlayer player)
        {
            if (!MySession.Static.Settings.RespawnShipDelete)
                return;

            System.Diagnostics.Debug.Assert(player.RespawnShip != null, "Closing a null respawn ship");
            if (player.RespawnShip == null) return;

            foreach (var entityId in player.RespawnShip)
            {
                MyCubeGrid oldHome;
                if (MyEntities.TryGetEntityById<MyCubeGrid>(entityId, out oldHome))
                {
                    foreach (var b in oldHome.GetBlocks())
                    {
                        var c = b.FatBlock as MyCockpit;
                        if (c != null && c.Pilot != null)
                            c.Use();
                    }
                    oldHome.SyncObject.SendCloseRequest();
                }
            }

            player.RespawnShip.Clear();
        }

        private void SpawnInSuit(MyPlayer player)
        {
            Vector3 direction, position;
            GetSpawnPosition(10, out direction, out position);

            //Create character
            Matrix matrix = Matrix.CreateWorld(position, direction, Vector3.Up);
            MyCharacter character = MyCharacter.CreateCharacter(matrix, Vector3.Zero, player.Identity.DisplayName, player.Identity.Model, null);

            Sync.Players.SetPlayerCharacter(player, character);
            Sync.Players.RevivePlayer(player);
        }

        public static void GetSpawnPosition(float collisionRadius, out Vector3 direction, out Vector3 position)
        {
            float distance = 0;
            foreach (var entity in MyEntities.GetEntities())
            {
                // Include only voxels
                if (entity is MyVoxelMap)
                {
                    distance = (float)MathHelper.Max(distance, entity.PositionComp.WorldVolume.Center.Length() + entity.PositionComp.WorldVolume.Radius);
                }
            }

            // 500 - 650m from last voxel
            distance += MyUtils.GetRandomFloat(500, 650);

            if (MyEntities.IsWorldLimited())
                distance = Math.Min(distance, MyEntities.WorldSafeHalfExtent());
            else
                distance = Math.Min(distance, 20000); // limited spawn area in infinite worlds

            direction = MyUtils.GetRandomVector3Normalized();
            var searchPosition = MyEntities.FindFreePlace((Vector3D)(direction * distance), collisionRadius);
            if (!searchPosition.HasValue)
                searchPosition = (Vector3D)(direction * distance); // Spawn in existing place (better than crash)

            position = (Vector3)searchPosition.Value;
        }

        public override MyIdentity CreateNewIdentity(string identityName, MyPlayer.PlayerId playerId, string modelName)
        {
            return Sync.Players.CreateNewIdentity(identityName, modelName);
        }

        public override void SetupCharacterDefault(MyPlayer player, MyWorldGenerator.Args args)
        {
            string respawnShipId = MyDefinitionManager.Static.GetFirstRespawnShip();
            SpawnAtShip(player, respawnShipId);
        }

        public override int CountAvailableSpawns(MyPlayer player)
        {
            return MyMedicalRoom.AvailableMedicalRoomsCount(player.Identity.IdentityId);
        }
        public override bool IsInRespawnScreen() 
        { 
            return MyGuiScreenMedicals.Static != null && MyGuiScreenMedicals.Static.State == MyGuiScreenState.OPENED;
        }
        public override void CloseRespawnScreen()
        {
            MyGuiScreenMedicals.Close();
        }
        public override void SetNoRespawnText(StringBuilder text, int timeSec)
        {
            MyGuiScreenMedicals.SetNoRespawnText(text, timeSec);
        }
    }
}

