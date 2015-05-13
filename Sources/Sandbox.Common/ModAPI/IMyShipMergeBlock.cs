using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyShipMergeBlock:ModAPI.Ingame.IMyShipMergeBlock
    {
        event Action BeforeMerge;
    }
}
