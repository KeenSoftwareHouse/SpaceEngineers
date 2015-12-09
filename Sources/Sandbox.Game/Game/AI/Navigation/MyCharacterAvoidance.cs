using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.Navigation
{
    public class MyCharacterAvoidance : MySteeringBase
    {
        private Vector3D m_debugDirection = Vector3D.Forward;

        public MyCharacterAvoidance(MyBotNavigation botNavigation, float weight)
            : base(botNavigation, weight)
        {
        }

        public override void AccumulateCorrection(ref Vector3 correction, ref float weight)
        {
            if (Parent.Speed < 0.01f)
                return;

            var characterBotEntity = Parent.BotEntity as MyCharacter; // remove me pls
            if (characterBotEntity == null)
                return;

            var position = Parent.PositionAndOrientation.Translation;
            BoundingBoxD box = new BoundingBoxD(position - Vector3D.One * 3, position + Vector3D.One * 3);
            Vector3D currentMovement = Parent.ForwardVector;

            var entities = MyEntities.GetEntitiesInAABB(ref box);
            foreach (var entity in entities)
            {
                var character = entity as MyCharacter;
                if (character == null || character == characterBotEntity)
                    continue;
                if (character.ModelName == characterBotEntity.ModelName) // remove me pls
                    continue;

                Vector3D characterPos = character.PositionComp.GetPosition();
                Vector3D dir = characterPos - position;
                double dist = dir.Normalize();
                dist = MathHelper.Clamp(dist, 0, 6);
                var cos = Vector3D.Dot(dir, currentMovement);

                var opposite = -dir;
                if (cos > -0.807)
                    correction += (6f - dist) * Weight * opposite;

                if (!correction.IsValid())
                {
                    System.Diagnostics.Debugger.Break();
                }

            }
            entities.Clear();
            weight += Weight;
        }

        public override void DebugDraw()
        {
        }

        public override string GetName()
        {
            return "Character avoidance steering";
        }
    }
}
