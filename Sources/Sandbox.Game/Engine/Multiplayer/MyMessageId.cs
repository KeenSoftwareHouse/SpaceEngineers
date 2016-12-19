
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
        CLIENT_ACKS = 17, // Client -> Server, state sync acks
        CLIENT_UPDATE = 9, // Client -> Server, client state (client physics, his context)
        REPLICATION_READY = 10, // Client -> Server, when replicable is ready on client
        REPLICATION_STREAM_BEGIN = 11,

        //Control messages, needs to be proces even when processing is stoped
        JOIN_RESULT = 12,
        WORLD_DATA = 13,
        CLIENT_CONNNECTED = 14,
        WORLD_BATTLE_DATA = 15,
        BATTLE_KEY_VALUE_CHANGED = 16,
    }
}
