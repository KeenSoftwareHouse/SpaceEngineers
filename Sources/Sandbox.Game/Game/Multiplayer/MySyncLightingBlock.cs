using System;
using System.Text;
using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using Sandbox.Game.Entities.Cube;
using VRageMath;
using Sandbox.Game.Entities.Blocks;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncLightingBlock
    {
        MyLightingBlock m_light;

        [MessageIdAttribute(15277, P2PMessageEnum.Reliable)]
        protected struct ChangeLightColorMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public Color Color;
            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(16289, P2PMessageEnum.Reliable)]
        protected struct ChangeLightRadiusMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float Radius;
            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(16290, P2PMessageEnum.Reliable)]
        protected struct ChangeLightFalloffMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float Falloff;
            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(16291, P2PMessageEnum.Reliable)]
        protected struct ChangeLightIntensityMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float Intensity;
            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }


        [MessageIdAttribute(16292, P2PMessageEnum.Reliable)]
        protected struct ChangeLightBlinkIntervalMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float BlinkIntervalSeconds;
            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(16293, P2PMessageEnum.Reliable)]
        protected struct ChangeLightBlinkLengthMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float BlinkLength;
            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(16294, P2PMessageEnum.Reliable)]
        protected struct ChangeLightBlinkOffsetMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float BlinkOffset;
            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        

        static MySyncLightingBlock()
        {
            MySyncLayer.RegisterMessage<ChangeLightColorMsg>(ChangeLightColorRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeLightColorMsg>(ChangeLightColorSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeLightRadiusMsg>(ChangeLightRadiusRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeLightRadiusMsg>(ChangeLightRadiusSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeLightFalloffMsg>(ChangeLightFalloffRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeLightFalloffMsg>(ChangeLightFalloffSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeLightIntensityMsg>(ChangeLightIntensityRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeLightIntensityMsg>(ChangeLightIntensitySuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeLightBlinkIntervalMsg>(ChangeLightBlinkIntervalRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeLightBlinkIntervalMsg>(ChangeLightBlinkIntervalSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);


            MySyncLayer.RegisterMessage<ChangeLightBlinkLengthMsg>(ChangeLightBlinkLengthRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeLightBlinkLengthMsg>(ChangeLightBlinkLengthSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeLightBlinkOffsetMsg>(ChangeLightBlinkOffsetRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeLightBlinkOffsetMsg>(ChangeLightBlinkOffseSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public MySyncLightingBlock(MyLightingBlock light)
        {
            m_light = light;
        }

        public void SendChangeLightColorRequest(Color color)
        {
            var msg = new ChangeLightColorMsg();

            msg.EntityId = m_light.EntityId;
            msg.Color = color;
          
            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
        static void ChangeLightColorRequest(ref ChangeLightColorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLightingBlock)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void ChangeLightColorSuccess(ref ChangeLightColorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var light = entity as MyLightingBlock;
            if (light !=  null)
            {
                light.Color = msg.Color;
            }
        }


        public void SendChangeLightRadiusRequest(float radius)
        {
            var msg = new ChangeLightRadiusMsg();

            msg.EntityId = m_light.EntityId;
            msg.Radius = radius;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
        static void ChangeLightRadiusRequest(ref ChangeLightRadiusMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLightingBlock)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void ChangeLightRadiusSuccess(ref ChangeLightRadiusMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var light = entity as MyLightingBlock;
            if (light != null)
            {
                light.Radius = msg.Radius;
            }
        }

        public void SendChangeLightFalloffRequest(float falloff)
        {
            var msg = new ChangeLightFalloffMsg();

            msg.EntityId = m_light.EntityId;
            msg.Falloff = falloff;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
        static void ChangeLightFalloffRequest(ref ChangeLightFalloffMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLightingBlock)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void ChangeLightFalloffSuccess(ref ChangeLightFalloffMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var light = entity as MyLightingBlock;
            if (light != null)
            {
                light.Falloff = msg.Falloff;
            }
        }

        public void SendChangeLightIntensityRequest(float intesity)
        {
            var msg = new ChangeLightIntensityMsg();

            msg.EntityId = m_light.EntityId;
            msg.Intensity = intesity;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
        static void ChangeLightIntensityRequest(ref ChangeLightIntensityMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLightingBlock)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void ChangeLightIntensitySuccess(ref ChangeLightIntensityMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var light = entity as MyLightingBlock;
            if (light != null)
            {
                light.Intensity = msg.Intensity;
            }
        }

        public void SendChangeLightBlinkIntervalRequest(float blinkInterval)
        {
            var msg = new ChangeLightBlinkIntervalMsg();

            msg.EntityId = m_light.EntityId;
            msg.BlinkIntervalSeconds = blinkInterval;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
        static void ChangeLightBlinkIntervalRequest(ref ChangeLightBlinkIntervalMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLightingBlock)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void ChangeLightBlinkIntervalSuccess(ref ChangeLightBlinkIntervalMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var light = entity as MyLightingBlock;
            if (light != null)
            {
                light.BlinkIntervalSeconds = msg.BlinkIntervalSeconds;
            }
        }

        public void SendChangeLightBlinkLengthRequest(float blinkLength)
        {
            var msg = new ChangeLightBlinkLengthMsg();

            msg.EntityId = m_light.EntityId;
            msg.BlinkLength = blinkLength;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
        static void ChangeLightBlinkLengthRequest(ref ChangeLightBlinkLengthMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLightingBlock)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }

        }
        static void ChangeLightBlinkLengthSuccess(ref ChangeLightBlinkLengthMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var light = entity as MyLightingBlock;
            if (light != null)
            {
                light.BlinkLength = msg.BlinkLength;
            }
        }

        public void SendChangeLightBlinkOffsetRequest(float blinkOffset)
        {
            var msg = new ChangeLightBlinkOffsetMsg();

            msg.EntityId = m_light.EntityId;
            msg.BlinkOffset = blinkOffset;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
        static void ChangeLightBlinkOffsetRequest(ref ChangeLightBlinkOffsetMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLightingBlock)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }

        }
        static void ChangeLightBlinkOffseSuccess(ref ChangeLightBlinkOffsetMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var light = entity as MyLightingBlock;
            if (light != null)
            {
                light.BlinkOffset = msg.BlinkOffset;
            }
        }
    }
}
