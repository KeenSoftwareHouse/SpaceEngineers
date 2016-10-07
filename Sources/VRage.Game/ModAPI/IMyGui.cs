using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;

namespace VRage.Game.ModAPI
{
    public enum MyTerminalPageEnum
    {
        None = -2,
        Properties = -1,
        Inventory = 0,
        ControlPanel = 1,
        Production = 2,
        Info = 3,
        Factions = 4,
        Gps = 6,
    }

    public interface IMyGui
    {
        /// <summary>
        /// Event triggered on gui control created.
        /// </summary>
        event Action<object> GuiControlCreated;

        /// <summary>
        /// Event triggered on gui control removed.
        /// </summary>
        event Action<object> GuiControlRemoved;

        /// <summary>
        /// Gets the name of the currently open GUI screen.
        /// </summary>
        string ActiveGamePlayScreen { get; }

        /// <summary>
        /// Gets the entity the player is currently interacting with.
        /// </summary>
        IMyEntity InteractedEntity { get; }

        /// <summary>
        /// Gets an enum describing the currently open GUI screen.
        /// </summary>
        MyTerminalPageEnum GetCurrentScreen { get; }

        /// <summary>
        /// Checks if the chat entry box is visible.
        /// </summary>
        bool ChatEntryVisible { get; }
    }
}
