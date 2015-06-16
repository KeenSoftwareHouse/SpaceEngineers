#region Using

using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.GameSystems;
using SteamSDK;

using VRage;
using Sandbox.Game.Localization;
using VRage.Library.Utils;
using VRage.Audio;

#endregion

namespace Sandbox.Game.Multiplayer
{
    delegate void ChangeMovementStateDelegate(MyCharacterMovementEnum state);
    delegate void SwitchCharacterModelDelegate(string model, Vector3 colorMaskHSV);
    delegate void ChangeFlagsDelegate(bool enableJetpack, bool enableDampeners, bool enableLights, bool enableIronsight, bool enableBroadcast);
    delegate void ChangeHeadOrSpineDelegate(float headLocalXAngle, float headLocalYAngle, Quaternion spineRotation,
    Quaternion headRotation, Quaternion handRotation, Quaternion upperHandRotation);
    delegate void DoDamageDelegate(float damage, MyDamageType damageType);

    [PreloadRequired]
    class MySyncCharacter : MySyncControllableEntity
    {
        [Flags]
        enum CharacterFlags
        {
            Jetpack = 0x1,
            Dampeners = 0x2,
            Lights = 0x4,
            Ironsight = 0x8,
            Broadcast = 0x10,
        }

        [MessageId(2, P2PMessageEnum.Reliable)]
        struct ChangeMovementStateMsg : IEntityMessage
        {
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            public MyCharacterMovementEnum MovementState;
        }

        [MessageId(3, P2PMessageEnum.Reliable)]
        struct ChangeFlagsMsg : IEntityMessage
        {
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            public CharacterFlags Flags;

            public bool EnableJetpack { get { return (Flags & CharacterFlags.Jetpack) != 0; } }
            public bool EnableDampeners { get { return (Flags & CharacterFlags.Dampeners) != 0; } }
            public bool EnableLights { get { return (Flags & CharacterFlags.Lights) != 0; } }
            public bool EnableIronsight { get { return (Flags & CharacterFlags.Ironsight) != 0; } }
            public bool EnableBroadcast { get { return (Flags & CharacterFlags.Broadcast) != 0; } }
        }

        [MessageId(4, SteamSDK.P2PMessageEnum.Unreliable)]
        struct ChangeHeadOrSpineMsg : IEntityMessage
        {
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            public float HeadLocalXAngle;
            public float HeadLocalYAngle;

            public Quaternion AdditionalSpineRotation;
            public Quaternion AdditionalHeadRotation;
            public Quaternion AdditionalHandRotation;
            public Quaternion AdditionalUpperHandRotation;
        }

        [MessageId(4758, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct SwitchCharacterModelMsg : IEntityMessage
        {
            [ProtoMember]
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            [ProtoMember]
            public string Model;

            [ProtoMember]
            public Vector3 ColorMaskHSV;
        }

        [MessageId(7414, SteamSDK.P2PMessageEnum.Unreliable)]
        public struct CharacterInputMsg : IEntityMessage
        {
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            public Quaternion Orientation;
            public Vector3D Position;

            public ushort ClientFrameId;

            public Vector3 MoveIndicator;
            public Vector3 RotationIndicator;
            public MyCharacterMovementFlags MovementFlags;
        }

        [MessageId(7415, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct AnimationCommandMsg : IEntityMessage
        {
            [ProtoMember]
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            [ProtoMember]
            public string AnimationSubtypeName;
            [ProtoMember]
            public bool Loop;
            [ProtoMember]
            public MyPlayAnimationMode Mode;
            [ProtoMember]
            public MyBonesArea Area;
            [ProtoMember]
            public float BlendTime;
            [ProtoMember]
            public float TimeScale;
        }

        [MessageId(7416, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct AttachToCockpitMsg : IEntityMessage
        {
            [ProtoMember]
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            [ProtoMember]
            public long CockpitEntityId;
        }

        [MessageId(7417, P2PMessageEnum.Unreliable)]
        struct UpdateOxygenMsg : IEntityMessage
        {
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            public float OxygenAmount;
        }

        [MessageId(7418, P2PMessageEnum.Reliable)]
        struct RefillFromBottleMsg : IEntityMessage
        {
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }
        }

        [MessageId(7419, P2PMessageEnum.Unreliable)]
        protected struct PlaySecondarySoundMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public MyCueId SoundId;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.SoundId);
            }
        }

        [MessageId(7420, P2PMessageEnum.Unreliable)]
        [ProtoContract]
        struct RagdollTransformsMsg : IEntityMessage
        {
            [ProtoMember]
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            [ProtoMember]            
            public int TransformsCount;

            [ProtoMember]
            public Vector3[] transformsPositions;

            [ProtoMember]
            public Quaternion[] transformsOrientations;

            [ProtoMember]
            public Quaternion worldOrientation;

            [ProtoMember]
            public Vector3 worldPosition;
        }

        static MySyncCharacter()
        {
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, ChangeMovementStateMsg>(OnMovementStateChanged, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, SwitchCharacterModelMsg>(OnSwitchCharacterModel, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, ChangeFlagsMsg>(OnFlagsChanged, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, ChangeHeadOrSpineMsg>(OnHeadOrSpineChanged, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, CharacterInputMsg>(OnCharacterInput, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, AnimationCommandMsg>(OnAnimationCommand, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, AttachToCockpitMsg>(OnAttachToCockpit, MyMessagePermissions.FromServer);

            //Chat messages
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, SendPlayerMessageMsg>(OnPlayerMessageRequest, MyMessagePermissions.ToServer, Engine.Multiplayer.MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, SendPlayerMessageMsg>(OnPlayerMessageSuccess, MyMessagePermissions.FromServer, Engine.Multiplayer.MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncCharacter, SendNewFactionMessageMsg>(OnFactionMessageRequest, MyMessagePermissions.ToServer, Engine.Multiplayer.MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, SendNewFactionMessageMsg>(OnFactionMessageSuccess, MyMessagePermissions.FromServer, Engine.Multiplayer.MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncCharacter, SendGlobalMessageMsg>(OnGlobalMessageRequest, MyMessagePermissions.ToServer, Engine.Multiplayer.MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, SendGlobalMessageMsg>(OnGlobalMessageSuccess, MyMessagePermissions.FromServer, Engine.Multiplayer.MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncCharacter, ConfirmFactionMessageMsg>(OnConfirmFactionMessageSuccess, MyMessagePermissions.FromServer, Engine.Multiplayer.MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncCharacter, UpdateOxygenMsg>(OnUpdateOxygen, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncCharacter, RefillFromBottleMsg>(OnRefillFromBottle, MyMessagePermissions.FromServer);

            MySyncLayer.RegisterEntityMessage<MySyncCharacter, PlaySecondarySoundMsg>(OnSecondarySoundPlay, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);

            MySyncLayer.RegisterEntityMessage<MySyncCharacter, RagdollTransformsMsg>(OnRagdollTransformsUpdate, MyMessagePermissions.FromServer);
        }

        private static void OnRagdollTransformsUpdate(MySyncCharacter syncObject, ref RagdollTransformsMsg message, MyNetworkClient sender)
        {
            if (syncObject.Entity.Physics == null) return;
            if (syncObject.Entity.Physics.Ragdoll == null) return;
            if (syncObject.Entity.RagdollMapper == null) return;
            if (!syncObject.Entity.Physics.Ragdoll.IsAddedToWorld) return;
            if (!syncObject.Entity.RagdollMapper.IsActive) return;
            Debug.Assert(message.worldOrientation != null && message.worldOrientation != Quaternion.Zero, "Received invalid ragdoll orientation from server!");
            Debug.Assert(message.worldPosition != null && message.worldPosition != Vector3.Zero, "Received invalid ragdoll orientation from server!");
            Debug.Assert(message.transformsOrientations != null && message.transformsPositions != null, "Received empty ragdoll transformations from server!");
            Debug.Assert(message.transformsPositions.Count() == message.TransformsCount && message.transformsOrientations.Count() == message.TransformsCount, "Received ragdoll data count doesn't match!");
            Matrix worldMatrix = Matrix.CreateFromQuaternion(message.worldOrientation);
            worldMatrix.Translation = message.worldPosition;
            Matrix[] transforms = new Matrix[message.TransformsCount];
                        
            for (int i = 0; i < message.TransformsCount; ++i)
            {
                transforms[i] = Matrix.CreateFromQuaternion(message.transformsOrientations[i]);
                transforms[i].Translation = message.transformsPositions[i];
            }

            syncObject.Entity.RagdollMapper.UpdateRigidBodiesTransformsSynced(message.TransformsCount, worldMatrix, transforms);
        }

        public void SendRagdollTransforms(Matrix world, Matrix[] localBodiesTransforms)
        {
            if (ResponsibleForUpdate(this))
            {
                var msg = new RagdollTransformsMsg();
                msg.CharacterEntityId = Entity.EntityId;
                msg.worldPosition = world.Translation;
                msg.TransformsCount = localBodiesTransforms.Count();
                msg.worldOrientation = Quaternion.CreateFromRotationMatrix(world.GetOrientation());
                msg.transformsPositions = new Vector3[msg.TransformsCount];
                msg.transformsOrientations = new Quaternion[msg.TransformsCount];
                for (int i = 0; i < localBodiesTransforms.Count(); ++i )
                {
                    msg.transformsPositions[i] = localBodiesTransforms[i].Translation;
                    msg.transformsOrientations[i] = Quaternion.CreateFromRotationMatrix(localBodiesTransforms[i].GetOrientation());
                }
                Sync.Layer.SendMessageToAll(ref msg);
            }
        }


        private ChangeHeadOrSpineMsg m_headMsg;
        private bool m_headDirty = false;

        public event ChangeMovementStateDelegate MovementStateChanged;
        public event SwitchCharacterModelDelegate CharacterModelSwitched;
        public event ChangeFlagsDelegate FlagsChanged;
        public event ChangeHeadOrSpineDelegate HeadOrSpineChanged;
        public event DoDamageDelegate DoDamageHandler;

        public new MyCharacter Entity
        {
            get { return (MyCharacter)base.Entity; }
        }

        public MySyncCharacter(MyCharacter character)
            : base(character)
        {
        }

        public void ChangeMovementState(MyCharacterMovementEnum state)
        {
            if (!MyFakes.CHARACTER_SERVER_SYNC)
            {
                if (ResponsibleForUpdate(this))
                {
                    var msg = new ChangeMovementStateMsg();
                    msg.CharacterEntityId = Entity.EntityId;
                    msg.MovementState = state;

                    Sync.Layer.SendMessageToAll(ref msg);
                }
            }
        }

        private static void OnMovementStateChanged(MySyncCharacter sync, ref ChangeMovementStateMsg msg, MyNetworkClient sender)
        {
            if (sync.ResponsibleForUpdate(sender))
            {
                var handler = sync.MovementStateChanged;
                if (handler != null)
                    handler(msg.MovementState);
            }
        }


        public void SendAnimationCommand(ref MyAnimationCommand command)
        {
            var msg = new AnimationCommandMsg();
            msg.CharacterEntityId = Entity.EntityId;

            msg.AnimationSubtypeName = command.AnimationSubtypeName;
            msg.Loop = command.Loop;
            msg.Mode = command.Mode;
            msg.BlendTime = command.BlendTime;
            msg.TimeScale = command.TimeScale;

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnAnimationCommand(MySyncCharacter sync, ref AnimationCommandMsg msg, MyNetworkClient sender)
        {
            sync.Entity.AddCommand(new MyAnimationCommand()
            {
                AnimationSubtypeName = msg.AnimationSubtypeName,
                Mode = msg.Mode,
                Area = msg.Area,
                BlendTime = msg.BlendTime,
                TimeScale = msg.TimeScale,
                Loop = msg.Loop
            });

        }

        public void ChangeCharacterModelAndColor(string model, Vector3 colorMaskHSV)
        {
            if (ResponsibleForUpdate(this))
            {
                var msg               = new SwitchCharacterModelMsg();
                msg.CharacterEntityId = Entity.EntityId;
                msg.Model             = model;
                msg.ColorMaskHSV      = colorMaskHSV;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }
        }

        private static void OnSwitchCharacterModel(MySyncCharacter sync, ref SwitchCharacterModelMsg msg, MyNetworkClient sender)
        {
            if (sync.ResponsibleForUpdate(sender))
            {
                var handler = sync.CharacterModelSwitched;
                if (handler != null)
                    handler(msg.Model, msg.ColorMaskHSV);
            }
        }

        public void ChangeFlags(bool enableJetpack, bool enableDampeners, bool enableLights, bool enableIronsight, bool enableBroadcast)
        {
            if (ResponsibleForUpdate(this))
            {
                var msg = new ChangeFlagsMsg();
                msg.CharacterEntityId = Entity.EntityId;
                msg.Flags = 0;
                msg.Flags |= enableJetpack ? CharacterFlags.Jetpack : 0;
                msg.Flags |= enableDampeners ? CharacterFlags.Dampeners : 0;
                msg.Flags |= enableLights ? CharacterFlags.Lights : 0;
                msg.Flags |= enableIronsight ? CharacterFlags.Ironsight : 0;
                msg.Flags |= enableBroadcast ? CharacterFlags.Broadcast : 0;

                Sync.Layer.SendMessageToAll(ref msg);
            }
            else
                if (MyFakes.CHARACTER_SERVER_SYNC && !Sync.IsServer)
                {
                    var msg = new ChangeFlagsMsg();
                    msg.CharacterEntityId = Entity.EntityId;
                    msg.Flags = 0;
                    msg.Flags |= enableJetpack ? CharacterFlags.Jetpack : 0;
                    msg.Flags |= enableDampeners ? CharacterFlags.Dampeners : 0;
                    msg.Flags |= enableLights ? CharacterFlags.Lights : 0;
                    msg.Flags |= enableIronsight ? CharacterFlags.Ironsight : 0;
                    msg.Flags |= enableBroadcast ? CharacterFlags.Broadcast : 0;

                    Sync.Layer.SendMessageToServer(ref msg);
                }
        }

        private static void OnFlagsChanged(MySyncCharacter sync, ref ChangeFlagsMsg msg, MyNetworkClient sender)
        {
            if (MyFakes.CHARACTER_SERVER_SYNC)
            {
                var handler = sync.FlagsChanged;
                if (handler != null)
                    handler(msg.EnableJetpack, msg.EnableDampeners, msg.EnableLights, msg.EnableIronsight, msg.EnableBroadcast);

                if (Sync.IsServer)
                    Sync.Layer.SendMessageToAll(ref msg);
            }
            else
            if (sync.ResponsibleForUpdate(sender))
            {
                var handler = sync.FlagsChanged;
                if (handler != null)
                    handler(msg.EnableJetpack, msg.EnableDampeners, msg.EnableLights, msg.EnableIronsight, msg.EnableBroadcast);
            }
        }

        public void ChangeHeadOrSpine(float headLocalXAngle, float headLocalYAngle, Quaternion spineRotation,
    Quaternion headRotation, Quaternion handRotation, Quaternion upperHandRotation)
        {
            if (ResponsibleForUpdate(this))
            {
                m_headMsg.CharacterEntityId = Entity.EntityId;
                m_headMsg.HeadLocalXAngle = headLocalXAngle;
                m_headMsg.HeadLocalYAngle = headLocalYAngle;
                m_headMsg.AdditionalSpineRotation = spineRotation != Quaternion.Zero ? spineRotation : m_headMsg.AdditionalSpineRotation;
                m_headMsg.AdditionalHeadRotation = headRotation;
                m_headMsg.AdditionalHandRotation = handRotation;
                m_headMsg.AdditionalUpperHandRotation = upperHandRotation;

                m_headDirty = true;
                if (MyMultiplayer.Static != null)
                {
                    MyMultiplayer.Static.RegisterForTick(this);
                }
            }
        }

        private void SendHeadMsg()
        {
            if (MyMultiplayer.Static != null && MyMultiplayer.Static.FrameCounter - m_lastUpdateFrame > m_updateFrameCount)
            {
                ResetUpdateTimer();
                Sync.Layer.SendMessageToAll(ref m_headMsg);
                m_headDirty = false;
            }
            else if (MyMultiplayer.Static != null)
            {
                MyMultiplayer.Static.RegisterForTick(this);
            }
        }

        private void ResetUpdateTimer()
        {
            if (MyMultiplayer.Static != null)
            {
                m_lastUpdateFrame = MyMultiplayer.Static.FrameCounter;
            }
        }

        public override void Tick()
        {
            base.Tick();

            if (!Entity.MarkedForClose && m_headDirty)
            {
                SendHeadMsg();
            }
        }

        private static void OnCharacterInput(MySyncCharacter sync, ref CharacterInputMsg msg, MyNetworkClient sender)
        {
            sender.ClientFrameId = msg.ClientFrameId;
            sync.CachedMovementState = msg;

            var matrix = MatrixD.CreateFromQuaternion(msg.Orientation);
            matrix.Translation = msg.Position;
            sync.Entity.WorldMatrix = matrix;

            if (Sync.IsServer)
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);            
        }


        public CharacterInputMsg CachedMovementState = new CharacterInputMsg();

        public void MoveAndRotate(Vector3 moveIndicator, Vector3 rotationIndicator, MyCharacterMovementFlags movementFlags)
        {
            bool changed = 
                CachedMovementState.MoveIndicator != moveIndicator ||
                CachedMovementState.RotationIndicator != rotationIndicator ||
                CachedMovementState.MovementFlags != movementFlags;

            if (changed)
            {
                CharacterInputMsg msg = new CharacterInputMsg();
                msg.CharacterEntityId = Entity.EntityId;
                msg.MoveIndicator = moveIndicator;
                msg.RotationIndicator = rotationIndicator;
                msg.MovementFlags = movementFlags;
                msg.Orientation = Quaternion.CreateFromRotationMatrix(Entity.WorldMatrix);
                msg.Position = Entity.WorldMatrix.Translation;
                CachedMovementState = msg;

                if (!Sync.IsServer)
                    Sync.Layer.SendMessageToServer(ref msg);
                else
                    Sync.Layer.SendMessageToAll(ref msg);            
            }
        }

        private static void OnHeadOrSpineChanged(MySyncCharacter sync, ref ChangeHeadOrSpineMsg msg, MyNetworkClient sender)
        {
            if (sync.ResponsibleForUpdate(sender))
            {
                var handler = sync.HeadOrSpineChanged;
                if (handler != null)
                    handler(msg.HeadLocalXAngle, msg.HeadLocalYAngle,
                        msg.AdditionalSpineRotation, msg.AdditionalHeadRotation, msg.AdditionalHandRotation, msg.AdditionalUpperHandRotation);
            }
        }

        

        #region Respawn 

        public static void SendCharacterCreated(MyObjectBuilder_Character character, MyCockpit cockpit)
        {
            MySyncCreate.SendEntityCreated(character);

            AttachToCockpitMsg msg = new AttachToCockpitMsg();
            msg.CharacterEntityId = character.EntityId;
            msg.CockpitEntityId = cockpit.EntityId;

            Sync.Layer.SendMessageToAll(msg);
        }

        static void OnAttachToCockpit(MySyncCharacter sync, ref AttachToCockpitMsg msg, MyNetworkClient sender)
        {
            MyCharacter character = sync.Entity;
            MyCockpit cockpit = MyEntities.GetEntityById(msg.CockpitEntityId) as MyCockpit;
            Debug.Assert(cockpit != null);
            if (cockpit == null) return;

            cockpit.AttachPilot(character, false);
        }

        #endregion

        #region Chat
        [ProtoContract]
        [MessageId(7620, P2PMessageEnum.Reliable)]
        struct SendPlayerMessageMsg : IEntityMessage
        {
            [ProtoMember]
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            [ProtoMember]
            public ulong SenderSteamId;
            [ProtoMember]
            public ulong ReceiverSteamId;
            [ProtoMember]
            public string Text;
            [ProtoMember]
            public long Timestamp;
        }

        [ProtoContract]
        [MessageId(7621, P2PMessageEnum.Reliable)]
        struct SendNewFactionMessageMsg : IEntityMessage
        {
            [ProtoMember]
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            [ProtoMember]
            public long FactionId1;
            [ProtoMember]
            public long FactionId2;
            [ProtoMember]
            public MyObjectBuilder_FactionChatItem ChatItem;
        }

        [ProtoContract]
        [MessageId(7623, P2PMessageEnum.Reliable)]
        struct ConfirmFactionMessageMsg : IEntityMessage
        {
            [ProtoMember]
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }
         
            [ProtoMember]
            public long FactionId1;
            [ProtoMember]
            public long FactionId2;
            [ProtoMember]
            public long OriginalSenderId;
            [ProtoMember]
            public long ReceiverId;
            [ProtoMember]
            public long Timestamp;
            [ProtoMember]
            public string Text;
        }

        [ProtoContract]
        [MessageId(7624, P2PMessageEnum.Reliable)]
        struct SendGlobalMessageMsg : IEntityMessage
        {
            [ProtoMember]
            public long CharacterEntityId;
            public long GetEntityId() { return CharacterEntityId; }

            [ProtoMember]
            public ulong SenderSteamId;
            [ProtoMember]
            public string Text;
        }

        #region Player
        public void SendNewPlayerMessage(MyPlayer.PlayerId senderId, MyPlayer.PlayerId receiverId, string text, TimeSpan timestamp)
        {
            var msg = new SendPlayerMessageMsg();
            msg.CharacterEntityId = Entity.EntityId;
            msg.SenderSteamId = senderId.SteamId;
            msg.ReceiverSteamId = receiverId.SteamId;
            msg.Text = text;
            msg.Timestamp = timestamp.Ticks;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        public bool CheckPlayerConnection(ulong senderSteamId, ulong receiverSteamId)
        {
            var receiverId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(receiverSteamId));
            var senderId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(senderSteamId));

            return (receiverId != null && receiverId.Character != null && senderId != null && senderId.Character != null && MyAntennaSystem.CheckConnection(senderId, receiverId));
        }

        private static void OnPlayerMessageRequest(MySyncCharacter sync, ref SendPlayerMessageMsg msg, MyNetworkClient sender)
        {
            //Ignore messages that have improper lengths
            if (msg.Text.Length == 0 || msg.Text.Length > MyChatConstants.MAX_CHAT_STRING_LENGTH)
            {
                return;
            }
            
            var receiverId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(msg.ReceiverSteamId));
            var senderId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(msg.SenderSteamId));

            //TODO(AF) Check if message was already received

            if (receiverId != null && receiverId.Character != null && senderId != null && senderId.Character != null && MyAntennaSystem.CheckConnection(senderId, receiverId))
            {
                Sync.Layer.SendMessage(ref msg, msg.ReceiverSteamId, MyTransportMessageEnum.Success);
                Sync.Layer.SendMessage(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);

                //Save chat history on server for non-server players
                if (receiverId.Character != MySession.LocalCharacter)
                {
                    MyChatSystem.AddPlayerChatItem(receiverId.IdentityId, senderId.IdentityId, new MyPlayerChatItem(msg.Text, senderId.IdentityId, msg.Timestamp, true));
                }
                if (senderId.Character != MySession.LocalCharacter)
                {
                    MyChatSystem.AddPlayerChatItem(senderId.IdentityId, receiverId.IdentityId, new MyPlayerChatItem(msg.Text, senderId.IdentityId, msg.Timestamp, true));
                }
            }
        }

        private static void OnPlayerMessageSuccess(MySyncCharacter sync, ref SendPlayerMessageMsg msg, MyNetworkClient sender)
        {
            var receiverId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(msg.ReceiverSteamId));
            var senderId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(msg.SenderSteamId));

            if (receiverId != null && receiverId.Character != null && senderId != null && senderId.Character != null)
            {
                if (receiverId.Character == MySession.LocalCharacter && receiverId.Character != senderId.Character)
                {
                    MyChatSystem.AddPlayerChatItem(receiverId.IdentityId, senderId.IdentityId, new MyPlayerChatItem(msg.Text, senderId.IdentityId, msg.Timestamp, true));
                    MySession.Static.ChatSystem.OnNewPlayerMessage(senderId.IdentityId, senderId.IdentityId);

                    MySession.Static.Gpss.ScanText(msg.Text, MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_NewFromPrivateComms));
                }
                else
                {
                    MyChatSystem.SetPlayerChatItemSent(senderId.IdentityId, receiverId.IdentityId, msg.Text, new TimeSpan(msg.Timestamp), true);
                    MySession.Static.ChatSystem.OnNewPlayerMessage(receiverId.IdentityId, senderId.IdentityId);
                }
            }
        }
        #endregion

        #region Faction
        private static HashSet<long> m_tempValidIds = new HashSet<long>();

        public void SendNewFactionMessage(long factionId1, long factionId2, MyFactionChatItem chatItem)
        {
            var msg = new SendNewFactionMessageMsg();
            
            msg.CharacterEntityId = Entity.EntityId;
            msg.FactionId1 = factionId1;
            msg.FactionId2 = factionId2;
            msg.ChatItem = chatItem.GetObjectBuilder();
            
            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        private static MyFactionChatItem FindFactionChatItem(long playerId, long factionId1, long factionId2, TimeSpan timestamp, string text)
        {
            var factionChat = MyChatSystem.FindFactionChatHistory(factionId1, factionId2);
            if (factionChat != null)
            {
                foreach (var factionChatItem in factionChat.Chat)
                {
                    if (factionChatItem.Timestamp == timestamp && factionChatItem.Text == text)
                    {
                        return factionChatItem;
                    }
                }
            }
            
            return null;
        }

        private static void SendConfirmMessageToFaction(long factionId, Dictionary<long, bool> PlayersToSendTo, ref ConfirmFactionMessageMsg confirmMessage)
        {
            var receiverFaction = MySession.Static.Factions.TryGetFactionById(factionId);
            foreach (var member in receiverFaction.Members)
            {
                MyPlayer.PlayerId playerId;
                bool sendToMember = false;
                if (PlayersToSendTo.TryGetValue(member.Key, out sendToMember))
                {
                    if (MySession.Static.Players.TryGetPlayerId(member.Value.PlayerId, out playerId) && sendToMember)
                    {
                        Sync.Layer.SendMessage(confirmMessage, playerId.SteamId, MyTransportMessageEnum.Success);

                        //Save chat history on server for non-server players
                        if (member.Value.PlayerId != MySession.LocalPlayerId)
                        {
                            ConfirmMessage(ref confirmMessage, member.Value.PlayerId);
                        }
                    }
                }
            }
        }

        private static void OnFactionMessageRequest(MySyncCharacter sync, ref SendNewFactionMessageMsg msg, MyNetworkClient sender)
        {
            //Ignore messages that have improper lengths
            if (msg.ChatItem.Text.Length == 0 || msg.ChatItem.Text.Length > MyChatConstants.MAX_CHAT_STRING_LENGTH)
            {
                return;
            }

            long currentSenderId = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, msg.ChatItem.IdentityIdUniqueNumber);
            var senderId = MySession.Static.Players.TryGetIdentity(currentSenderId);

            var chatItem = new MyFactionChatItem();
            chatItem.Init(msg.ChatItem);

            //Find all members that can receive this messages
            m_tempValidIds.Clear();
            for (int i = 0; i < msg.ChatItem.PlayersToSendToUniqueNumber.Count; i++)
            {
                if (!msg.ChatItem.IsAlreadySentTo[i])
                {
                    long receiverIdentityId = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, msg.ChatItem.PlayersToSendToUniqueNumber[i]);
                    var receiverId = MySession.Static.Players.TryGetIdentity(receiverIdentityId);
                    if (receiverId != null && receiverId.Character != null && MyAntennaSystem.CheckConnection(senderId, receiverId))
                    {
                        m_tempValidIds.Add(receiverIdentityId);
                    }
                }
            }

            //Set their sent flag to true, so that everyone knows they already got it (no need for confirm message)
            foreach (var id in m_tempValidIds)
            {
                chatItem.PlayersToSendTo[id] = true;
            }

            //Save the flags back in the message
            msg.ChatItem = chatItem.GetObjectBuilder();

            //Send success message back to all recepient members
            foreach (var id in m_tempValidIds)
            {
                MyPlayer.PlayerId receiverPlayerId;
                MySession.Static.Players.TryGetPlayerId(id, out receiverPlayerId);
                ulong steamId = receiverPlayerId.SteamId;
                
                Sync.Layer.SendMessage(ref msg, steamId, MyTransportMessageEnum.Success);
            }

            //Save chat history on server for non-server players
            if (senderId.Character != MySession.LocalCharacter)
            {
                MyChatSystem.AddFactionChatItem(senderId.IdentityId, msg.FactionId1, msg.FactionId2, chatItem);
            }
        }

        private static void OnFactionMessageSuccess(MySyncCharacter sync, ref SendNewFactionMessageMsg msg, MyNetworkClient sender)
        {
            long senderIdentityId = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, msg.ChatItem.IdentityIdUniqueNumber);
            
            var factionChatItem = new MyFactionChatItem();
            factionChatItem.Init(msg.ChatItem);
            if (!(Sync.IsServer && senderIdentityId != MySession.LocalPlayerId))
            {
                MyChatSystem.AddFactionChatItem(MySession.LocalPlayerId, msg.FactionId1, msg.FactionId2, factionChatItem);
            }
            if (senderIdentityId != MySession.LocalPlayerId)
            {
                MySession.Static.Gpss.ScanText(factionChatItem.Text, MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_NewFromFactionComms));
            }
            MySession.Static.ChatSystem.OnNewFactionMessage(msg.FactionId1, msg.FactionId2, senderIdentityId, true);
        }

        public static bool RetryFactionMessage(long factionId1, long factionId2, MyFactionChatItem chatItem, MyIdentity currentSenderIdentity)
        {
            Debug.Assert(Sync.IsServer, "Faction message retries should only be done on server");

            if (currentSenderIdentity.Character == null)
            {
                return false;
            }

            m_tempValidIds.Clear();
            foreach (var playerToSendTo in chatItem.PlayersToSendTo)
            {
                if (!playerToSendTo.Value)
                {
                    long receiverIdentityId = playerToSendTo.Key;
                    var receiverId = MySession.Static.Players.TryGetIdentity(receiverIdentityId);
                    if (receiverId != null && receiverId.Character != null && MyAntennaSystem.CheckConnection(currentSenderIdentity, receiverId))
                    {
                        m_tempValidIds.Add(receiverIdentityId);
                    }
                }
            }

            if (m_tempValidIds.Count == 0)
            {
                return false;
            }

            foreach (var id in m_tempValidIds)
            {
                chatItem.PlayersToSendTo[id] = true;
            }

            var msg = new SendNewFactionMessageMsg();

            msg.FactionId1 = factionId1;
            msg.FactionId2 = factionId2;
            msg.CharacterEntityId = currentSenderIdentity.Character.EntityId;
            msg.ChatItem = chatItem.GetObjectBuilder();

            var confirmMessage = new ConfirmFactionMessageMsg();
            confirmMessage.CharacterEntityId = currentSenderIdentity.Character.EntityId;

            confirmMessage.FactionId1 = msg.FactionId1;
            confirmMessage.FactionId2 = msg.FactionId2;
            confirmMessage.OriginalSenderId = chatItem.IdentityId;
            confirmMessage.Timestamp = chatItem.Timestamp.Ticks;
            confirmMessage.Text = msg.ChatItem.Text;

            foreach (var id in m_tempValidIds)
            {
                MyPlayer.PlayerId receiverPlayerId;
                MySession.Static.Players.TryGetPlayerId(id, out receiverPlayerId);
                ulong steamId = receiverPlayerId.SteamId;

                Sync.Layer.SendMessage(ref msg, steamId, MyTransportMessageEnum.Success);
            }
            foreach (var id in m_tempValidIds)
            {
                confirmMessage.ReceiverId = id;

                //Send confimation to members of both factions
                SendConfirmMessageToFaction(confirmMessage.FactionId1, chatItem.PlayersToSendTo, ref confirmMessage);
                if (confirmMessage.FactionId1 != confirmMessage.FactionId2)
                {
                    SendConfirmMessageToFaction(confirmMessage.FactionId2, chatItem.PlayersToSendTo, ref confirmMessage);
                }
            }

            return true;
        }

        private static void OnConfirmFactionMessageSuccess(MySyncCharacter syncObject, ref ConfirmFactionMessageMsg message, MyNetworkClient sender)
        {
            ConfirmMessage(ref message, MySession.LocalPlayerId);
        }

        private static void ConfirmMessage(ref ConfirmFactionMessageMsg message, long localPlayerId)
        {
            MyChatHistory chatHistory;
            if (!MySession.Static.ChatHistory.TryGetValue(localPlayerId, out chatHistory))
            {
                chatHistory = new MyChatHistory(localPlayerId);
            }

            var timestamp = new TimeSpan(message.Timestamp);
            var chatItem = FindFactionChatItem(localPlayerId, message.FactionId1, message.FactionId2, timestamp, message.Text);
            if (chatItem != null)
            {
                chatItem.PlayersToSendTo[message.ReceiverId] = true;
                if (!MySandboxGame.IsDedicated)
                {
                    MySession.Static.ChatSystem.OnNewFactionMessage(message.FactionId1, message.FactionId2, message.OriginalSenderId, false);
                }
            }
            else
            {
                Debug.Fail("Could not find faction chat history between faction " + message.FactionId1 + " and " + message.FactionId2);
            }
        }
        #endregion

        #region Global
        public void SendNewGlobalMessage(MyPlayer.PlayerId senderId, string text)
        {
            var msg = new SendGlobalMessageMsg();
            msg.CharacterEntityId = Entity.EntityId;
            msg.SenderSteamId = senderId.SteamId;
            msg.Text = text;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        private static void OnGlobalMessageRequest(MySyncCharacter sync, ref SendGlobalMessageMsg msg, MyNetworkClient sender)
        {
            //Ignore messages that have improper lengths
            if (msg.Text.Length == 0 || msg.Text.Length > MyChatConstants.MAX_CHAT_STRING_LENGTH)
            {
                return;
            }

            var senderId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(msg.SenderSteamId));
            var allPlayers = MySession.Static.Players.GetOnlinePlayers();
            foreach (var player in allPlayers)
            {
                var receiverId = player.Identity;
                if (receiverId != null && receiverId.Character != null && senderId != null && senderId.Character != null && MyAntennaSystem.CheckConnection(senderId, receiverId))
                {
                    Sync.Layer.SendMessage(ref msg, player.Id.SteamId, MyTransportMessageEnum.Success);

                    //Save chat history on server for non-server players
                    if (receiverId.Character != MySession.LocalCharacter)
                    {
                        MyChatSystem.AddGlobalChatItem(player.Identity.IdentityId, new MyGlobalChatItem(msg.Text, senderId.IdentityId));
                    }
                }
            }
        }

        private static void OnGlobalMessageSuccess(MySyncCharacter sync, ref SendGlobalMessageMsg msg, MyNetworkClient sender)
        {
            var senderId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(msg.SenderSteamId));
            if (MySession.LocalCharacter != null)
            {
                MyChatSystem.AddGlobalChatItem(MySession.LocalPlayerId, new MyGlobalChatItem(msg.Text, senderId.IdentityId));
                MySession.Static.ChatSystem.OnNewGlobalMessage(senderId.IdentityId);

                if (MySession.LocalPlayerId != senderId.IdentityId)
                {
                    MySession.Static.Gpss.ScanText(msg.Text, MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_NewFromBroadcast));
                }
            }
        }
        #endregion

        #endregion

        public void UpdateOxygen(float oxygenAmount)
        {
            var msg = new UpdateOxygenMsg();
            msg.CharacterEntityId = Entity.EntityId;
            msg.OxygenAmount = oxygenAmount;

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnUpdateOxygen(MySyncCharacter syncObject, ref UpdateOxygenMsg message, MyNetworkClient sender)
        {
            syncObject.Entity.SuitOxygenAmount = message.OxygenAmount;
        }

        public void SendRefillFromBottle()
        {
            Debug.Assert(Sync.IsServer);

            var msg = new RefillFromBottleMsg();
            msg.CharacterEntityId = Entity.EntityId;
            
            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnRefillFromBottle(MySyncCharacter syncObject, ref RefillFromBottleMsg message, MyNetworkClient sender)
        {
            if (syncObject.Entity == MySession.LocalCharacter)
            {
                syncObject.Entity.ShowRefillFromBottleNotification();
            }
        }

        internal void PlaySecondarySound(MyCueId soundId)
        {
            var msg = new PlaySecondarySoundMsg()
            {
                EntityId = this.SyncedEntityId,
                SoundId = soundId,
            };

            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAll(ref msg);
            }
            else
            {
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }

        private static void OnSecondarySoundPlay(MySyncCharacter syncObject, ref PlaySecondarySoundMsg msg, MyNetworkClient sender)
        {
            if (!MySandboxGame.IsDedicated)
            {
                syncObject.Entity.StartSecondarySound(msg.SoundId, sync: false);
            }

            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }
    }
}
