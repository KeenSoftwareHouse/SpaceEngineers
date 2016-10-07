using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Interfaces
{
    public delegate void LockModeChangedHandler(IMyLandingGear gear, LandingGearMode oldMode);

    public interface IMyLandingGear
    {
        bool AutoLock { get; }
        LandingGearMode LockMode { get; }

        event LockModeChangedHandler LockModeChanged;

        void RequestLock(bool enable);
        void ResetAutolock();
        IMyEntity GetAttachedEntity();
    }
}
