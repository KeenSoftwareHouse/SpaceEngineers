﻿using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
﻿using VRage.Game;
﻿using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_BlueprintDefinition))]
    public class MyBlueprintDefinition : MyBlueprintDefinitionBase
    {
        protected override void Init(MyObjectBuilder_DefinitionBase ob)
        {
            base.Init(ob);

            MyObjectBuilder_BlueprintDefinition builder = (MyObjectBuilder_BlueprintDefinition)ob;

            Prerequisites = new Item[builder.Prerequisites.Length];
            for (int i = 0; i < Prerequisites.Length; ++i)
            {
                Prerequisites[i] = Item.FromObjectBuilder(builder.Prerequisites[i]);
            }
            if (builder.Result != null)
            {
                Results = new Item[1];
                Results[0] = Item.FromObjectBuilder(builder.Result);
            }
            else
            {
                Results = new Item[builder.Results.Length];
                for (int i = 0; i < Results.Length; ++i)
                {
                    Results[i] = Item.FromObjectBuilder(builder.Results[i]);
                }
            }
            BaseProductionTimeInSeconds = builder.BaseProductionTimeInSeconds;
            PostprocessNeeded = true;
            ProgressBarSoundCue = builder.ProgressBarSoundCue;
        }

        public override void Postprocess()
        {
            bool atomic = false;
            float volume = 0.0f;
            foreach (var result in Results)
            {
                if (result.Id.TypeId != typeof(MyObjectBuilder_Ore) &&
                    result.Id.TypeId != typeof(MyObjectBuilder_Ingot))
                    atomic = true;

                MyPhysicalItemDefinition resultItem;
                MyDefinitionManager.Static.TryGetPhysicalItemDefinition(result.Id, out resultItem);
                if (resultItem == null) return;

                volume += (float)result.Amount * resultItem.Volume;
            }

            Atomic = atomic;
            OutputVolume = volume;
            PostprocessNeeded = false;
        }

        public override int GetBlueprints(List<MyBlueprintDefinitionBase.ProductionInfo> blueprints)
        {
            blueprints.Add(new ProductionInfo() { Blueprint = this, Amount = 1 });
            return 1;
        }
    }
}