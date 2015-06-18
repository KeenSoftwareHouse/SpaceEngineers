using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncSoundBlock : MySyncCubeBlock
    {
        [MessageId(331, P2PMessageEnum.Reliable)]
        struct PlaySoundMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
        }

        [MessageId(332, P2PMessageEnum.Reliable)]
        struct SelectSoundMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
            public MyCueId CueId;
        }

        [MessageId(333, P2PMessageEnum.Reliable)]
        struct StopSoundMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            } 
        }

        [MessageId(334, P2PMessageEnum.Reliable)]
        struct ChangeLoopPeriodMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
            public float LoopPeriod;
        }

        [MessageId(335, P2PMessageEnum.Reliable)]
        struct ChangeSoundVolumeMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
            public float Volume;
        }

        [MessageId(336, P2PMessageEnum.Reliable)]
        struct ChangeSoundRangeMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }
            public float Range;
        }

        static MySyncSoundBlock()
        {
            MySyncLayer.RegisterEntityMessage<MySyncSoundBlock, PlaySoundMsg>(OnPlaySound, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncSoundBlock, SelectSoundMsg>(OnSelectSound, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncSoundBlock, StopSoundMsg>(OnStopSound, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncSoundBlock, ChangeLoopPeriodMsg>(OnChangeLoopPeriod, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncSoundBlock, ChangeSoundVolumeMsg>(OnChangeSoundVolume, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncSoundBlock, ChangeSoundRangeMsg>(OnChangeSoundRange, MyMessagePermissions.Any);
        }

        public MySyncSoundBlock(MySoundBlock block) : base(block)
        {
        }

        public new MySoundBlock Entity
        {
            get { return (MySoundBlock)base.Entity; }
        }

        static void OnPlaySound(MySyncSoundBlock sync, ref PlaySoundMsg msg, MyNetworkClient sender)
        {
            sync.Entity.PlaySound();
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(msg);
        }

        static void OnSelectSound(MySyncSoundBlock sync, ref SelectSoundMsg msg, MyNetworkClient sender)
        {
            sync.Entity.SelectSound(msg.CueId, false);
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(msg);
        }

        static void OnStopSound(MySyncSoundBlock sync, ref StopSoundMsg msg, MyNetworkClient sender)
        {
            sync.Entity.StopSound();
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(msg);
        }

        static void OnChangeLoopPeriod(MySyncSoundBlock sync, ref ChangeLoopPeriodMsg msg, MyNetworkClient sender)
        {
            sync.Entity.LoopPeriod = msg.LoopPeriod;
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(msg);
        }

        static void OnChangeSoundVolume(MySyncSoundBlock sync, ref ChangeSoundVolumeMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Volume = msg.Volume;
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(msg);
        }

        static void OnChangeSoundRange(MySyncSoundBlock sync, ref ChangeSoundRangeMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Range = msg.Range;
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(msg);
        }

        public void SendPlaySoundRequest()
        {
            if (!Entity.IsWorking)
                return;

            PlaySoundMsg msg = new PlaySoundMsg();

            msg.EntityId = Entity.EntityId;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        public void SendSelectSoundRequest(MyCueId cueId)
        {
            SelectSoundMsg msg = new SelectSoundMsg();

            msg.EntityId = Entity.EntityId;
            msg.CueId = cueId;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        public void SendStopSoundRequest()
        {
            if (!Entity.IsWorking)
                return;

            StopSoundMsg msg = new StopSoundMsg();

            msg.EntityId = Entity.EntityId;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        public void SendChangeLoopPeriodRequest(float loopPeriod)
        {
            ChangeLoopPeriodMsg msg = new ChangeLoopPeriodMsg();

            msg.EntityId = Entity.EntityId;
            msg.LoopPeriod = loopPeriod;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        public void SendChangeSoundVolumeRequest(float volume)
        {
            ChangeSoundVolumeMsg msg = new ChangeSoundVolumeMsg();

            msg.EntityId = Entity.EntityId;
            msg.Volume = volume;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        public void SendChangeSoundRangeRequest(float range)
        {
            ChangeSoundRangeMsg msg = new ChangeSoundRangeMsg();

            msg.EntityId = Entity.EntityId;
            msg.Range = range;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
    }
}
