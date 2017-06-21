using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities
{
    public enum MyTrashRemovalFlags : int
    {
        None = 0,
        Default = WithBlockCount | DistanceFromPlayer,

        Fixed          = 1,
        Stationary     = 2,
        Linear         = 8,
        Accelerating   = 16,
        Powered        = 32,
        Controlled     = 64,
        WithProduction = 128,
        WithMedBay     = 256,
        WithBlockCount = 512,
        DistanceFromPlayer  = 1024,
        }

    public enum MyTrashRemovalOperation
    {
        None = 0,
        Remove = 1,
        Stop = 2,
        Depower = 4,
    }

    public struct MyTrashRemovalSettings
    {
        public static readonly MyTrashRemovalSettings Default = new MyTrashRemovalSettings()
        {
            Flags = MyTrashRemovalFlags.Default,
            BlockCountThreshold = 20,
            PlayerDistanceThreshold = 100
        };
        public static readonly MyTrashRemovalSettings DevilishlyAggresive = new MyTrashRemovalSettings()
        {
            Flags = MyTrashRemovalFlags.WithBlockCount | MyTrashRemovalFlags.DistanceFromPlayer
                    | MyTrashRemovalFlags.Stationary | MyTrashRemovalFlags.Linear,
            BlockCountThreshold = 40,
            PlayerDistanceThreshold = 3
        };

        public MyTrashRemovalFlags Flags;
        public int BlockCountThreshold;
        public float PlayerDistanceThreshold;

        public bool HasFlag(MyTrashRemovalFlags flag)
        {
            return (Flags & flag) == flag;
        }
    }
}
