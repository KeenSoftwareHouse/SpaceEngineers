using Sandbox.Definitions;
using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.WorldEnvironment;
using Sandbox.Game.WorldEnvironment.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GameSystems
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 1111, typeof(MyObjectBuilder_EnvironmentBotSpawningSystem))]
    public class MyEnvironmentBotSpawningSystem : MySessionComponentBase
    {
        private static readonly int DELAY_BETWEEN_TICKS_IN_MS = 120000;
        private static readonly float BOT_SPAWN_RANGE_MIN = 80f;
        private static readonly float BOT_SPAWN_RANGE_MIN_SQ = BOT_SPAWN_RANGE_MIN * BOT_SPAWN_RANGE_MIN;
        private static readonly float BOT_DESPAWN_DISTANCE = 400f;
        private static readonly float BOT_DESPAWN_DISTANCE_SQ = BOT_DESPAWN_DISTANCE * BOT_DESPAWN_DISTANCE;
        private static readonly int MAX_SPAWN_ATTEMPTS = 5;

        public static MyEnvironmentBotSpawningSystem Static;

        private MyRandom m_random = new MyRandom();
        private List<Vector3D> m_tmpPlayerPositions;
        private HashSet<MyBotSpawningEnvironmentProxy> m_activeBotSpawningProxies;
        private int m_lastSpawnEventTimeInMs;
        private int m_timeSinceLastEventInMs;
        private int m_tmpSpawnAttempts;

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
                return MyPerGameSettings.EnableAi;
            }
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            var ob = sessionComponent as MyObjectBuilder_EnvironmentBotSpawningSystem;

            m_timeSinceLastEventInMs = ob.TimeSinceLastEventInMs;
            m_lastSpawnEventTimeInMs = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_timeSinceLastEventInMs;
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_EnvironmentBotSpawningSystem;

            ob.TimeSinceLastEventInMs = m_timeSinceLastEventInMs;

            return ob;
        }

        public override void LoadData()
        {
            base.LoadData();

            Static = this;

            m_tmpPlayerPositions = new List<Vector3D>();
            m_activeBotSpawningProxies = new HashSet<MyBotSpawningEnvironmentProxy>();

            if (!Sync.IsServer)
                return;
        }

        public override void BeforeStart()
        {
            base.BeforeStart();

            if (Sync.IsServer)
            {
            }
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            Static = null;
            m_tmpPlayerPositions = null;
            m_activeBotSpawningProxies = null;

            if (!Sync.IsServer)
                return;
        }

        public override void Draw()
        {
            base.Draw();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (!Sync.IsServer) return;

            m_timeSinceLastEventInMs = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastSpawnEventTimeInMs;
            if (m_timeSinceLastEventInMs >= DELAY_BETWEEN_TICKS_IN_MS)
            {
                ProfilerShort.Begin("Distant Bot Removal");
                RemoveDistantBots();
                MyAIComponent.Static.CleanUnusedIdentities();
                ProfilerShort.End();
                m_tmpSpawnAttempts = 0;
                ProfilerShort.Begin("Spawning Bots");
                SpawnTick();
                ProfilerShort.End();
                m_lastSpawnEventTimeInMs = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                m_timeSinceLastEventInMs = 0;
            }
        }

        public void RemoveDistantBots()
        {
            var players = Sync.Players.GetOnlinePlayers();
            m_tmpPlayerPositions.Capacity = Math.Max(m_tmpPlayerPositions.Capacity, players.Count);
            m_tmpPlayerPositions.Clear();

            foreach (var player in players)
            {
                if (player.Id.SerialId == 0)
                {
                    if (player.Controller.ControlledEntity == null) continue;
                    var pos = player.GetPosition();
                    m_tmpPlayerPositions.Add(pos);
                }
            }

            foreach (var player in players)
            {
                if (player.Controller.ControlledEntity == null)
                    continue;

                if (player.Id.SerialId != 0)
                {
                    bool remove = true;
                    var pos = player.GetPosition();
                    foreach (var playerPosition in m_tmpPlayerPositions)
                    {
                        if (Vector3D.DistanceSquared(pos, playerPosition) < BOT_DESPAWN_DISTANCE_SQ)
                            remove = false;
                    }

                    if (remove)
                        MyAIComponent.Static.RemoveBot(player.Id.SerialId, removeCharacter: true);
                }
            }
        }

        public void SpawnTick()
        {
            if (m_activeBotSpawningProxies.Count == 0 || m_tmpSpawnAttempts > MAX_SPAWN_ATTEMPTS)
                return;

            m_tmpSpawnAttempts++;
            var index = MyUtils.GetRandomInt(0, m_activeBotSpawningProxies.Count);            
            if (!m_activeBotSpawningProxies.ElementAt(index).OnSpawnTick())
                SpawnTick();
        }

        public void RegisterBotSpawningProxy(MyBotSpawningEnvironmentProxy proxy)
        {
            m_activeBotSpawningProxies.Add(proxy);
        }

        public void UnregisterBotSpawningProxy(MyBotSpawningEnvironmentProxy proxy)
        {
            m_activeBotSpawningProxies.Remove(proxy);
        }

        public bool IsHumanPlayerWithinRange(Vector3 position)
        {
            var players = Sync.Players.GetOnlinePlayers();

            foreach (var player in players)
            {
                if (player.Id.SerialId == 0)
                {
                    if (player.Controller.ControlledEntity == null) continue;
                    var pos = player.GetPosition();
                    if (Vector3.DistanceSquared(pos, position) < BOT_SPAWN_RANGE_MIN_SQ)
                        return false;
                }
            }
            return true;
        }
    }
}
