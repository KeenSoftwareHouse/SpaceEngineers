using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// Ship welder interface
    /// </summary>
    public interface IMyShipWelder : IMyShipToolBase, Ingame.IMyShipWelder
    {
    }
}
