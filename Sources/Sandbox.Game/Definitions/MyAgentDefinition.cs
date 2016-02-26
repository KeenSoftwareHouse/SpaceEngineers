using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AgentDefinition))]
    public class MyAgentDefinition : MyBotDefinition
    {
        public string BotModel;
        public string TargetType;
        public bool InventoryContentGenerated = false;
        public MyDefinitionId InventoryContainerTypeId;

        public bool RemoveAfterDeath;
        public int RespawnTimeMs;
        public int RemoveTimeMs;

        public string FactionTag;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_AgentDefinition;
            this.BotModel = ob.BotModel;

            this.TargetType = ob.TargetType;

            InventoryContentGenerated = ob.InventoryContentGenerated;
            if (ob.InventoryContainerTypeId.HasValue)
            {
                InventoryContainerTypeId = ob.InventoryContainerTypeId.Value;
            }

            RemoveAfterDeath = ob.RemoveAfterDeath;
            RespawnTimeMs = ob.RespawnTimeMs;
            RemoveTimeMs = ob.RemoveTimeMs;
            FactionTag = ob.FactionTag;
        }

        public override void AddItems(Sandbox.Game.Entities.Character.MyCharacter character)
        {
            System.Diagnostics.Debug.Assert((character.GetInventory(0) as MyInventory) != null, "Null or unexpected inventory type returned!");
            (character.GetInventory(0) as MyInventory).Clear();

            if (InventoryContentGenerated)
            {
                MyContainerTypeDefinition cargoContainerDefinition = MyDefinitionManager.Static.GetContainerTypeDefinition(InventoryContainerTypeId.SubtypeName);
                if (cargoContainerDefinition != null)
                {
                    (character.GetInventory(0) as MyInventory).GenerateContent(cargoContainerDefinition);
                }
                else
                {
                    System.Diagnostics.Debug.Fail("CargoContainer type definition " + InventoryContainerTypeId + " wasn't found.");
                }
            }
        }

    }
}
