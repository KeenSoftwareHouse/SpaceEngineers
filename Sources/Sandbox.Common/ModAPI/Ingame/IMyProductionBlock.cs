using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyProductionBlock:IMyFunctionalBlock
    {
        bool IsProducing { get; }
        bool IsQueueEmpty { get; }
        void MoveQueueItemRequest(uint queueItemId, int targetIdx);
        uint NextItemId { get; }
        //event Action<IMyProductionBlock> QueueChanged;
        bool UseConveyorSystem { get; }
    }
}
