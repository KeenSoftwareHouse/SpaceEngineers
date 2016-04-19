using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyMotorStator : ModAPI.Ingame.IMyMotorStator, ModAPI.IMyMotorBase
    {
        event Action<bool> LimitReached;
    }
}
