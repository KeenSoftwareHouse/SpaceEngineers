using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyReactor : IMyFunctionalBlock
    {
        bool UseConveyorSystem { get; }

        /// <summary>
        /// Current output of solar panel in kW
        /// </summary>
        float CurrentOutput { get; }

        /// <summary>
        /// Maximum output of solar panel in kW
        /// </summary>
        float MaxOutput { get; }
    }
}
