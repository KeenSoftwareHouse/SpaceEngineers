using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_BlockBlueprintDefinition))]
    public class MyBlockBlueprintDefinition: MyBlueprintDefinition
    {

        protected override void Init(MyObjectBuilder_DefinitionBase ob)
        {
            base.Init(ob);

            var builder = ob as MyObjectBuilder_BlockBlueprintDefinition;
        }

        public override void Postprocess()
        {
            Atomic = false;
            float mass = 0.0f;

            foreach (var result in Results)
            {
                MyCubeBlockDefinition resultBlock;
                MyDefinitionManager.Static.TryGetCubeBlockDefinition(result.Id, out resultBlock);
                if (resultBlock == null) return;

                mass += (float)result.Amount * resultBlock.Mass;
            }

            OutputVolume = mass;            
            PostprocessNeeded = false;
        }
    }
}
