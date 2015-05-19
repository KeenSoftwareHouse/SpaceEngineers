using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI
{
    public class MySandboxBotFactory : IMyBotFactory
    {
        public IMyBot CreateBot(MyPlayer player, MyObjectBuilder_Bot botBuilder, MyBotDefinition botDefinition)
        {
            var output = new MySandboxBot(player, botDefinition);
            if (botBuilder != null)
                output.Init(botBuilder);
            return output;
        }

        public MyAbstractBotActionProxy GetActionsByBehaviorName(IMyBot bot, string name)
        {
            throw new NotImplementedException();
        }

        public bool CanCreateBotOfType(string behaviorType, bool load)
        {
            return false;
        }

        public int MaximumUncontrolledBotCount 
        { 
            get 
            {
                return 0;
            } 
        }

        public int MaximumBotPerPlayer { get { return 0; } }

        public bool GetBotSpawnPosition(string behaviorType, out VRageMath.Vector3D spawnPosition)
        {
            throw new NotImplementedException();
        }
    }
}
