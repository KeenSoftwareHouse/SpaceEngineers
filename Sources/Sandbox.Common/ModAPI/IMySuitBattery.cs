using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMySuitBattery
    {
        IMyCharacter Owner { get; }

        bool Enabled { get; }

        float CurrentPowerOutput { get; }

        float MaxPowerOutput { get; }

        bool HasCapacityRemaining { get; }

        /// <summary>
        /// Battery percent
        /// </summary>
        float RemainingCapacity { get; }

        /// <summary>
        /// Set the battery's current percentage.
        /// This is automatically synchronized.
        /// </summary>
        /// <param name="level">from 0.0 to 1.0</param>
        void SetRemainingCapacity(float level);

        /// <summary>
        /// Current charging current applied to the battery
        /// </summary>
        float CurrentInput { get; }

        /// <summary>
        /// Required input in [MW]
        /// </summary>
        float RequiredInput { get; }

        /// <summary>
        /// Theoretical maximum of required input. This can be different from RequiredInput, but
        /// it has to be >= RequiredInput. It is used to check whether current power supply can meet
        /// demand under stress.
        /// </summary>
        float MaxRequiredInput { get; }

        float InputSuppliedRatio { get; }

        bool InputIsPowered { get; }

        /// <summary>
        /// Adaptible consumers can work on less than their required input,
        /// but they will be less effective.
        /// </summary>
        bool InputIsAdaptible { get; }
    }
}
