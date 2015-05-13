using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMySensorBlock : IMyFunctionalBlock
    {
        /// <summary>
        /// Param - active
        /// </summary>
        event Action<bool> StateChanged;
    }
}
