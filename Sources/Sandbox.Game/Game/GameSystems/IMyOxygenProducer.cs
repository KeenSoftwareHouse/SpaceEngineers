using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.GameSystems
{
    interface IMyOxygenProducer : IMyOxygenBlock
    {
        float ProductionCapacity(float deltaTime);
        void Produce(float amount);

        // The lower the number, the higher the priority
        int GetPriority();
    }
}
