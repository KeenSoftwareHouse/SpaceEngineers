using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Responsible for synchronizing entity physics over network
    /// </summary>
    class MyCharacterPhysicsStateGroup : MyEntityPhysicsStateGroup
    {
        PrioritySettings m_highQuality = new PrioritySettings()
        {
            // Send position updates for close characters every other frame
            AcceleratingUpdateCount = 2,
            LinearMovingUpdateCount = 2,
            LinearMovingPriority = 1.0f,
        };

        public new MyCharacter Entity { get { return (MyCharacter)base.Entity; } }

        public MyCharacterPhysicsStateGroup(MyEntity entity, IMyReplicable ownerReplicable)
            : base(entity, ownerReplicable)
        {
        }

        protected override float GetGroupPriority(int frameCountWithoutSync, MyClientStateBase client, PrioritySettings settings)
        {
            const float HighQualityDistance = 8; // under 8m, character physics sync gets high priority to have smooth movement

            var clientPos = ((MyClientState)client).Position;
            var characterPos = Entity.PositionComp.GetPosition();
            bool isHighQuality = Vector3D.DistanceSquared(clientPos, characterPos) < HighQualityDistance * HighQualityDistance;
            isHighQuality = isHighQuality && !Entity.IsDead;

            return base.GetGroupPriority(frameCountWithoutSync, client, isHighQuality ? m_highQuality : settings);
        }
    }
}
