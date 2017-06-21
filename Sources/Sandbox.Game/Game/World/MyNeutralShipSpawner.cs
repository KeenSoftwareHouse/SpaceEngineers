using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Game.GameSystems;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.World
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    class MyNeutralShipSpawner : MySessionComponentBase
    {
        public const float NEUTRAL_SHIP_SPAWN_DISTANCE = 8000.0f;
        public const float NEUTRAL_SHIP_FORBIDDEN_RADIUS = 2000.0f;
        public const float NEUTRAL_SHIP_DIRECTION_SPREAD = 0.5f;
        public const float NEUTRAL_SHIP_MINIMAL_ROUTE_LENGTH = 10000.0f;
        public const float NEUTRAL_SHIP_SPAWN_OFFSET = 500.0f;
        public static TimeSpan NEUTRAL_SHIP_RESCHEDULE_TIME = TimeSpan.FromSeconds(10); // If spawning does not succeed, retry in 10 seconds
        public static TimeSpan NEUTRAL_SHIP_MIN_TIME = TimeSpan.FromMinutes(13); // Re-spawn time = 13-17 minutes
        public static TimeSpan NEUTRAL_SHIP_MAX_TIME = TimeSpan.FromMinutes(17);
        private const int EVENT_SPAWN_TRY_MAX = 3;

        private static List<MyPhysics.HitInfo> m_raycastHits = new List<MyPhysics.HitInfo>();

        private static List<float> m_spawnGroupCumulativeFrequencies = new List<float>();
        private static float m_spawnGroupTotalFrequencies = 0.0f;
        private static float[] m_upVecMultipliers = { 1.0f, 1.0f, -1.0f, -1.0f };
        private static float[] m_rightVecMultipliers = { 1.0f, -1.0f, -1.0f, 1.0f };

        private static List<MySpawnGroupDefinition> m_spawnGroups = new List<MySpawnGroupDefinition>();

        private static int m_eventSpawnTry = 0;

        public override bool IsRequiredByGame
        {
            get
            {
                return MyPerGameSettings.Game == GameEnum.SE_GAME;
            }
        }

        public override void LoadData()
        {
            MySandboxGame.Log.WriteLine("Pre-loading neutral ship spawn groups...");

            var spawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();
            foreach (var spawnGroup in spawnGroups)
            {
                if (spawnGroup.IsEncounter == false && spawnGroup.IsPirate == false)
                {
                    m_spawnGroups.Add(spawnGroup);
                }
            }

            m_spawnGroupTotalFrequencies = 0.0f;
            m_spawnGroupCumulativeFrequencies.Clear();

            foreach (var spawnGroup in m_spawnGroups)
            {
                m_spawnGroupTotalFrequencies += spawnGroup.Frequency;
                m_spawnGroupCumulativeFrequencies.Add(m_spawnGroupTotalFrequencies);
            }

            MySandboxGame.Log.WriteLine("End pre-loading neutral ship spawn groups.");
        }

        protected override void UnloadData()
        {
            m_spawnGroupTotalFrequencies = 0.0f;
            m_spawnGroupCumulativeFrequencies.Clear();
        }

        public override void BeforeStart()
        {
            base.BeforeStart();

            if (!Sync.IsServer) return;

            bool shouldHaveCargoShips = MyFakes.ENABLE_CARGO_SHIPS && MySession.Static.CargoShipsEnabled;

            var cargoShipEvent = MyGlobalEvents.GetEventById(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), "SpawnCargoShip"));
            if (cargoShipEvent == null && shouldHaveCargoShips)
            {
                var globalEvent = MyGlobalEventFactory.CreateEvent(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), "SpawnCargoShip"));
                MyGlobalEvents.AddGlobalEvent(globalEvent);
            }
            else if (cargoShipEvent != null)
            {
                if (shouldHaveCargoShips)
                    cargoShipEvent.Enabled = true;
                else
                    cargoShipEvent.Enabled = false;
            }
        }

        private static Predicate<MyCockpit> IsCockpitClosed = (MyCockpit cockpit) => cockpit.Closed;
        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (!MyDebugDrawSettings.DEBUG_DRAW_NEUTRAL_SHIPS) return;

            var entities = MyEntities.GetEntities();
            foreach (var entity in entities)
            {
                var grid = entity as MyCubeGrid;
                if (grid == null) continue;

                var cockpits = grid.GetFatBlocks<MyCockpit>();

                foreach (var cockpit in cockpits)
                {
                    if (cockpit.AiPilot == null) continue;

                    cockpit.AiPilot.DebugDraw();
                }
            }
        }

        private static MySpawnGroupDefinition PickRandomSpawnGroup()
        {
            ProfilerShort.Begin("Pick spawn group");
            if (m_spawnGroupCumulativeFrequencies.Count == 0)
            {
                ProfilerShort.End();
                return null;
            }

            float rnd = MyUtils.GetRandomFloat(0.0f, m_spawnGroupTotalFrequencies);
            int i = 0;
            while (i < m_spawnGroupCumulativeFrequencies.Count)
            {
                if (rnd <= m_spawnGroupCumulativeFrequencies[i])
                    break;

                ++i;
            }

            Debug.Assert(i < m_spawnGroupCumulativeFrequencies.Count, "Could not sample a spawn group");
            if (i >= m_spawnGroupCumulativeFrequencies.Count)
                i = m_spawnGroupCumulativeFrequencies.Count - 1;

            ProfilerShort.End();
            return m_spawnGroups[i];
        }

        private static void GetSafeBoundingBoxForPlayers(Vector3D start, double spawnDistance, out BoundingBoxD output)
        {
            double tolerance = 10.0f;
            BoundingSphereD sphere = new BoundingSphereD(start, tolerance);

            var players = MySession.Static.Players.GetOnlinePlayers();
            bool tryIncludeOtherPlayers = true;

            // We have to try adding other players until the bounding sphere stays the same
            while (tryIncludeOtherPlayers)
            {
                tryIncludeOtherPlayers = false;
                foreach (var player in players)
                {
                    Vector3D playerPosition = player.GetPosition();
                    double distanceFromSphere = (sphere.Center - playerPosition).Length() - sphere.Radius;

                    if (distanceFromSphere <= 0.0) continue;
                    if (distanceFromSphere > spawnDistance * 2.0f) continue;

                    sphere.Include(new BoundingSphereD(playerPosition, tolerance));
                    tryIncludeOtherPlayers = true;
                }
            }

            sphere.Radius += spawnDistance;
            output = new BoundingBoxD(sphere.Center - new Vector3D(sphere.Radius), sphere.Center + new Vector3D(sphere.Radius));

            var entities = MyEntities.GetEntitiesInAABB(ref output);
            foreach (var entity in entities)
            {
                if (entity is MyCubeGrid)
                {
                    var cubeGrid = entity as MyCubeGrid;
                    if (cubeGrid.IsStatic)
                    {
                        Vector3D gridPosition = cubeGrid.PositionComp.GetPosition();

                        // If grid is close to picked player we need to include it's "safe" bounding box for spawning ships,
                        // so cargo ships don't spawn near it.

                        output.Include(new BoundingBoxD(new Vector3D(gridPosition - spawnDistance), new Vector3D(gridPosition + spawnDistance)));
                    }
                }
            }
            entities.Clear();
        }

        [MyGlobalEventHandler(typeof(MyObjectBuilder_GlobalEventBase), "SpawnCargoShip")]
        public static void OnGlobalSpawnEvent(object senderEvent)
        {
            // Select a spawn group to spawn
            MySpawnGroupDefinition spawnGroup = PickRandomSpawnGroup();
            if (spawnGroup == null)
            {
                return;
            }

            spawnGroup.ReloadPrefabs();

            ProfilerShort.Begin("Generate position and direction");
            
            double spawnDistance = NEUTRAL_SHIP_SPAWN_DISTANCE;
            Vector3D playerPosition = Vector3D.Zero;
            bool isWorldLimited = MyEntities.IsWorldLimited();
            int numPlayers = 0;
            if (isWorldLimited)
            {
                spawnDistance = Math.Min(spawnDistance, MyEntities.WorldSafeHalfExtent() - spawnGroup.SpawnRadius);
            }
            else
            {
                // In infinite worlds players can be thousands of kilometers away, so spawn ship around random player
                // so cargo ships will be spawned around every player at some time
                var players = MySession.Static.Players.GetOnlinePlayers();
                // In DS there can be no players connected
                numPlayers = Math.Max(0, players.Count - 1);
                int randomPlayerPosition = MyUtils.GetRandomInt(0, numPlayers);
                int i = 0;
                foreach (var player in players)
                {
                    if (i == randomPlayerPosition)
                    {
                        if (player.Character != null)
                        {
                            playerPosition = player.GetPosition();
                        }
                        break;
                    }
                    i++;
                }

            }
            if (spawnDistance < 0.0f)
            {
                MySandboxGame.Log.WriteLine("Not enough space in the world to spawn such a huge spawn group!");
                return;
            }

            double forbiddenRadius = NEUTRAL_SHIP_FORBIDDEN_RADIUS;
            BoundingBoxD spawnBox;
            if (isWorldLimited)
            {
                spawnBox = new BoundingBoxD(new Vector3D(playerPosition - spawnDistance), new Vector3D(playerPosition + spawnDistance));
            }
            else
            {
                // We need to extend bouding box so cargo ships aren't spawned near other players
                GetSafeBoundingBoxForPlayers(playerPosition, spawnDistance, out spawnBox);
                // Forbidden radius is sphere around all players in box.
                // Bounding box is generated from players positions so their distance to center shall be same for all players
                forbiddenRadius += spawnBox.HalfExtents.Max() - NEUTRAL_SHIP_FORBIDDEN_RADIUS;
            }

            // Get the direction to the center and deviate it randomly
            Vector3D? origin = MyUtils.GetRandomBorderPosition(ref spawnBox);
            origin = MyEntities.TestPlaceInSpace(origin.Value, spawnGroup.SpawnRadius);
            if (!origin.HasValue)
            {
                RetryEventWithMaxTry(senderEvent as MyGlobalEventBase);
                ProfilerShort.End();
                return;
            }

            // Radius in arc units of the forbidden sphere in the center, when viewed from origin
            float centerArcRadius = (float)Math.Atan(forbiddenRadius / (origin.Value - spawnBox.Center).Length());

            // Generate direction with elevation from centerArcRadius radians to (cAR + N_S_D_S) radians
            Vector3D direction = -Vector3D.Normalize(origin.Value);
            float theta = MyUtils.GetRandomFloat(centerArcRadius, centerArcRadius + NEUTRAL_SHIP_DIRECTION_SPREAD);
            float phi = MyUtils.GetRandomRadian();
            Vector3D cosVec = Vector3D.CalculatePerpendicularVector(direction);
            Vector3D sinVec = Vector3D.Cross(direction, cosVec);
            cosVec *= (Math.Sin(theta) * Math.Cos(phi));
            sinVec *= (Math.Sin(theta) * Math.Sin(phi));
            direction = direction * Math.Cos(theta) + cosVec + sinVec;

            Vector3D destination = Vector3D.Zero;
            RayD ray = new RayD(origin.Value, direction);
            double? intersection = ray.Intersects(spawnBox);
            Vector3D directionMult;
            if (!intersection.HasValue || intersection.Value < NEUTRAL_SHIP_MINIMAL_ROUTE_LENGTH)
            {
                directionMult = direction * NEUTRAL_SHIP_MINIMAL_ROUTE_LENGTH;
            }
            else
            {
                directionMult = direction * intersection.Value;
            }
            destination = origin.Value + directionMult;

            Vector3D upVector = Vector3D.CalculatePerpendicularVector(direction);
            Vector3D rightVector = Vector3D.Cross(direction, upVector);
            MatrixD originMatrix = MatrixD.CreateWorld(origin.Value, direction, upVector);

            ProfilerShort.End();

            ProfilerShort.Begin("Check free space");

            // CH:TODO: Convex cast to detect collision
            // Check ships' path to avoid possible collisions. (TODO: But only if it is said in the definitions)
            m_raycastHits.Clear();
            foreach (var shipPrefab in spawnGroup.Prefabs)
            {
                var prefabDef = MyDefinitionManager.Static.GetPrefabDefinition(shipPrefab.SubtypeId);
                Debug.Assert(prefabDef != null);

                Vector3D shipPosition = Vector3.Transform(shipPrefab.Position, originMatrix);
                Vector3D shipDestination = shipPosition + directionMult;
                float radius = prefabDef == null ? 10.0f : prefabDef.BoundingSphere.Radius;

                //these point checks could be done in the trajectory intersect, but checking points is faster than ray intersect
                if (MyGravityProviderSystem.IsPositionInNaturalGravity(shipPosition, spawnGroup.SpawnRadius))
                {
                    if (!MyFinalBuildConstants.IS_OFFICIAL)
                        MySandboxGame.Log.WriteLine("Could not spawn neutral ships: spawn point is inside gravity well");
                    RetryEventWithMaxTry(senderEvent as MyGlobalEventBase);
                    ProfilerShort.End();
                    return;
                }
                if (MyGravityProviderSystem.IsPositionInNaturalGravity(shipDestination, spawnGroup.SpawnRadius))
                {
                    if (!MyFinalBuildConstants.IS_OFFICIAL)
                        MySandboxGame.Log.WriteLine("Could not spawn neutral ships: destination point is inside gravity well");
                    RetryEventWithMaxTry(senderEvent as MyGlobalEventBase);
                    ProfilerShort.End();
                    return;
                }
                if (MyGravityProviderSystem.DoesTrajectoryIntersectNaturalGravity(shipPosition, shipDestination, spawnGroup.SpawnRadius + NEUTRAL_SHIP_SPAWN_OFFSET))
                {
                    if (!MyFinalBuildConstants.IS_OFFICIAL)
                        MySandboxGame.Log.WriteLine("Could not spawn neutral ships: flight path intersects gravity well");
                    RetryEventWithMaxTry(senderEvent as MyGlobalEventBase);
                    ProfilerShort.End();
                    return;
                }

                MyPhysics.CastRay(shipPosition, shipDestination, m_raycastHits, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
                if (m_raycastHits.Count > 0)
                {
                    if (!MyFinalBuildConstants.IS_OFFICIAL)
                        MySandboxGame.Log.WriteLine("Could not spawn neutral ships due to collision");
                    RetryEventWithMaxTry(senderEvent as MyGlobalEventBase);
                    ProfilerShort.End();
                    return;
                }

                for (int i = 0; i < 4; ++i)
                {
                    Vector3D shiftVector = upVector * m_upVecMultipliers[i] * radius + rightVector * m_rightVecMultipliers[i] * radius;
                    MyPhysics.CastRay(shipPosition + shiftVector, shipDestination + shiftVector, m_raycastHits, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);

                    if (m_raycastHits.Count > 0)
                    {
                        if (!MyFinalBuildConstants.IS_OFFICIAL)
                            MySandboxGame.Log.WriteLine("Could not spawn neutral ships due to collision");
                        RetryEventWithMaxTry(senderEvent as MyGlobalEventBase);
                        ProfilerShort.End();
                        return;
                    }
                }
            }

            ProfilerShort.End();

            ProfilerShort.Begin("Spawn ships");

            long spawnGroupId = MyPirateAntennas.GetPiratesId();

            // The ships were collision-free. Now spawn them
            foreach (var shipPrefab in spawnGroup.Prefabs)
            {
                ProfilerShort.Begin(shipPrefab.BeaconText);

                // Yes, this could have been saved in the previous loop, but compared to (e.g.) raycasts, this does not take too much time to recalculate
                Vector3D shipPosition = Vector3D.Transform((Vector3D)shipPrefab.Position, originMatrix);
                Vector3D shipDestination = shipPosition + directionMult;
                Vector3D up = Vector3D.CalculatePerpendicularVector(-direction);

                List<MyCubeGrid> tmpGridList = new List<MyCubeGrid>();

                // CH: We don't want a new identity for each ship anymore. We should handle that in a better way...
                /*if (shipPrefab.ResetOwnership)
                {
                    if (spawnGroupId == 0)
                    {
                        //This is not an NPC so that it doesn't show up in assign ownership drop down menu
                        MyIdentity spawnGroupIdentity = Sync.Players.CreateNewIdentity("Neutral NPC");
                        spawnGroupId = spawnGroupIdentity.IdentityId;
                    }
                }*/

                // Deploy ship
                ProfilerShort.Begin("Spawn cargo ship");
                Stack<Action> callbacks = new Stack<Action>();
                callbacks.Push(delegate()
                {
                    InitAutopilot(tmpGridList, shipDestination, direction);
                    foreach (var grid in tmpGridList)
                    {
                        Debug.Assert(grid.Physics.Enabled);
                        grid.ActivatePhysics(); //last chance to activate physics
                    }
                });
                MyPrefabManager.Static.SpawnPrefab(
                    resultList: tmpGridList,
                    prefabName: shipPrefab.SubtypeId,
                    position: shipPosition,
                    forward: direction,
                    up: up,
                    initialLinearVelocity: shipPrefab.Speed * direction,
                    beaconName: shipPrefab.BeaconText,
                    spawningOptions: VRage.Game.ModAPI.SpawningOptions.RotateFirstCockpitTowardsDirection |
                                     VRage.Game.ModAPI.SpawningOptions.SpawnRandomCargo |
                                     VRage.Game.ModAPI.SpawningOptions.DisableDampeners,
                                     ownerId: shipPrefab.ResetOwnership ? spawnGroupId : 0,
                    callbacks: callbacks);
                ProfilerShort.End();
                ProfilerShort.End();
            }
            m_eventSpawnTry = 0;
            ProfilerShort.End();
        }

        private static void InitAutopilot(List<MyCubeGrid> tmpGridList, Vector3D shipDestination, Vector3D direction)
        {
                foreach (var grid in tmpGridList)
                {
                    var cockpit = grid.GetFirstBlockOfType<MyCockpit>();
                    if (cockpit != null)
                    {
                        MySimpleAutopilot ai = new MySimpleAutopilot(shipDestination, (Vector3)direction);
                        cockpit.AttachAutopilot(ai);
                        break;
                    }
                }
            }

        private static void RetryEventWithMaxTry(MyGlobalEventBase evt)
        {
            if (++m_eventSpawnTry <= EVENT_SPAWN_TRY_MAX)
            {
                MySandboxGame.Log.WriteLine("Could not spawn event. Try " + m_eventSpawnTry + " of " + EVENT_SPAWN_TRY_MAX);

                MyGlobalEvents.RescheduleEvent(evt, NEUTRAL_SHIP_RESCHEDULE_TIME);
                return;
            }
            else
            {
                m_eventSpawnTry = 0;
                return;
            }
        }

        }
    }
