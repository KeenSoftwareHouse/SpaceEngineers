using System;
using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.Game.ModAPI.Ingame
{
    /// <summary>
    /// AirVent block for pressurizing and depresurizing rooms
    /// </summary>
    public interface IMyAirVent : IMyFunctionalBlock
    {
        /// <summary>
        /// Room can be pressurized
        /// </summary>
        /// <returns>true if containing room is airtight</returns>
        [Obsolete("IsPressurized() is deprecated, please use CanPressurize instead.")]
        bool IsPressurized();

        /// <summary>
        /// Can fill room with air 
        /// true - room is airtight
        /// false - room is not airtight
        /// </summary>
        bool CanPressurize { get; }

        /// <summary>
        /// Oxygen level in room
        /// </summary>
        /// <returns>Oxygen fill level as decimal (0.5 = 50%)</returns>
        float GetOxygenLevel();

        /// <summary>
        /// Vent mode
        /// false - pressurize (filling room)
        /// true - depressurize (sucking air out)
        /// </summary>
        bool IsDepressurizing { get; }
    }
}
