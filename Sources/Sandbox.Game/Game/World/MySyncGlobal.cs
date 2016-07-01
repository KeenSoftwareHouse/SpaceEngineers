using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRageMath;
using SteamSDK;
using System.Diagnostics;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using SharpDX;
using SharpDX.Mathematics;
using VRage.Utils;
using VRage.Audio;
using VRage.Library.Utils;
using VRage;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public class MySyncGlobal
    {
        [MessageIdAttribute(1643, P2PMessageEnum.Reliable)]
        [ProtoContract]
        protected struct PlayMusicMsg 
        {
            [ProtoMember]
            public MyStringId Transition;

            [ProtoMember]
            public MyStringId Category;

            [ProtoMember]
            public BoolBlit Loop;
        }


        [MessageIdAttribute(1644, P2PMessageEnum.Reliable)]
        protected struct ShowNotificationMsg 
        {
            public MyStringId Text;
            public int Time;
        }

        [MessageIdAttribute(1645, P2PMessageEnum.Unreliable)]
        struct SimulationInfoMsg
        {
            public Half SimulationSpeed;
        }

        [MessageIdAttribute(1646, P2PMessageEnum.Unreliable)]
        struct ElapsedGameTimeMsg
        {
            public long ElapsedGameTicks;
        }

        static MySyncGlobal()
        {
            MySyncLayer.RegisterMessage<SimulationInfoMsg>(OnSimulationInfo, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<PlayMusicMsg>(OnPlayMusic, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<ShowNotificationMsg>(OnShowNotification, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<ElapsedGameTimeMsg>(OnElapsedGameTime, MyMessagePermissions.FromServer);
        }

        public MySyncGlobal()
        {
        }

        public void PlayMusic(MyStringId transition,MyStringId category, bool loop)
        {
            var msg = new PlayMusicMsg();
            msg.Transition = transition;
            msg.Category = category;
            msg.Loop = loop;

            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnPlayMusic(ref PlayMusicMsg msg, MyNetworkClient sender)
        {
            MyAudio.Static.ApplyTransition(msg.Transition, 0, msg.Category, msg.Loop);
        }

        public void ShowNotification(MyStringId text, int time)
        {
            var msg = new ShowNotificationMsg();
            msg.Text = text;
            msg.Time = time;

            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnShowNotification(ref ShowNotificationMsg msg, MyNetworkClient sender)
        {
            var notification = new MyHudNotification(msg.Text, msg.Time, level: MyNotificationLevel.Important);
            MyHud.Notifications.Add(notification);
        }

        public static void SendSimulationInfo()
        {
            Debug.Assert(Sync.IsServer, "Only server should send simulation ratio");

            var msg = new SimulationInfoMsg();
            ProfilerShort.Begin("GetSimSpeed");
            msg.SimulationSpeed = Sandbox.Engine.Physics.MyPhysics.SimulationRatio;
            ProfilerShort.End();
            ProfilerShort.Begin("SendSimSpeed");
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
            ProfilerShort.End();
        }

        static void OnSimulationInfo(ref SimulationInfoMsg msg, MyNetworkClient sender)
        {
            Sync.ServerSimulationRatio = msg.SimulationSpeed;
        }

        public static void SendElapsedGameTime()
        {
            Debug.Assert(Sync.IsServer, "Only server can send time info");

            var msg = new ElapsedGameTimeMsg();
            msg.ElapsedGameTicks = MySession.Static.ElapsedGameTime.Ticks;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnElapsedGameTime(ref ElapsedGameTimeMsg msg, MyNetworkClient sender)
        {
            MySession.Static.ElapsedGameTime = new TimeSpan(msg.ElapsedGameTicks);
        }
    }
}
