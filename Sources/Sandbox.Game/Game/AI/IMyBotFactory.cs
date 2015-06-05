using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI
{
    public interface IMyBotFactory
    {
        int MaximumUncontrolledBotCount { get; }
        int MaximumBotPerPlayer { get; }
        bool CanCreateBotOfType(string behaviorType, bool load);
        IMyBot CreateBot(MyPlayer player, MyObjectBuilder_Bot botBuilder, MyBotDefinition botDefinition);
        bool GetBotSpawnPosition(string behaviorType, out Vector3D spawnPosition);
    }
}
