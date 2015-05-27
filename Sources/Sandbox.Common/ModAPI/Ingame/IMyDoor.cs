using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyDoor : IMyFunctionalBlock
    {
        /// <summary>
        /// Indicates whether door is opened or closed. True when door is opened.
        /// </summary>
        bool Open { get; }

        /// <summary>
        /// Door state, zero is fully closed. One is fully opened.
        /// </summary>
        float OpenRatio { get; }
    }
}
