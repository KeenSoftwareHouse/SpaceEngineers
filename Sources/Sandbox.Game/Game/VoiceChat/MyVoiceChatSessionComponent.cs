using Sandbox.Common;
using Sandbox.Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamSDK;
using Sandbox.Game.Multiplayer;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.Entities.Character;
using VRageMath;
using VRage.Audio;
using Sandbox.Engine.Utils;
using VRage;
using Sandbox.Game.Entities;
using VRage.FileSystem;
using System.Diagnostics;
using Sandbox.Graphics;
using VRage.Library.Utils;
using VRage.Data.Audio;
using Sandbox.Graphics.TransparentGeometry;
using Sandbox.Game.Gui;

namespace Sandbox.Game.VoiceChat
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyVoiceChatSessionComponent : MySessionComponentBase
    {
        private struct ReceivedData
        {
            public List<byte> UncompressedBuffer;
            public MyTimeSpan Timestamp;
            public MyTimeSpan SpeakerTimestamp;

            public void ClearData()
            {
                UncompressedBuffer.Clear();
                Timestamp = MyTimeSpan.Zero;
            }

            public void ClearSpeakerTimestamp()
            {
                SpeakerTimestamp = MyTimeSpan.Zero;
            }
        }

        public static MyVoiceChatSessionComponent Static { get; private set; }
        private VoIP m_VoIP;
        private bool m_recording;
        private byte[] m_compressedVoiceBuffer;
        private byte[] m_uncompressedVoiceBuffer;
        private Dictionary<ulong, MyEntity3DSoundEmitter> m_voices;
        private Dictionary<ulong, ReceivedData> m_receivedVoiceData;
        private int m_frameCount = 0;
        private List<ulong> m_keys;

        private IMyVoiceChatLogic m_voiceChatLogic;

        // MW:TODO probably each client should know about this value so they dont send data mindlessly
        private bool m_enabled;

        private const uint COMPRESSED_SIZE = 8 * 1024;
        private const uint UNCOMPRESSED_SIZE = 22 * 1024;

        private Dictionary<ulong, bool> m_debugSentVoice = new Dictionary<ulong, bool>();
        private Dictionary<ulong, MyTuple<int, TimeSpan>> m_debugReceivedVoice = new Dictionary<ulong, MyTuple<int, TimeSpan>>();

        public bool IsRecording
        {
            get { return m_recording; }
        }

        public MyVoiceChatSessionComponent()
        {
        }

        public override void LoadData()
        {
            base.LoadData();

            Static = this;
            m_VoIP = new VoIP();
            m_voiceChatLogic = Activator.CreateInstance(MyPerGameSettings.VoiceChatLogic) as IMyVoiceChatLogic;
            m_recording = false;
            m_compressedVoiceBuffer = new byte[COMPRESSED_SIZE];
            m_uncompressedVoiceBuffer = new byte[UNCOMPRESSED_SIZE];
            m_voices = new Dictionary<ulong, MyEntity3DSoundEmitter>();
            m_receivedVoiceData = new Dictionary<ulong, ReceivedData>();
            m_keys = new List<ulong>();

            Sync.Players.PlayerRemoved += Players_PlayerRemoved;

            m_enabled = MyAudio.Static.EnableVoiceChat;
            MyAudio.Static.VoiceChatEnabled += Static_VoiceChatEnabled;
            MyHud.VoiceChat.VisibilityChanged += VoiceChat_VisibilityChanged;

            MyNetworkReader.SetHandler(MyMultiplayer.VoiceChatChannel, VoiceMessageReceived);
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            if (m_recording)
                StopRecording();

            MyNetworkReader.ClearHandler(MyMultiplayer.VoiceChatChannel);

            foreach (var pair in m_voices)
            {
                m_voices[pair.Key].StopSound(true, true);
                m_voices[pair.Key].Cleanup();
            }

            m_compressedVoiceBuffer = null;
            m_uncompressedVoiceBuffer = null;
            m_voiceChatLogic = null;
            m_VoIP = null;
            Static = null;
            m_receivedVoiceData = null;
            m_voices = null;
            m_keys = null;

            Sync.Players.PlayerRemoved -= Players_PlayerRemoved;
            MyAudio.Static.VoiceChatEnabled -= Static_VoiceChatEnabled;
            MyHud.VoiceChat.VisibilityChanged -= VoiceChat_VisibilityChanged;
        }

        public override bool IsRequiredByGame
        {
            get
            {
                return base.IsRequiredByGame && MySteam.IsActive && MyPerGameSettings.VoiceChatEnabled;
            }
        }

        private void Players_PlayerRemoved(MyPlayer.PlayerId pid)
        {
            if (pid.SerialId != 0)
                return;

            var userId = pid.SteamId;
            if (m_receivedVoiceData.ContainsKey(userId))
                m_receivedVoiceData.Remove(userId);

            if (m_voices.ContainsKey(userId))
            {
                m_voices[userId].StopSound(true, true);
                m_voices[userId].Cleanup();
                m_voices[userId] = null;
                m_voices.Remove(userId);
            }
        }

        private void Static_VoiceChatEnabled(bool isEnabled)
        {
            m_enabled = isEnabled;

            if (!m_enabled)
            {
                if (m_recording)
                {
                    m_recording = false;
                    StopRecording();
                }

                foreach (var voice in m_voices)
                {
                    voice.Value.StopSound(true, true);
                    voice.Value.Cleanup();
                }

                m_voices.Clear();
                m_receivedVoiceData.Clear();
            }
        }

        private void VoiceChat_VisibilityChanged(bool isVisible)
        {
            if (m_recording != isVisible)
            {
                if (m_recording)
                {
                    m_recording = false;
                    StopRecording();
                }
                else
                {
                    StartRecording();
                }
            }
        }

        public void StartRecording()
        {
            if (!m_enabled)
                return;

            m_recording = true;
            m_VoIP.StartVoiceRecording();
            MyHud.VoiceChat.Show();
        }

        public void StopRecording()
        {
            if (!m_enabled)
                return;

            m_VoIP.StopVoiceRecording();
            MyHud.VoiceChat.Hide();
        }

        public void ClearDebugData()
        {
            m_debugSentVoice.Clear();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (!m_enabled)
                return;

            if (!IsCharacterValid(MySession.LocalCharacter))
                return;

            if (m_recording)
                UpdateRecording();
            UpdatePlayback();
        }

        private bool IsCharacterValid(MyCharacter character)
        {
            return character != null && !character.IsDead && !character.MarkedForClose;
        }

        private void VoiceMessageReceived(byte[] data, int dataSize, ulong sender, TimeSpan timestamp)
        {
            if (!m_enabled)
                return;

            if (!IsCharacterValid(MySession.LocalCharacter))
                return;

            ProcessBuffer(data, dataSize, sender);
            if (MyFakes.ENABLE_VOICE_CHAT_DEBUGGING)
                m_debugReceivedVoice[sender] = new MyTuple<int, TimeSpan>(dataSize, timestamp);
        }

        private void PlayVoice(byte[] uncompressedBuffer, int uncompressedSize, ulong playerId, MySoundDimensions dimension, float maxDistance)
        {
            if (!m_voices.ContainsKey(playerId))
            {
                var player = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(playerId));
                m_voices[playerId] = new MyEntity3DSoundEmitter(player.Character);
            }

            //Debug.Assert(uncompressedSize != 0, "Playing empty buffer");
            var emitter = m_voices[playerId];
            emitter.PlaySound(uncompressedBuffer, (int)uncompressedSize, m_VoIP.SampleRate, maxDistance: maxDistance, dimension: dimension);
        }

        private void ProcessBuffer(byte[] compressedBuffer, int bufferSize, ulong sender)
        {
            uint uncompressedSize;
            var result = m_VoIP.DecompressVoice(compressedBuffer, (uint)bufferSize, m_uncompressedVoiceBuffer, out uncompressedSize);     
            Debug.Assert(result == VoiceResult.OK, result.ToString());
            if (result == VoiceResult.OK)
            {
                ReceivedData senderData;
                if (!m_receivedVoiceData.TryGetValue(sender, out senderData))
                {
                    senderData = new ReceivedData()
                    {
                        UncompressedBuffer = new List<byte>(),
                        Timestamp = MyTimeSpan.Zero,
                    };
                }
                if (senderData.Timestamp == MyTimeSpan.Zero)
                    senderData.Timestamp = MySandboxGame.Static.UpdateTime;
                senderData.SpeakerTimestamp = MySandboxGame.Static.UpdateTime;
                senderData.UncompressedBuffer.AddArray(m_uncompressedVoiceBuffer, (int)uncompressedSize);
                m_receivedVoiceData[sender] = senderData;
            }
        }

        private void UpdatePlayback()
        {
            var now = MySandboxGame.Static.UpdateTime;
            var speakerFadeout = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 60 * 1000;

            m_keys.AddRange(m_receivedVoiceData.Keys);
            foreach (var id in m_keys)
            {
                bool update = false;
                var playerId = id;
                var data = m_receivedVoiceData[id];
                var player = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(playerId));
                float maxDistance = 0;
                MySoundDimensions dimension = MySoundDimensions.D2;
                if (data.Timestamp != MyTimeSpan.Zero && m_voiceChatLogic.ShouldPlayVoice(player, data.Timestamp, out dimension, out maxDistance))
                {
                    PlayVoice(data.UncompressedBuffer.ToArray(), data.UncompressedBuffer.Count, playerId, dimension, maxDistance);
                    data.ClearData();
                    update = true;
                }

                if (data.SpeakerTimestamp != MyTimeSpan.Zero && (now - data.SpeakerTimestamp).Miliseconds > speakerFadeout)
                {
                    data.ClearSpeakerTimestamp();
                    update = true;
                }

                if (update)
                {
                    m_receivedVoiceData[id] = data;
                }
            }
            m_keys.Clear();
        }

        private void UpdateRecording()
        {
            uint size = 0;
            var result = (m_VoIP.GetAvailableVoice(out size));
            if (result == VoiceResult.OK)
            {
                result = m_VoIP.GetVoice(m_compressedVoiceBuffer, out size);
                Debug.Assert(result == VoiceResult.OK, "Get voice failed: " + result.ToString());

                if (MyFakes.ENABLE_VOICE_CHAT_DEBUGGING)
                {
                    ProcessBuffer(m_compressedVoiceBuffer, (int)size, MySteam.UserId);
                }

                foreach (var player in Sync.Players.GetOnlinePlayers())
                {
                    if (player.Id.SerialId == 0 
                        && player.Id.SteamId != MySession.LocalHumanPlayer.Id.SteamId 
                        && IsCharacterValid(player.Character) 
                        && m_voiceChatLogic.ShouldSendVoice(player))
                    {
                        SendVoice(player.Id.SteamId, m_compressedVoiceBuffer, size);
                        if (MyFakes.ENABLE_VOICE_CHAT_DEBUGGING)
                            m_debugSentVoice[player.Id.SteamId] = true;
                    }
                    else
                    {
                        if (MyFakes.ENABLE_VOICE_CHAT_DEBUGGING)
                            m_debugSentVoice[player.Id.SteamId] = false;
                    }
                }
            }
            else if (result == VoiceResult.NotRecording)
            {
                m_recording = false;

                if (MyFakes.ENABLE_VOICE_CHAT_DEBUGGING)
                {
                    var localUser = MySteam.UserId;
                    if (!m_voices.ContainsKey(localUser))
                    {
                        var player = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(localUser));
                        m_voices[localUser] = new MyEntity3DSoundEmitter(player.Character);
                    }

                    var emitter = m_voices[localUser];
                    if (m_receivedVoiceData.ContainsKey(localUser))
                    {
                        var data = m_receivedVoiceData[localUser];
                        emitter.PlaySound(data.UncompressedBuffer.ToArray(), (int)data.UncompressedBuffer.Count, m_VoIP.SampleRate);
                        data.ClearData();
                        data.ClearSpeakerTimestamp();
                        m_receivedVoiceData[localUser] = data;
                    }
                }
            }
        }

        private void SendVoice(ulong user, byte[] data, uint dataSize)
        {
            Peer2Peer.SendPacket(user, data, (int)dataSize, P2PMessageEnum.UnreliableNoDelay, MyMultiplayer.VoiceChatChannel);
        }

        public override void Draw()
        {
            base.Draw();

            if (MyDebugDrawSettings.DEBUG_DRAW_VOICE_CHAT && MyFakes.ENABLE_VOICE_CHAT_DEBUGGING)
                DebugDraw();

            foreach (var pair in m_receivedVoiceData)
            {
                if (pair.Value.SpeakerTimestamp != MyTimeSpan.Zero)
                {
                    var player = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(pair.Key, 0));
                    var position = player.Character.PositionComp.GetPosition() + new Vector3D(0, 2.2, 0);
                    var color = Color.White;
                    MyTransparentGeometry.AddPointBillboard("VoiceChatSpeaker", color, position, 0.25f, 0, 0, true);
                }
            }
        }

        private void DebugDraw()
        {
            const float size = 30;
            const float fontSize = 1.0f;

            Vector2 initPos = new Vector2(300, 100);
            VRageRender.MyRenderProxy.DebugDrawText2D(initPos, "Sent voice to:", Color.White, fontSize);
            initPos.Y += size;
            foreach (var sent in m_debugSentVoice)
            {
                string final = string.Format("id: {0} => {1}", sent.Key, sent.Value ? "SENT" : "NOT");
                VRageRender.MyRenderProxy.DebugDrawText2D(initPos, final, Color.White, fontSize);
                initPos.Y += size;
            }
            VRageRender.MyRenderProxy.DebugDrawText2D(initPos, "Received voice from:", Color.White, fontSize);
            initPos.Y += size;
            foreach (var rec in m_debugReceivedVoice)
            {
                string final = string.Format("id: {0} => size: {1} (timestamp {2})", rec.Key, rec.Value.Item1, rec.Value.Item2.ToString());
                VRageRender.MyRenderProxy.DebugDrawText2D(initPos, final, Color.White, fontSize);
                initPos.Y += size;
            }

            VRageRender.MyRenderProxy.DebugDrawText2D(initPos, "Uncompressed buffers:", Color.White, fontSize);
            initPos.Y += size;
            foreach (var rec in m_receivedVoiceData)
            {
                string final = string.Format("id: {0} => size: {1}", rec.Key, rec.Value.UncompressedBuffer.Count);
                VRageRender.MyRenderProxy.DebugDrawText2D(initPos, final, Color.White, fontSize);
                initPos.Y += size;
            }

            //if (IsCharacterValid(MySession.LocalCharacter))
            //{
            //    var worldMatrix = MySession.LocalCharacter.PositionComp.WorldMatrix;
            //    var color = Color.DarkBlue;
            //    color.A = 100;
            //    MySimpleObjectDraw.DrawTransparentSphere(ref worldMatrix, (float)20, ref color, MySimpleObjectRasterizer.SolidAndWireframe, 20);
            //}
        }
    }
}
