using Sandbox.Definitions;
using Sandbox.Game.AI;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Components;
using VRage.Library.Utils;

namespace Sandbox.Game.WorldEnvironment.Modules
{
    public class MyBotSpawningEnvironmentProxy : IMyEnvironmentModuleProxy
    {
        private MyEnvironmentSector m_sector;
        protected readonly MyRandom m_random = new MyRandom();

        // this is here for debug stuff for now
        public long SectorId
        {
            get { return m_sector.SectorId; }
        }

        protected List<int> m_items;
        protected Queue<int> m_spawnQueue;

        public void Init(MyEnvironmentSector sector, List<int> items)
        {
            m_sector = sector;
            m_items = items;
            m_spawnQueue = new Queue<int>();

            foreach (var item in m_items)
                m_spawnQueue.Enqueue(item);
        }

        public void Close()
        {
            m_spawnQueue.Clear();
        }

        public void CommitLodChange(int lodBefore, int lodAfter)
        {
            if (lodAfter == 0)
                MyEnvironmentBotSpawningSystem.Static.RegisterBotSpawningProxy(this);
            else
                MyEnvironmentBotSpawningSystem.Static.UnregisterBotSpawningProxy(this);
        }

        public void CommitPhysicsChange(bool enabled)
        {
        }

        public void OnItemChange(int index, short newModel)
        {
        }

        public void OnItemChangeBatch(List<int> items, int offset, short newModel)
        {
        }

        public void HandleSyncEvent(int item, object data, bool fromClient)
        {
        }

        public void DebugDraw()
        {}

        public bool OnSpawnTick()
        {
            if (m_spawnQueue.Count == 0 || MyAIComponent.Static.GetAvailableUncontrolledBotsCount() < 1)
                return false;

            var count = m_spawnQueue.Count;
            int attempt = 0;

            while (attempt < count)
            {
                attempt++;
                var index = m_spawnQueue.Dequeue();
                m_spawnQueue.Enqueue(index);

                if (m_sector.DataView.Items.Count < index)
                    continue;

                var envItem = m_sector.DataView.Items[index];
                var position = m_sector.SectorCenter + envItem.Position;
                if (MyEnvironmentBotSpawningSystem.Static.IsHumanPlayerWithinRange(position))
                {
                    MyRuntimeEnvironmentItemInfo it;
                    m_sector.Owner.GetDefinition((ushort)envItem.DefinitionIndex, out it);
                    var definitionId = new MyDefinitionId(typeof(MyObjectBuilder_BotCollectionDefinition), it.Subtype);
                    var definition = MyDefinitionManager.Static.GetDefinition<MyBotCollectionDefinition>(definitionId);
                    Debug.Assert(definition != null, "Definition not found!");

                    using (m_random.PushSeed(index.GetHashCode()))
                    {
                        var botDefinitionId = definition.Bots.Sample(m_random);
                        //var botDefinition = MyDefinitionManager.Static.GetDefinition<MyBotDefinition>(botDefinitionId) as MyAgentDefinition;
                        var botDefinition = MyDefinitionManager.Static.GetBotDefinition(botDefinitionId) as MyAgentDefinition;
                        MyAIComponent.Static.SpawnNewBot(botDefinition, envItem.Position + m_sector.SectorCenter, false);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
