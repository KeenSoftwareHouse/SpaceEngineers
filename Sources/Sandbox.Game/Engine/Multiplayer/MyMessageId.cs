using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Engine.Multiplayer
{
    public enum MyMessageId : byte
    {
        OLD_GAME_EVENT = 1,
        OLD_GAME_EVENT_FLUSH = 2,
        RPC = 3,
        REPLICATION_CREATE = 4,
        REPLICATION_DESTROY = 5,
        SERVER_DATA = 6, // Server -> Client, initial server data, response to client ready
        SERVER_STATE_SYNC = 7, // Server -> Client, state sync data (physics, inventory, terminal)
        CLIENT_READY = 8, // Client -> Server, when world is loaded
        CLIENT_UPDATE = 9, // Client -> Server, client state (client physics, his context, state sync acks)
        REPLICATION_READY = 10, // Client -> Server, when replicable is ready on client
    }
}
