using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyCharacter
    {
        VRageMath.MatrixD HeadMatrix { get; }
        long PlayerId { get; }
    }
}
