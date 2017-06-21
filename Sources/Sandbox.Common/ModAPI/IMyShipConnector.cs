using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.ModAPI
{
    public interface IMyShipConnector : IMyFunctionalBlock, Ingame.IMyShipConnector
    {
        IMyShipConnector OtherConnector { get; }
    }
}
