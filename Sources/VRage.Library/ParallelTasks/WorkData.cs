using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelTasks
{
    public class WorkData
    {
        public enum WorkStateEnum
        {
            NOT_STARTED = 0,
            RUNNING,
            SUCCEEDED,
            FAILED,
        }

        public WorkStateEnum WorkState { get; internal set; }

        public WorkData()
        {
            WorkState = WorkStateEnum.NOT_STARTED;
        }

        public void FlagAsFailed()
        {
            WorkState = WorkStateEnum.FAILED;
        }
    }
}
