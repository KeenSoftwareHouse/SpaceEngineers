using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using SteamSDK;

namespace Sandbox.Game.Multiplayer
{
    // Syncing current ammo in MyGunBase (when used magazines can be in inventory then this class can be removed).
    [PreloadRequired]
    public class MySyncGunBase
    {
        [ProtoContract]
        [MessageId(6221, P2PMessageEnum.Reliable)]
        struct CurrentAmmoCountChangedMsg
        {
            [ProtoMember]
            public long WeaponId;

            [ProtoMember]
            public int AmmoCount;
        }

        public static event Action<long, int> AmmoCountChanged;


        static MySyncGunBase()
        {
            MySyncLayer.RegisterMessage<CurrentAmmoCountChangedMsg>(OnCurrentAmmoCountChangedMsg, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public static void RequestCurrentAmmoCountChangedMsg(long weaponId, int ammoCount)
        {
            Debug.Assert(Sync.IsServer);

            var msg = new CurrentAmmoCountChangedMsg();
            msg.WeaponId = weaponId;
            msg.AmmoCount = ammoCount;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void OnCurrentAmmoCountChangedMsg(ref CurrentAmmoCountChangedMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            var handler = AmmoCountChanged;
            if (handler != null)
                handler(msg.WeaponId, msg.AmmoCount);
        }

    }
}
