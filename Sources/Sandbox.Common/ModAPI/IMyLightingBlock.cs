using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.ModAPI
{
    public interface IMyLightingBlock : IMyFunctionalBlock, Ingame.IMyLightingBlock
    {
    }
}
