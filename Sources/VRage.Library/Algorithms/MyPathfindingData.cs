using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace VRage.Algorithms
{
    public class MyPathfindingData : HeapItem<float>
    {
        public object Parent { get; private set; }
        internal long Timestamp;
        internal MyPathfindingData Predecessor;
        internal float PathLength;

        public MyPathfindingData(object parent)
        {
            Parent = parent;
        }

        public long GetTimestamp()
        {
            return Timestamp;
        }
    }
}
