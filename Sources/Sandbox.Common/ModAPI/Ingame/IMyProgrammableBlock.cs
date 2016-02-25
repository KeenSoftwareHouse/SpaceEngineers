using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyProgrammableBlock : IMyFunctionalBlock
    {
        /// <summary>
        /// This programmable block is currently running its program.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Contains the value of the default terminal argument.
        /// </summary>
        string TerminalRunArgument { get; }

        /// <summary>
        /// Attempts to run this programmable block using the given argument. An already running
        /// programmable block cannot be run again.
        /// This is equivalent to running <c>block.ApplyAction("Run", argumentsList);</c>
        /// </summary>
        /// <param name="argument"></param>
        /// <returns><c>true</c> if the action was applied, <c>false</c> otherwise</returns>
        bool TryRun(string argument);
    }
}
