using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Game.WorldEnvironment.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_BotCollectionDefinition))]
    public class MyBotCollectionDefinition: MyDefinitionBase
    {
        public MyDiscreteSampler<MyDefinitionId> Bots;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_BotCollectionDefinition;

            if (ob == null)
                return;

            MyDebug.AssertDebug(ob.Bots != null);

            List<MyDefinitionId> bots = new List<MyDefinitionId>();
            List<float> probabilities = new List<float>();
            for (int i = 0; i < ob.Bots.Length; i++)
            {
                var bot = ob.Bots[i];
                bots.Add(bot.Id);
                probabilities.Add(bot.Probability);
            }

            Bots = new MyDiscreteSampler<MyDefinitionId>(bots, probabilities);
        }
    }
}
