using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Interfaces
{
    delegate void LockModeChangedHandler(IMyLandingGear gear, LandingGearMode oldMode);

    public enum LandingGearMode
    {
        Unlocked = 0,
        ReadyToLock = 1,
        Locked = 2,
    }

    interface IMyLandingGear
    {
        LandingGearMode LockMode { get; }

        event LockModeChangedHandler LockModeChanged;

        void RequestLock(bool enable);

        bool AutoLock { get; }

        void ResetAutolock();
    }
}
