using System;
using VRage.Game;

namespace Sandbox.ModAPI
{
    public interface IMyProjector : Sandbox.ModAPI.Ingame.IMyProjector, Sandbox.ModAPI.IMyFunctionalBlock
    {
        MyObjectBuilder_CubeGrid LoadedBlueprint { get; }
    }
}
