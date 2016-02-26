using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using VRage;
using VRage.Serialization;
using VRage.Trace;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common;
using Sandbox.Game.GameSystems;
using Sandbox.Common.ObjectBuilders;
using VRageMath.PackedVector;
using SteamSDK;
using VRage.Game;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Game.Entity;

namespace Sandbox.Game.Multiplayer
{
    delegate void RazeBlockInCompoundDelegate(List<Tuple<Vector3I, ushort>> blocksToRemove, List<Tuple<Vector3I, ushort>> removedBlocks);

    [PreloadRequired]
    partial class MySyncGrid : MySyncEntity
    {
        [ProtoContract]
        public struct MySingleOwnershipRequest
        {
            [ProtoMember]
            public long BlockId;

            [ProtoMember]
            public long Owner; //PlayerId

            public MySingleOwnershipRequest(long blockId, long owner)
            {
                BlockId = blockId;
                Owner = owner;
            }
        }

        [ProtoContract]
        [MessageId(15166, P2PMessageEnum.Reliable)]
        struct ChangeOwnershipsMsg
        {
            [ProtoMember]
            public long RequestingPlayer; // PlayerId

            [ProtoMember]
            public MyOwnershipShareModeEnum ShareMode;

            [ProtoMember]
            public List<MySingleOwnershipRequest> Requests;
        }

        [ProtoContract]
        [MessageId(15282, P2PMessageEnum.Reliable)]
        struct RazeBlockInCompoundBlockMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public List<Tuple<Vector3I, ushort>> LocationsAndIds;
        }

        static List<Tuple<Vector3I, ushort>> m_tmpLocationsAndIdsReceive = new List<Tuple<Vector3I, ushort>>();

        static MySyncGrid()
        {
            MySyncLayer.RegisterEntityMessage<MySyncGrid, RazeBlockInCompoundBlockMsg>(OnRazeBlockInCompoundBlockRequest, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);

            MySyncLayer.RegisterMessage<ChangeOwnershipsMsg>(OnChangeOwnersRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeOwnershipsMsg>(OnChangeOwners, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

        }

        public event RazeBlockInCompoundDelegate RazedBlockInCompoundBlock;

        public new MyCubeGrid Entity
        {
            get { return (MyCubeGrid)base.Entity; }
        }

        public MySyncGrid(MyCubeGrid grid)
            : base(grid)
        {
        }

        public void RazeBlockInCompoundBlock(List<Tuple<Vector3I, ushort>> locationsAndIds)
        {
            var msg = new RazeBlockInCompoundBlockMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.LocationsAndIds = locationsAndIds;

            Sync.Layer.SendAsRpcToServer(ref msg);
        }

        private static void OnRazeBlockInCompoundBlockRequest(MySyncGrid sync, ref RazeBlockInCompoundBlockMsg msg, MyNetworkClient sender)
        {
            m_tmpLocationsAndIdsReceive.Clear();
            Debug.Assert(m_tmpLocationsAndIdsReceive != msg.LocationsAndIds, "The raze block in compound block message was received via loopback using the same list. This causes erasing of the message.");
            var handler = sync.RazedBlockInCompoundBlock;
            if (handler != null) handler(msg.LocationsAndIds, m_tmpLocationsAndIdsReceive);

            if (Sync.IsServer && m_tmpLocationsAndIdsReceive.Count > 0)
            {
                // Broadcast to clients, use result collection
                msg.LocationsAndIds = m_tmpLocationsAndIdsReceive;
                Sync.Layer.SendAsRpcToAll(ref msg);
            }
        }
    
        public static void ChangeOwnersRequest(MyOwnershipShareModeEnum shareMode, List<MySingleOwnershipRequest> requests, long requestingPlayer)
        {
            System.Diagnostics.Debug.Assert((int)shareMode >= 0);

            var msg = new ChangeOwnershipsMsg();
            msg.RequestingPlayer = requestingPlayer;
            msg.ShareMode = shareMode;

            msg.Requests = requests;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        private static void OnChangeOwnersRequest(ref ChangeOwnershipsMsg msg, MyNetworkClient sender)
        {
            MyCubeBlock block = null;
            int c = 0;

            while (c < msg.Requests.Count)
            {
                var request = msg.Requests[c];
                if (MyEntities.TryGetEntityById<MyCubeBlock>(request.BlockId, out block))
                {
                    if (Sync.IsServer && ((block.IDModule.Owner == 0) || block.IDModule.Owner == msg.RequestingPlayer || (request.Owner == 0)))
                    {
                        c++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Fail("Invalid ownership change request!");
                        msg.Requests.RemoveAtFast(c);
                    }
                }
                else
                {
                    c++;
                }
            }

            if (msg.Requests.Count > 0)
            {
                OnChangeOwners(ref msg, sender);
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
        }
        private static void OnChangeOwners(ref ChangeOwnershipsMsg msg, MyNetworkClient sender)
        {
            foreach (var request in msg.Requests)
            {
                MyCubeBlock block = null;
                if (MyEntities.TryGetEntityById<MyCubeBlock>(request.BlockId, out block))
                {
                    block.ChangeOwner(request.Owner, msg.ShareMode);
                }
            }
        }  
    }
}
