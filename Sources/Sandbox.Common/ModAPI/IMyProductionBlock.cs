using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyProductionBlock : ModAPI.Ingame.IMyProductionBlock
    {
        event Action StartedProducing;
        event Action StoppedProducing;
    }
}
