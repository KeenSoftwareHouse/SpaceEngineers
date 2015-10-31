using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.EnvironmentItems;
using System.Collections.Generic;
using VRageMath;

namespace Sandbox.Game.AI.Navigation
{
    // CH: TODO: This is a dirty and temporary solution. It would be better to index the trees
    public class MyTreeAvoidance : MySteeringBase
    {
        private List<Vector3D> m_trees = new List<Vector3D>();

        public MyTreeAvoidance(MyBotNavigation navigation, float weight)
            : base(navigation, weight) { }

        public override string GetName()
        {
            return "Tree avoidance steering";
        }

        public override void AccumulateCorrection(ref VRageMath.Vector3 correction, ref float weight)
        {
            Vector3D position = Parent.PositionAndOrientation.Translation;

            BoundingBoxD box = new BoundingBoxD(position - Vector3D.One, position + Vector3D.One);

            Vector3D currentMovement = Parent.ForwardVector * Parent.Speed;
            if (Parent.Speed > 0.01f)
                currentMovement.Normalize();

            // Don't do any correction if we're not moving
            if (currentMovement.LengthSquared() < 0.01)
            {
                return;
            }

            var entities = MyEntities.GetEntitiesInAABB(ref box);
            foreach (var entity in entities)
            {
                var environmentItems = entity as MyEnvironmentItems;
                if (environmentItems == null) continue;

                environmentItems.GetItemsInRadius(ref position, 6.0f, m_trees);

                foreach (var item in m_trees)
                {
                    Vector3D dir = item - position;
                    var dist = dir.Normalize();
                    dir = Vector3D.Reject(currentMovement, dir);

                    dir.Y = 0.0f;
                    if (dir.Z * dir.Z + dir.X * dir.X < 0.1)
                    {
                        Vector3D dirLocal = Vector3D.TransformNormal(dir, Parent.PositionAndOrientationInverted);
                        dir = position - item;
                        dir = Vector3D.Cross(Vector3D.Up, dir);
                        if (dirLocal.X < 0)
                            dir = -dir;
                    }

                    dir.Normalize();

                    correction += (6f - dist) * Weight * dir;
                    if (!correction.IsValid())
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                }
                m_trees.Clear();
            }

            weight += Weight;

            entities.Clear();
        }
    }
}
