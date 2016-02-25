using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using System;
using VRage.Common;
using VRage.Common.Utils;
using VRageMath;

// TODO: rename this file to MyFakesLocal.cs to enable Brain Simulator integration (and never commit it!)

namespace Sandbox.Engine.Utils
{
    public static class MyFakesLocal
    {
        static MyFakesLocal()
        {
            // NOTE: Set your fakes here. Never commit this!
            MyFakes.ENABLE_BRAIN_SIMULATOR = true;
        }

        public static void SetupLocalPerGameSettings()
        {
            // NOTE: Change per game settings in this method. It will override the game's per game settings. Never commit this!
        }
    }
}
