﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyBatteryBlock : IMyFunctionalBlock
    {
        bool HasCapacityRemaining { get; }

        float CurrentStoredPower { get; }
        float MaxStoredPower { get; }
    }
}
