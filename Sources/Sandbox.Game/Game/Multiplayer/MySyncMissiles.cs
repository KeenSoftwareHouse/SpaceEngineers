using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Weapons.Ammo
{
    [PreloadRequired]
    class MySyncMissiles
    {
        [MessageId(3246, SteamSDK.P2PMessageEnum.Unreliable)]
        struct MissileShootMsg
        {
            public Vector3 Position;
            public Vector3 InitialVelocity;
            public Vector3 Direction;
            public float CustomMaxDistance;
            public MyAmmoBase.MyAmmoBaseFlags Flags;
            public long LauncherEntityId;
            public long Owner;
        }

        static MySyncMissiles()
        {
            MySyncLayer.RegisterMessage<MissileShootMsg>(MissileCreatedSuccess, MyMessagePermissions.FromServer);
        }

        public static void SendMissileCreated(IMyMissileGunObject launcher, Vector3 position, Vector3 initialVelocity, Vector3 direction, float customMaxDistance, MyAmmoBase.MyAmmoBaseFlags flags, long owner)
        {
            Debug.Assert(Sync.IsServer, "Only server can shoot missiles!");
            Debug.Assert(launcher is MyEntity, "Missile launching object should be an entity!");

            if (!Sync.IsServer || !(launcher is MyEntity))
                return;

            MissileShootMsg msg = new MissileShootMsg();
            msg.Position = position;
            msg.InitialVelocity = initialVelocity;
            msg.Direction = direction;
            msg.CustomMaxDistance = customMaxDistance;
            msg.Flags = flags;
            msg.LauncherEntityId = (launcher as MyEntity).EntityId;
            msg.Owner = owner;

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void MissileCreatedSuccess(ref MissileShootMsg msg, MyNetworkClient sender)
        {
            MyEntity shootingEntity;
            MyEntities.TryGetEntityById(msg.LauncherEntityId, out shootingEntity);
            Debug.Assert(shootingEntity != null, "Could not find missile shooting entity");
            if (shootingEntity == null)
                return;

            var shootingLauncher = shootingEntity as IMyMissileGunObject;
            Debug.Assert(shootingLauncher != null, "Shooting entity was not an IMyMissileGunObject");
            if (shootingLauncher == null)
                return;

            shootingLauncher.ShootMissile(msg.InitialVelocity);
        }
    }
}
