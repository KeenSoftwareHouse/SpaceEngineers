using System;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using VRage.Library.Collections;
using VRageMath;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// Currently used to inject new state read from character state group to MyCharacter
    /// </summary>
    public struct MyCharacterNetState
    {
        public bool Valid;
        public float HeadX;
        public float HeadY;
        public MyCharacterMovementEnum MovementState;
        public MyCharacterMovementFlags MovementFlags;
        public bool Jetpack;
        public bool Dampeners;
        public bool TargetFromCamera;
        public Vector3 MoveIndicator;
        public Quaternion Rotation;

        public MyCharacterNetState(BitStream stream)
        {
            HeadX = stream.ReadHalf();
            if (HeadX.IsValid() == false)
            {
                HeadX = 0.0f;
            }

            HeadY = stream.ReadHalf();
            MovementState = (MyCharacterMovementEnum)stream.ReadUInt16();
            MovementFlags = (MyCharacterMovementFlags)stream.ReadUInt16();
            Jetpack = stream.ReadBool();
            Dampeners = stream.ReadBool();
            TargetFromCamera = stream.ReadBool();
            MoveIndicator = stream.ReadNormalizedSignedVector3(8);
            Rotation = stream.ReadQuaternion();
            Valid = true;
        }

        public void Serialize(BitStream stream)
        {
            stream.WriteHalf(HeadX); // 2B
            stream.WriteHalf(HeadY); // 2B

            stream.WriteUInt16((ushort)MovementState);
            stream.WriteUInt16((ushort)MovementFlags);

            stream.WriteBool(Jetpack);
            stream.WriteBool(Dampeners);
            stream.WriteBool(TargetFromCamera);

            stream.WriteNormalizedSignedVector3(MoveIndicator, 8);

            stream.WriteQuaternion(Rotation);
        }
    }
}
