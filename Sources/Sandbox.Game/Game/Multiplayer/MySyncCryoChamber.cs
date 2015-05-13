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
            MySyncLayer.RegisterEntityMessage<MySyncCryoChamber, ControlPilotMsg>(OnControlPilotMsg, MyMessagePermissions.Any);
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

            MySession.Static.SyncLayer.SendMessageToAllAndSelf(ref msg);
        }

        private static void OnControlPilotMsg(MySyncCryoChamber syncObject, ref ControlPilotMsg msg, World.MyNetworkClient sender)
        {
            var playerId = new MyPlayer.PlayerId(msg.SteamId, msg.SerialId);
            var player = Sync.Players.TryGetPlayerById(playerId);

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
