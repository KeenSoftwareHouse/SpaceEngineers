using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRageMath;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// Currently used to inject new state read from character state group to MyCharacter
    /// </summary>
    public struct MyCharacterNetState
    {
        public float HeadX;
        public float HeadY;
        public Quaternion Spine;
        public Quaternion Head;
        public MyCharacterMovementEnum MovementState;
        public MyCharacterMovementFlags MovementFlag;
        public bool Jetpack;
        public bool Dampeners;
        public bool Lights;
        public bool Ironsight;
        public bool Broadcast;
        public bool TargetFromCamera;
        public Vector3 Movement;
        public Vector2 Rotation;
        public float Roll;
        public float Speed;
    }
}
