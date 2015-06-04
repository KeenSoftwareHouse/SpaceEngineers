﻿using System;
using Sandbox.ModAPI.Ingame;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// The interface for the grid program provides extra access for the game and for mods. See <see cref="MyGridProgram"/> for the class the scripts
    /// actually derive from.
    /// </summary>
    public interface IMyGridProgram
    {
        /// <summary>
        /// Gets or sets the GridTerminalSystem available for the grid programs.
        /// </summary>
        IMyGridTerminalSystem GridTerminalSystem { get; set; }

        /// <summary>
        /// Gets or sets the programmable block which is currently running this grid program.
        /// </summary>
        IMyProgrammableBlock Me { get; set; }

        /// <summary>
        /// Gets or sets the amount of time elapsed since the last time this grid program was run.
        /// </summary>
        TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// Gets or sets the storage string for this grid program.
        /// </summary>
        string Storage { get; set; }

        /// <summary>
        /// Gets or sets the action which prints out text onto the currently running programmable block's detail info area.
        /// </summary>
        Action<string> Echo { get; set; }

        /// <summary>
        /// Determines whether this grid program has a valid Main method.
        /// </summary>
        bool HasMainMethod { get; }

        /// <summary>
        /// Invokes this grid program.
        /// </summary>
        /// <param name="argument"></param>
        void Main(string argument);
    }
}