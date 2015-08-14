using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.GameSystems
{
    public interface IMyOxygenConsumer : IMyOxygenBlock
    {
        float ConsumptionNeed(float deltaTime);
        void Consume(float amount);

        // The lower the number, the higher the priority
        int GetPriority();
    }
}
