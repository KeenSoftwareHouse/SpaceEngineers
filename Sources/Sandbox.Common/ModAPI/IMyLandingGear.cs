﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyLandingGear : ModAPI.Ingame.IMyLandingGear, IMyBlockDetector
    {
        event Action<bool> StateChanged;
    }
}
