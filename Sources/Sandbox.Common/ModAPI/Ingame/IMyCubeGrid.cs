using System;
using System.Collections.Generic;
namespace Sandbox.ModAPI.Ingame
{
    public interface IMyCubeGrid
    {     
        float GridSize { get; }
        Sandbox.Common.ObjectBuilders.MyCubeSize GridSizeEnum { get; }
        bool IsStatic { get; }
        VRageMath.Vector3I Max { get; }
        VRageMath.Vector3I Min { get; }
        IMySlimBlock GetCubeBlock(VRageMath.Vector3I pos);
    }
}
