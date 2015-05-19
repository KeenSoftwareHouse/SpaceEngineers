using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyReactor : Sandbox.ModAPI.Ingame.IMyReactor, Sandbox.ModAPI.IMyFunctionalBlock
    {
        float PowerOutputMultiplier { get; set; }
    }
}
