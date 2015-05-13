using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SteamSDK;

namespace Sandbox.Engine.Multiplayer
{
    public class MyMultiplayerHostResult
    {
        public event Action<Result, MyMultiplayerBase> Done;

        public bool Cancelled { get; private set; }

        public void Cancel()
        {
            Cancelled = true;
        }

        public void RaiseDone(Result result, MyMultiplayerBase multiplayer)
        {
            Debug.Assert(!Cancelled, "Action is canceled, it should not raise event");
            var handler = Done;
            if (handler != null) handler(result, multiplayer);
        }
    }
}
