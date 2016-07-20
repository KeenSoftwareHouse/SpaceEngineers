using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Multiplayer;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PirateAntennaDefinition))]
    public class MyPirateAntennaDefinition : MyDefinitionBase
    {
        public string Name;
        public float SpawnDistance;
        public int SpawnTimeMs;
        public int FirstSpawnTimeMs;
        public int MaxDrones;
        public MyDiscreteSampler<MySpawnGroupDefinition> SpawnGroupSampler = null;

        private List<string> m_spawnGroups;


        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var pirateBuilder = builder as MyObjectBuilder_PirateAntennaDefinition;

            Name = pirateBuilder.Name;
            SpawnDistance = pirateBuilder.SpawnDistance;
            SpawnTimeMs = pirateBuilder.SpawnTimeMs;
            FirstSpawnTimeMs = pirateBuilder.FirstSpawnTimeMs;
            MaxDrones = pirateBuilder.MaxDrones;

            m_spawnGroups = new List<string>();
            foreach (var spawnGroupId in pirateBuilder.SpawnGroups)
            {
                m_spawnGroups.Add(spawnGroupId);
            }
        }

        public void Postprocess()
        {
            List<MySpawnGroupDefinition> spawnGroups = new List<MySpawnGroupDefinition>();
            List<float> frequencies = new List<float>();

            foreach (var spawnGroupId in m_spawnGroups)
            {
                MySpawnGroupDefinition spawnGroupDef = null;
                var defId = new MyDefinitionId(typeof(MyObjectBuilder_SpawnGroupDefinition), spawnGroupId);
                MyDefinitionManager.Static.TryGetDefinition(defId, out spawnGroupDef);

                Debug.Assert(spawnGroupDef != null, "Could not find spawn group for pirate antenna " + Name);
                if (spawnGroupDef != null)
                {
                    spawnGroups.Add(spawnGroupDef);
                    frequencies.Add(spawnGroupDef.Frequency);
                }
            }

            m_spawnGroups = null;

            if (frequencies.Count != 0)
            {
                SpawnGroupSampler = new MyDiscreteSampler<MySpawnGroupDefinition>(spawnGroups, frequencies);
            }
        }
    }
}
