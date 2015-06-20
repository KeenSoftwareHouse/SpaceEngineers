using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// Interface for accessing oxygen bottle properties
    /// </summary>
    public interface IMyOxygenBottle : Ingame.IMyOxygenBottle
    {
        /* SET is not safe atm. not implementing
        /// <summary>
        /// Current oxygen level
        /// </summary>
        float OxygenLevel { get; set; }
        */
    }
}
