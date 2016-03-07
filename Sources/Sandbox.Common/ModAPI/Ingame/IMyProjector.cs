using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyProjector : IMyFunctionalBlock
    {
        int ProjectionOffsetX { get; }
        int ProjectionOffsetY { get; }
        int ProjectionOffsetZ { get; }

        int ProjectionRotX { get; }
        int ProjectionRotY { get; }
        int ProjectionRotZ { get; }

        int RemainingBlocks { get; }

        bool LoadRandomBlueprint(string searchPattern);
        bool LoadBlueprint(string name);
    }
}
