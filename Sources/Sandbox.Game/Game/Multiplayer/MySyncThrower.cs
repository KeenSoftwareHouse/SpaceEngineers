#region Using

using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Components;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    static class MySyncThrower
    {
        [ProtoContract]
        [MessageId(11889, P2PMessageEnum.Reliable)]
        struct ThrowMsg
        {
            [ProtoMember]
            public MyObjectBuilder_CubeGrid Grid;
            [ProtoMember]
            public Vector3D Position;
            [ProtoMember]
            public Vector3D LinearVelocity;
            [ProtoMember]
            public float Mass;
            [ProtoMember]
            public MyCueId ThrowSound;
        }


        static MySyncThrower()
        {
            MySyncLayer.RegisterMessage<ThrowMsg>(OnThrowMessageRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ThrowMsg>(OnThrowMessageSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public static void RequestThrow(MyObjectBuilder_CubeGrid grid, Vector3D position, Vector3D linearVelocity, float mass, MyCueId throwSound)
        {
            ThrowMsg msg = new ThrowMsg();
            msg.Grid = grid;
            msg.Position = position;
            msg.LinearVelocity = linearVelocity;
            msg.Mass = mass;
            msg.ThrowSound = throwSound;
            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        static void OnThrowMessageRequest(ref ThrowMsg msg, MyNetworkClient sender)
        {
            MySession.Static.SyncLayer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
        }

        static void OnThrowMessageSuccess(ref ThrowMsg msg, MyNetworkClient sender)
        {
            MySessionComponentThrower.Static.Throw(msg.Grid, msg.Position, msg.LinearVelocity, msg.Mass, msg.ThrowSound);
        }
    }
}
