using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyProgrammableBlock : IMyFunctionalBlock
    {
        bool IsRunning { get; }
        
        /// <summary>
        /// Retrieves the argument currently set to be included when the Run button is clicked in the control panel.
        /// </summary>
        /// <remarks>This property should never be used for any other purpose than for the run button.</remarks>
        string TerminalRunArgument { get; }
        
        /// <summary>
        /// Determines whether the <see cref="TerminalRunArgument"/> will be cleared once the program has been run.
        /// </summary>
        bool ClearArgumentOnRun { get; }
    }
}
