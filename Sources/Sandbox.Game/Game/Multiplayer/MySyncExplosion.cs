using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using System;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public class MySyncExplosions
    {
        [MessageId(35, P2PMessageEnum.Reliable)]
        protected struct ProxyExplosionMsg 
        {
            public Vector3 Center;
            public float Radius;
            public MyExplosionTypeEnum Type;
            public Vector3 VoxelCenter;
            public float ParticleScale;
        }

        static MySyncExplosions()
        {
            MySyncLayer.RegisterMessage<ProxyExplosionMsg>(ProxyExplosionRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ProxyExplosionMsg>(ProxyExplosionSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public MySyncExplosions()
        {
        }

        public void RequestExplosion(Vector3 center, float radius, MyExplosionTypeEnum type, Vector3 voxelCenter, float particleScale)
        {
            var msg = new ProxyExplosionMsg();
            msg.Center = center;
            msg.Radius = radius;
            msg.Type = type;
            msg.VoxelCenter = voxelCenter;
            msg.ParticleScale = particleScale;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        static void ProxyExplosionRequest(ref ProxyExplosionMsg msg, MyNetworkClient sender)
        {
            //There is not sending to Self by purpose
            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void ProxyExplosionSuccess(ref ProxyExplosionMsg msg, MyNetworkClient sender)
        {
            //Dont create explosion particles if message is bufferred, it is useless to create hundred explosion after scene load
            if (MySession.Static.Ready)
            {
                //  Create explosion
                MyExplosionInfo info = new MyExplosionInfo()
                {
                    PlayerDamage = 0,
                    //Damage = m_ammoProperties.Damage,
                    Damage = 200,
                    ExplosionType = msg.Type,
                    ExplosionSphere = new BoundingSphere(msg.Center, msg.Radius),
                    LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN,
                    CascadeLevel = 0,
                    HitEntity = null,
                    ParticleScale = msg.ParticleScale,
                    OwnerEntity = null,
                    Direction = Vector3.Forward,
                    VoxelExplosionCenter = msg.VoxelCenter,
                    ExplosionFlags = MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.CREATE_SHRAPNELS,
                    VoxelCutoutScale = 1.0f,
                    PlaySound = true,
                    ObjectsRemoveDelayInMiliseconds = 40
                };
                MyExplosions.AddExplosion(ref info, false);
            }
        }

    }
}
