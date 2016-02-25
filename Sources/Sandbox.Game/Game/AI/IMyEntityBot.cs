using Sandbox.Definitions;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.AI
{
    public interface IMyEntityBot : IMyBot
    {
        MyEntity BotEntity { get; }
        void Spawn(Vector3D? spawnPosition, bool spawnedByPlayer);

        // This is a hack!
        bool ShouldFollowPlayer { get; set; }
    }
}
