using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Provides runtime info for a running grid program.
    /// </summary>
    public interface IMyGridProgramRuntimeInfo
    {
        /// <summary>
        /// Gets the time elapsed since the last time the Main method of this program was run. This property returns no
        /// valid data neither in the constructor nor the Save method.
        /// </summary>
        TimeSpan TimeSinceLastRun { get; }

        /// <summary>
        /// Gets the number of milliseconds it took to execute the Main method the last time it was run. This property returns no valid
        /// data neither in the constructor nor the Save method.
        /// </summary>
        double LastRunTimeMs { get; }

        /// <summary>
        /// Gets the maximum number of significant instructions that can be executing during a single run, including
        /// any other programmable blocks invoked immediately.
        /// </summary>
        int MaxInstructionCount { get; }

        /// <summary>
        /// Gets the current number of significant instructions executed.
        /// </summary>
        int CurrentInstructionCount { get; }

        /// <summary>
        /// Gets the maximum number of method calls that can be executed during a single run, including
        /// any other programmable blocks invoked immediately.
        /// </summary>
        int MaxMethodCallCount { get; }

        /// <summary>
        /// Gets the current number of method calls.
        /// </summary>
        int CurrentMethodCallCount { get; }
    }
}
