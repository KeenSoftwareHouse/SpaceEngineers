using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public enum MyChannelEnum : byte
    {
        Default = 0,
        ControlMessage,
        Chat,
        Replication,
        StateDataSync,

        Mods = 255,
    }
}
