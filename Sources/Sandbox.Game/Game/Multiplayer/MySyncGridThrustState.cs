using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    class MySyncGridThrustState
    {
        public Vector3B LastSendState;
        public int SleepFrames = 0;

        public bool ShouldSend(Vector3B newThrust)
        {
            if (SleepFrames > 4 && LastSendState != newThrust)
            {
                SleepFrames = 0;
                LastSendState = newThrust;
                return true;
            }
            else
            {
                SleepFrames++;
                return false;
            }
        }
    }
}
