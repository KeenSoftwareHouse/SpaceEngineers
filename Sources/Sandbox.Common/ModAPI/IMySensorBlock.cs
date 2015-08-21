﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMySensorBlock : ModAPI.Ingame.IMySensorBlock, IMyBlockDetector
    {
        event Action<bool> StateChanged;
    }
}
