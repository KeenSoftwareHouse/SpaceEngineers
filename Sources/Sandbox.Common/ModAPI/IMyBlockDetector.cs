using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyBlockDetector
    {
        List<Sandbox.ModAPI.Ingame.IMyCubeBlock> DetectedBlocks { get; }
    }
}
