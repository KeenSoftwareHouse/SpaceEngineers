using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using VRage.Collections;

namespace VRage.Algorithms
{
    public class MyPathfindingData : HeapItem<float>
    {
        private Dictionary<Thread, long> threadedTimestamp = new Dictionary<Thread, long>(); 

        public object Parent { get; private set; }
        internal long Timestamp
        {
            get
            {
                if (!threadedTimestamp.ContainsKey(Thread.CurrentThread))
                    return 0;
                return threadedTimestamp[Thread.CurrentThread];
            }
            set
            {
                if (!threadedTimestamp.ContainsKey(Thread.CurrentThread))
                    threadedTimestamp.Add(Thread.CurrentThread, value);
                threadedTimestamp[Thread.CurrentThread] = value;
            }
        }
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
