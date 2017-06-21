using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.GameSystems.ContextHandling
{
    /// <summary>
    /// Defines interface for handling and releasing game focus.
    /// </summary>
    public interface IMyFocusHolder
    {
        /// <summary>
        /// Called when lost game focus.
        /// </summary>
        void OnLostFocus();

    }
}
