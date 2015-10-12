using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyProgrammableBlock : IMyFunctionalBlock
    {
        bool IsRunning { get; }
        string TerminalRunArgument { get; set; }
    }
}
