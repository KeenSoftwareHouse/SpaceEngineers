using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities
{
    public interface IMyVirtualMass : Sandbox.ModAPI.Ingame.IMyVirtualMass
    {
        float VirtualMass { get; }
    }
}
