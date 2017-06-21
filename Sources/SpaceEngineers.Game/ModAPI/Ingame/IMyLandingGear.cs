using Sandbox.ModAPI.Ingame;
using VRage.ModAPI;

namespace SpaceEngineers.Game.ModAPI.Ingame
{
   
    public interface IMyLandingGear : IMyFunctionalBlock
    {
        float BreakForce
        {
            get;
        }

        bool IsLocked { get; }
    }
}
