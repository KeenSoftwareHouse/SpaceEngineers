using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.Game.ModAPI.Ingame
{
    public interface IMyVirtualMass : IMyFunctionalBlock
    {
        /// <summary>
        /// Virtualmass weight
        /// </summary>
        float VirtualMass { get; }
    }
}
