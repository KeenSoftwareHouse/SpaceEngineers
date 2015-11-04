using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public enum IngameScriptAccessibility
    {
        // Ingame scripts have no access to this block
        noAccess = 0,
        // Ingame scripts can get information from this block, but cannot modify it
        readAccess,
        // Ingame scripts have full access to this block
        readWriteAccess
    }
}
