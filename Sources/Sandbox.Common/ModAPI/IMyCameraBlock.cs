using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Interfaces;

namespace Sandbox.ModAPI
{
    public interface IMyCameraBlock : IMyFunctionalBlock, Ingame.IMyCameraBlock, IMyCameraController
    {
    }
}
