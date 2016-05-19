using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is an interface wrapper for terminal actions that appear on a toolbar.  An instance of this interface is created via 
    /// MyAPIGateway.TerminalControls.CreateAction.  Once created, you may modify various fields to control how the action behaves.
    /// </summary>
    public interface IMyTerminalAction : ITerminalAction
    {
        /// <summary>
        /// Allows you to set if this action is enabled or disabled
        /// </summary>
        Func<IMyTerminalBlock, bool> Enabled { set; }
        /// <summary>
        /// Allows you to set which toolbar type this action is invalid for.  Adding to this means this action may not be added to that toolbar type.
        /// </summary>
        List<MyToolbarType> InvalidToolbarTypes { get; set; }
        /// <summary>
        /// Allows you to set if this action is valid in groups
        /// </summary>
        bool ValidForGroups { get; set; }
        /// <summary>
        /// Allows you to set the name of the Action
        /// </summary>
        new StringBuilder Name { get; set; }
        /// <summary>
        /// Allows you to set the Icon of this action.  It's a link to an icon texture.
        /// </summary>
        new string Icon { get; set; }
        /// <summary>
        /// This is the action taken when an action is performed.
        /// </summary>
        Action<IMyTerminalBlock> Action { get; set; }
        /// <summary>
        /// This allows you to set the "Icon Text" of an action (the text that appears under the icon in the toolbar)
        /// </summary>
        Action<IMyTerminalBlock, StringBuilder> Writer { get; set; }
    }

}
