using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncMeteorShower
    {

        [ProtoContract]
        [MessageId(7687, P2PMessageEnum.Reliable)]
        protected struct UpdateShowerTargetMsg
        {
            [ProtoMember]
            public bool HasTarget;

            [ProtoMember]
            public Vector3 Center;

            [ProtoMember]
            public float Radius;
        }

        static MySyncMeteorShower()
        {
            MySyncLayer.RegisterMessage<UpdateShowerTargetMsg>(OnUpdateShowerTarget, MyMessagePermissions.FromServer);
        }
        
        public static void UpdateShowerTarget(BoundingSphere? target)
        {
            var msg = new UpdateShowerTargetMsg();

            if (target.HasValue)
            {
                msg.HasTarget = true;
                msg.Center = target.Value.Center;
                msg.Radius = target.Value.Radius;
            }
            else
                msg.HasTarget = false;

            Sync.Layer.SendMessageToAll(ref msg);
        }

        static void OnUpdateShowerTarget(ref UpdateShowerTargetMsg msg, MyNetworkClient sender)
        {
            if (msg.HasTarget)
                MyMeteorShower.CurrentTarget = new BoundingSphere(msg.Center, msg.Radius);
            else
                MyMeteorShower.CurrentTarget = null;
        }
    }
}
