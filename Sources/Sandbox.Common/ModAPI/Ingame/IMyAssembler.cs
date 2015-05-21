using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Assembler block interface
    /// </summary>
    public interface IMyAssembler : IMyProductionBlock
    {
        /// <summary>
        /// true - assembler in dissasemble mode, false - aseembler in normal mode
        /// </summary>
        bool DisassembleEnabled { get; }

        /// <summary>
        /// Production speed - in decimals (0.5=50%)
        /// </summary>
        float Productivity { get; }
        /// <summary>
        /// Power efficiency - in decimals (0.5=50%)
        /// </summary>
        float PowerEfficiency { get; }
    }
}
