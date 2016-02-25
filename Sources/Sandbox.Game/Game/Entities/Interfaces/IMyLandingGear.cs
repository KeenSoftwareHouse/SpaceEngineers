using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Interfaces
{
    delegate void LockModeChangedHandler(IMyLandingGear gear, LandingGearMode oldMode);

    interface IMyLandingGear
    {
        bool AutoLock { get; }
        LandingGearMode LockMode { get; }

        event LockModeChangedHandler LockModeChanged;

        void RequestLock(bool enable);
        void ResetAutolock();
    }
}
