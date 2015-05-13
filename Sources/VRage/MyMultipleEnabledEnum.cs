using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage
{
    /// <summary>
    /// Enumeration describing Enabled state of multiple objects.
    /// </summary>
    public enum MyMultipleEnabledEnum : byte
    {
        NoObjects   = 0,
        AllDisabled = 1,
        Mixed       = 2,
        AllEnabled  = 3,
    }
}
