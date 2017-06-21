using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRageMath;

namespace VRage.Network
{
    public class MyStateDataEntry
    {
        public long LastSyncedFrame;
        public float Priority;

        public readonly NetworkId GroupId;
        public readonly IMyStateGroup Group;

        public MyStateDataEntry(NetworkId groupId, IMyStateGroup group)
        {
            Priority = 0;
            GroupId = groupId;
            Group = group;
        }

        public override string ToString()
        {
            return string.Format("{0:0.00}, {1}, {2}", Priority, GroupId, Group);
        }
    }
}
