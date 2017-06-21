using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Game.GUI.TextPanel
{
    [Flags]
    public enum TextPanelAccessFlag : byte
    {
        NONE = 0,
        READ_FACTION = (1 << 1),
        WRITE_FACTION = (1 << 2),
        READ_AND_WRITE_FACTION = (READ_FACTION | WRITE_FACTION),
        READ_ENEMY = (1 << 3),
        WRITE_ENEMY = (1 << 4),
        READ_ALL = READ_ENEMY | READ_FACTION,
        WRITE_ALL = WRITE_ENEMY | WRITE_FACTION,
        READ_AND_WRITE_ALL = (READ_ALL | WRITE_ALL),
    }

    [Flags]
    public enum ShowTextOnScreenFlag : byte
    {
        NONE = 0,
        PUBLIC = (1 << 1),
        PRIVATE = (1 << 2),
    }
}
