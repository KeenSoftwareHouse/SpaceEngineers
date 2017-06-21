using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ProtoBuf;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.GUI;
using SteamSDK;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.Network;
using VRage.Utils;
using VRageMath;
using Sandbox.Engine.Networking;
using Sandbox.Game;

namespace SpaceEngineers.Game.World
{
    [StaticEventOwner]
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MySpaceRespawnComponent : MyRespawnComponentBase
    {
        struct RespawnCooldownEntry
        {
            public int ControllerId;
            public string ShipId;
            public int RelativeRespawnTime;
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
        }

        public void RequestSync()
        {
            MyMultiplayer.RaiseStaticEvent(s => OnSyncCooldownRequest);
        }

        public override void InitFromCheckpoint(MyObjectBuilder_Checkpoint checkpoint)
        {
            var cooldowns = checkpoint.RespawnCooldowns;

            m_lastUpdate = MySandboxGame.TotalTimeInMilliseconds;
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

                var item = new MyObjectBuilder_Checkpoint.RespawnCooldownItem();
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

            m_lastUpdate = MySandboxGame.TotalTimeInMilliseconds;
            m_updatingStopped = true;
            m_updateCtr = 0;

            if (!Sync.IsServer)
            {
                m_synced = false;
                RequestSync();
            }
            else
            {
                RequestSync();
                m_synced = true;
            }
        }

        public override void LoadData()
        {
            base.LoadData();

            Sync.Players.RespawnComponent = this;
            Sync.Players.LocalRespawnRequested += OnLocalRespawnRequest;

            ShowPermaWarning = false;
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            Sync.Players.LocalRespawnRequested -= OnLocalRespawnRequest;
            Sync.Players.RespawnComponent = null;
        }

        [Event, Reliable, Server]
        static void OnSyncCooldownRequest()
        {
            if (MyEventContext.Current.IsLocallyInvoked)
            {
                MySpaceRespawnComponent.Static.SyncCooldownToPlayer(Sync.MyId, true);
            }
            else
            {
                MySpaceRespawnComponent.Static.SyncCooldownToPlayer(MyEventContext.Current.Sender.Value, false);
            }

        }

        [Event, Reliable, Client]
        static void OnSyncCooldownResponse(List<RespawnCooldownEntry> entries)
        {
            MySpaceRespawnComponent.Static.SyncCooldownResponse(entries);
        }

        private void SyncCooldownResponse(List<RespawnCooldownEntry> entries)
        {
            int currentTime = MySandboxGame.TotalTimeInMilliseconds;

            // msg.respawnTimes can be null, if the server sent empty list
            if (entries != null)
            {
                foreach (var respawnTime in entries)
                {
                    var controllerId = new MyPlayer.PlayerId() { SteamId = Sync.MyId, SerialId = respawnTime.ControllerId };
                    var key = new RespawnKey() { ControllerId = controllerId, RespawnShipId = respawnTime.ShipId };

                    m_globalRespawnTimesMs.Add(key, currentTime + respawnTime.RelativeRespawnTime, immediate: true);
                }
            }

            m_synced = true;
        }

        public void SyncCooldownToPlayer(ulong steamId, bool isLocal)
        {
            int currentTime = MySandboxGame.TotalTimeInMilliseconds;

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

            if (isLocal)
            {
                OnSyncCooldownResponse(m_tmpRespawnTimes);
            }
            else
            {
                MyMultiplayer.RaiseStaticEvent(s => OnSyncCooldownResponse, m_tmpRespawnTimes, new EndpointId(steamId));
            }

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

            int currentTime = MySandboxGame.TotalTimeInMilliseconds;
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
                GetNearestRespawn(MySession.Static.LocalCharacter == null ? Vector3.Zero : (Vector3)MySession.Static.LocalCharacter.PositionComp.GetPosition(), out respawns, MySession.Static.LocalHumanPlayer.Identity.IdentityId);
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
            int currentTime = MySandboxGame.TotalTimeInMilliseconds;
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
            int currentTime = MySandboxGame.TotalTimeInMilliseconds;
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
            int currentTime = MySandboxGame.TotalTimeInMilliseconds;
            int time = currentTime;
            m_globalRespawnTimesMs.TryGetValue(key, out time);
            return Math.Max((time - currentTime) / 1000, 0);
        }

        private void OnLocalRespawnRequest()
        {
           
            if (MyFakes.SHOW_FACTIONS_GUI)
            {
                ulong playerId = MySession.Static.LocalHumanPlayer != null ? MySession.Static.LocalHumanPlayer.Id.SteamId : Sync.MyId;
                int serialId = MySession.Static.LocalHumanPlayer != null ? MySession.Static.LocalHumanPlayer.Id.SerialId : 0;
                MyMultiplayer.RaiseStaticEvent(s => RespawnRequest_Implementation, playerId, serialId);

            }
            else
            {
                MyPlayerCollection.RespawnRequest(MySession.Static.LocalHumanPlayer == null, false, 0, null);
            }
        }

        [Event, Reliable, Server]
        static void RespawnRequest_Implementation(ulong steamPlayerId, int serialId)
        {
            var playerId = new MyPlayer.PlayerId(steamPlayerId, serialId);
            var player = Sync.Players.GetPlayerById(playerId);

            if (MyScenarioSystem.Static != null && 
                (MyScenarioSystem.Static.GameState == MyScenarioSystem.MyState.JoinScreen || MyScenarioSystem.Static.GameState == MyScenarioSystem.MyState.WaitingForClients) ||
                player == null)
            {
                return;
            }
            LoadRespawnShip(player);
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
                            MyMultiplayer.RaiseStaticEvent(x => MySession.SetSpectatorPositionFromServer, cubeGrid.PositionComp.GetPosition(), new EndpointId(player.Id.SteamId));
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
            if(!MyCampaignManager.Static.IsCampaignRunning)
                MyGuiSandbox.AddScreen(new MyGuiScreenMedicals());
        }

        public override bool HandleRespawnRequest(bool joinGame, bool newIdentity, long medicalRoomId, string respawnShipId, MyPlayer.PlayerId playerId, Vector3D? spawnPosition, VRage.ObjectBuilders.SerializableDefinitionId? botDefinitionId)
        {
            MyPlayer player = Sync.Players.GetPlayerById(playerId);

            bool spawnAsNewPlayer = newIdentity || player == null;
            Debug.Assert(player == null || player.Identity != null, "Respawning player has no identity!");

            if (!MySessionComponentMissionTriggers.CanRespawn(playerId))
                return false;

            Vector3D currentPosition = Vector3D.Zero;
            if (player != null && player.Character != null) 
                currentPosition = player.Character.PositionComp.GetPosition();

            if (TryFindCryoChamberCharacter(player))
            {
                //Player found in chamber;
                return true;
            }

            MyBotDefinition botDefinition = null;
            if (botDefinitionId != null)
                MyDefinitionManager.Static.TryGetBotDefinition((MyDefinitionId)botDefinitionId, out botDefinition);

            if (!spawnAsNewPlayer)
            {
                if (respawnShipId != null)
                {
                    SpawnAtShip(player, respawnShipId, botDefinition);
                    return true;
                }

                if (spawnPosition.HasValue && player != null)
                {
                    Vector3D gravity = MyGravityProviderSystem.CalculateTotalGravityInPoint(spawnPosition.Value);
                    if (Vector3D.IsZero(gravity))
                        gravity = Vector3D.Down;
                    else
                        gravity.Normalize();
                    Vector3D perpendicular;
                    gravity.CalculatePerpendicularVector(out perpendicular);
                    player.SpawnAt(MatrixD.CreateWorld(spawnPosition.Value, perpendicular, -gravity), Vector3.Zero, null, botDefinition, true);

                    return true;
                }

                // Find respawn block to spawn at
                MyRespawnComponent foundRespawn = null;
                if (medicalRoomId == 0 || !MyFakes.SHOW_FACTIONS_GUI)
                {
                    List<MyRespawnComponent> respawns = null;
                    var nearestRespawn = GetNearestRespawn(currentPosition, out respawns, MySession.Static.CreativeMode ? (long?)null : (player != null ? player.Identity.IdentityId : (long?)null));
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
                    SpawnInRespawn(player, foundRespawn, botDefinition);
                else
                    spawnAsNewPlayer = true;
            }

            if (spawnAsNewPlayer)
            {
                bool resetIdentity = false;
                if (MySession.Static.Settings.PermanentDeath.Value)
                {
                    var oldIdentity = Sync.Players.TryGetPlayerIdentity(playerId);
                    if (oldIdentity != null)
                        resetIdentity = oldIdentity.FirstSpawnDone;
                }

                if (player == null)
                {
                    //TODO: Cannot use Displayname if player is null...
                    var identity = Sync.Players.CreateNewIdentity(playerId.SteamId.ToString());
                    player = Sync.Players.CreateNewPlayer(identity, playerId, playerId.SteamId.ToString());
                    resetIdentity = false;
                }

                if (MySession.Static.CreativeMode)
                {
                    Vector3D? correctedPos = MyEntities.FindFreePlace(currentPosition, 2, 200);
                    if (correctedPos.HasValue) currentPosition = correctedPos.Value;
                    player.SpawnAt(Matrix.CreateTranslation(currentPosition), Vector3.Zero, null, botDefinition);
                }
                else
                {
                    SpawnAsNewPlayer(player, currentPosition, respawnShipId, resetIdentity, botDefinition);
                }
            }

            return true;
        }

        private void SpawnInRespawn(MyPlayer player, MyRespawnComponent respawn, MyBotDefinition botDefinition)
        {
            if (respawn.Entity == null)
            {
                Debug.Assert(false, "Respawn does not have entity!");
                SpawnInSuit(player, null, botDefinition);
                return;
            }
            var parent = respawn.Entity.GetTopMostParent();

            if (parent.Physics == null)
            {
                Debug.Assert(false, "Respawn entity parent does not have physics!");
                SpawnInSuit(player, (MyEntity)parent, botDefinition);
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

            MyMultiplayer.ReplicateImmediatelly(MyExternalReplicable.FindByObject(parent), new EndpointId(player.Id.SteamId));

            player.SpawnAt(pos, velocity, (MyEntity)parent, botDefinition, true);

            if (medRoom != null)
            {
                medRoom.TryTakeSpawneeOwnership(player);
                medRoom.TrySetFaction(player);

                if (medRoom.ForceSuitChangeOnRespawn)
                {
                    player.Character.ChangeModelAndColor(medRoom.RespawnSuitName, player.Character.ColorMask);
                    if (MySession.Static.Settings.EnableOxygen && player.Character.OxygenComponent != null && player.Character.OxygenComponent.NeedsOxygenFromSuit)
                    {
                        player.Character.OxygenComponent.SwitchHelmet();
                    }
                }
            }
        }

        private MyRespawnComponent FindRespawnById(long respawnBlockId, MyPlayer player)
        {
            MyCubeBlock respawnBlock = null;
            if (!MyEntities.TryGetEntityById(respawnBlockId, out respawnBlock)) return null;

            if (!respawnBlock.IsWorking) return null;

            var medicalRoom = respawnBlock as MyMedicalRoom;

            if (medicalRoom == null || (!medicalRoom.SpawnWithoutOxygenEnabled && medicalRoom.GetOxygenLevel() == 0)) return null;
            // CH: TODO: Move the extra functionality to SpaceRespawnEntityComponent or something...
            if (player != null && !medicalRoom.HasPlayerAccess(player.Identity.IdentityId) && !medicalRoom.SetFactionToSpawnee)
                return null;

            var respawnComponent = respawnBlock.Components.Get<MyRespawnComponent>();
            if (respawnComponent == null) return null;

            return respawnComponent;
        }

        private MyRespawnComponent GetNearestRespawn(Vector3D position, out List<MyRespawnComponent> respawns, long? identityId = null)
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

                    distance = (float)Vector3D.Distance(position, block.PositionComp.GetPosition());
                }
                else
                {
                    if (respawn.Entity == null) continue;
                    if (respawn.Entity.PositionComp == null) continue;

                    distance = (float)Vector3D.Distance(position, respawn.Entity.PositionComp.GetPosition());
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

        public void SpawnAsNewPlayer(MyPlayer player, Vector3D currentPosition, string respawnShipId, bool resetIdentity, MyBotDefinition botDefinition)
        {
            Debug.Assert(Sync.IsServer, "Calling SpawnAsNewPlayer on client!");
            Debug.Assert(player != null, "Spawning with empty player!");
            Debug.Assert(player.Identity != null, "Spawning with empty identity!");
            if (!Sync.IsServer || player == null || player.Identity == null) return;

            if (resetIdentity)
            {
                ResetPlayerIdentity(player);
            }

            if (respawnShipId != null)
            {
                SpawnAtShip(player, respawnShipId, botDefinition);
            }
            else
            {
                SpawnInSuit(player, null, botDefinition);
            }

            if (MySession.Static != null && player.Character != null && MySession.Static.Settings.EnableOxygen && player.Character.OxygenComponent != null && player.Character.OxygenComponent.NeedsOxygenFromSuit)
            {
                player.Character.OxygenComponent.SwitchHelmet();
            }
        }

        public void SpawnAtShip(MyPlayer player, string respawnShipId, MyBotDefinition botDefinition)
        {
            Debug.Assert(Sync.IsServer, "Spawning can only be called on the server!");
            if (!Sync.IsServer) return;

            ResetRespawnCooldown(player.Id);
            if (Sync.MultiplayerActive)
                SyncCooldownToPlayer(player.Id.SteamId, player.Id.SteamId == Sync.MyId);

            List<MyCubeGrid> respawnGrids = new List<MyCubeGrid>();

            var respawnShipDef = MyDefinitionManager.Static.GetRespawnShipDefinition(respawnShipId);
            MyPrefabDefinition prefabDef = respawnShipDef.Prefab;

            // Deploy ship
            Vector3D position = Vector3D.Zero;
            float planetSpawnHeightRatio = 0.3f;
            float spawnRangeMin = 500f;
            float spawnRangeMax = 650f;
            if (prefabDef.CubeGrids != null && prefabDef.CubeGrids.Length > 0)
            {
                MyObjectBuilder_CubeGrid firstGrid = prefabDef.CubeGrids[0];
                if (firstGrid.UsePositionForSpawn && !MyEntities.IsWorldLimited())
                {
                    position = new Vector3D(
                        firstGrid.PositionAndOrientation.Value.Position.x,
                        firstGrid.PositionAndOrientation.Value.Position.y,
                        firstGrid.PositionAndOrientation.Value.Position.z);
                }

                planetSpawnHeightRatio = MyMath.Clamp(firstGrid.PlanetSpawnHeightRatio, 0.05f, 0.95f); // Clamped to prevent crazy data
                spawnRangeMin = firstGrid.SpawnRangeMin;
                spawnRangeMax = firstGrid.SpawnRangeMax;
            }
            Vector3D forward = Vector3.Forward;
            Vector3D up = Vector3D.Up;

            GetSpawnPosition(prefabDef.BoundingSphere.Radius, ref position, out forward, out up, planetSpawnHeightRatio, spawnRangeMin, spawnRangeMax);

            Stack<Action> callback = new Stack<Action>();
            callback.Push(delegate() { PutPlayerInRespawnGrid(player, respawnGrids, botDefinition); });

            MyPrefabManager.Static.SpawnPrefab(
                respawnGrids,
                prefabDef.Id.SubtypeName,
                position,
                forward,
                up,
                spawningOptions: VRage.Game.ModAPI.SpawningOptions.RotateFirstCockpitTowardsDirection,
                updateSync: true,
                callbacks: callback);
        }

        private void PutPlayerInRespawnGrid(MyPlayer player, List<MyCubeGrid> respawnGrids, MyBotDefinition botDefinition)
        {
            MyCharacter character = null;
            MyCockpit cockpit = null;

            // Find cockpits
            List<MyCockpit> shipCockpits = new List<MyCockpit>();
            foreach (MyCubeGrid grid in respawnGrids)
            {
                foreach (MyCockpit gridCockpit in grid.GetFatBlocks<MyCockpit>())
                {
                    // Ignore non-functional cockpits
                    if (!gridCockpit.IsFunctional)
                        continue;
                    shipCockpits.Add(gridCockpit);
                }
            }

            // First sort cockpits by order: Ship controlling cockpits set to main, then ship controlling cockpits not set to main, lastly whatever remains, e.g. CryoChambers and Passenger Seats
            if (shipCockpits.Count > 1)
            {
                shipCockpits.Sort(delegate(MyCockpit cockpitA, MyCockpit cockpitB)
                {
                    int controlCompare = cockpitB.EnableShipControl.CompareTo(cockpitA.EnableShipControl);
                    if (controlCompare != 0) return controlCompare;

                    int mainCompare = cockpitB.IsMainCockpit.CompareTo(cockpitA.IsMainCockpit);
                    if (mainCompare != 0) return mainCompare;

                    return 0;
                });
            }

            // Finally, select the most important cockpit
            if (shipCockpits.Count > 0)
                cockpit = shipCockpits[0];

            // No cockpit on grid. It's not good, but it's safe.
            // Can be caused by modded respawn ship, wrongly loaded definition (not probably), 
            // or not spawned RS because of blocked place (because of grid spawn paralelization).
            //System.Diagnostics.Debug.Assert(cockpit != null, "Character is spawning in ship without cockpit!");

            // Create character
            MatrixD matrix = MatrixD.Identity;
            if (cockpit != null)
            {
                matrix = cockpit.WorldMatrix;
                matrix.Translation = cockpit.WorldMatrix.Translation - Vector3.Up - Vector3.Forward;
            }
            else if (respawnGrids.Count > 0)
            {
                matrix.Translation = respawnGrids[0].PositionComp.WorldAABB.Center + respawnGrids[0].PositionComp.WorldAABB.HalfExtents;
            }

            character = MyCharacter.CreateCharacter(matrix, Vector3.Zero, player.Identity.DisplayName, player.Identity.Model, null, botDefinition, cockpit: cockpit, playerSteamId: player.Id.SteamId);

            CloseRespawnShip(player);
            foreach (var respawnGrid in respawnGrids)
            {
                respawnGrid.ChangeGridOwnership(player.Identity.IdentityId, MyOwnershipShareModeEnum.None);
                respawnGrid.IsRespawnGrid = true;
                respawnGrid.m_playedTime = 0;
                player.RespawnShip.Add(respawnGrid.EntityId);
            }
            //SaveRespawnShip(player);

            if (cockpit != null)
            {
                cockpit.AttachPilot(character, false);
                MyMultiplayer.ReplicateImmediatelly(MyExternalReplicable.FindByObject(cockpit.CubeGrid), new EndpointId(player.Id.SteamId));
            }

            if (cockpit == null)
            {
                Sync.Players.SetPlayerCharacter(player, character, null);
            }
            else
            {
                character.SetPlayer(player);
                Sync.Players.SetControlledEntity(player.Id, cockpit);
               // Sync.Players.SetPlayerToCockpit(player, cockpit);
            }
            Sync.Players.RevivePlayer(player);
        }

        public override void AfterRemovePlayer(MyPlayer player)
        {
            //SaveRespawnShip(player);
            //TODOA - save respawn ship and remove it from space
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

        private static void SaveRespawnShip(MyPlayer player)
        {
            if (!MySession.Static.Settings.RespawnShipDelete)
                return;

            System.Diagnostics.Debug.Assert(player.RespawnShip != null, "Saving a null respawn ship");
            if (player.RespawnShip == null) return;

            MyCubeGrid oldHome;
            if (MyEntities.TryGetEntityById<MyCubeGrid>(player.RespawnShip[0], out oldHome))
            {
                ulong sizeInBytes = 0;
                string sessionPath = MySession.Static.CurrentPath;
                Console.WriteLine(sessionPath);
                string fileName = "RS_" + player.Client.SteamUserId + ".sbr";
                ParallelTasks.Parallel.Start(delegate()
                {
                    MyLocalCache.SaveRespawnShip((MyObjectBuilder_CubeGrid)oldHome.GetObjectBuilder(), sessionPath, fileName, out sizeInBytes);
                });
            }
        }

        private static void LoadRespawnShip(MyPlayer player)
        {
            ulong playerID = player.Client.SteamUserId;
            string fileName = "RS_" + playerID;


        }

        private void SpawnInSuit(MyPlayer player, MyEntity spawnedBy, MyBotDefinition botDefinition)
        {
            Vector3D position = Vector3D.Zero;
            Vector3D forward = Vector3D.Forward;
            Vector3D up = Vector3D.Up;
            GetSpawnPosition(10, ref position, out forward, out up);

            //Create character
            Matrix matrix = Matrix.CreateWorld(position, forward, up);
            MyCharacter character = MyCharacter.CreateCharacter(matrix, Vector3.Zero, player.Identity.DisplayName, player.Identity.Model, null, botDefinition, playerSteamId: player.Id.SteamId);

            Sync.Players.SetPlayerCharacter(player, character, spawnedBy);
            Sync.Players.RevivePlayer(player);
        }

        /// <summary>
        /// Returns a position adjusted for planets that should be safe to spawn at given the radius and position.
        /// </summary>
        /// <param name="collisionRadius">The radius of the object that is trying to spawn.</param>
        /// <param name="position">The position the object would like to spawn at.</param>
        /// <param name="forward">(Out) The forward vector the object should spawn with.</param>
        /// <param name="up">(Out) The up vector the object should spawn with.</param>
        /// <param name="planetAtmosphereRatio">The ratio within the planet's max radius and atmosphere radius you are positioned in.</param>
        /// <param name="randomRangeMin">The minimum randomized distance that is added.</param>
        /// <param name="randomRangeMax">The minimum randomized distance that is added.</param>
        private static void GetSpawnPositionNearPlanet(MyPlanet planet, float collisionRadius, ref Vector3D position, out Vector3D forward, out Vector3D up, float planetAtmosphereRatio = 0.3f, float randomRangeMin = 500, float randomRangeMax = 650)
        {
            // Position us at a desirable height on the planet
            // Roughl halfway the planet's upper atmosphere and outer radius
            float randomHeightAdjustment = MyUtils.GetRandomFloat(randomRangeMin, randomRangeMax);
            float height = planet.MaximumRadius + planetAtmosphereRatio * (planet.AtmosphereRadius - planet.MaximumRadius) + randomHeightAdjustment;

            // Compute random position
            position = planet.PositionComp.WorldVolume.Center + MyUtils.GetRandomVector3Normalized() * height;
            Vector3D? freePlace = MyEntities.FindFreePlace(position, collisionRadius);
            int numAttempts = 1;

            // While we have no valid spawn position, try again 3 more times
            while (!freePlace.HasValue && numAttempts <= 3)
            {
                position = planet.PositionComp.WorldVolume.Center + MyUtils.GetRandomVector3Normalized() * height;
                freePlace = MyEntities.FindFreePlace(position, collisionRadius);
                numAttempts += 1;
            }

            // If we have a valid position, use it
            if (freePlace.HasValue)
                position = freePlace.Value;

            // Return orientation perpendicular to planet surface
            Vector3 gravityAtPosition = MyGravityProviderSystem.CalculateNaturalGravityInPoint(position);
            if (Vector3.IsZero(gravityAtPosition)) // If we are somehow randomized outside of gravity range, make sure some orientation is used
                gravityAtPosition = Vector3.Up;

            Vector3D normalizedGravityVector = Vector3D.Normalize(gravityAtPosition);
            forward = Vector3.CalculatePerpendicularVector(-normalizedGravityVector);
            up = -normalizedGravityVector;
        }

        /// <summary>
        /// Returns a position that should be safe to spawn at given the radius and position.
        /// </summary>
        /// <param name="collisionRadius">The radius of the object that is trying to spawn.</param>
        /// <param name="position">The position the object would like to spawn at.</param>
        /// <param name="forward">(Out) The forward vector the object should spawn with.</param>
        /// <param name="up">(Out) The up vector the object should spawn with.</param>
        /// <param name="planetSpawnHeightRatio">The ratio within the planet's max radius and atmosphere radius you are positioned in.</param>
        /// <param name="randomRangeMin">The minimum randomized distance that is added.</param>
        /// <param name="randomRangeMax">The minimum randomized distance that is added.</param>
        public static void GetSpawnPosition(float collisionRadius, ref Vector3D position, out Vector3D forward, out Vector3D up, float planetSpawnHeightRatio = 0.3f, float randomRangeMin = 500, float randomRangeMax = 650)
        {
            // Are we spawning near a planet?
            MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(position);
            if (planet != null)
            {
                GetSpawnPositionNearPlanet(planet, collisionRadius, ref position, out forward, out up, planetSpawnHeightRatio, randomRangeMin, randomRangeMax);
                return;
            }

            // Old logic, testing for asteroids and other objects
            double distance = 0;
            foreach (var entity in MyEntities.GetEntities())
            {
                MyVoxelMap voxelMap = entity as MyVoxelMap;

                // Only test against voxels
                if (entity == null) continue;

                distance = MathHelper.Max(distance, entity.PositionComp.WorldVolume.Center.Length() + entity.PositionComp.WorldVolume.Radius);
            }

            // Random range from last voxel
            distance += MyUtils.GetRandomFloat(randomRangeMin, randomRangeMax);

            if (MyEntities.IsWorldLimited())
                distance = Math.Min(distance, MyEntities.WorldSafeHalfExtent());
            else
                distance = Math.Min(distance, 20000); // limited spawn area in infinite worlds

            // Compute random position
            forward = MyUtils.GetRandomVector3Normalized();
            up = Vector3D.CalculatePerpendicularVector(forward);
            Vector3D randomizedPosition = position + (forward * distance);

            // Test if we can spawn here
            Vector3D? searchPosition = MyEntities.FindFreePlace(randomizedPosition, collisionRadius);
            if (searchPosition.HasValue)
                randomizedPosition = searchPosition.Value;

            position = randomizedPosition;
        }

        public override MyIdentity CreateNewIdentity(string identityName, MyPlayer.PlayerId playerId, string modelName)
        {
            return Sync.Players.CreateNewIdentity(identityName, modelName);
        }

        public override void SetupCharacterDefault(MyPlayer player, MyWorldGenerator.Args args)
        {
            string respawnShipId = MyDefinitionManager.Static.GetFirstRespawnShip();
            SpawnAtShip(player, respawnShipId, null);
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

        public override void SetupCharacterFromStarts(MyPlayer player, MyWorldGeneratorStartingStateBase[] playerStarts, MyWorldGenerator.Args args)
        {
            var randomStart = playerStarts[MyUtils.GetRandomInt(playerStarts.Length)];
            randomStart.SetupCharacter(args);
        }
    }
}

