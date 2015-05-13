using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyShipMergeBlock : IMyFunctionalBlock
    {
        event Action BeforeMerge;
    }
}
