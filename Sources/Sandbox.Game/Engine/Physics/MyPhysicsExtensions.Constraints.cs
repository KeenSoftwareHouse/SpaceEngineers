using Havok;
using VRageMath;

namespace Sandbox.Engine.Physics
{
    public static partial class MyPhysicsExtensions
    {
        public static void SetInBodySpace(this HkWheelConstraintData data, Vector3 posA, Vector3 posB, Vector3 axisA, Vector3 axisB, Vector3 suspension, Vector3 steering, MyPhysicsBody bodyA, MyPhysicsBody bodyB)
        {
            if (bodyA.IsWelded)
            {
                posA = Vector3.Transform(posA, bodyA.WeldInfo.Transform);
                axisA = Vector3.TransformNormal(axisA, bodyA.WeldInfo.Transform);
            }
            if (bodyB.IsWelded)
            {
                posB = Vector3.Transform(posB, bodyB.WeldInfo.Transform);
                axisB = Vector3.TransformNormal(axisB, bodyB.WeldInfo.Transform);
                suspension = Vector3.TransformNormal(suspension, bodyB.WeldInfo.Transform);
                steering = Vector3.TransformNormal(steering, bodyB.WeldInfo.Transform);
            }
            data.SetInBodySpaceInternal(ref posA, ref posB, ref axisA, ref axisB, ref suspension, ref steering);
        }

        public static void SetInBodySpace(this HkLimitedHingeConstraintData data, Vector3 posA, Vector3 posB, Vector3 axisA, Vector3 axisB, Vector3 axisAPerp, Vector3 axisBPerp, MyPhysicsBody bodyA, MyPhysicsBody bodyB)
        {
            if (bodyA.IsWelded)
            {
                posA = Vector3.Transform(posA, bodyA.WeldInfo.Transform);
                axisA = Vector3.TransformNormal(axisA, bodyA.WeldInfo.Transform);
                axisAPerp = Vector3.TransformNormal(axisAPerp, bodyA.WeldInfo.Transform);
            }
            if (bodyB.IsWelded)
            {
                posB = Vector3.Transform(posB, bodyB.WeldInfo.Transform);
                axisB = Vector3.TransformNormal(axisB, bodyB.WeldInfo.Transform);
                axisBPerp = Vector3.TransformNormal(axisBPerp, bodyB.WeldInfo.Transform);
            }

            data.SetInBodySpaceInternal(ref posA, ref posB, ref axisA, ref axisB, ref axisAPerp, ref axisBPerp);
        }

        public static void SetInBodySpace(this HkHingeConstraintData data, Vector3 posA, Vector3 posB, Vector3 axisA, Vector3 axisB, MyPhysicsBody bodyA, MyPhysicsBody bodyB)
        {
            if (bodyA.IsWelded)
            {
                posA = Vector3.Transform(posA, bodyA.WeldInfo.Transform);
                axisA = Vector3.TransformNormal(axisA, bodyA.WeldInfo.Transform);
            }
            if (bodyB.IsWelded)
            {
                posB = Vector3.Transform(posB, bodyB.WeldInfo.Transform);
                axisB = Vector3.TransformNormal(axisB, bodyB.WeldInfo.Transform);
            }

            data.SetInBodySpaceInternal(ref posA, ref posB, ref axisA, ref axisB);
        }

        public static void SetInBodySpace(this HkPrismaticConstraintData data, Vector3 posA, Vector3 posB, Vector3 axisA, Vector3 axisB, Vector3 axisAPerp, Vector3 axisBPerp, MyPhysicsBody bodyA, MyPhysicsBody bodyB)
        {
            if (bodyA.IsWelded)
            {
                posA = Vector3.Transform(posA, bodyA.WeldInfo.Transform);
                axisA = Vector3.TransformNormal(axisA, bodyA.WeldInfo.Transform);
                axisAPerp = Vector3.TransformNormal(axisAPerp, bodyA.WeldInfo.Transform);
            }
            if (bodyB.IsWelded)
            {
                posB = Vector3.Transform(posB, bodyB.WeldInfo.Transform);
                axisB = Vector3.TransformNormal(axisB, bodyB.WeldInfo.Transform);
                axisBPerp = Vector3.TransformNormal(axisBPerp, bodyB.WeldInfo.Transform);
            }

            data.SetInBodySpaceInternal(ref posA, ref posB, ref axisA, ref axisB, ref axisAPerp, ref axisBPerp);
        }

        public static void SetInBodySpace(this HkFixedConstraintData data, Matrix pivotA, Matrix pivotB, MyPhysicsBody bodyA, MyPhysicsBody bodyB)
        {
            if ((bodyA != null) && bodyA.IsWelded)
                pivotA = pivotA * bodyA.WeldInfo.Transform;

            if ((bodyB != null) && bodyB.IsWelded)
                pivotB = pivotB * bodyB.WeldInfo.Transform;

            data.SetInBodySpaceInternal(ref pivotA, ref pivotB);
        }

        public static void SetInBodySpace(this HkRopeConstraintData data, Vector3 pivotA, Vector3 pivotB, MyPhysicsBody bodyA, MyPhysicsBody bodyB)
        {
            if (bodyA.IsWelded)
                pivotA = Vector3.Transform(pivotA, bodyA.WeldInfo.Transform);
            if (bodyB.IsWelded)
                pivotB = Vector3.Transform(pivotB, bodyB.WeldInfo.Transform);

            data.SetInBodySpaceInternal(ref pivotA, ref pivotB);
        }

        public static void SetInBodySpace(this HkBallAndSocketConstraintData data, Vector3 pivotA, Vector3 pivotB, MyPhysicsBody bodyA, MyPhysicsBody bodyB)
        {
            if (bodyA.IsWelded)
                pivotA = Vector3.Transform(pivotA, bodyA.WeldInfo.Transform);
            if (bodyB.IsWelded)
                pivotB = Vector3.Transform(pivotB, bodyB.WeldInfo.Transform);

            data.SetInBodySpaceInternal(ref pivotA, ref pivotB);
        }

        public static void SetInBodySpace(this HkCogWheelConstraintData data, Vector3 pivotA, Vector3 rotationA, float radius1, Vector3 pivotB, Vector3 rotationB, float radius2, MyPhysicsBody bodyA, MyPhysicsBody bodyB)
        {
            if (bodyA.IsWelded)
            {
                pivotA = Vector3.Transform(pivotA, bodyA.WeldInfo.Transform);
                rotationA = Vector3.TransformNormal(rotationA, bodyA.WeldInfo.Transform);
            }
            if (bodyB.IsWelded)
            {
                pivotB = Vector3.Transform(pivotB, bodyB.WeldInfo.Transform);
                rotationB = Vector3.TransformNormal(rotationB, bodyB.WeldInfo.Transform);
            }

            data.SetInBodySpaceInternal(ref pivotA, ref rotationA, radius1, ref pivotB, ref rotationB, radius2);
        }
    }
}
