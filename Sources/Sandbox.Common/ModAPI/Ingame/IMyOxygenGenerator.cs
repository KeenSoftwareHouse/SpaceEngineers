using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Oxygen generator interface
    /// </summary>
    public interface IMyOxygenGenerator : IMyFunctionalBlock
    {
        /// <summary>
        /// Autorefill enabled
        /// </summary>
        bool AutoRefill { get; }
    }
}
