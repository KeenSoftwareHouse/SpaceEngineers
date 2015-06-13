using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;

namespace Sandbox.ModAPI.Ingame
{
   
    public interface IMyLandingGear : IMyFunctionalBlock
    {
        float BreakForce
        {
            get;
        }

        bool IsLocked { get; }
        IMyEntity GetAttachedEntity();
    }
}
