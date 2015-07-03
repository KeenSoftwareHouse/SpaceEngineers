
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.World;


namespace Sandbox.Game.Gui
{
    public interface ITerminalControl
    {
        string Id { get; }

        /// <summary>
        /// If control supports multiple blocks
        /// The only control which does not is Name editor control
        /// </summary>
        bool SupportsMultipleBlocks { get; }

        /// <summary>
        /// Returns control to show in terminal.
        /// When control does not exists yet, it creates it
        /// </summary>
        MyGuiControlBase GetGuiControl();

        /// <summary>
        /// Sets blocks which are controlled now
        /// </summary>
        MyTerminalBlock[] TargetBlocks { get; set; }

        /// <summary>
        /// Updates gui controls
        /// </summary>
        void UpdateVisual();

        /// <summary>
        /// Returns true when control is visible for given block
        /// </summary>
        bool IsVisible(MyTerminalBlock block);

        /// <summary>
        /// Returns terminal actions
        /// </summary>
        ITerminalAction[] Actions { get; }
    }
}
