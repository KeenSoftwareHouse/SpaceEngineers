using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    [Flags]
    public enum MyInvocationFlags
    {
        None = 0,
        Invoke = 0x1,
        Validate = 0x2,
    }
}
