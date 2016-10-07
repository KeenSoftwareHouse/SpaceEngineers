using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMySensorBlock : IMyFunctionalBlock, ModAPI.Ingame.IMySensorBlock
    {
        event Action<bool> StateChanged;
    }
}
