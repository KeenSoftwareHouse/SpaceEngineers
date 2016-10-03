using Sandbox;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SpaceEngineers.Game.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageMath.Spatial;
using VRageRender;

namespace SpaceEngineers.AI
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 502, typeof(MyObjectBuilder_SpaceFaunaComponent))]
    public class MySpaceFaunaComponent : MySessionComponentBase
    {
        private class PlanetAIInfo
        {
            public MyPlanet Planet;
            public int BotNumber;

            public PlanetAIInfo(MyPlanet planet)
            {
                this.Planet = planet;
                BotNumber = 0;
            }
        }

        private class SpawnInfo
        {
            public int SpawnTime;
            public int AbandonTime;
            public Vector3D Position;
            public MyPlanet Planet;
            public bool SpawnDone;

            public SpawnInfo(Vector3D position, int gameTime, MyPlanet planet)
            {
                var animalSpawnInfo = MySpaceBotFactory.GetDayOrNightAnimalSpawnInfo(planet, position);
                Debug.Assert(animalSpawnInfo != null);
                SpawnTime = gameTime + MyUtils.GetRandomInt(animalSpawnInfo.SpawnDelayMin, animalSpawnInfo.SpawnDelayMax);
                AbandonTime = gameTime + ABANDON_DELAY;
                Position = position;
                Planet = planet;
                SpawnDone = false;
            }

            public SpawnInfo(MyObjectBuilder_SpaceFaunaComponent.SpawnInfo info, int currentTime)
            {
                SpawnTime = currentTime + info.SpawnTime;
                AbandonTime = currentTime + info.SpawnTime;
                Position = new Vector3D(info.X, info.Y, info.Z);
                Planet = MyGamePruningStructure.GetClosestPlanet(Position);
                SpawnDone = false;
            }

            public bool ShouldSpawn(int currentTime)
            {
                return SpawnTime - currentTime < 0;
            }

            public bool IsAbandoned(int currentTime)
            {
                return AbandonTime - currentTime < 0;
            }

            public void UpdateAbandoned(int currentTime)
            {
                AbandonTime = currentTime + ABANDON_DELAY;
            }
        }

        private class SpawnTimeoutInfo
        {
            public int TimeoutTime;
            public Vector3D Position;
            public MyPlanetAnimalSpawnInfo AnimalSpawnInfo;

            public SpawnTimeoutInfo(Vector3D position, int currentTime)
            {
                TimeoutTime = currentTime;
                Position = position;
                var planet = MyGamePruningStructure.GetClosestPlanet(Position);
                Debug.Assert(planet != null);
                AnimalSpawnInfo = MySpaceBotFactory.GetDayOrNightAnimalSpawnInfo(planet, Position);
                if (AnimalSpawnInfo == null)
                {
                    TimeoutTime = currentTime;
                }
            }

            public SpawnTimeoutInfo(MyObjectBuilder_SpaceFaunaComponent.TimeoutInfo info, int currentTime)
            {
                TimeoutTime = currentTime + info.Timeout;
                Position = new Vector3D(info.X, info.Y, info.Z);
                var planet = MyGamePruningStructure.GetClosestPlanet(Position);
                AnimalSpawnInfo = MySpaceBotFactory.GetDayOrNightAnimalSpawnInfo(planet, Position);
                if (AnimalSpawnInfo == null)
                {
                    TimeoutTime = currentTime;
                }
            }

            internal void AddKillTimeout()
            {
                if (AnimalSpawnInfo != null)
                    TimeoutTime += AnimalSpawnInfo.KillDelay;
            }

            internal bool IsTimedOut(int currentTime)
            {
                return TimeoutTime - currentTime < 0;
            }
        }

        // CH: TODO: Put the constants into definitions, when possible
        const string Wolf_SUBTYPE_ID = "Wolf";
        private static readonly int UPDATE_DELAY = 120;        // Interval between updates of this component
        private static readonly int CLEAN_DELAY = 2 * 60 * 20; // Interval between cleanup of spawn infos and unused identities
        private static readonly int ABANDON_DELAY = 45000;     // How long can a spawn info be abandoned

        private static readonly float DESPAWN_DIST = 400.0f;  // How far do the bots have to be to disappear
        private static readonly float SPHERE_SPAWN_DIST = 150.0f;    // Radius of the spawn info spheres
        private static readonly float PROXIMITY_DIST = 50.0f; // How close to planet you have to be to spawn fauna
        private static readonly float TIMEOUT_DIST = 150.0f;  // Radius of the timeout area that appears after killing a spider

        private static readonly int MAX_BOTS_PER_PLANET = 10;

        private int m_waitForUpdate = UPDATE_DELAY;
        private int m_waitForClean = CLEAN_DELAY;

        private Dictionary<long, PlanetAIInfo> m_planets = new Dictionary<long, PlanetAIInfo>();
        private List<Vector3D> m_tmpPlayerPositions = new List<Vector3D>();

        private MyVector3DGrid<SpawnInfo> m_spawnInfoGrid = new MyVector3DGrid<SpawnInfo>(SPHERE_SPAWN_DIST);
        private List<SpawnInfo> m_allSpawnInfos = new List<SpawnInfo>();

        private MyVector3DGrid<SpawnTimeoutInfo> m_timeoutInfoGrid = new MyVector3DGrid<SpawnTimeoutInfo>(TIMEOUT_DIST);
        private List<SpawnTimeoutInfo> m_allTimeoutInfos = new List<SpawnTimeoutInfo>();

        private MyObjectBuilder_SpaceFaunaComponent m_obForLoading = null;

        public override Type[] Dependencies
        {
            get
            {
                return new Type[] { typeof(MyAIComponent) };
            }
        }

        public override bool IsRequiredByGame
        {
            get
            {
                return MyPerGameSettings.Game == GameEnum.SE_GAME && MyPerGameSettings.EnableAi == true;
            }
        }

        static MySpaceFaunaComponent()
        {
        }

        public override void LoadData()
        {
            base.LoadData();

            if (!Sync.IsServer) return;

            MyEntities.OnEntityAdd += EntityAdded;
            MyEntities.OnEntityRemove += EntityRemoved;

            MyAIComponent.Static.BotCreatedEvent += OnBotCreatedEvent;
            //MyCestmirDebugInputComponent.TestAction += EraseAllInfos;

            m_botCharacterDied = BotCharacterDied;
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            if (!Sync.IsServer) return;

            MyEntities.OnEntityAdd -= EntityAdded;
            MyEntities.OnEntityRemove -= EntityRemoved;

            MyAIComponent.Static.BotCreatedEvent -= OnBotCreatedEvent;
            //MyCestmirDebugInputComponent.TestAction -= EraseAllInfos;

            m_botCharacterDied = null;

            m_planets.Clear();
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            m_obForLoading = sessionComponent as MyObjectBuilder_SpaceFaunaComponent;
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_SpaceFaunaComponent;

            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            int num = 0;
            foreach (var info in m_allSpawnInfos)
            {
                if (info.SpawnDone) continue;
                num++;
            }
            ob.SpawnInfos.Capacity = num;

            foreach (var info in m_allSpawnInfos)
            {
                if (info.SpawnDone) continue;

                var infoBuilder = new MyObjectBuilder_SpaceFaunaComponent.SpawnInfo();
                infoBuilder.X = info.Position.X;
                infoBuilder.Y = info.Position.Y;
                infoBuilder.Z = info.Position.Z;
                infoBuilder.AbandonTime = Math.Max(0, info.AbandonTime - currentTime);
                infoBuilder.SpawnTime = Math.Max(0, info.SpawnTime - currentTime);

                ob.SpawnInfos.Add(infoBuilder);
            }

            ob.TimeoutInfos.Capacity = m_allTimeoutInfos.Count;
            foreach (var info in m_allTimeoutInfos)
            {
                var infoBuilder = new MyObjectBuilder_SpaceFaunaComponent.TimeoutInfo();
                infoBuilder.X = info.Position.X;
                infoBuilder.Y = info.Position.Y;
                infoBuilder.Z = info.Position.Z;
                infoBuilder.Timeout = Math.Max(0, info.TimeoutTime - currentTime);

                ob.TimeoutInfos.Add(infoBuilder);
            }

            return ob;
        }

        public override void BeforeStart()
        {
            base.BeforeStart();

            if (m_obForLoading == null) return;

            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            m_allSpawnInfos.Capacity = m_obForLoading.SpawnInfos.Count;
            foreach (var info in m_obForLoading.SpawnInfos)
            {
                var spawnInfo = new SpawnInfo(info, currentTime);
                m_allSpawnInfos.Add(spawnInfo);
                m_spawnInfoGrid.AddPoint(ref spawnInfo.Position, spawnInfo);
            }

            m_allTimeoutInfos.Capacity = m_obForLoading.TimeoutInfos.Count;
            foreach (var info in m_obForLoading.TimeoutInfos)
            {
                var timeoutInfo = new SpawnTimeoutInfo(info, currentTime);
                if (timeoutInfo.AnimalSpawnInfo == null) // do not add invalid timeout objects
                    continue;
                m_allTimeoutInfos.Add(timeoutInfo);
                m_timeoutInfoGrid.AddPoint(ref timeoutInfo.Position, timeoutInfo);
            }

            m_obForLoading = null;
        }

        // Add a planet to our list. Event registered in MyEntities.
        private void EntityAdded(MyEntity entity)
        {
            var planet = entity as MyPlanet;
            if (planet != null && PlanetHasFauna(planet))
            {
                m_planets.Add(entity.EntityId, new PlanetAIInfo(planet));
            }
        }

        // Remove the planet from our list. Event registered in MyEntities.
        private void EntityRemoved(MyEntity entity)
        {
            var planet = entity as MyPlanet;
            if (planet != null)
            {
                m_planets.Remove(entity.EntityId);
                // CH: TODO: Remove all the planet's spawn infos
            }
        }

        private bool PlanetHasFauna(MyPlanet planet)
        {
            // compulsory: at least normal spawning info, planet can exist without night animals
            return planet.Generator.AnimalSpawnInfo != null
                && planet.Generator.AnimalSpawnInfo.Animals != null 
                && planet.Generator.AnimalSpawnInfo.Animals.Length > 0;
        }

        private void SpawnBot(SpawnInfo spawnInfo, MyPlanet planet, MyPlanetAnimalSpawnInfo animalSpawnInfo)
        {          
            PlanetAIInfo planetInfo = null;
            if (!m_planets.TryGetValue(planet.EntityId, out planetInfo))
            {
                Debug.Assert(false, "Could not get planet info!");
                return;
            }

            if (planetInfo.BotNumber >= MAX_BOTS_PER_PLANET) 
                return;

            Debug.Assert(animalSpawnInfo != null);
            double spawnDistMin = animalSpawnInfo.SpawnDistMin;
            double spawnDistMax = animalSpawnInfo.SpawnDistMax;
            Vector3D center = spawnInfo.Position;
            Vector3D planetGravityVec = MyGravityProviderSystem.CalculateNaturalGravityInPoint(center);
            //GR: if gravity is zero provide a random Vector to normalize
            if (planetGravityVec == Vector3D.Zero)
            {
                planetGravityVec = Vector3D.Up;
            }
            planetGravityVec.Normalize();
            Vector3D planetTangent = Vector3D.CalculatePerpendicularVector(planetGravityVec);
            Vector3D planetBitangent = Vector3D.Cross(planetGravityVec, planetTangent);
            planetTangent.Normalize();
            planetBitangent.Normalize();
            Vector3D spawnPos = MyUtils.GetRandomDiscPosition(ref center, spawnDistMin, spawnDistMax, ref planetTangent, ref planetBitangent);
            
            spawnPos = planet.GetClosestSurfacePointGlobal(ref spawnPos);
            Vector3D? spawnPosCorrected = MyEntities.FindFreePlace(spawnPos, 2.0f);
            if (spawnPosCorrected.HasValue)
                spawnPos = spawnPosCorrected.Value;

            planet.CorrectSpawnLocation(ref spawnPos, 2.0f);

            MyAgentDefinition botBehavior = GetAnimalDefinition(animalSpawnInfo) as MyAgentDefinition;
            if (botBehavior != null)
            {
                if (botBehavior.Id.SubtypeName == Wolf_SUBTYPE_ID && MySession.Static.EnableWolfs)
                {
                    MyAIComponent.Static.SpawnNewBot(botBehavior, spawnPos);
                }
                else if (botBehavior.Id.SubtypeName != Wolf_SUBTYPE_ID && MySession.Static.EnableSpiders)
                {
                    MyAIComponent.Static.SpawnNewBot(botBehavior, spawnPos);
                }
            }
        }

        private void OnBotCreatedEvent(int botSerialNum, MyBotDefinition botDefinition)
        {
 	        var agentDefinition = botDefinition as MyAgentDefinition;
            if (agentDefinition != null && agentDefinition.FactionTag == "SPID")
            {
                MyPlayer player = null;
                if (Sync.Players.TryGetPlayerById(new MyPlayer.PlayerId(Sync.MyId, botSerialNum), out player))
                {
                    player.Controller.ControlledEntityChanged += OnBotControlledEntityChanged;
                    MyCharacter character = (player.Controller.ControlledEntity as MyCharacter);
                    if (character != null)
                    {
                        character.CharacterDied += BotCharacterDied;
                    }
                }
            }
        }

        private void OnBotControlledEntityChanged(IMyControllableEntity oldControllable, IMyControllableEntity newControllable)
        {
            var oldCharacter = oldControllable as MyCharacter;
            var newCharacter = newControllable as MyCharacter;

            if (oldCharacter != null)
            {
                oldCharacter.CharacterDied -= BotCharacterDied;
            }

            if (newCharacter != null)
            {
                newCharacter.CharacterDied += BotCharacterDied;
            }
        }

        private Action<MyCharacter> m_botCharacterDied;
        void BotCharacterDied(MyCharacter obj)
        {
            Vector3D position = obj.PositionComp.GetPosition();
            obj.CharacterDied -= BotCharacterDied;

            int timeoutNum = 0;
            var enumerator = m_timeoutInfoGrid.GetPointsCloserThan(ref position, TIMEOUT_DIST);
            while (enumerator.MoveNext())
            {
                timeoutNum++;
                enumerator.Current.AddKillTimeout();
            }

            if (timeoutNum == 0)
            {
                int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                var newInfo = new SpawnTimeoutInfo(position, currentTime);
                newInfo.AddKillTimeout();
                m_timeoutInfoGrid.AddPoint(ref position, newInfo);
                m_allTimeoutInfos.Add(newInfo);
            }
        }

        private MyBotDefinition GetAnimalDefinition(MyPlanetAnimalSpawnInfo animalSpawnInfo)
        {
            Debug.Assert(animalSpawnInfo != null, "Missing animal spawn info in planet definition.");
            Debug.Assert(animalSpawnInfo.Animals != null, "Missing array of animals in planet definition.");
            Debug.Assert(animalSpawnInfo.Animals.Length > 0, "Array of animals is empty in planet definition.");

            int animalIndex = MyUtils.GetRandomInt(0, animalSpawnInfo.Animals.Length);
            var animalDefinition = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_AnimalBot), animalSpawnInfo.Animals[animalIndex].AnimalType);
            return MyDefinitionManager.Static.GetBotDefinition(animalDefinition) as MyAgentDefinition;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (!Sync.IsServer) return;

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_FAUNA_COMPONENT)
            {
                DebugDraw();
            }

            ProfilerShort.Begin("MySpaceFaunaComponent.UpdateAfter");

            m_waitForUpdate--;
            if (m_waitForUpdate > 0)
            {
                return;
            }
            m_waitForUpdate = UPDATE_DELAY;

            var players = Sync.Players.GetOnlinePlayers();
            m_tmpPlayerPositions.Capacity = Math.Max(m_tmpPlayerPositions.Capacity, players.Count);
            m_tmpPlayerPositions.Clear();

            // Reset bot numbers
            foreach (var planet in m_planets)
            {
                planet.Value.BotNumber = 0;
            }

            // Update bot numbers and save player positions
            foreach (var player in players)
            {
                // Human player
                if (player.Id.SerialId == 0)
                {
                    if (player.Controller.ControlledEntity == null) continue;
                    var pos = player.GetPosition();
                    m_tmpPlayerPositions.Add(pos);
                }
                // Bot
                else
                {
                    if (player.Controller.ControlledEntity == null) continue;
                    var pos = player.GetPosition();

                    var planet = MyGamePruningStructure.GetClosestPlanet(pos);
                    if (planet != null)
                    {
                        PlanetAIInfo planetInfo;
                        if (m_planets.TryGetValue(planet.EntityId, out planetInfo))
                        {
                            planetInfo.BotNumber++;
                        }
                    }
                }
            }

            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            // Spawn bots near players on planets, only if in survival/myfakes macro set
            if (MyFakes.SPAWN_SPACE_FAUNA_IN_CREATIVE)
            {
                foreach (var player in players)
                {

                    if (player.Controller.ControlledEntity == null)
                        continue;

                    var pos = player.GetPosition();
                    var planet = MyGamePruningStructure.GetClosestPlanet(pos);
                    if (planet == null || !PlanetHasFauna(planet))
                        continue;

                    PlanetAIInfo planetInfo = null;
                    if (!m_planets.TryGetValue(planet.EntityId, out planetInfo))
                        continue;

                    // Human player - spawn spiders
                    if (player.Id.SerialId == 0)
                    {
                        // Distance to surface check
                        Vector3D toSurface = planet.GetClosestSurfacePointGlobal(ref pos) - pos;
                        if (toSurface.LengthSquared() >= PROXIMITY_DIST * PROXIMITY_DIST || planetInfo.BotNumber >= MAX_BOTS_PER_PLANET)
                            continue;

                        int spawnPointCount = 0;
                        var pointEnum = m_spawnInfoGrid.GetPointsCloserThan(ref pos, SPHERE_SPAWN_DIST);
                        while (pointEnum.MoveNext())
                        {
                            var spawnPoint = pointEnum.Current;
                            spawnPointCount++;
                            if (spawnPoint.SpawnDone)
                                continue; // Don't take not-yet cleaned spawn info into consideration

                            if (spawnPoint.ShouldSpawn(currentTime))
                            {
                                spawnPoint.SpawnDone = true;
                                var timeouts = m_timeoutInfoGrid.GetPointsCloserThan(ref pos, TIMEOUT_DIST);
                                bool timeoutPresent = false;
                                while (timeouts.MoveNext())
                                {
                                    if (timeouts.Current.IsTimedOut(currentTime))
                                        continue;
                                    timeoutPresent = true;
                                    break;
                                }
                                if (timeoutPresent) continue;

                                var animalSpawnInfo = MySpaceBotFactory.GetDayOrNightAnimalSpawnInfo(planet, spawnPoint.Position);
                                if (animalSpawnInfo == null) continue;

                                int numBots = MyUtils.GetRandomInt(animalSpawnInfo.WaveCountMin, animalSpawnInfo.WaveCountMax);
                                for (int i = 0; i < numBots; ++i)
                                {
                                    SpawnBot(spawnPoint, planet, animalSpawnInfo);
                                }
                            }
                            else
                            {
                                spawnPoint.UpdateAbandoned(currentTime);
                            }
                        }

                        if (spawnPointCount == 0) // we dont have any spawn points near human players position
                        {
                            var spawnInfo = new SpawnInfo(pos, currentTime, planet);
                            m_spawnInfoGrid.AddPoint(ref pos, spawnInfo);
                            m_allSpawnInfos.Add(spawnInfo);
                        }
                    }
                    // Despawn bots that are too far from all players
                    else //if (player.Id.SteamId == Sync.MyId)
                    {
                        double closestDistSq = double.MaxValue;
                        foreach (Vector3D playerPosition in m_tmpPlayerPositions)
                        {
                            closestDistSq = Math.Min(Vector3D.DistanceSquared(pos, playerPosition), closestDistSq);
                        }

                        if (closestDistSq > DESPAWN_DIST * DESPAWN_DIST)
                        {
                            MyAIComponent.Static.RemoveBot(player.Id.SerialId, removeCharacter: true);
                        }
                    }
                }
            }
            m_tmpPlayerPositions.Clear();

            m_waitForClean -= UPDATE_DELAY;

            if (m_waitForClean <= 0)
            {
                MyAIComponent.Static.CleanUnusedIdentities();
                m_waitForClean = CLEAN_DELAY;

                for (int i = 0; i < m_allSpawnInfos.Count; ++i)
                {
                    var spawnInfo = m_allSpawnInfos[i];
                    if (spawnInfo.IsAbandoned(currentTime) || spawnInfo.SpawnDone)
                    {
                        m_allSpawnInfos.RemoveAtFast(i);
                        Vector3D point = spawnInfo.Position;
                        m_spawnInfoGrid.RemovePoint(ref point);
                        --i;
                    }
                }

                for (int i = 0; i < m_allTimeoutInfos.Count; ++i)
                {
                    var timeoutInfo = m_allTimeoutInfos[i];
                    if (timeoutInfo.IsTimedOut(currentTime))
                    {
                        m_allTimeoutInfos.RemoveAtFast(i);
                        Vector3D point = timeoutInfo.Position;
                        m_timeoutInfoGrid.RemovePoint(ref point);
                        --i;
                    }
                }
            }

            ProfilerShort.End();
        }

        private void EraseAllInfos()
        {
            foreach (var info in m_allSpawnInfos)
            {
                info.SpawnTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            }

            foreach (var info in m_allTimeoutInfos)
            {
                m_timeoutInfoGrid.RemovePoint(ref info.Position);
            }
            m_allTimeoutInfos.Clear();
        }

        public void DebugDraw()
        {
            int y = 0;
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "Cleanup in " + m_waitForClean.ToString(), Color.Red, 0.5f);
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "Planet infos:", Color.GreenYellow, 0.5f);
            foreach (var info in m_planets)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "  Name: " + info.Value.Planet.Generator.FolderName + ", Id: " + info.Key + ", Bots: " + info.Value.BotNumber.ToString(), Color.LightYellow, 0.5f);
            }
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, (y++) * 13.0f), "Num. of spawn infos: " + m_allSpawnInfos.Count + "/" + m_timeoutInfoGrid.Count, Color.GreenYellow, 0.5f);
            
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            foreach (var spawnInfo in m_allSpawnInfos)
            {
                Vector3D position = spawnInfo.Position;
                Vector3 down = spawnInfo.Planet.PositionComp.GetPosition() - position;
                down.Normalize();

                int secondsRemaining = Math.Max(0, (spawnInfo.SpawnTime - currentTime) / 1000);
                int abandonedIn = Math.Max(0, (spawnInfo.AbandonTime - currentTime) / 1000);
                if (secondsRemaining == 0 || abandonedIn == 0) continue;

                MyRenderProxy.DebugDrawSphere(position, SPHERE_SPAWN_DIST, Color.Yellow, 1.0f, false);
                MyRenderProxy.DebugDrawText3D(position, "Spawning in: " + secondsRemaining.ToString(), Color.Yellow, 0.5f, false);
                MyRenderProxy.DebugDrawText3D(position - down * 0.5f, "Abandoned in: " + abandonedIn.ToString(), Color.Yellow, 0.5f, false);
            }

            foreach (var timeoutInfo in m_allTimeoutInfos)
            {
                Vector3D position = timeoutInfo.Position;
                int secondsRemaining = Math.Max(0, (timeoutInfo.TimeoutTime - currentTime) / 1000);

                MyRenderProxy.DebugDrawSphere(position, TIMEOUT_DIST, Color.Blue, 1.0f, false);
                MyRenderProxy.DebugDrawText3D(position, "Timeout: " + secondsRemaining.ToString(), Color.Blue, 0.5f, false);
            }
        }
    }
}
