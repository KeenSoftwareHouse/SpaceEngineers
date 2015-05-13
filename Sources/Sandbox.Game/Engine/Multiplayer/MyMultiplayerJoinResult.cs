using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Engine.Multiplayer
{
    public class MyMultiplayerJoinResult
    {
        public event Action<Result, LobbyEnterInfo, MyMultiplayerBase> JoinDone;

        public bool Cancelled { get; private set; }

        public void Cancel()
        {
            Cancelled = true;
        }

        public void RaiseJoined(Result result, LobbyEnterInfo info, MyMultiplayerBase multiplayer)
        {
            Debug.Assert(!Cancelled, "Cancelled action should not raise events");
            var handler = JoinDone;
            if (handler != null) 
                handler(result, info, multiplayer);
        }
    }
}
