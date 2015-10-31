using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Multiplayer
{
    class MySyncCryoChamber : MySyncCockpit
    {
        [MessageIdAttribute(8200, P2PMessageEnum.Reliable)]
        protected struct ControlPilotMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public ulong SteamId;
            public int SerialId;
        }

        static MySyncCryoChamber()
        {
            MySyncLayer.RegisterEntityMessage<MySyncCryoChamber, ControlPilotMsg>(OnControlPilotMsg, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
        }

        public MySyncCryoChamber(MyCryoChamber chamber):
            base(chamber)
        {

        }

        public void SendControlPilotMsg(MyPlayer player)
        {
            var msg = new ControlPilotMsg();

            msg.EntityId = Entity.Entity.EntityId;

            msg.SteamId = player.Id.SteamId;
            msg.SerialId = player.Id.SerialId;

            MySession.Static.SyncLayer.SendMessageToServerAndSelf(ref msg);
        }

        private static void OnControlPilotMsg(MySyncCryoChamber syncObject, ref ControlPilotMsg msg, World.MyNetworkClient sender)
        {
            var playerId = new MyPlayer.PlayerId(msg.SteamId, msg.SerialId);
            var player = Sync.Players.GetPlayerById(playerId);

            var cryoChamber = syncObject.Entity as MyCryoChamber;

            if (player != null)
            {
                if (cryoChamber.Pilot != null)
                {
                    if (player == MySession.LocalHumanPlayer)
                    {
                        cryoChamber.OnPlayerLoaded();

                        if (MySession.Static.CameraController != cryoChamber)
                        {
                            MySession.SetCameraController(MyCameraControllerEnum.Entity, cryoChamber);
                        }
                    }
                 
                    player.Controller.TakeControl(cryoChamber);
                    player.Identity.ChangeCharacter(cryoChamber.Pilot);
                    if (Sync.IsServer)
                    {
                        Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                    }
                }
                else
                {
                    Debug.Fail("Selected cryo chamber doesn't have a pilot!");
                }
            }
            else
            {
                Debug.Fail("Failed to find player to put in cryo chamber!");
            }
        }
    }
}
