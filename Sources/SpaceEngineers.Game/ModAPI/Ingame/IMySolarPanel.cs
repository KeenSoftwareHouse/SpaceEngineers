using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.Game.ModAPI.Ingame
{
    public interface IMySolarPanel : IMyTerminalBlock
    {

        /// <summary>
        /// Current output of solar panel in Megawatts
        /// </summary>
        float CurrentOutput { get; }

        /// <summary>
        /// Maximum output of solar panel in Megawatts
        /// </summary>
        float MaxOutput { get; }
    }
}
