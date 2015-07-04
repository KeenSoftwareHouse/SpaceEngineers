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

        /// <summary>
        /// Often a consumer may share a space (which it adds oxygen to) with other producers/consumers.
        /// The common example would be multiple air vents connected to the same room. By returning an
        /// object representing this space, the consumer allows detection and load balancing with other
        /// consumers (who return the same object) sharing the same space.
        /// </summary>
        /// <returns>Space this consumer may share with other producers/consumers, or null if none</returns>
        IMyOxygenSharedSpace GetSharedSpace();
    }
}
