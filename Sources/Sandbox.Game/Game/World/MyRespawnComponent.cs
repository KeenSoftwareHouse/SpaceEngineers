using ProtoBuf;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
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
using VRageMath;
using VRageRender;

namespace Sandbox.Game.World
{
    [PreloadRequired]
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MyRespawnComponent : MySessionComponentBase, IMyRespawnComponent
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

        static int m_lastUpdate;
        static bool m_updatingStopped;
        static int m_updateCtr;

        static bool m_synced;
        public static bool IsSynced { get { return m_synced; } }

        private static List<RespawnCooldownEntry> m_tmpRespawnTimes = new List<RespawnCooldownEntry>();

        static int MAX_DISTANCE_TO_RESPAWN = 50000;

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

        private static CachingDictionary<RespawnKey, int> m_globalRespawnTimesMs = new CachingDictionary<RespawnKey, int>();

        public override bool IsRequiredByGame
        {
            get
            {
                return MyPerGameSettings.RespawnComponentType == typeof(MyRespawnComponent);
            }
        }

        static MyRespawnComponent()
        {
            MySyncLayer.RegisterMessage<SyncCooldownRequestMessage>(SyncCooldownRequest, MyMessagePermissions.ToServer);
            MySyncLayer.RegisterMessage<SyncCooldownResponseMessage>(SyncCooldownResponse, MyMessagePermissions.FromServer);
        }

        public static void RequestSync()
        {
            var msg = new SyncCooldownRequestMessage();

            Sync.Layer.SendMessageToServer(ref msg);
        }

        public static void InitFromCheckpoint(List<Common.ObjectBuilders.MyObjectBuilder_Checkpoint.RespawnCooldownItem> list)
        {
            m_lastUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_globalRespawnTimesMs.Clear();

            if (list == null) return;

            foreach (var item in list)
            {
                var controllerId = new MyPlayer.PlayerId() { SteamId = item.PlayerSteamId, SerialId = item.PlayerSerialId };
                var key = new RespawnKey() { ControllerId = controllerId, RespawnShipId = item.RespawnShipId };
                m_globalRespawnTimesMs.Add(key, item.Cooldown + m_lastUpdate, immediate: true);
            }
        }

        public static void SaveToCheckpoint(List<Common.ObjectBuilders.MyObjectBuilder_Checkpoint.RespawnCooldownItem> list)
        {
            foreach (var pair in m_globalRespawnTimesMs)
            {
                int cooldown = pair.Value - m_lastUpdate;
                if (cooldown <= 0) continue;

                var item = new Common.ObjectBuilders.MyObjectBuilder_Checkpoint.RespawnCooldownItem();
                item.PlayerSteamId = pair.Key.ControllerId.SteamId;
                item.PlayerSerialId = pair.Key.ControllerId.SerialId;
                item.RespawnShipId = pair.Key.RespawnShipId;
                item.Cooldown = cooldown;

                list.Add(item);
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

        static void SyncCooldownRequest(ref SyncCooldownRequestMessage msg, MyNetworkClient sender)
        {
            SyncCooldownToPlayer(sender.SteamUserId);
        }

        public static void SyncCooldownToPlayer(ulong steamId)
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

            Sync.Layer.SendMessage(response, steamId);
            m_tmpRespawnTimes.Clear();
        }

        static void SyncCooldownResponse(ref SyncCooldownResponseMessage msg, MyNetworkClient sender)
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

            #warning Fix the debug draw
            /*if (MyFakes.DEBUG_DRAW_RESPAWN_SHIP_COUNTERS)
            {
                StringBuilder sb = new StringBuilder();

                float y = 0.0f;
                foreach (var player in Sync.Controllers.AllPlayers)
                {
                    if (player.Value.IsDead) continue;
                    if (!Sync.IsServer && player.Value.SteamId != MySession.Player.SteamUserId) continue;

                    sb.Append(player.Value.DisplayName);
                    MyRenderProxy.DebugDrawText2D(new VRageMath.Vector2(0.0f, y), sb.ToString(), VRageMath.Color.White, 0.5f);
                    y += 10.0f;
                    sb.Clear();

                    foreach (var pair in MyDefinitionManager.Static.GetRespawnShipDefinitions())
                    {
                        int seconds = GetRespawnCooldownSeconds(player.Value.SteamId, pair.Key);
                        MyValueFormatter.AppendTimeExact(seconds, sb);
                        sb.Append("   ");
                        sb.Append(pair.Key);

                        MyRenderProxy.DebugDrawText2D(new VRageMath.Vector2(0.0f, y), sb.ToString(), VRageMath.Color.White, 0.5f);
                        y += 10.0f;
                        sb.Clear();
                    }
                    y += 5.0f;
                }
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

        public static void ResetRespawnCooldown(MyPlayer.PlayerId controllerId)
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

        public static int GetRespawnCooldownSeconds(MyPlayer.PlayerId controllerId, string respawnShipId)
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

        private static void OnLocalRespawnRequest()
        {
            if (MyFakes.SHOW_FACTIONS_GUI && !MySession.Static.CreativeMode)
            {
                //First check all the Cryo Chambers
                if (!TryFindCryoChamberCharacter(MySession.LocalHumanPlayer))
                {
                    //If nothing was found, go to respawn screen
                    MyGuiSandbox.AddScreen(new MyGuiScreenMedicals());
                }
            }
            else
                MyPlayerCollection.RespawnRequest(MySession.LocalHumanPlayer == null, false, 0, null);
        }

        private static bool TryFindCryoChamberCharacter(MyPlayer player)
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

        public bool HandleRespawnRequest(bool joinGame, bool newIdentity, long medicalRoomId, string respawnShipId, MyPlayer.PlayerId playerId, Vector3D? spawnPosition)
        {
            MyPlayer player = Sync.Players.TryGetPlayerById(playerId);

            bool spawnAsNewPlayer = newIdentity || player == null;
            Debug.Assert(player == null || player.Identity != null, "Respawning player has no identity!");

            Vector3D currentPosition = Vector3D.Zero;
            if (player != null && player.Character != null) currentPosition = player.Character.PositionComp.GetPosition();

            if (TryFindCryoChamberCharacter(player))
            {
                //Player found in chamber;
                return true;
            }

            if (!spawnAsNewPlayer)
            {
                // Find medical room to spawn at
                MyMedicalRoom medicalRoom = null;
                if (medicalRoomId == 0 || !MyFakes.SHOW_FACTIONS_GUI)
                {
                    List<MyMedicalRoom> medRooms = new List<MyMedicalRoom>();
                    medicalRoom = GetNearestMedRoom(currentPosition, out medRooms, MySession.Static.CreativeMode ? (long?)null : player.Identity.IdentityId);
                    if (joinGame && medRooms.Count > 0)
                        medicalRoom = medRooms[MyRandom.Instance.Next(0, medRooms.Count)];
                }
                else
                {
                    medicalRoom = FindRespawnMedicalRoom(medicalRoomId, player);
                    if (medicalRoom == null)
                    {
                        return false;
                    }
                }

                // If spawning in medical room fails, we will spawn as a new player
                if (medicalRoom != null)
                    SpawnInMedicalRoom(player, medicalRoom, joinGame);
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
                    player.SpawnAsNewPlayer(currentPosition, respawnShipId, resetIdentity);
                }
            }

            return true;
        }

        private static void SpawnInMedicalRoom(MyPlayer player, MyMedicalRoom medical, bool joiningGame)
        {
            if (MySession.Static.Settings.EnableOxygen)
            {
                player.Identity.ChangeToOxygenSafeSuit();
            }

            if (medical.HasSpawnPosition())
            {
                Matrix matrix = medical.GetSpawnPosition();
                player.SpawnAt(matrix, medical.Parent.Physics.LinearVelocity, false);
            }
            else if (joiningGame)
            {
                Vector3 medicalPosition = medical.PositionComp.GetPosition();
                medicalPosition += -medical.WorldMatrix.Up + medical.WorldMatrix.Right;
                Matrix matrix = medical.WorldMatrix;
                matrix.Translation = medicalPosition;
                player.SpawnAt(matrix, medical.Parent.Physics.LinearVelocity);
            }
            else
            {
                Matrix invWorldRot = Matrix.Invert(medical.WorldMatrix.GetOrientation());
                Vector3 relativeVelocity = Vector3.Transform(medical.Parent.Physics.LinearVelocity, invWorldRot);
                player.SpawnAtRelative(medical, MyMedicalRoom.GetSafePlaceRelative(), relativeVelocity);
            }
        }

        private static MyMedicalRoom FindRespawnMedicalRoom(long medicalRoomId, MyPlayer player)
        {
            MyMedicalRoom medicalRoom = null;
            if (MyEntities.TryGetEntityById(medicalRoomId, out medicalRoom))
            {
                if (!medicalRoom.IsWorking)
                    return null;

                if (player != null && !medicalRoom.HasPlayerAccess(player.Identity.IdentityId))
                    return null;
            }
            return medicalRoom;
        }

        private static MyMedicalRoom GetNearestMedRoom(Vector3 position, out List<MyMedicalRoom> medicalRooms, long? identityId = null)
        {
            List<MyCubeGrid> cubeGrids = MyEntities.GetEntities().OfType<MyCubeGrid>().ToList();

            medicalRooms = new List<MyMedicalRoom>();

            MyMedicalRoom closestMedicalRoom = null;
            float closestDistance = float.MaxValue;
            foreach (var grid in cubeGrids)
            {
                foreach (var slimBlock in grid.GetBlocks())
                {
                    MyMedicalRoom medicalRoom = slimBlock.FatBlock as MyMedicalRoom;
                    if (medicalRoom != null && medicalRoom.IsWorking)
                    {
                        if (!identityId.HasValue || medicalRoom.HasPlayerAccess(identityId.Value))
                        {
                            float distanceFromCenter = (float)medicalRoom.PositionComp.GetPosition().Length();

                            //Limit spawn position to be inside the world (with some safe margin)
                            if ((!MyEntities.IsWorldLimited() && distanceFromCenter > MAX_DISTANCE_TO_RESPAWN) ||
                                (MyEntities.IsWorldLimited() && distanceFromCenter > MyEntities.WorldSafeHalfExtent()))
                                continue;

                            float distance = Vector3.Distance(position, medicalRoom.PositionComp.GetPosition());

                            medicalRooms.Add(medicalRoom);

                            if (distance < closestDistance)
                            {
                                closestMedicalRoom = medicalRoom;
                                closestDistance = distance;
                            }
                        }
                    }
                }
            }
            return closestMedicalRoom;
        }

        public MyIdentity CreateNewIdentity(string identityName, MyPlayer.PlayerId playerId, string modelName)
        {
            return Sync.Players.CreateNewIdentity(identityName, "Default_Astronaut");
        }
    }
}

