using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace Sandbox.Game.Gui
{
    public interface ITerminalControlSync
    {
        /// <summary>
        /// (De)serializes block data.
        /// </summary>
        void Serialize(BitStream stream, MyTerminalBlock block);
    }
}
