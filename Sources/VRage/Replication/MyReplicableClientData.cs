using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;

namespace VRage.Replication
{
    public class MyReplicableClientData
    {
        public MyTimeSpan CreationTime;
        public MyTimeSpan SleepTime;

        public bool IsPending = true;
        public bool IsStreaming = false;

        public float Priority;

        /// <summary>
        /// When replicable is sleeping, it should not receive state updates. But it has to receive events.
        /// </summary>
        public bool IsSleeping
        {
            get { return SleepTime != MyTimeSpan.Zero; }
        }

        /// <summary>
        /// Returns true when replicable is not pending and is not sleeping.
        /// </summary>
        public bool HasActiveStateSync
        {
            get { return !IsSleeping && !IsPending; }
        }

        public void UpdateSleep(bool isRelevant, MyTimeSpan currentTime)
        {
            if (isRelevant)
                SleepTime = MyTimeSpan.Zero;
            else if (!IsSleeping)
                SleepTime = currentTime;
        }

        public bool ShouldRemove(MyTimeSpan currentTime, MyTimeSpan maxSleep)
        {
            return IsSleeping && (currentTime - SleepTime) > maxSleep;
        }
    }
}
