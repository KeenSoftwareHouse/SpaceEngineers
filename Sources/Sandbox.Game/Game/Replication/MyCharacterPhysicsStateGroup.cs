using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.ModAPI;
using VRage.Network;
using VRageMath;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// Responsible for synchronizing entity physics over network
    /// </summary>
    public class MyCharacterPhysicsStateGroup : MyEntityPhysicsStateGroupWithSupport
    {
        PrioritySettings m_highQuality = new PrioritySettings()
        {
            // Send position updates for close characters every other frame
            AcceleratingUpdateCount = 2,
            LinearMovingUpdateCount = 2,
            LinearMovingPriority = 1.0f,
        };


        float m_previousRotationSpeed = 0.0f;

        override protected bool IsMoving(MyEntity entity)
        {
            // Never know if somebody is moving entity when physics is null
            return Entity.Physics == null
                || Vector3.IsZero(entity.Physics.LinearVelocity, PRECISION) == false
                || Vector2.IsZero(Entity.RotationIndicator, PRECISION) == false 
                || Math.Abs(Entity.RollIndicator - 0.0f) > 0.001f;
        }

        public new MyCharacter Entity { get { return (MyCharacter)base.Entity; } }

        public MyCharacterPhysicsStateGroup(MyEntity entity, IMyReplicable ownerReplicable)
            : base(entity, ownerReplicable)
        {
            // This is 9 bits per component which is more than enough (512 discrete values per-axis)
            m_lowPrecisionOrientation = true;
            FindSupportDelegate = () => MySupportHelper.FindSupportForCharacter(Entity);
        }

        public override MyPlayer GetControllingPlayer()
        {
            var player = base.GetControllingPlayer();
            if (player == null && Sync.Players != null && Entity.CurrentRemoteControl != null)
            {
                // This was originally in MySyncEntity by Alex Florea
                player = Sync.Players.GetControllingPlayer(Entity.CurrentRemoteControl as MyEntity);
            }
            return player;
        }

        protected override float GetGroupPriority(int frameCountWithoutSync, MyClientInfo client, PrioritySettings settings)
        {
            const float HighQualityDistance = 20; // under 8m, character physics sync gets high priority to have smooth movement

            if (ResponsibleForUpdate(Entity, client.EndpointId))
            {
                return 0.0f;
            }

            var clientPos = ((MyClientState)client.State).Position;
            var characterPos = Entity.PositionComp.GetPosition();
            bool isHighQuality = Vector3D.DistanceSquared(clientPos, characterPos) < HighQualityDistance * HighQualityDistance;

            var priority = base.GetGroupPriority(frameCountWithoutSync, client, isHighQuality ? m_highQuality : settings);
            return priority;
        }

        public override bool Serialize(BitStream stream, EndpointId forClient,uint timeStamp, byte packetId, int maxBitPosition)
        {
            base.Serialize(stream, forClient,timeStamp, packetId, maxBitPosition);

            if (stream.Writing)
            {
                // Head and spine stuff, 36 - 152b (4.5B - 19 B)
                stream.WriteHalf(Entity.HeadLocalXAngle); // 2B
                stream.WriteHalf(Entity.HeadLocalYAngle); // 2B

                // TODO: Spine has only one angle (bending forward backward)
                // Getting EULER angles from Matrix seems good way to get it (z-component)
                stream.WriteQuaternionNormCompressedIdentity(Entity.GetAdditionalRotation(Entity.Definition.SpineBone)); // 1b / 30b
                stream.WriteQuaternionNormCompressedIdentity(Entity.GetAdditionalRotation(Entity.Definition.HeadBone)); // 1b / 30b

                // Movement state, 2B
                stream.WriteUInt16((ushort)Entity.GetCurrentMovementState());
                // Movement flag.
                stream.WriteUInt16((ushort)Entity.MovementFlags);

                // Flags, 6 bits
                bool hasJetpack = Entity.JetpackComp != null;
                stream.WriteBool(hasJetpack ? Entity.JetpackComp.TurnedOn : false);
                stream.WriteBool(hasJetpack ? Entity.JetpackComp.DampenersTurnedOn : false);
                stream.WriteBool(Entity.LightEnabled); // TODO: Remove
                stream.WriteBool(Entity.ZoomMode == MyZoomModeEnum.IronSight);
                stream.WriteBool(Entity.RadioBroadcaster.WantsToBeEnabled); // TODO: Remove
                stream.WriteBool(Entity.TargetFromCamera);

                stream.WriteNormalizedSignedVector3(Entity.MoveIndicator, 8);

                float speed = Entity.Physics.CharacterProxy != null ? Entity.Physics.CharacterProxy.Speed : 0.0f;
                stream.WriteFloat(speed);

                stream.WriteFloat(Entity.RotationIndicator.X);
                stream.WriteFloat(Entity.RotationIndicator.Y);
                stream.WriteFloat(Entity.RollIndicator);

            }
            else
            {
                Vector3 move;
                MyCharacterNetState charNetState = ReadCharacterState(stream);

                if (!IsControlledLocally && !Entity.Closed)
                {
                   Entity.SetStateFromNetwork(ref charNetState);
                }             
            }

            m_previousRotationSpeed = Entity.RotationSpeed;
            return true;
        }

        public static void ReadAndSkipCharacterState(BitStream stream)
        {
            ReadCharacterState(stream);
        }

        static MyCharacterNetState ReadCharacterState(BitStream stream)
        {
            MyCharacterNetState charNetState = new MyCharacterNetState();

            charNetState.HeadX = stream.ReadHalf();
            if (charNetState.HeadX.IsValid() == false)
            {
                charNetState.HeadX = 0.0f;
            }

            charNetState.HeadY = stream.ReadHalf();
            charNetState.Spine = stream.ReadQuaternionNormCompressedIdentity();
            charNetState.Head = stream.ReadQuaternionNormCompressedIdentity();
            charNetState.MovementState = (MyCharacterMovementEnum)stream.ReadUInt16();
            charNetState.MovementFlag = (MyCharacterMovementFlags)stream.ReadUInt16();
            charNetState.Jetpack = stream.ReadBool();
            charNetState.Dampeners = stream.ReadBool();
            charNetState.Lights = stream.ReadBool(); // TODO: Remove
            charNetState.Ironsight = stream.ReadBool();
            charNetState.Broadcast = stream.ReadBool(); // TODO: Remove
            charNetState.TargetFromCamera = stream.ReadBool();
            charNetState.Movement = stream.ReadNormalizedSignedVector3(8);
            charNetState.Rotation.X = stream.ReadFloat();
            charNetState.Rotation.Y = stream.ReadFloat();
            charNetState.Speed = stream.ReadFloat();
            charNetState.Roll = stream.ReadFloat();

            return charNetState;
        }
    }
}
