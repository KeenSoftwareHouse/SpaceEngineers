using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Upgrade type
    /// </summary>
    public enum MyUpgradeModifierType
    {
        /// <summary>
        /// Base value is multiplied by upgrade value
        /// </summary>
        Multiplicative,
        /// <summary>
        /// Upgrade value is added to base value
        /// </summary>
        Additive,
    }

    /// <summary>
    /// Upgrade information
    /// </summary>
    public interface IMyUpgradeInfo
    {
        /// <summary>
        /// Type of upgrade (efficiency/speed/etc.)
        /// </summary>
        string UpgradeType { get; }
        /// <summary>
        /// Modifier value as float (2 means double on addtive, 1 means +100% on aditive)
        /// </summary>
        float Modifier { get; }
        /// <summary>
        /// Modifier type
        /// </summary>
        MyUpgradeModifierType ModifierType { get; }
    }
}
