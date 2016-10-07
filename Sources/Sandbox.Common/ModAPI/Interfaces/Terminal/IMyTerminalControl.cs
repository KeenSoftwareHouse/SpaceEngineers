using System;
using System.Text;
using System.Collections.Generic;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is the base terminal control interface.  All controls implement this.
    /// </summary>
    public interface IMyTerminalControl
    {
        /// <summary>
        /// Identifier of control
        /// </summary>
        string Id { get; }
        /// <summary>
        /// Allows you to set if the control is enabled
        /// </summary>
        Func<IMyTerminalBlock, bool> Enabled { set; }
        /// <summary>
        /// Allows you to set if the control is visible
        /// </summary>
        Func<IMyTerminalBlock, bool> Visible { set; }
        /// <summary>
        /// Allows you to set if the control is visible when the block is selected as a group
        /// </summary>
        bool SupportsMultipleBlocks { get; set; }
        /// <summary>
        /// Recreates the control GUI.  This allows you to update the Title of some controls.
        /// </summary>
        void RedrawControl();
        /// <summary>
        /// This updates a control that is currently displayed, allowing you to refresh it's state
        /// </summary>
        void UpdateVisual();
    }
}
