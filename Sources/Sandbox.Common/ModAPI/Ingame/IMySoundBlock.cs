using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMySoundBlock : IMyFunctionalBlock
    {
        float Volume { get; }
        float Range { get; }
        bool IsSoundSelected{ get; }
        float LoopPeriod { get; }
    }
}
