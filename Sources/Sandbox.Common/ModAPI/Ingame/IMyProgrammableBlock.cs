using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyProgrammableBlock : IMyFunctionalBlock
    {
        bool IsRunning { get; }
        string TerminalRunArgument { get; }
        
        /// <summary>
        /// Runs the program in this programmable block immediately. This call is ignored if the program is already running!
        /// </summary>
        /// <param name="argument"></param>
        void Run(string argument = null);
        
        /// <summary>
        /// Enqueues a run for the next available time slot. Each slot takes up a single tick for a programmable block, 
        /// so if another program has enqueued a run here before you, there will be two ticks before yours are run.
        /// </summary>
        /// <param name="argument"></param>
        /// <returns><c>true</c> if the run could be enqueued; <c>false</c> otherwise.</returns>
        bool EnqueueRun(string argument = null);

        /// <summary>
        /// Returns how runs is currently enqueued. You can use this in conjunction with <see cref="MaxRunQueueCount"/> to determine if a run can be enqueued.
        /// </summary>
        int CurrentRunQueueCount { get; }
        
        /// <summary>
        /// Returns the maximum number of runs allowed to be enqueued.
        /// </summary>
        int MaxRunQueueCount { get; }
    }
}
