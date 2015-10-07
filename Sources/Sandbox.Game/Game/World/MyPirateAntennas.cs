using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World.Generator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Components;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.World
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 1000, typeof(MyObjectBuilder_PirateAntennas))]
    public class MyPirateAntennas : MySessionComponentBase
    {
        // CH: TODO: Do static identities in a proper way!
        private static readonly string IDENTITY_NAME = "Space Pirates";
        private static readonly int DRONE_DESPAWN_TIMER = 600000;
        private static readonly int DRONE_DESPAWN_RETRY = 5000;

        private class PirateAntennaInfo
        {
            public MyPirateAntennaDefinition AntennaDefinition;
            public int LastGenerationGameTime;
            public int SpawnedDrones;

            public static List<PirateAntennaInfo> m_pool = new List<PirateAntennaInfo>();

            public static PirateAntennaInfo Allocate(MyPirateAntennaDefinition antennaDef)
            {
                PirateAntennaInfo info = null;
                if (m_pool.Count == 0)
                {
                    info = new PirateAntennaInfo();
                }
                else
                {
                    info = m_pool[m_pool.Count - 1];
                    m_pool.RemoveAt(m_pool.Count - 1);
                }

                info.Reset(antennaDef);
                return info;
            }

            public static void Deallocate(PirateAntennaInfo toDeallocate)
            {
                toDeallocate.AntennaDefinition = null;
                m_pool.Add(toDeallocate);
            }

            public void Reset(MyPirateAntennaDefinition antennaDef)
            {
                AntennaDefinition = antennaDef;
                LastGenerationGameTime = MySandboxGame.TotalGamePlayTimeInMilliseconds + (int)antennaDef.FirstSpawnTimeMs - (int)antennaDef.SpawnTimeMs;
                SpawnedDrones = 0;
            }
        }
        private class DroneInfo
        {
            public long AntennaEntityId = 0;
            public int DespawnTime = 0;

            public static List<DroneInfo> m_pool = new List<DroneInfo>();

            public static DroneInfo Allocate(long antennaEntityId, int despawnTime)
            {
                DroneInfo info = null;
                if (m_pool.Count == 0)
                {
                    info = new DroneInfo();
                }
                else
                {
                    info = m_pool[m_pool.Count - 1];
                    m_pool.RemoveAt(m_pool.Count - 1);
                }

                info.AntennaEntityId = antennaEntityId;
                info.DespawnTime = despawnTime;

                return info;
            }

            public static void Deallocate(DroneInfo toDeallocate)
            {
                toDeallocate.AntennaEntityId = 0;
                toDeallocate.DespawnTime = 0;
                m_pool.Add(toDeallocate);
            }
        }

        private static Dictionary<long, PirateAntennaInfo> m_pirateAntennas;
        private static Dictionary<string, MyPirateAntennaDefinition> m_definitionsByAntennaName;
        private static int m_ctr = 0;
        private static int m_ctr2 = 0;

        // CH: TODO: Serialization (& sync)!
        private static CachingDictionary<long, DroneInfo> m_droneInfos;

        private static List<MyCubeGrid> m_tmpGridList = new List<MyCubeGrid>();
        private static long m_piratesIdentityId = 0;

        public override bool IsRequiredByGame
        {
            get
            {
                return MyPerGameSettings.Game == GameEnum.SE_GAME;
            }
        }

        public override void LoadData()
        {
            base.LoadData();

            m_pirateAntennas = new Dictionary<long, PirateAntennaInfo>();
            m_definitionsByAntennaName = new Dictionary<string, MyPirateAntennaDefinition>();
            m_droneInfos = new CachingDictionary<long, DroneInfo>();
            foreach (var antennaDefinition in MyDefinitionManager.Static.GetPirateAntennaDefinitions())
            {
                m_definitionsByAntennaName[antennaDefinition.Name] = antennaDefinition;
            }
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            var ob = sessionComponent as MyObjectBuilder_PirateAntennas;

            m_piratesIdentityId = ob.PiratesIdentity;

            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (ob.Drones != null)
            {
                foreach (var entry in ob.Drones)
                {
                    m_droneInfos.Add(entry.EntityId, DroneInfo.Allocate(entry.AntennaEntityId, currentTime + entry.DespawnTimer), immediate: true);
                }
            }
        }

        public override void BeforeStart()
        {
            base.BeforeStart();

            // Make sure that the pirate identity exists
            if (m_piratesIdentityId != 0)
            {
                MyIdentity pirateIdentity = Sync.Players.TryGetIdentity(m_piratesIdentityId);
                Debug.Assert(pirateIdentity != null, "The pirate identity does not exist, although its ID was saved!");

                if (Sync.IsServer && pirateIdentity == null)
                {
                    Sync.Players.CreateNewIdentity(IDENTITY_NAME, m_piratesIdentityId, null);
                }
            }
            else
            {
                var identity = Sync.Players.CreateNewIdentity(IDENTITY_NAME);
                m_piratesIdentityId = identity.IdentityId;
            }

            if (!Sync.Players.IdentityIsNpc(m_piratesIdentityId))
            {
                Sync.Players.MarkIdentityAsNPC(m_piratesIdentityId);
            }

            // Make sure that all the drone entities exist
            foreach (var drone in m_droneInfos)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(drone.Key, out entity);
                if (entity == null)
                {
                    DroneInfo.Deallocate(drone.Value);
                    m_droneInfos.Remove(drone.Key);
                }
                else
                {
                    if (!MySession.Static.Settings.EnableDrones)
                    {
                        MyCubeGrid grid = entity as MyCubeGrid;
                        var remote = entity as MyRemoteControl;
                        if (grid == null)
                        {
                            grid = remote.CubeGrid;
                        }

                        UnregisterDrone(entity, immediate: false);
                        grid.SyncObject.SendCloseRequest();
                    }
                    else
                    {
                        RegisterDrone(drone.Value.AntennaEntityId, entity, immediate: false);
                    }
                }
            }
            m_droneInfos.ApplyRemovals();
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            m_piratesIdentityId = 0;
            m_definitionsByAntennaName = null;

            foreach (var entry in m_droneInfos)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(entry.Key, out entity);

                if (entity != null)
                {
                    UnregisterDrone(entity, immediate: false);
                }
            }
            m_droneInfos.Clear();
            m_droneInfos = null;

            m_pirateAntennas = null;
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_PirateAntennas;

            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            ob.PiratesIdentity = m_piratesIdentityId;

            var drones = m_droneInfos.Reader;
            ob.Drones = new MyObjectBuilder_PirateAntennas.MyPirateDrone[drones.Count()];
            int i = 0;
            foreach (var entry in drones)
            {
                ob.Drones[i] = new MyObjectBuilder_PirateAntennas.MyPirateDrone();
                ob.Drones[i].EntityId = entry.Key;
                ob.Drones[i].AntennaEntityId = entry.Value.AntennaEntityId;
                ob.Drones[i].DespawnTimer = Math.Max(0, entry.Value.DespawnTime - currentTime);
                i++;
            }

            return ob;
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
            {
                DebugDraw();
            }

            if (!Sync.IsServer) return;

            if (++m_ctr > 30)
            {
                m_ctr = 0;
                UpdateDroneSpawning();
            }

            if (++m_ctr2 > 100)
            {
                m_ctr2 = 0;
                UpdateDroneDespawning();
            }
        }

        private void UpdateDroneSpawning()
        {
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            foreach (var antennaEntry in m_pirateAntennas)
            {
                PirateAntennaInfo antennaInfo = antennaEntry.Value;
                if (currentTime - antennaInfo.LastGenerationGameTime > antennaInfo.AntennaDefinition.SpawnTimeMs)
                {
                    MyRadioAntenna antenna = null;
                    MyEntities.TryGetEntityById(antennaEntry.Key, out antenna);
                    Debug.Assert(antenna != null, "Could not find antenna for spawning enemy drones!");

                    var spawnGroup = antennaInfo.AntennaDefinition.SpawnGroupSampler.Sample();
                    Debug.Assert(spawnGroup != null, "Could not find spawnGroup for spawning enemy drones!");

                    if
                    (
                        !MySession.Static.Settings.EnableDrones ||
                        antennaInfo.SpawnedDrones >= antennaInfo.AntennaDefinition.MaxDrones ||
                        antenna == null ||
                        spawnGroup == null ||
                        m_droneInfos.Reader.Count() >= MySession.Static.Settings.MaxDrones
                    )
                    {
                        antennaInfo.LastGenerationGameTime = currentTime;
                        continue;
                    }

                    spawnGroup.ReloadPrefabs();

                    BoundingSphereD antennaSphere = new BoundingSphereD(antenna.WorldMatrix.Translation, antenna.GetRadius());

                    var players = MySession.Static.Players.GetOnlinePlayers();
                    bool successfulSpawn = false;
                    foreach (var player in players)
                    {
                        if (antennaSphere.Contains(player.GetPosition()) == ContainmentType.Contains)
                        {
                            Vector3D? spawnPosition = null;
                            for (int i = 0; i < 10; ++i)
                            {
                                Vector3D position = antenna.WorldMatrix.Translation + MyUtils.GetRandomVector3Normalized() * antennaInfo.AntennaDefinition.SpawnDistance;
                                spawnPosition = MyEntities.FindFreePlace(position, spawnGroup.SpawnRadius);
                                if (spawnPosition.HasValue) break;
                            }

                            if (spawnPosition.HasValue)
                            {
                                successfulSpawn = SpawnDrone(antenna.EntityId, antenna.OwnerId, spawnPosition.Value, spawnGroup);
                                break;
                            }

                            break;
                        }
                    }

                    // Don't reschedule if there was no player inside
                    if (successfulSpawn)
                    {
                        antennaInfo.LastGenerationGameTime = currentTime;
                    }
                }
            }
        }

        private void UpdateDroneDespawning()
        {
            foreach (var entry in m_droneInfos)
            {
                if (entry.Value.DespawnTime < MySandboxGame.TotalGamePlayTimeInMilliseconds)
                {
                    MyEntity droneEntity = null;
                    MyEntities.TryGetEntityById(entry.Key, out droneEntity);
                    Debug.Assert(droneEntity != null, "Could not find the drone entity to despawn!");

                    if (droneEntity != null)
                    {
                        MyCubeGrid grid = droneEntity as MyCubeGrid;
                        var remote = droneEntity as MyRemoteControl;
                        if (grid == null)
                        {
                            grid = remote.CubeGrid;
                        }

                        if (CanDespawn(grid, remote))
                        {
                            UnregisterDrone(droneEntity, immediate: false);
                            grid.SyncObject.SendCloseRequest();
                        }
                        else
                        {
                            entry.Value.DespawnTime = MySandboxGame.TotalGamePlayTimeInMilliseconds + DRONE_DESPAWN_RETRY;
                        }
                    }
                    else
                    {
                        DroneInfo.Deallocate(entry.Value);
                        m_droneInfos.Remove(entry.Key);
                    }
                }
            }

            m_droneInfos.ApplyChanges();
        }

        public bool CanDespawn(MyCubeGrid grid, MyRemoteControl remote)
        {
            if (remote != null && !remote.IsFunctional) return false;

            BoundingSphereD bs = grid.PositionComp.WorldVolume;
            bs.Radius += 4000.0;

            foreach (var player in Sync.Players.GetOnlinePlayers())
            {
                if (bs.Contains(player.GetPosition()) == ContainmentType.Contains)
                {
                    return false;
                }
            }

            foreach (var gunSet in grid.GridSystems.WeaponSystem.GetGunSets().Values)
            {
                foreach (var gun in gunSet)
                {
                    if (gun.IsShooting) return false;
                }
            }

            return true;
        }

        private bool SpawnDrone(long antennaEntityId, long ownerId, Vector3D position, MySpawnGroupDefinition spawnGroup)
        {
            Vector3D direction = MyUtils.GetRandomVector3Normalized();
            Vector3D upVector = Vector3D.CalculatePerpendicularVector(direction);
            MatrixD originMatrix = MatrixD.CreateWorld(position, direction, upVector);

            foreach (var shipPrefab in spawnGroup.Prefabs)
            {
                Vector3D shipPosition = Vector3D.Transform((Vector3D)shipPrefab.Position, originMatrix);

                m_tmpGridList.Clear();

                MyPrefabManager.Static.SpawnPrefab(
                    resultList: m_tmpGridList,
                    prefabName: shipPrefab.SubtypeId,
                    position: shipPosition,
                    forward: direction,
                    up: upVector,
                    initialLinearVelocity: default(Vector3),
                    beaconName: null,
                    spawningOptions: Sandbox.ModAPI.SpawningOptions.None,
                    ownerId: ownerId,
                    updateSync: true);

                foreach (var grid in m_tmpGridList)
                {
                    grid.ChangeGridOwnership(ownerId, MyOwnershipShareModeEnum.None);

                    MyRemoteControl firstRemote = null;

                    foreach (var block in grid.CubeBlocks)
                    {
                        if (block.FatBlock == null) continue;

                        var pb = block.FatBlock as MyProgrammableBlock;
                        if (pb != null)
                        {
                            pb.SendRecompile();
                        }

                        var remote = block.FatBlock as MyRemoteControl;
                        if (firstRemote == null)
                            firstRemote = remote;
                    }

                    // If there's no remote control on the grid, we have to register it as is
                    RegisterDrone(antennaEntityId, (MyEntity)firstRemote ?? (MyEntity)grid);
                }

                m_tmpGridList.Clear();
            }
            return true;
        }

        private void RegisterDrone(long antennaEntityId, MyEntity droneMainEntity, bool immediate = true)
        {
            var newInfo = DroneInfo.Allocate(antennaEntityId, MySandboxGame.TotalGamePlayTimeInMilliseconds + DRONE_DESPAWN_TIMER);
            m_droneInfos.Add(droneMainEntity.EntityId, newInfo, immediate: immediate);
            droneMainEntity.OnClose += DroneMainEntityOnClose;

            PirateAntennaInfo antennaInfo = null;
            m_pirateAntennas.TryGetValue(antennaEntityId, out antennaInfo);
            if (antennaInfo != null)
            {
                antennaInfo.SpawnedDrones++;
            }
            Debug.Assert(antennaEntityId == 0 || antennaInfo != null, "Antenna info not present when registering a drone!");

            var remote = droneMainEntity as MyRemoteControl;
            if (remote != null)
            {
                remote.OwnershipChanged += DroneRemoteOwnershipChanged;
            }
        }

        private void UnregisterDrone(MyEntity entity, bool immediate = true)
        {
            int antennaEntityId = 0;

            DroneInfo info = null;
            m_droneInfos.TryGetValue(entity.EntityId, out info);
            if (info != null)
            {
                DroneInfo.Deallocate(info);
            }
            m_droneInfos.Remove(entity.EntityId, immediate: immediate);

            PirateAntennaInfo antennaInfo = null;
            m_pirateAntennas.TryGetValue(antennaEntityId, out antennaInfo);
            if (antennaInfo != null)
            {
                antennaInfo.SpawnedDrones--;
                Debug.Assert(antennaInfo.SpawnedDrones >= 0, "Inconsistence in registered drone counts!");
            }

            entity.OnClose -= DroneMainEntityOnClose;
            var remote = entity as MyRemoteControl;
            if (remote != null)
            {
                remote.OwnershipChanged -= DroneRemoteOwnershipChanged;
            }
        }

        private void DroneMainEntityOnClose(MyEntity entity)
        {
            UnregisterDrone(entity);
        }

        private void DroneRemoteOwnershipChanged(MyTerminalBlock remote)
        {
            long newOwner = remote.OwnerId;
            if (!Sync.Players.IdentityIsNpc(newOwner))
            {
                UnregisterDrone(remote);
            }
        }

        public static void UpdatePirateAntenna(long antennaEntityId, bool remove, StringBuilder antennaName)
        {
            Debug.Assert(Sync.IsServer, "Pirate antennas can only be registered on the server");

            // This can happen while unloading the game, because this component unloads before entities.
            if (m_pirateAntennas == null) return;

            if (remove == true)
            {
                m_pirateAntennas.Remove(antennaEntityId);
                return;
            }

            string antennaNameStr = antennaName.ToString();

            PirateAntennaInfo antennaInfo = null;
            if (!m_pirateAntennas.TryGetValue(antennaEntityId, out antennaInfo))
            {
                MyPirateAntennaDefinition antennaDef = null;
                if (m_definitionsByAntennaName.TryGetValue(antennaNameStr, out antennaDef))
                {
                    antennaInfo = PirateAntennaInfo.Allocate(antennaDef);
                    m_pirateAntennas.Add(antennaEntityId, antennaInfo);
                }
            }
            else if (antennaInfo.AntennaDefinition.Name != antennaNameStr)
            {
                MyPirateAntennaDefinition antennaDef = null;
                if (!m_definitionsByAntennaName.TryGetValue(antennaNameStr, out antennaDef))
                {
                    PirateAntennaInfo.Deallocate(antennaInfo);
                    m_pirateAntennas.Remove(antennaEntityId);
                }
                else
                {
                    antennaInfo.Reset(antennaDef);
                }
            }
        }

        public static long GetPiratesId()
        {
            return m_piratesIdentityId;
        }

        private static void DebugDraw()
        {
            foreach (var antennaEntry in m_pirateAntennas)
            {
                MyRadioAntenna antenna = null;
                MyEntities.TryGetEntityById(antennaEntry.Key, out antenna);
                if (antenna != null)
                {
                    var dt = Math.Max(0, antennaEntry.Value.AntennaDefinition.SpawnTimeMs - MySandboxGame.TotalGamePlayTimeInMilliseconds + antennaEntry.Value.LastGenerationGameTime);
                    MyRenderProxy.DebugDrawText3D(antenna.WorldMatrix.Translation, "Time ramaining: " + dt.ToString(), Color.Red, 1.0f, false);
                }
            }

            foreach (var value in m_pirateAntennas)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(value.Key, out entity);
                if (entity != null)
                {
                    MyRenderProxy.DebugDrawSphere(entity.WorldMatrix.Translation, (float)entity.PositionComp.WorldVolume.Radius, Color.BlueViolet, 1.0f, false);
                }
            }

            foreach (var value in m_droneInfos)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(value.Key, out entity);
                if (entity != null)
                {
                    MyCubeGrid grid = entity as MyCubeGrid;
                    if (grid == null)
                    {
                        var remote = entity as MyRemoteControl;
                        grid = remote.CubeGrid;
                    }
                    MyRenderProxy.DebugDrawSphere(grid.PositionComp.WorldVolume.Center, (float)grid.PositionComp.WorldVolume.Radius, Color.Cyan, 1.0f, false);
                    MyRenderProxy.DebugDrawText3D(grid.PositionComp.WorldVolume.Center, ((value.Value.DespawnTime - MySandboxGame.TotalGamePlayTimeInMilliseconds) / 1000).ToString(), Color.Cyan, 0.7f, false);
                }
            }
        }
    }
}
