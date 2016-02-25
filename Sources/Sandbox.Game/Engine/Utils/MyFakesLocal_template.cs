using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using System;
using VRage.Common;
using VRage.Common.Utils;
using VRageMath;

// TODO: rename this file to MyFakesLocal.cs (and never commit it!)

namespace Sandbox.Engine.Utils
{
    public static class MyFakesLocal
    {
        static MyFakesLocal()
        {
            // Never commit this!
            // TODO: Set your fakes here in the form "MyFakes.<FLAG> = true;"
            // You can find the list of available flags in MyFakes.cs
            //
            // MyFakes.ENABLE_BRAIN_SIMULATOR = true;  // example
        }

        public static void SetupLocalPerGameSettings()
        {
            // NOTE: Change per game settings in this method. It will override the game's per game settings. Never commit this!
        }
    }
}

