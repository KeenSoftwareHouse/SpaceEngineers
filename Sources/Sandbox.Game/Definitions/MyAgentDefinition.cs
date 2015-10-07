using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AgentDefinition))]
    public class MyAgentDefinition : MyBotDefinition
    {
        public string BotModel;
        public bool InventoryContentGenerated = false;
        public MyDefinitionId InventoryContainerTypeId;

        public bool RemoveAfterDeath;
        public int RespawnTimeMs;
        public int RemoveTimeMs;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_AgentDefinition;
            this.BotModel = ob.BotModel;

            InventoryContentGenerated = ob.InventoryContentGenerated;
            if (ob.InventoryContainerTypeId.HasValue)
            {
                InventoryContainerTypeId = ob.InventoryContainerTypeId.Value;
            }

            RemoveAfterDeath = ob.RemoveAfterDeath;
            RespawnTimeMs = ob.RespawnTimeMs;
            RemoveTimeMs = ob.RemoveTimeMs;
        }
    }
}
