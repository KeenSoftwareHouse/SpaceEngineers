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
        /// Current output of reactor in Megawatts
        /// </summary>
        float CurrentOutput { get; }

        /// <summary>
        /// Maximum output of reactor in Megawatts
        /// </summary>
        float MaxOutput { get; }
    }
}
