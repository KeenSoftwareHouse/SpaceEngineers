using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.GameSystems
{
    interface IMyGasProducer : IMyGasBlock
    {
        float ProductionCapacity(float deltaTime);
        void Produce(float amount);

        // The lower the number, the higher the priority
        int GetPriority();
    }
}
