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
        private object m_lockObject = new object();

        private Dictionary<Thread, long> threadedTimestamp = new Dictionary<Thread, long>(); 

        public object Parent { get; private set; }
        internal long Timestamp
        {
            get
            {
                long returnValue = 0;
                lock (m_lockObject)
                {
                    if (!threadedTimestamp.TryGetValue(Thread.CurrentThread, out returnValue))
                        returnValue = 0;
                }
                return returnValue;
            }
            set
            {
                lock (m_lockObject)
                {
                    threadedTimestamp[Thread.CurrentThread] = value;
                }
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
