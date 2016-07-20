using Havok;
using Sandbox.Engine.Physics;
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
        private List<HkBodyCollision> m_trees = new List<HkBodyCollision>();

        public MyTreeAvoidance(MyBotNavigation navigation, float weight)
            : base(navigation, weight) { }

        public override string GetName()
        {
            return "Tree avoidance steering";
        }

        public override void AccumulateCorrection(ref VRageMath.Vector3 correction, ref float weight)
        {
            // Don't do any correction if we're not moving
            if (Parent.Speed < 0.01)
                return;

            Vector3D position = Parent.PositionAndOrientation.Translation;

            // Find trees
            Quaternion rotation = Quaternion.Identity;
            HkShape sphereShape = new HkSphereShape(6);
            MyPhysics.GetPenetrationsShape(sphereShape, ref position, ref rotation, m_trees, MyPhysics.CollisionLayers.NoVoxelCollisionLayer);

            foreach (var tree in m_trees)
            {
                // Make sure the tree is actually a tree
                if (tree.Body == null) continue;
                MyPhysicsBody physicsBody = tree.Body.UserObject as MyPhysicsBody;
                if (physicsBody == null) continue;

                HkShape bodyShape = tree.Body.GetShape();
                if (bodyShape.ShapeType != HkShapeType.StaticCompound)
                    continue;

                // Get the static compound shape
                HkStaticCompoundShape staticCompoundShape = (HkStaticCompoundShape)bodyShape;

                int instanceId;
                uint childKey;
                staticCompoundShape.DecomposeShapeKey(tree.ShapeKey, out instanceId, out childKey);

                // Get the local shape position, and add entity world position
                Vector3D item = staticCompoundShape.GetInstanceTransform(instanceId).Translation;
                item += physicsBody.GetWorldMatrix().Translation;

                // Avoid tree
                Vector3D dir = item - position;
                var dist = dir.Normalize();
                dir = Vector3D.Reject(Parent.ForwardVector, dir);

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

            weight += Weight;
        }
    }
}
