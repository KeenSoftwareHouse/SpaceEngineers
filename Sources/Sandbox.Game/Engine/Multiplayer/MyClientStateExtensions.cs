using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Network;

namespace Sandbox
{
    public static class MyClientStateExtensions
    {
        public static MyNetworkClient GetClient(this MyClientStateBase state)
        {
            if (state == null)
                return null;

            MyNetworkClient client;
            Sync.Clients.TryGetClient(state.EndpointId.Value, out client);
            return client;
        }

        public static MyPlayer GetPlayer(this MyClientStateBase state)
        {
            return GetClient(state).FirstPlayer;
        }
    }
}
