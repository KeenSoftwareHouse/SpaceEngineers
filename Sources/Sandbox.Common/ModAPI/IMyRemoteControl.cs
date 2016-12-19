using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyRemoteControl : IMyShipController, Ingame.IMyRemoteControl
    {
        Vector3D GetFreeDestination(Vector3D originalDestination, float checkRadius, float shipRadius);
    }
}
