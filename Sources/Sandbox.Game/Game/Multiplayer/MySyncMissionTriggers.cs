using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using VRageMath;
using Sandbox.Game.Entities.Cube;
using Sandbox.Common.ObjectBuilders;
using ProtoBuf;
using Sandbox.Game.World.Triggers;
using Sandbox.Game.SessionComponents;

namespace Sandbox.Game.Multiplayer
{

   [PreloadRequired]
    class MySyncMissionTriggers
    {
        [MessageIdAttribute(6200, P2PMessageEnum.Reliable)]
        protected struct WonMsg
        {
            public MyPlayer.PlayerId playerId;
            public int index;
            public override string ToString() { return String.Format("Won #{0}", index); }
        }
        [MessageIdAttribute(6201, P2PMessageEnum.Reliable)]
        protected struct LostMsg 
        {
            public MyPlayer.PlayerId playerId;
            public int index;
            public override string ToString() { return String.Format("Lost #{0}", index); }
        }
        
        static MySyncMissionTriggers()
        {
            //these messages are from server to client only
            MySyncLayer.RegisterMessage<WonMsg>(PlayerWonSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<LostMsg>(PlayerLostSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }


        public static void PlayerWon(MyPlayer.PlayerId id, int triggerIndex)
        {
            MySessionComponentMissionTriggers.Static.SetWon(id, triggerIndex);
            if (!Sync.MultiplayerActive || !MySession.Static.IsScenario)
                return;
            WonMsg msg = new WonMsg();
            msg.index = triggerIndex;
            msg.playerId = id;
            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }
        static void PlayerWonSuccess(ref WonMsg msg, MyNetworkClient sender)
        {
            MySessionComponentMissionTriggers.Static.SetWon(msg.playerId, msg.index);
        }


        public static void PlayerLost(MyPlayer.PlayerId id, int triggerIndex)
        {
            MySessionComponentMissionTriggers.Static.SetLost(id, triggerIndex);
            if (!Sync.MultiplayerActive || !MySession.Static.IsScenario)
                return;
            LostMsg msg = new LostMsg();
            msg.index = triggerIndex;
            msg.playerId = id;
            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }
        static void PlayerLostSuccess(ref LostMsg msg, MyNetworkClient sender)
        {
            MySessionComponentMissionTriggers.Static.SetLost(msg.playerId, msg.index);
        }


    }
}
