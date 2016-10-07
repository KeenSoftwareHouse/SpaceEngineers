using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.ModAPI
{
    public interface IMyRefinery : IMyProductionBlock, Ingame.IMyRefinery
    {
    }
}
