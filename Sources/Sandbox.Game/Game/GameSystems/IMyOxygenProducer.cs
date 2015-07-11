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

        /// <summary>
        /// Often a producer may share a space (which it pulls oxygen from) with other producers/consumers.
        /// The common example would be multiple air vents connected to the same room. By returning an
        /// object representing this space, the producer allows detection of other
        /// producers (who return the same object) sharing the same space, so that the system doesn't try
        /// to extract more oxygen than the space has.
        /// </summary>
        /// <returns>Space this producer may share with other producers/consumers, or null if none</returns>
        IMyOxygenSharedSpace GetSharedSpace();
    }
}
