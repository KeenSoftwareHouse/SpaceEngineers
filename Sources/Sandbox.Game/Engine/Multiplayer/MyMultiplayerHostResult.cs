using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SteamSDK;
using System.Threading;

namespace Sandbox.Engine.Multiplayer
{
    public class MyMultiplayerHostResult
    {
        public event Action<Result, MyMultiplayerBase> Done;

        private bool m_done = false;
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
            m_done = true;
        }

        public void Wait(bool runCallbacks = true)
        {
            while(!Cancelled && !m_done)
            {
                if (runCallbacks)
                {
                    SteamAPI.Instance.RunCallbacks();
                }
                Thread.Sleep(10);
            }
        }
    }
}
